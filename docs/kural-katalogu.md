# Temsili kural kataloğu

Bu 40 kural eğitim/portföy içindir; resmî SUT veya SGK uyumluluk onayı değildir. Klinik karar ve kod önerisi üretmez. İşlem/tanı referansları işlem tarihinde, kurallar fatura döneminin ilk gününde değerlendirilir. Başlangıç kataloğu sürüm 1 ve 01.01.2020 yürürlük tarihlidir; bitişi açıktır. Yeni sürümler veritabanında geçmişi koruyarak oluşturulur ve aylık yürürlük aralıkları çakışamaz. ISL-003 parametreleri `periyotGun` ve `azamiAdet` verilmezse referans limitleri kullanılır.

| Kod | Kategori | Ad | Önem | Ağırlık | Parametre | Gerekçe | Önerilen aksiyon |
|---|---|---|---|---:|---|---|---|
| PRV-001 | PRV | Provizyon numarası eksik | Engelleyici | 30 | `{}` | Provizyon numarası boş bırakılamaz. | Başvurunun provizyon kaydını doğrulayarak eksik alanı tamamlayın. |
| PRV-002 | PRV | Provizyondan önce hizmet | Yüksek | 20 | `{}` | Hizmet tarihi provizyon tarihinden öncedir. | Hizmet ve provizyon tarihlerini kaynak kayıtlarla karşılaştırın. |
| PRV-003 | PRV | Yatış ve çıkış tarihleri tutarsız | Engelleyici | 30 | `{}` | Çıkış tarihi yatış tarihinden öncedir. | Yatış ve çıkış kayıtlarındaki giriş hatasını düzeltin. |
| PRV-004 | PRV | Dönemde mükerrer takip | Engelleyici | 30 | `{}` | Aynı takip numarası aynı dönemde birden fazla faturada kullanılmıştır. | İlgili faturaları inceleyerek mükerrer kaydı giderin. |
| PRV-005 | PRV | Takip numarası eksik | Yüksek | 15 | `{}` | Başvuruda takip numarası bulunmamaktadır. | Kaynak başvuru belgesinden takip numarasını doğrulayın. |
| TANI-001 | TANI | Ana tanı eksik | Engelleyici | 35 | `{}` | Başvuruda ana tanı kaydı bulunmamaktadır. | Yetkili hekim ve kodlama uzmanının kayıt incelemesini isteyin. |
| TANI-002 | TANI | Tanı yürürlük tarihi uyumsuz | Yüksek | 15 | `{}` | Tanı kodunun hizmet tarihinde geçerli referans kaydı yoktur. | Hizmet tarihini ve referans sürümünü kodlama uzmanıyla doğrulayın. |
| TANI-003 | TANI | Tanı ve cinsiyet tutarsız | Yüksek | 20 | `{}` | Tanının temsili cinsiyet kısıtı hasta kaydıyla çelişmektedir. | Demografik veriyi ve klinik belgelendirmeyi uzmanla inceleyin. |
| TANI-004 | TANI | Tanı ve yaş tutarsız | Yüksek | 20 | `{}` | Hizmet tarihindeki yaş tanının temsili aralığı dışındadır. | Doğum tarihini ve klinik belgelendirmeyi uzmanla inceleyin. |
| TANI-005 | TANI | Birden fazla ana tanı | Orta | 10 | `{}` | Başvuruda birden fazla ana tanı vardır. | Ana ve yan tanı işaretlemelerini kodlama uzmanıyla doğrulayın. |
| TANI-006 | TANI | Tanı referansta bulunamadı | Yüksek | 15 | `{}` | Tanı kodu yüklenmiş temsili referans alt kümesinde yoktur. | Referans kapsamını ve kayıt aktarımını inceleyin. |
| ITU-001 | ITU | İşlem için tanı kaydı uyumsuz | Yüksek | 25 | `{}` | İşlemin zorunlu tanı ön eklerinden hiçbiri başvuruda yer almamaktadır. | Mevcut klinik belgeler ile işlem ve tanı kayıtlarını uzmanla karşılaştırın. |
| ITU-002 | ITU | Başvuru tipi uyumsuz | Yüksek | 20 | `{}` | İşlemin izin verilen başvuru tipi kayıtla uyuşmamaktadır. | Başvuru tipini ve hizmet belgesini doğrulayın. |
| ISL-001 | ISL | Birlikte faturalandırma çakışması | Engelleyici | 30 | `{}` | Temsili birlikte faturalandırılamama ilişkisi aynı gün veya başvuruda ihlal edilmiştir. | İlgili hizmet kayıtlarını ve fatura kapsamını inceleyin. |
| ISL-002 | ISL | Paket kapsamındaki kalem ayrıca ücretli | Yüksek | 25 | `{}` | Başvurudaki paket kapsamına giren kalem ayrıca ücretlendirilmiştir. | Paket kapsamını doğrulayarak mükerrer ücretlendirmeyi giderin. |
| ISL-003 | ISL | Tekrar limiti aşıldı | Yüksek | 20 | `{}` | Hasta bazında kayan zaman aralığındaki adet temsili limiti aşmaktadır. | Hizmet tekrarlarını ve mükerrer giriş olasılığını inceleyin. |
| ISL-004 | ISL | İşlem ve cinsiyet tutarsız | Yüksek | 20 | `{}` | İşlemin temsili cinsiyet kısıtı hasta kaydıyla çelişmektedir. | Demografik kayıt ve hizmet belgesini uzmanla doğrulayın. |
| ISL-005 | ISL | İşlem ve yaş tutarsız | Yüksek | 20 | `{}` | Hizmet tarihindeki yaş işlemin temsili aralığı dışındadır. | Doğum tarihini ve hizmet belgesini doğrulayın. |
| ISL-006 | ISL | İşlem yürürlük tarihi uyumsuz | Engelleyici | 25 | `{}` | Hizmet tarihinde geçerli işlem referansı bulunamamıştır. | İşlem tarihini ve kullanılan referans sürümünü doğrulayın. |
| ISL-007 | ISL | Mükerrer hizmet kaydı | Yüksek | 25 | `{}` | Aynı hasta, işlem ve tarih birleşiminde başka kalem bulunmaktadır. | İlgili kalemleri inceleyerek mükerrer girişi giderin. |
| ISL-008 | ISL | Adet geçersiz | Engelleyici | 25 | `{}` | Adet pozitif tam sayı değildir. | Hizmet belgesine göre adet girişini düzeltin. |
| ISL-009 | ISL | Gelecek tarihli hizmet | Yüksek | 15 | `{}` | Hizmet tarihi kontrol gününden ileridedir. | Hizmet tarihindeki giriş hatasını doğrulayın. |
| TUT-001 | TUT | Kalem tutarı hesapla uyuşmuyor | Yüksek | 25 | `{"tolerans": 0.01}` | Kalem tutarı adet ile geçerli temsili birim fiyatın çarpımından farklıdır. | Adet, fiyat ve yuvarlama kayıtlarını karşılaştırın. |
| TUT-002 | TUT | Dönem fiyatı uyumsuz | Yüksek | 20 | `{"tolerans": 0.01}` | Uygulanan birim fiyat hizmet tarihinde geçerli temsili fiyattan farklıdır. | Fiyat sürümünü ve hizmet tarihini doğrulayın. |
| TUT-003 | TUT | Fatura toplamı tutarsız | Engelleyici | 30 | `{"tolerans": 0.01}` | Fatura toplamı kalem tutarları toplamına eşit değildir. | Kalemleri doğrulayarak fatura toplamını yeniden hesaplayın. |
| TUT-004 | TUT | Negatif tutar | Engelleyici | 25 | `{"tolerans": 0.01}` | Kalem tutarı veya birim fiyat negatiftir. | Tutar girişini ve düzeltme belgesini inceleyin. |
| BLG-001 | BLG | Zorunlu belge eksik | Engelleyici | 30 | `{}` | Hizmet tarihinde geçerli zorunlu belge bulunamamıştır. | Mevcut hizmete ait eksik belgeyi yetkili kişiden temin edin. |
| BLG-002 | BLG | Zorunlu belge imzasız | Yüksek | 25 | `{}` | Mevcut zorunlu belgede gerekli e-imza işareti yoktur. | Belgenin yetkili imza sürecini tamamlayın. |
| BLG-003 | BLG | Kurul raporu süresi dolmuş | Yüksek | 25 | `{}` | Gerekli kurul raporunun geçerliliği hizmet tarihinde sona ermiştir. | Hizmet tarihinde geçerli raporun bulunup bulunmadığını doğrulayın. |
| BLG-004 | BLG | Yatan başvuruda epikriz eksik | Engelleyici | 30 | `{}` | Yatan hasta başvurusunda epikriz belgesi yoktur. | Hizmete ait epikriz belgesini tamamlayın. |
| BLG-005 | BLG | Ameliyat belgeleri eksik | Engelleyici | 30 | `{}` | Ameliyat için ameliyat notu veya anestezi formu eksiktir. | Ameliyata ait mevcut belgelerin kaydını tamamlayın. |
| BLG-006 | BLG | Gelecek tarihli belge | Orta | 10 | `{}` | Belge tarihi kontrol tarihinden ileridedir. | Belgenin tarih girişini kaynak belgeyle karşılaştırın. |
| ILC-001 | ILC | Barkod referansta yok | Yüksek | 20 | `{}` | İlaç veya malzeme barkodu temsili referansta bulunamamıştır. | Barkod girişini ve referans kapsamını doğrulayın. |
| ILC-002 | ILC | İlaç raporu eksik veya geçersiz | Engelleyici | 25 | `{}` | Rapor zorunlu kalemde geçerli ve imzalı kurul raporu yoktur. | Hizmet tarihine ait rapor ve imza kaydını doğrulayın. |
| ILC-003 | ILC | Ödeme koşulu belgelenmemiş | Yüksek | 20 | `{}` | Temsili ödeme kısıtının sağlandığı kayıtta doğrulanmamıştır. | Koşula ilişkin mevcut belgeyi inceleyip kayıt durumunu doğrulayın. |
| ILC-004 | ILC | Barkod yürürlük tarihi uyumsuz | Yüksek | 15 | `{}` | Barkod kaydı hizmet tarihinde geçerli değildir. | Hizmet tarihi ve referans sürümünü kontrol edin. |
| SUR-001 | SUR | Gönderim süresi aşıldı | Yüksek | 20 | `{"ekGun": 0}` | Gönderim veya kontrol tarihi tanımlı dönem kapanışını aşmıştır. | Dönem takvimini ve gönderim kaydını inceleyin. |
| SUR-002 | SUR | Kapalı döneme kalem eklenmiş | Engelleyici | 25 | `{}` | Kalem kapalı dönemin kapanış tarihinden sonra oluşturulmuştur. | Kayıt oluşturma zamanını ve dönem yetkilerini inceleyin. |
| SUR-003 | SUR | Hizmet dönemi uyumsuz | Yüksek | 15 | `{}` | Hizmet tarihi faturanın dönemine ait değildir. | Fatura dönemi ve hizmet tarihi girişini doğrulayın. |
| SUR-004 | SUR | Çıkış sonrasında hizmet | Yüksek | 15 | `{}` | Yatan başvurudaki hizmet tarihi çıkış tarihinden sonradır. | Hizmet ve taburcu kayıtlarını karşılaştırın. |
