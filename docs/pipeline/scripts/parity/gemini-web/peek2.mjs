import { chromium } from 'playwright-core';
const b = await chromium.connectOverCDP('http://127.0.0.1:9222');
const pg = await b.contexts()[0].newPage();
await pg.goto('https://gemini.google.com/app', { waitUntil: 'domcontentloaded' });
await pg.waitForTimeout(6000);
console.log(pg.url());
console.log((await pg.locator('body').innerText()).slice(0,500).replace(/\n/g,' | '));
await pg.screenshot({ path: 'peek.png' });
await pg.close(); await b.close();
