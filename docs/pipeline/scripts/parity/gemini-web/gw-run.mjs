// Gui 1 de bai cho Gemini WEB (gemini.google.com) qua Chrome CDP (cong 9222), luu cau tra loi ra file.
// Dung: node gw-run.mjs <promptFile> <outFile> [timeoutSec=600] [session] [preambleFile]
// TOI UU NGU CANH: co [session] -> DUNG LAI 1 chat cho nhieu cau hoi (toi da GW_MAX_TURNS luot roi mo chat moi). Chat MOI duoc gui kem <preambleFile> (noi quy+bai hoc,~60KB)
// MOT LAN; cac luot sau chi gui <promptFile> (phan rieng cua hang muc). Preamble doi theo kieu CHI THEM CUOI (LESSONS) -> chi gui phan them.
// Exit: 0 = co tra loi | 3 = Chrome/CDP khong chay | 4 = chua dang nhap / het han muc | 5 = loi khac | 6 = het gio
import { chromium } from 'playwright-core';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const [promptFile, outFile, tmo = '600', sessionName = '', preambleFile = ''] = process.argv.slice(2);
if (!promptFile || !outFile) { console.error('usage: node gw-run.mjs <promptFile> <outFile> [timeoutSec]'); process.exit(5); }
let prompt = fs.readFileSync(promptFile, 'utf8');
const deadline = Date.now() + Number(tmo) * 1000;
const log = (m) => fs.appendFileSync(outFile + '.log', `[${new Date().toISOString()}] ${m}\n`);

// ===== MAC DINH 1 LUONG (nguoi dung chot 16/10 sau khi Google chan CAPTCHA luc 10 lan gui lien tiep) =====
// - khoa toan cuc gw.lock: moi lenh goi (lead soan de bai + worker gweb) di LAN LUOT, khong song song
// - gian cach toi thieu GAP_SEC giua 2 lan gui (gw.last)
// - gap CAPTCHA -> gw.blocked: tu choi (exit 4) BLOCK_HOURS gio, khong thu lai
const dir = path.dirname(fileURLToPath(import.meta.url)) + path.sep;
// NHIEU TAI KHOAN: GW_PORT (cong CDP cua Chrome dang nhap tai khoan do, mac dinh 9222) + GW_TAG (hau to file khoa/phien, vd 'b') => moi tai khoan co khoa, khoang cach, khoa CAPTCHA va phien chat RIENG.
const GW_PORT = process.env.GW_PORT || '9222', GW_TAG = process.env.GW_TAG ? '.' + process.env.GW_TAG : '';
const LANES = Math.max(1, Number(process.env.GW_LANES || 1));
let LOCK = dir + 'gw.lock' + GW_TAG, LAST = dir + 'gw.last' + GW_TAG, BLOCKED = dir + 'gw.blocked' + GW_TAG;
const GAP_SEC = Number(process.env.GW_GAP_SEC || 10), BLOCK_HOURS = 6;
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
try { const b = Number(fs.readFileSync(BLOCKED, 'utf8')); if (Date.now() < b) { log('DANG_BI_KHOA_CAPTCHA den ' + new Date(b).toISOString()); process.exit(4); } } catch {}
let haveLock = false, lane = 0;
while (!haveLock) {
  for (lane = 0; lane < LANES && !haveLock; lane++) {
    const lf = dir + 'gw.lock' + GW_TAG + (lane ? '.' + lane : '');
    try { fs.writeFileSync(lf, String(Date.now()), { flag: 'wx' }); haveLock = true; LOCK = lf; break; }
    catch { try { if (Date.now() - Number(fs.readFileSync(lf, 'utf8')) > 25 * 60000) fs.unlinkSync(lf); } catch {} }
  }
  if (!haveLock) { if (Date.now() > deadline) { log('HET_GIO_CHO_KHOA'); process.exit(6); } await sleep(3000); }
}
log('lane=' + lane + '/' + LANES);
const release = () => { try { fs.writeFileSync(LAST, String(Date.now())); } catch {} try { fs.unlinkSync(LOCK); } catch {} };
process.on('exit', release);
try { const w = Number(fs.readFileSync(LAST, 'utf8')) + GAP_SEC * 1000 - Date.now(); if (w > 0) { log('gian cach: cho ' + Math.round(w / 1000) + 's'); await sleep(w); } } catch {}

