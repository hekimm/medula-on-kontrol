"""Generate the sample rule catalog and its documentation."""
from pathlib import Path
import json
WORKSPACE_ROOT=Path(__file__).resolve().parents[1]
rule_definitions=[
('PRV-001','Provizyon numarası eksik','Blocking',30,'Provizyon numarası boş bırakılamaz.','Başvurunun provizyon kaydını doğrulayarak eksik alanı tamamlayın.'),
('PRV-002','Provizyondan önce hizmet','High',20,'Hizmet tarihi provizyon tarihinden öncedir.','Hizmet ve provizyon tarihlerini kaynak kayıtlarla karşılaştırın.'),
('PRV-003','Yatış ve çıkış tarihleri tutarsız','Blocking',30,'Çıkış tarihi yatış tarihinden öncedir.','Yatış ve çıkış kayıtlarındaki giriş hatasını düzeltin.'),
('PRV-004','Dönemde mükerrer takip','Blocking',30,'Aynı takip numarası aynı dönemde birden fazla faturada kullanılmıştır.','İlgili faturaları inceleyerek mükerrer kaydı giderin.'),
('PRV-005','Takip numarası eksik','High',15,'Başvuruda takip numarası bulunmamaktadır.','Kaynak başvuru belgesinden takip numarasını doğrulayın.'),
('TANI-001','Ana tanı eksik','Blocking',35,'Başvuruda ana tanı kaydı bulunmamaktadır.','Yetkili hekim ve kodlama uzmanının kayıt incelemesini isteyin.'),
('TANI-002','Tanı yürürlük tarihi uyumsuz','High',15,'Tanı kodunun hizmet tarihinde geçerli referans kaydı yoktur.','Hizmet tarihini ve referans sürümünü kodlama uzmanıyla doğrulayın.'),
('TANI-003','Tanı ve cinsiyet tutarsız','High',20,'Tanının temsili cinsiyet kısıtı hasta kaydıyla çelişmektedir.','Demografik veriyi ve klinik belgelendirmeyi uzmanla inceleyin.'),
('TANI-004','Tanı ve yaş tutarsız','High',20,'Hizmet tarihindeki yaş tanının temsili aralığı dışındadır.','Doğum tarihini ve klinik belgelendirmeyi uzmanla inceleyin.'),
('TANI-005','Birden fazla ana tanı','Medium',10,'Başvuruda birden fazla ana tanı vardır.','Ana ve yan tanı işaretlemelerini kodlama uzmanıyla doğrulayın.'),
('TANI-006','Tanı referansta bulunamadı','High',15,'Tanı kodu yüklenmiş temsili referans alt kümesinde yoktur.','Referans kapsamını ve kayıt aktarımını inceleyin.'),
('ITU-001','İşlem için tanı kaydı uyumsuz','High',25,'İşlemin zorunlu tanı ön eklerinden hiçbiri başvuruda yer almamaktadır.','Mevcut klinik belgeler ile işlem ve tanı kayıtlarını uzmanla karşılaştırın.'),
('ITU-002','Başvuru tipi uyumsuz','High',20,'İşlemin izin verilen başvuru tipi kayıtla uyuşmamaktadır.','Başvuru tipini ve hizmet belgesini doğrulayın.'),
('ISL-001','Birlikte faturalandırma çakışması','Blocking',30,'Temsili birlikte faturalandırılamama ilişkisi aynı gün veya başvuruda ihlal edilmiştir.','İlgili hizmet kayıtlarını ve fatura kapsamını inceleyin.'),
('ISL-002','Paket kapsamındaki kalem ayrıca ücretli','High',25,'Başvurudaki paket kapsamına giren kalem ayrıca ücretlendirilmiştir.','Paket kapsamını doğrulayarak mükerrer ücretlendirmeyi giderin.'),
('ISL-003','Tekrar limiti aşıldı','High',20,'Hasta bazında kayan zaman aralığındaki adet temsili limiti aşmaktadır.','Hizmet tekrarlarını ve mükerrer giriş olasılığını inceleyin.'),
('ISL-004','İşlem ve cinsiyet tutarsız','High',20,'İşlemin temsili cinsiyet kısıtı hasta kaydıyla çelişmektedir.','Demografik kayıt ve hizmet belgesini uzmanla doğrulayın.'),
('ISL-005','İşlem ve yaş tutarsız','High',20,'Hizmet tarihindeki yaş işlemin temsili aralığı dışındadır.','Doğum tarihini ve hizmet belgesini doğrulayın.'),
('ISL-006','İşlem yürürlük tarihi uyumsuz','Blocking',25,'Hizmet tarihinde geçerli işlem referansı bulunamamıştır.','İşlem tarihini ve kullanılan referans sürümünü doğrulayın.'),
('ISL-007','Mükerrer hizmet kaydı','High',25,'Aynı hasta, işlem ve tarih birleşiminde başka kalem bulunmaktadır.','İlgili kalemleri inceleyerek mükerrer girişi giderin.'),
('ISL-008','Adet geçersiz','Blocking',25,'Adet pozitif tam sayı değildir.','Hizmet belgesine göre adet girişini düzeltin.'),
('ISL-009','Gelecek tarihli hizmet','High',15,'Hizmet tarihi kontrol gününden ileridedir.','Hizmet tarihindeki giriş hatasını doğrulayın.'),
('TUT-001','Kalem tutarı hesapla uyuşmuyor','High',25,'Kalem tutarı adet ile geçerli temsili birim fiyatın çarpımından farklıdır.','Adet, fiyat ve yuvarlama kayıtlarını karşılaştırın.'),
('TUT-002','Dönem fiyatı uyumsuz','High',20,'Uygulanan birim fiyat hizmet tarihinde geçerli temsili fiyattan farklıdır.','Fiyat sürümünü ve hizmet tarihini doğrulayın.'),
('TUT-003','Fatura toplamı tutarsız','Blocking',30,'Fatura toplamı kalem tutarları toplamına eşit değildir.','Kalemleri doğrulayarak fatura toplamını yeniden hesaplayın.'),
('TUT-004','Negatif tutar','Blocking',25,'Kalem tutarı veya birim fiyat negatiftir.','Tutar girişini ve düzeltme belgesini inceleyin.'),
('BLG-001','Zorunlu belge eksik','Blocking',30,'Hizmet tarihinde geçerli zorunlu belge bulunamamıştır.','Mevcut hizmete ait eksik belgeyi yetkili kişiden temin edin.'),
('BLG-002','Zorunlu belge imzasız','High',25,'Mevcut zorunlu belgede gerekli e-imza işareti yoktur.','Belgenin yetkili imza sürecini tamamlayın.'),
('BLG-003','Kurul raporu süresi dolmuş','High',25,'Gerekli kurul raporunun geçerliliği hizmet tarihinde sona ermiştir.','Hizmet tarihinde geçerli raporun bulunup bulunmadığını doğrulayın.'),
('BLG-004','Yatan başvuruda epikriz eksik','Blocking',30,'Yatan hasta başvurusunda epikriz belgesi yoktur.','Hizmete ait epikriz belgesini tamamlayın.'),
('BLG-005','Ameliyat belgeleri eksik','Blocking',30,'Ameliyat için ameliyat notu veya anestezi formu eksiktir.','Ameliyata ait mevcut belgelerin kaydını tamamlayın.'),
('BLG-006','Gelecek tarihli belge','Medium',10,'Belge tarihi kontrol tarihinden ileridedir.','Belgenin tarih girişini kaynak belgeyle karşılaştırın.'),
('ILC-001','Barkod referansta yok','High',20,'İlaç veya malzeme barkodu temsili referansta bulunamamıştır.','Barkod girişini ve referans kapsamını doğrulayın.'),
('ILC-002','İlaç raporu eksik veya geçersiz','Blocking',25,'Rapor zorunlu kalemde geçerli ve imzalı kurul raporu yoktur.','Hizmet tarihine ait rapor ve imza kaydını doğrulayın.'),
('ILC-003','Ödeme koşulu belgelenmemiş','High',20,'Temsili ödeme kısıtının sağlandığı kayıtta doğrulanmamıştır.','Koşula ilişkin mevcut belgeyi inceleyip kayıt durumunu doğrulayın.'),
('ILC-004','Barkod yürürlük tarihi uyumsuz','High',15,'Barkod kaydı hizmet tarihinde geçerli değildir.','Hizmet tarihi ve referans sürümünü kontrol edin.'),
('SUR-001','Gönderim süresi aşıldı','High',20,'Gönderim veya kontrol tarihi tanımlı dönem kapanışını aşmıştır.','Dönem takvimini ve gönderim kaydını inceleyin.'),
('SUR-002','Kapalı döneme kalem eklenmiş','Blocking',25,'Kalem kapalı dönemin kapanış tarihinden sonra oluşturulmuştur.','Kayıt oluşturma zamanını ve dönem yetkilerini inceleyin.'),
('SUR-003','Hizmet dönemi uyumsuz','High',15,'Hizmet tarihi faturanın dönemine ait değildir.','Fatura dönemi ve hizmet tarihi girişini doğrulayın.'),
('SUR-004','Çıkış sonrasında hizmet','High',15,'Yatan başvurudaki hizmet tarihi çıkış tarihinden sonradır.','Hizmet ve taburcu kayıtlarını karşılaştırın.')]
def rule_identifier(code): return code.split('-')[0].title()+code.split('-')[1]
rule_constants='namespace MedulaOnKontrol.Domain.Rules;\npublic static class KuralKodlari\n{\n'+''.join(f'    public const string {rule_identifier(kural[0])} = "{kural[0]}";\n' for kural in rule_definitions)+'}\n'
(WORKSPACE_ROOT/'src/MedulaOnKontrol.Domain/Rules/KuralKodlari.cs').write_text(rule_constants,encoding='utf-8')
catalog=[]
for code,name,severity,weight,rationale,action in rule_definitions:
    parameters={'tolerans':0.01} if code.startswith('TUT') else ({'ekGun':0} if code=='SUR-001' else {})
    catalog.append(dict(KuralKodu=code,Name=name,Category=code.split('-')[0],Description='Sentetik portföy kuralı; resmî SUT hükmü değildir.',Severity=severity,Agirlik=weight,ParametersJson=json.dumps(parameters),Version=1,YururlukBaslangic='2020-01-01T00:00:00',IsActive=True,Gerekce=rationale,OnerilenAksiyon=action))
