import { test, expect } from '@playwright/test';
import fs from 'node:fs';

fs.mkdirSync('artifacts/verification', { recursive: true });
fs.mkdirSync('artifacts/screenshots', { recursive: true });
import AxeBuilder from '@axe-core/playwright';
const password = fs.readFileSync(process.env.MEDULA_ADMIN_PASSWORD_FILE || 'secrets/admin-password','utf8').trim();
async function login(page,user='admin'){
 await page.goto('/Hesap/Login');await page.getByLabel('Kullanıcı adı',{exact:true}).fill(user);await page.getByLabel('Parola',{exact:true}).fill(password);await page.getByRole('button',{name:'Oturum aç',exact:true}).click();await expect(page.getByRole('heading',{name:'Gösterge paneli',exact:true})).toBeVisible();
}
test('Türkçe ekranlar, kontrol, bulgu vurgulama, raporlar ve mobil görünüm',async({page})=>{
 const errors=[];page.on('pageerror',error=>errors.push(error.message));await page.goto('/Hesap/Login');await page.screenshot({path:'artifacts/screenshots/login.png',fullPage:true});await login(page);
 await page.getByRole('button',{name:'Dönemi kontrol et'}).click();await expect(page.getByRole('status')).toContainText('kuyruğa');
 await expect.poll(async()=>{const response=await page.request.get('/Panel/Jobs');const jobs=await response.json();return jobs[0]?.status;},{timeout:90000,intervals:[1000,2000]}).toBe('TAMAMLANDI');
 await page.goto('/');await page.screenshot({path:'artifacts/screenshots/dashboard.png',fullPage:true});
 await page.goto('/Fatura');await expect(page.getByRole('heading',{name:'Faturalar',exact:true})).toBeVisible();await page.screenshot({path:'artifacts/screenshots/faturalar.png',fullPage:true});
 await page.locator('a.record-link').first().click();await expect(page.getByRole('heading',{name:'Fatura kalemleri',exact:true})).toBeVisible();await page.getByRole('button',{name:'Kontrol et',exact:false}).first().click();
 const link=page.locator('[data-fatura-line]').first();if(await link.count()){await link.click();await expect(page.locator('.highlighted')).toHaveCount(1);}await page.screenshot({path:'artifacts/screenshots/fatura-details.png',fullPage:true});
 for(const [url,title,file] of [['/Kural','Kural kataloğu','rules'],['/Red','Red geri beslemesi','rejection-analysis'],['/Rapor','Raporlar','reports'],['/Denetim','Denetim izi','audit-trail']]){await page.goto(url);await expect(page.getByRole('heading',{name:title,exact:true})).toBeVisible();await page.screenshot({path:`artifacts/screenshots/${file}.png`,fullPage:true});}
 await page.goto('/Rapor');const pdfPromise=page.waitForEvent('download');await page.getByRole('link',{name:'PDF raporunu indir'}).click();const pdf=await pdfPromise;await pdf.saveAs('artifacts/period-report.pdf');expect(fs.readFileSync('artifacts/period-report.pdf').subarray(0,5).toString()).toBe('%PDF-');
 const excelPromise=page.waitForEvent('download');await page.getByRole('link',{name:'Excel dosyasını indir'}).click();const excel=await excelPromise;await excel.saveAs('artifacts/bulgular.xlsx');expect(fs.readFileSync('artifacts/bulgular.xlsx').subarray(0,2).toString()).toBe('PK');
 await page.setViewportSize({width:390,height:844});await page.goto('/');await expect(page.getByRole('heading',{name:'Gösterge paneli',exact:true})).toBeVisible();await page.screenshot({path:'artifacts/screenshots/mobile.png',fullPage:true});expect(await page.evaluate(()=>document.documentElement.scrollWidth<=window.innerWidth)).toBe(true);expect(errors).toEqual([]);
});
test('Yetki, oturum ve CSRF koruması',async({page})=>{
 await login(page,'fatura');await page.goto('/Kural');await expect(page.getByText('Bu işlem için yetkiniz bulunmuyor.')).toBeVisible();
 const response=await page.request.post('/Fatura/Validate',{form:{id:'1'}});expect(response.status()).toBe(400);
 await page.goto('/Fatura');await expect(page.locator('tbody')).not.toContainText('KARDIYOLOJI');await page.getByRole('button',{name:'Oturumu kapat'}).click();await expect(page.getByRole('heading',{name:'Kullanıcı girişi'})).toBeVisible();
});
test('İstisna diyaloğu klavye ile açılır ve gerekçe zorunludur',async({page})=>{
 await login(page);
 await page.goto('/Fatura');
 await page.locator('a.record-link').first().click();
 const button=page.getByRole('button',{name:'Gerekçeli istisna tanımla'}).first();
 await button.focus();await page.keyboard.press('Enter');
 const dialog=page.getByRole('dialog');await expect(dialog).toBeVisible();
 await expect(dialog).toBeFocused();
 await expect(dialog.getByLabel('Gerekçe',{exact:true})).toHaveAttribute('required','');
 await expect(dialog.getByLabel('Geçerlilik bitişi')).toHaveAttribute('required','');
 await page.keyboard.press('Escape');await expect(dialog).toHaveCount(0);
 await expect(button).toBeFocused();
});