const SESS = dir + 'gw.sessions' + GW_TAG + '.json', MAX_TURNS = Number(process.env.GW_MAX_TURNS || 6);
const loadSess = () => { try { return JSON.parse(fs.readFileSync(SESS, 'utf8')); } catch { return {}; } };
const preamble = preambleFile && fs.existsSync(preambleFile) ? fs.readFileSync(preambleFile, 'utf8') : '';
const sessKey = sessionName ? sessionName + (lane ? '#' + lane : '') : '';
let sess = sessKey ? (loadSess()[sessKey] || null) : null;
let reuseUrl = null, preambleSent = '';
if (sess && sess.url && sess.turns < MAX_TURNS) {
  reuseUrl = sess.url; preambleSent = sess.preamble || '';
  if (preamble && preamble !== preambleSent) {
    if (preambleSent && preamble.startsWith(preambleSent)) prompt = '[CAP NHAT TAI LIEU - chi phan MOI them vao noi quy/bai hoc, ap dung tu bay gio]\n' + preamble.slice(preambleSent.length) + '\n\n' + prompt;
    else reuseUrl = null;   // doi kieu khac (khong phai them cuoi) -> chat moi
  }
}
if (!reuseUrl && preamble) { prompt = preamble + '\n\n===== YEU CAU CUA LUOT NAY =====\n' + prompt; }
log('session=' + (sessionName || '-') + ' ' + (reuseUrl ? 'DUNG LAI chat (luot ' + (sess.turns + 1) + '/' + MAX_TURNS + ')' : 'CHAT MOI'));

let browser;
try { browser = await chromium.connectOverCDP('http://127.0.0.1:' + GW_PORT, { timeout: Number(process.env.GW_CDP_TIMEOUT_MS || 150000) }); }
catch (e) { log('CDP_DEAD ' + e.message); process.exit(3); }