(WORKSPACE_ROOT/'data').mkdir(exist_ok=True)
(WORKSPACE_ROOT/'data/kural-katalogu.json').write_text(json.dumps(catalog,ensure_ascii=False,indent=2),encoding='utf-8')
(WORKSPACE_ROOT/'docs').mkdir(exist_ok=True)
severity_labels={'Blocking':'Engelleyici','High':'Yüksek','Medium':'Orta','Information':'Bilgi'}
intro='# Temsili kural kataloğu\n\nBu 40 kural eğitim/portföy içindir; resmî SUT veya SGK uyumluluk onayı değildir. Klinik karar ve kod önerisi üretmez. İşlem/tanı referansları işlem tarihinde, kurallar fatura döneminin ilk gününde değerlendirilir. Başlangıç kataloğu sürüm 1 ve 01.01.2020 yürürlük tarihlidir; bitişi açıktır. Yeni sürümler veritabanında geçmişi koruyarak oluşturulur ve aylık yürürlük aralıkları çakışamaz. ISL-003 parametreleri `periyotGun` ve `azamiAdet` verilmezse referans limitleri kullanılır.\n\n'
header='| Kod | Kategori | Ad | Önem | Ağırlık | Parametre | Gerekçe | Önerilen aksiyon |\n|---|---|---|---|---:|---|---|---|\n'
table=''.join(f'| {rule["KuralKodu"]} | {rule["Category"]} | {rule["Name"]} | {severity_labels[rule["Severity"]]} | {rule["Agirlik"]} | `{rule["ParametersJson"]}` | {rule["Gerekce"]} | {rule["OnerilenAksiyon"]} |\n' for rule in catalog)
(WORKSPACE_ROOT/'docs/kural-katalogu.md').write_text(intro+header+table,encoding='utf-8',newline='\n')
print(f'{len(rule_definitions)} kural üretildi.')
