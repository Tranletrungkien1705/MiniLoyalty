import { chromium } from 'playwright-core';
const b = await chromium.connectOverCDP('http://127.0.0.1:9222');
for (const p of b.contexts()[0].pages()) { if (!p.url().includes('gemini.google.com')) continue; const t = await p.locator('body').innerText().catch(()=> ''); console.log('###', p.url(), t.length); console.log(t.slice(0,300).replace(/\n/g,' | ')); console.log('...', t.slice(-700).replace(/\n/g,' | ')); }
await b.close();