try {
  const ctx = browser.contexts()[0];
  for (const op of ctx.pages().slice(1)) await op.close().catch(() => {});   // don tab ro ri cua cac lan loi truoc (giu lock nen khong co lan chay khac cung tai khoan)
  const page = await ctx.newPage();
  await page.goto(reuseUrl || 'https://gemini.google.com/app', { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(reuseUrl ? 5000 : 3000);
  if (/accounts\.google\.com/.test(page.url())) { log('CHUA_DANG_NHAP'); process.exit(4); }
  // Google chan luu luong tu dong (CAPTCHA /sorry/): KHONG vuot, KHONG thu lai - exit 4 de pipeline lui lai
  if (/google\.com\/sorry/.test(page.url())) { log('CAPTCHA_BI_CHAN: ' + page.url().slice(0, 80)); fs.writeFileSync(BLOCKED, String(Date.now() + BLOCK_HOURS * 3600000)); await page.close().catch(() => {}); process.exit(4); }

  const box = page.locator('rich-textarea .ql-editor, div[contenteditable="true"]').first();
  await box.waitFor({ timeout: 30000 });
  await page.keyboard.press('Escape').catch(() => {});   // dong dialog/overlay (cdk-overlay-backdrop chan click)
  await page.evaluate(() => document.querySelectorAll('.cdk-overlay-backdrop').forEach((e) => e.click()));
  // chon model cho chat MOI: uu tien GW_MODELS (mac dinh Argon > Pro > 3.8 Flash); muc het han muc (co chu "Han muc") bi bo qua -> giu model hien tai (Flash-Lite)
  if (!reuseUrl && process.env.GW_MODELS) {
    try {
      const prefs = (process.env.GW_MODELS).split(',').map((s) => s.trim());
      const btn = page.locator('bard-mode-switcher button, [data-test-id="bard-mode-menu-button"]').first();
      const cur = (await btn.innerText()).trim();
      await btn.click(); await page.waitForTimeout(1200);
      const items = await page.locator('.cdk-overlay-container [role="menuitem"], .cdk-overlay-container button').all();
      let picked = '';
      for (const p of prefs) {
        if (cur.includes(p.replace(/^[d.]+ /, ''))) { picked = cur + ' (san co)'; break; }
        for (const it of items) { const tx = (await it.innerText().catch(() => '')).replace(/s+/g, ' '); if (tx.includes(p) && !/H.n m.c/.test(tx)) { await it.click(); picked = p; break; } }
        if (picked) break;
      }
      if (!picked) await page.keyboard.press('Escape');
      log('model: ' + (picked || 'giu ' + cur + ' (cac model tot het han muc)'));
      await page.waitForTimeout(800);
    } catch (e) { log('chon model loi: ' + e.message.slice(0, 80)); await page.keyboard.press('Escape').catch(() => {}); }
  }
  // dan nguyen van (khong go tung phim) bang clipboard-less insertText
  await page.evaluate((t) => {
    const el = document.querySelector('rich-textarea .ql-editor') || document.querySelector('div[contenteditable="true"]');
    el.focus(); document.execCommand('insertText', false, t);
  }, prompt);
  await page.waitForTimeout(800);
  const before = await page.locator('model-response, .model-response-text, message-content').count();
  await page.keyboard.press('Enter');
  log('da gui, so tra loi truoc = ' + before);

  // cho tra loi moi xuat hien roi on dinh (khong con nut Stop, noi dung khong doi 8s)
  let last = '', stable = 0;
  while (Date.now() < deadline) {
    await page.waitForTimeout(4000);
    const body = await page.locator('body').innerText();
    if (/reached your limit|đã đạt giới hạn|usage limit|hết lượt/i.test(body) && !last) { log('QUOTA'); process.exit(4); }
    const n = await page.locator('model-response, .model-response-text, message-content').count();
    if (n <= before) continue;
    const txt = await page.locator('model-response, message-content').last().innerText();
    const generating = await page.locator('button[aria-label*="Stop" i], button[aria-label*="Dừng" i]').count();
    if (txt === last && !generating) stable++; else stable = 0;
    last = txt;
    if (stable >= 2 && txt.length > 0) {
      fs.writeFileSync(outFile, txt, 'utf8');
      // khoi code: innerText lam mat dau ``` -> lay rieng textContent tung <pre><code> theo thu tu (cho worker ghi file)
      const blocks = await page.locator('model-response, message-content').last().locator('pre code').evaluateAll((els) => els.map((e) => e.textContent));
      fs.writeFileSync(outFile + '.blocks.json', JSON.stringify(blocks), 'utf8');
      log('OK len=' + txt.length + ' blocks=' + blocks.length);
      if (sessKey && /gemini\.google\.com\/app\/[A-Za-z0-9]+/.test(page.url())) {
        const all = loadSess(); const keep = reuseUrl ? sess : null;
        all[sessKey] = { url: page.url(), turns: (keep ? keep.turns : 0) + 1, preamble: preamble || (keep ? keep.preamble : ''), at: new Date().toISOString() };
        fs.writeFileSync(SESS, JSON.stringify(all, null, 1), 'utf8');
      }
      if (txt.length < 300) { fs.writeFileSync(outFile + '.body.txt', await page.locator('body').innerText(), 'utf8'); await page.screenshot({ path: outFile + '.png' }); }
      await page.close().catch(() => {});
      process.exit(0);
    }
  }
  if (last) fs.writeFileSync(outFile, last, 'utf8');
  log('TIMEOUT'); process.exit(6);
} catch (e) { log('LOI ' + e.message); process.exit(5); }
finally { try { await browser.close(); } catch {} }