test('Kalibrasyon ekranı ve gezinme bağlantıları',async({page})=>{
 await login(page);
 await page.getByRole('link',{name:'Red geri beslemesi',exact:true}).click();
 await expect(page.getByRole('heading',{name:'Red geri beslemesi',exact:true})).toBeVisible();
 await page.getByRole('link',{name:'Kural kataloğu',exact:true}).click();
 await page.getByRole('link',{name:'Red verileriyle kalibrasyon'}).click();
 await expect(page.getByRole('heading',{name:'Kural kalibrasyonu',exact:true})).toBeVisible();
 await expect(page.locator('tbody tr')).toHaveCount(40);
 await page.screenshot({path:'artifacts/screenshots/kalibrasyon.png',fullPage:true});
 const invalid=await page.request.get('/Kural/Calibration?donem=202613');
 expect(invalid.status()).toBe(400);
});

test('Geçmiş dönem kural simülasyonu',async({page})=>{
 await login(page);
 await page.goto('/Kural/Details/PRV-001');
 await expect(page.getByRole('heading',{name:'Versiyon geçmişi',exact:true})).toBeVisible();
 await page.getByRole('button',{name:'Simülasyonu çalıştır'}).click();
 await expect(page.getByText('Seçilen parametrelerle geçmiş dönem değerlendirmesi')).toBeVisible();
 await expect(page.locator('.kpi').filter({hasText:'BULGU SAYISI'}).locator('strong')).not.toHaveText('0');
 await page.getByRole('link',{name:'Kurala dön'}).click();
 await expect(page.getByRole('heading',{name:'Yeni versiyon',exact:true})).toBeVisible();
});

test('Red kaydı gerekçeyle sınıflandırılır ve metrikler güncellenir',async({page})=>{
 await login(page);
 await page.goto('/Fatura?Status=1&SortBy=risk&SortDirection=ascending');
 await page.locator('a.record-link').first().click();
 const faturaId=await page.locator('form[action="/Fatura/Validate"] input[name="id"]').inputValue();
 for(let attempt=0;attempt<15;attempt++){
  const blocking=page.locator('article.severity-0').filter({has:page.getByRole('button',{name:'Gerekçeli istisna tanımla'})});
  if(!await blocking.count())break;
  await blocking.first().getByRole('button',{name:'Gerekçeli istisna tanımla'}).click();
  const dialog=page.getByRole('dialog');
  await dialog.getByLabel('Gerekçe',{exact:true}).fill('Sentetik tarayıcı testi için uzman istisna değerlendirmesi.');
  const tomorrow=new Date(Date.now()+86400000).toISOString().slice(0,10);
  await dialog.getByLabel('Geçerlilik bitişi').fill(tomorrow);
  await dialog.getByRole('button',{name:'İstisnayı kaydet'}).click();
  await page.getByRole('button',{name:'Kontrol et',exact:true}).click();
 }
 const approval=page.locator('form[action="/Fatura/Approve"]');
 await approval.locator('input[name="password"]').fill(password);
 await approval.getByRole('button').click();
 await expect(page.getByRole('status')).toContainText('onaylandı');
 await page.locator('form[action="/Fatura/Submit"]').getByRole('button').click();
 await expect(page.getByRole('status')).toContainText('Simülatör');
 await page.goto('/Red');
 let row=page.locator('tr[data-red-id]').filter({has:page.locator(`a[href="/Fatura/Details/${faturaId}"]`)});
 if(!await row.count()){
  const create=page.locator('form[action="/Red/Create"]');
  await create.locator('[name="FaturaId"]').fill(faturaId);
  await create.locator('[name="SgkRedKodu"]').fill('TEST-TARAYICI');
  await create.locator('[name="Tutar"]').fill('1');
  await create.locator('[name="Description"]').fill('Sentetik tarayıcı red geri besleme kontrolü.');
  await page.waitForTimeout(1100);
  const now=new Date();
  const local=new Date(now.getTime()-now.getTimezoneOffset()*60000).toISOString().slice(0,19);
  await create.locator('[name="KayitTarihi"]').fill(local);
  await create.getByRole('button').click();
  await expect(page.getByRole('status')).toContainText('kaydedildi');
 }
 row=page.locator('tr[data-red-id]').filter({has:page.locator(`a[href="/Fatura/Details/${faturaId}"]`)});
 await expect(row).toHaveCount(1);
 const amount=await row.locator('td').nth(3).innerText();
 await row.locator('summary').click();
 await row.getByLabel('Kural kodu',{exact:true}).selectOption('TUT-001');
 await row.getByLabel('Sınıflandırma gerekçesi',{exact:true}).fill('Sentetik red açıklaması kaynak kayıt üzerinden sınıflandırıldı.');
 const scan=await new AxeBuilder({page}).withTags(['wcag2a','wcag2aa','wcag21aa']).analyze();
 expect(scan.violations).toEqual([]);
 await row.getByRole('button',{name:'Sınıflandırmayı kaydet'}).click();
 await expect(page.getByRole('status')).toContainText('isabet analizi güncellendi');
 await expect(row.locator('td').nth(5)).toContainText('TUT-001');
 await expect(row.locator('td').nth(3)).toHaveText(amount);
 await page.screenshot({path:'artifacts/screenshots/red-siniflandirma.png',fullPage:true});
});
