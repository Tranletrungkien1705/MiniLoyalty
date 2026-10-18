// CAU NOI "chat Gemini web <-> may that": vong lap agent. Web ra lenh (dong chu thuong) -> tool chay that -> ket qua dan nguoc vao CUNG chat -> lap.
// Dung: node gw-agent.mjs <worktree> <taskFile> <outDir> [maxSteps=14] [session=agent] [preambleFile] [portBase=8700]
// GIAO THUC (web phai tuan thu; dong lenh o DAU DONG, moi dong 1 lenh, ket qua tra ve theo thu tu):
//   READ: <duong dan>[:tu-den]          doc file (chi trong worktree, docs, nguon cu D:\idocNet\2022.7.Loyalty, .parity-run) - toi da 400 dong/lan
//   GREP: <regex> :: <duong dan thu muc/file>   tim dong khop (toi da 60 dong)
//   LIST: <duong dan thu muc>           liet ke file
//   FILE: <duong dan tuong doi>         + NGAY SAU 1 khoi ```csharp noi dung DAY DU => ghi (chi Controllers/Port, Models/Port, MiniLoyalty.Tests)
//   RUN: build                          dotnet build -c Release (loi rut gon)
//   RUN: test                           dotnet test MiniLoyalty.Tests
//   RUN: diff | RUN: status             git diff / status (chi doc)
//   HTTP: POST /api/xxx/yyy {json}      tu bat app tren DB SQLite tam (cong rieng), goi that, tra ve status + body (cat 6KB). Xuat xlsx: ghi file, bao kich thuoc + header
//   DONE: <tom tat theo khuon de bai>   ket thuc
// Moi lenh bi gioi han: khong git commit/push/reset, khong ghi ngoai pham vi, khong mang ngoai (chi localhost app tam).
import fs from 'node:fs';
import path from 'node:path';
import { spawn, spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const [WT, taskFile, outDir, maxStepsArg = '14', session = 'agent', preamble = '', portBaseArg = '8700'] = process.argv.slice(2);
if (!WT || !taskFile || !outDir) { console.error('usage: node gw-agent.mjs <worktree> <taskFile> <outDir> [maxSteps] [session] [preamble] [portBase]'); process.exit(5); }
const here = path.dirname(fileURLToPath(import.meta.url));
const PROJ = (path.basename(path.resolve(WT)).match(/^(Mini[A-Za-z]+?)(-[A-Za-z0-9]+)?$/) || [])[1] || 'MiniLoyalty';   // MiniContract-gweb2 -> MiniContract
const SRC = 'D:\\idocNet', ROOT = 'C:\\Users\\Public\\Pictures\\kientran_private';   // nguon cu: doc-chi
const READ_OK = [WT, ROOT + '\\docs', SRC, ROOT + '\\repos\\' + PROJ + '\\.parity-run'].map((p) => path.resolve(p).toLowerCase());
const WRITE_OK = new RegExp('^(Controllers/Port|Models/Port|' + PROJ + '\\.Tests)/[A-Za-z0-9_]+\\.cs$');
const maxSteps = Number(maxStepsArg);
fs.mkdirSync(outDir, { recursive: true });
const tlog = path.join(outDir, 'gw_agent.log');
const log = (m) => fs.appendFileSync(tlog, `[${new Date().toISOString()}] ${m}\n`);
const okRead = (p) => { const r = path.resolve(p).toLowerCase(); return READ_OK.some((b) => r === b || r.startsWith(b + '\\')); };
const clip = (s, n) => (s.length > n ? s.slice(0, n) + `\n...[cat ${s.length - n} ky tu]` : s);
// Gemini web nuot dau '\' truoc dau cham trong duong dan (markdown) -> sua lai; ho tro bi danh @dir/<file> = thu muc nhat ky cua hang muc (source_excerpt.txt, DEBAI.md)
const resolveP = (p) => {
  p = p.trim().replace(/^["'`]+|["'`]+$/g, '');
  if (/^@dir[\\/]/i.test(p)) return path.join(path.resolve(outDir), p.slice(5).replace(/\//g, '\\'));
  p = p.replace(/(Mini[A-Za-z]+)\.parity-run/i, '$1\\.parity-run').replace(/\//g, '\\');
  const pr = path.resolve(outDir).toLowerCase().indexOf('\\.parity-run\\');
  if (/^\.parity-run\\/i.test(p) && pr >= 0) return path.join(path.resolve(outDir).slice(0, pr), p);   // nhat ky o repo chinh, khong o worktree
  return path.isAbsolute(p) ? p : path.join(WT, p);
};

let app = null, appPort = 0, dbFile = '';
const st = { dirty: false, buildOk: false, tested: false, http: 0, wrote: 0 };
async function ensureApp() {
  if (app) return;
  appPort = Number(portBaseArg) + Math.floor(Math.random() * 30);
  dbFile = path.join(path.resolve(outDir), `agent_${Date.now()}.db`);
  app = spawn('dotnet', ['run', '-c', 'Release', '--no-build', '--no-launch-profile'], { cwd: WT, env: { ...process.env, PORT: String(appPort), CONNECTION_STRING: 'Data Source=' + dbFile }, stdio: 'ignore', windowsHide: true });
  for (let i = 0; i < 40; i++) { await new Promise((r) => setTimeout(r, 1500)); try { const r = await fetch(`http://127.0.0.1:${appPort}/`, { signal: AbortSignal.timeout(2000) }); if (r) return; } catch {} }
}
const stopApp = () => { if (app) { try { spawnSync('taskkill', ['/PID', String(app.pid), '/T', '/F'], { stdio: 'ignore' }); } catch {} app = null; } };
process.on('exit', () => { stopApp(); try { if (dbFile) for (const s of ['', '-shm', '-wal']) fs.rmSync(dbFile + s, { force: true }); } catch {} });

function sh(cmd, args, cwd, tmo = 300000) {
  const r = spawnSync(cmd, args, { cwd, encoding: 'utf8', timeout: tmo, maxBuffer: 20 * 1024 * 1024, windowsHide: true });
  return (r.stdout || '') + (r.stderr || '');
}

async function runTool(line, blocks, blockIdx) {
  const m = line.match(/^(READ|GREP|LIST|FILE|RUN|HTTP|DONE):\s*(.*)$/);
  if (!m) return null;
  const [, kind, arg] = m;
  try {
    if (kind === 'READ') {
      const mm = arg.trim().match(/^(.*?)(?::(\d+)-(\d+))?$/); const p = resolveP(mm[1].trim());
      if (!okRead(p)) return `READ ${arg}: TU CHOI (ngoai pham vi cho phep)`;
      if (!fs.existsSync(p)) return `READ ${arg}: khong ton tai`;
      const L = fs.readFileSync(p, 'utf8').split(/\r?\n/); const a = mm[2] ? Number(mm[2]) : 1, b = Math.min(mm[3] ? Number(mm[3]) : a + 399, a + 399, L.length);
      return `READ ${arg} (${a}-${b}/${L.length}):\n` + L.slice(a - 1, b).map((x, i) => `${String(a + i).padStart(5)}| ${x}`).join('\n');
    }
    if (kind === 'GREP') {
      const [pat, where = '.'] = arg.split('::').map((s) => s.trim()); const p = resolveP(where);
      if (!okRead(p)) return `GREP: TU CHOI (ngoai pham vi)`;
      const re = new RegExp(pat, 'i'); const hits = [];
      const walk = (d) => { for (const e of fs.readdirSync(d, { withFileTypes: true })) { if (hits.length >= 60) return; const f = path.join(d, e.name); if (e.isDirectory()) { if (!/^(bin|obj|node_modules|\.git)$/.test(e.name)) walk(f); } else if (/\.(cs|md|txt|json|config|sql)$/i.test(e.name)) { const L = fs.readFileSync(f, 'utf8').split(/\r?\n/); L.forEach((x, i) => { if (hits.length < 60 && re.test(x)) hits.push(`${f}:${i + 1}: ${x.trim().slice(0, 200)}`); }); } } };
      if (fs.statSync(p).isDirectory()) walk(p); else fs.readFileSync(p, 'utf8').split(/\r?\n/).forEach((x, i) => { if (hits.length < 60 && re.test(x)) hits.push(`${p}:${i + 1}: ${x.trim().slice(0, 200)}`); });
      return `GREP ${arg}: ${hits.length} dong\n` + hits.join('\n');
    }
    if (kind === 'LIST') { const p = resolveP(arg.trim()); if (!okRead(p) || !fs.existsSync(p)) return `LIST ${arg}: khong duoc/khong co`; return `LIST ${arg}:\n` + fs.readdirSync(p).slice(0, 200).join('\n'); }
    if (kind === 'FILE') {
      const rel = arg.trim().replace(/\\/g, '/').replace(/[`*]/g, '');
      if (!WRITE_OK.test(rel)) return `FILE ${rel}: TU CHOI (chi Controllers/Port, Models/Port, ${PROJ}.Tests)`;
      const b = blocks[blockIdx.i]; if (b == null) return `FILE ${rel}: thieu khoi code ngay sau dong FILE`; blockIdx.i++;
      fs.writeFileSync(path.join(WT, rel.replace(/\//g, '\\')), b.trimEnd() + '\r\n', 'utf8'); st.dirty = true; st.buildOk = false; st.tested = false; st.wrote++; return `FILE ${rel}: da ghi (${b.length} ky tu)`;
    }
    if (kind === 'RUN') {
      const c = arg.trim().toLowerCase().replace(/^(dotnet|git)\s+/, '').replace(/^(build|test|diff|status)\b.*$/, '$1');
      if (c === 'build') { stopApp(); const o = sh('dotnet', ['build', '-c', 'Release', '-clp:ErrorsOnly', '--nologo'], WT); const e = [...new Set(o.split('\n').filter((x) => /error|Build succeeded/.test(x)).map((x) => x.replace(/\s*\[[A-Za-z]:[^\]]*\]\s*$/, '').replace(/^.*?(Controllers|Models)\\Port\\/, '$1\\Port\\')))].slice(0, 40); st.buildOk = /Build succeeded/.test(o) && !/error CS/.test(o); if (st.buildOk) st.dirty = false; return 'RUN build:\n' + (e.join('\n') || clip(o, 3000)); }
      if (c === 'test') { stopApp(); const o = sh('dotnet', ['test', PROJ + '.Tests', '-c', 'Release', '--nologo'], WT, 600000); st.tested = /Passed!/.test(o) && !/Failed!/.test(o); return 'RUN test:\n' + clip(o.split('\n').filter((x) => /Passed!|Failed|error|Total|\[FAIL\]/.test(x)).join('\n') || o, 3500); }
      if (c === 'diff') return 'RUN diff:\n' + clip(sh('git', ['diff', 'HEAD', '--stat'], WT), 3000);
      if (c === 'status') return 'RUN status:\n' + clip(sh('git', ['status', '--short'], WT), 2000);
      return `RUN ${arg}: TU CHOI (chi build|test|diff|status)`;
    }
    if (kind === 'HTTP') {
      const mm = arg.trim().match(/^(POST|GET)\s+(\S+)\s*([\s\S]*)$/i); if (!mm) return 'HTTP: sai cu phap (HTTP: POST /api/x/y {json})';
      if (!/^\/(api\/)?[A-Za-z0-9_\/\.\-?=&]*$/.test(mm[2])) return 'HTTP: duong dan khong hop le';
      await ensureApp(); st.http++;
      const r = await fetch(`http://127.0.0.1:${appPort}${mm[2]}`, { method: mm[1].toUpperCase(), headers: { 'Content-Type': 'application/json' }, body: mm[1].toUpperCase() === 'POST' ? (mm[3] || '{}') : undefined, signal: AbortSignal.timeout(60000) });
      const ct = r.headers.get('content-type') || ''; const buf = Buffer.from(await r.arrayBuffer());
      return `HTTP ${mm[1]} ${mm[2]} -> ${r.status} ${ct}\n` + (/json|text/.test(ct) ? clip(buf.toString('utf8'), 6000) : `[nhi phan ${buf.length} byte, dau: ${buf.subarray(0, 4).toString('hex')}]`);
    }
  } catch (e) { return `${kind} ${arg}: LOI ${String(e.message).slice(0, 200)}`; }
  return null;
}

function turn(promptText, i) {
  const pf = path.join(outDir, `gw_agent_p${i}.txt`), of = path.join(outDir, `gw_agent_o${i}.txt`);
  fs.writeFileSync(pf, promptText, 'utf8');
  for (const x of [of, of + '.blocks.json', of + '.log']) try { fs.unlinkSync(x); } catch {}
  const args = ['gw-run.mjs', pf, of, '900', session]; if (i === 0 && preamble) args.push(preamble); else if (preamble) args.push(preamble);
  const r = spawnSync('node', args, { cwd: here, encoding: 'utf8', timeout: 1000000, env: { ...process.env, GW_MAX_TURNS: '60' } });
  if (r.status !== 0) return { code: r.status };
  return { code: 0, text: fs.readFileSync(of, 'utf8'), blocks: JSON.parse(fs.readFileSync(of + '.blocks.json', 'utf8')) };
}

const PROTOCOL = `
=== GIAO THUC LAM VIEC (BAT BUOC) ===
Ban dang noi chuyen voi 1 CONG CU chay lenh that tren may. Moi cau tra loi cua ban co the chua cac DONG LENH (bat dau o DAU DONG), moi lenh 1 dong; cong cu chay het theo thu tu roi gui KET QUA THAT cho ban o tin nhan sau. Hay lam tung buoc nho, KHONG doan - can biet gi thi doc/tim/chay.
  READ: <duong dan>[:tu-den]        (vd READ: Models/Port/Mst_Major.cs hoac READ: D:\\idocNet\\2022.7.Loyalty\\...\\X.cs:100-250)
  GREP: <regex> :: <thu muc hoac file>
  LIST: <thu muc>
  FILE: <duong dan tuong doi>       roi NGAY SAU DO 1 khoi \`\`\`csharp chua NOI DUNG DAY DU cua file (ghi duoc: Controllers/Port, Models/Port, MiniLoyalty.Tests)
  RUN: build | RUN: test | RUN: diff | RUN: status
  HTTP: POST /api/<controller>/<action> {json}    (cong cu tu bat app voi DB tam; dung de chay that & debug)
  DONE: <TOM TAT dung khuon de bai>   (chi khi build+test xanh va da chay that cac action bang HTTP)
Khong viet dong lenh trong giai thich. Moi tin nhan: toi da 8 lenh. Viet ngan gon, khong nhac lai ket qua cu.
`;

let task = fs.readFileSync(taskFile, 'utf8') + '\n' + PROTOCOL + '\nBat dau: cho lenh dau tien cua ban.';
let steps = 0, finished = false;
log(`bat dau, toi da ${maxSteps} luot`);
while (steps < maxSteps && !finished) {
  let t = turn(task, steps);
  for (let k = 0; k < 2 && t.code && t.code !== 4 && t.code !== 3; k++) { log(`gw-run exit ${t.code} -> thu lai luot`); await new Promise((r) => setTimeout(r, 15000)); t = turn(task, steps); }
  if (t.code !== 0) { log(`gw-run exit ${t.code}`); fs.writeFileSync(path.join(outDir, 'gw_agent.result'), `ERR ${t.code}`); process.exit(t.code === 4 ? 4 : 2); }
  const lines = t.text.split(/\r?\n/).map((l) => l.replace(/^[\s>*`-]+/, '').trim());
  const bi = { i: 0 }; const outs = [];
  for (const ln of lines) {
    if (/^DONE\b/.test(ln)) {
      const why = st.dirty || !st.buildOk ? 'chua RUN: build xanh SAU lan ghi file cuoi' : (st.wrote && !st.tested ? 'chua RUN: test xanh' : (st.wrote && st.http === 0 ? 'chua chay that bang HTTP:' : ''));
      if (why) { outs.push('DONE BI TU CHOI: ' + why + '. Hay lam buoc do roi DONE lai.'); break; }
      finished = true; fs.writeFileSync(path.join(outDir, 'gw_agent_summary.txt'), t.text, 'utf8'); outs.push('DONE nhan.'); break; }
    if (outs.length >= 8) break;
    const o = await runTool(ln, t.blocks, bi); if (o) outs.push(o);
  }
  steps++; log(`luot ${steps}: ${outs.length} lenh`);
  if (finished) break;
  task = outs.length ? 'KET QUA CONG CU:\n\n' + clip(outs.join('\n\n----\n'), 24000) + '\n\nTiep tuc (lenh tiep theo, hoac DONE:).' : 'Khong thay dong lenh nao hop le. Viet lenh o DAU DONG (READ:/GREP:/FILE:/RUN:/HTTP:/DONE:).';
}
fs.writeFileSync(path.join(outDir, 'gw_agent.result'), finished ? 'DONE' : 'MAXSTEPS');
log(finished ? 'DONE' : 'het luot'); stopApp(); process.exit(finished ? 0 : 6);
