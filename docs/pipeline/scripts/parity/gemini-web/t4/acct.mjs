import { chromium } from 'playwright-core';
for (const port of [9223, 9224]) {
  const b = await chromium.connectOverCDP('http://127.0.0.1:' + port);
  const p = await b.contexts()[0].newPage();
  await p.goto('https://gemini.google.com/app'); await p.waitForTimeout(4000);
  const lab = await p.evaluate(() => [...document.querySelectorAll('[aria-label*="@"], a[href*="accounts.google.com/SignOutOptions"]')].map(e => e.getAttribute('aria-label')).filter(Boolean).slice(0,2));
  const btn = await p.locator('bard-mode-switcher button, [data-test-id="bard-mode-menu-button"]').first().innerText().catch(() => '?');
  await p.locator('bard-mode-switcher button, [data-test-id="bard-mode-menu-button"]').first().click().catch(() => {});
  await p.waitForTimeout(1200);
  const menu = await p.locator('.cdk-overlay-container').innerText().catch(() => '');
  console.log(port, JSON.stringify(lab), '| model:', btn, '|', menu.replace(/\s+/g, ' ').slice(0, 330));
  await p.keyboard.press('Escape'); await p.close(); await b.close();
}
