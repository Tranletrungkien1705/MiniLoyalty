import { chromium } from 'playwright-core';
const port = process.argv[2]; const t0 = Date.now();
try { const b = await chromium.connectOverCDP('http://127.0.0.1:' + port, { timeout: 60000 }); const pages = b.contexts().flatMap(c => c.pages()); console.log(port, 'OK', Date.now() - t0, 'ms', 'pages', pages.length, pages.map(p => p.url().slice(0, 40)).join(' | ')); await b.close(); }
catch (e) { console.log(port, 'FAIL', Date.now() - t0, 'ms', e.message.split('\n')[0]); }
