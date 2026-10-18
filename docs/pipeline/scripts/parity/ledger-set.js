// Dat 1 dong so cai thanh "Da lam" + hash, dem lai tien do. Dung: node ledger-set.js <so> <hash>
const fs = require('fs'); const [no, hash] = process.argv.slice(2);
const f = 'PARITY_PROGRESS.md'; const lines = fs.readFileSync(f, 'utf8').split('\n');
const DONE = 'Đã làm'; let hit = false;
for (let i = 0; i < lines.length; i++) {
  const p = lines[i].split('|');
  if (p.length >= 5 && p[1].trim() === no) { p[3] = ' ' + DONE + ' '; p[4] = ' ' + hash + ' '; lines[i] = p.join('|'); hit = true; break; }
}
if (!hit) { console.error('khong thay dong ' + no); process.exit(3); }
let done = 0; for (const l of lines) { const p = l.split('|'); if (p.length >= 5 && /^\d+$/.test(p[1].trim()) && p[3].trim() === DONE) done++; }
const hi = lines.findIndex((l) => l.includes('Tiến độ hiện tại'));
if (hi >= 0) lines[hi] = lines[hi].replace(/\d+\/337 \([\d.]+%\)/, done + '/337 (' + (done / 337 * 100).toFixed(2) + '%)');
fs.writeFileSync(f, lines.join('\n')); console.log('tien do ' + done);
