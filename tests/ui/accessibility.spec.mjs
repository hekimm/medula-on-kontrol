import {test,expect} from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import fs from 'node:fs';

fs.mkdirSync('artifacts/verification', { recursive: true });
fs.mkdirSync('artifacts/screenshots', { recursive: true });
test('Giriş ve gösterge paneli WCAG 2.1 AA otomatik taraması',async({page})=>{
 await page.goto('/Hesap/Login');await page.keyboard.press('Tab');await expect(page.getByRole('link',{name:'İçeriğe geç'})).toBeFocused();
 let result=await new AxeBuilder({page}).withTags(['wcag2a','wcag2aa','wcag21aa']).analyze();
 fs.writeFileSync('artifacts/verification/accessibility-login.json',JSON.stringify(result.violations,null,2));expect(result.violations).toEqual([]);
 await page.getByLabel('Kullanıcı adı',{exact:true}).fill('admin');await page.getByLabel('Parola',{exact:true}).fill(fs.readFileSync(process.env.MEDULA_ADMIN_PASSWORD_FILE||'secrets/admin-password','utf8').trim());await page.getByRole('button',{name:'Oturum aç',exact:true}).click();await expect(page.getByRole('heading',{name:'Gösterge paneli',exact:true})).toBeVisible();
 result=await new AxeBuilder({page}).withTags(['wcag2a','wcag2aa','wcag21aa']).analyze();fs.writeFileSync('artifacts/verification/accessibility-dashboard.json',JSON.stringify(result.violations,null,2));expect(result.violations).toEqual([]);
 for(const path of ['/Fatura','/Kural','/Red','/Rapor','/Denetim','/?donem=202001','/Kural/Details/PRV-001','/Kural/Calibration']){
  const response=await page.goto(path);
  expect(response.status(),`${path} HTTP status`).toBe(200);
  const scan=await new AxeBuilder({page}).withTags(['wcag2a','wcag2aa','wcag21aa']).analyze();
  fs.writeFileSync(`artifacts/verification/accessibility-${path.includes('?') ? 'empty-period' : path.slice(1).toLowerCase().replace(/[^a-z0-9-]/g,'-')}.json`,JSON.stringify(scan.violations,null,2));
  expect(scan.violations,`${path} accessibility`).toEqual([]);
  await page.setViewportSize({width:390,height:844});
  expect(await page.evaluate(()=>document.documentElement.scrollWidth<=window.innerWidth),`${path} mobile overflow`).toBe(true);
  await page.setViewportSize({width:1440,height:1000});
 }
});
