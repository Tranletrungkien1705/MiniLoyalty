import { chromium } from 'playwright-core';
const b = await chromium.connectOverCDP('http://127.0.0.1:9223');
for (const p of b.contexts()[0].pages()) console.log(p.url().slice(0,110));
await b.close();
