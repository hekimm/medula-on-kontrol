# Mimari ve iş davranışı kararları

## Bağımlılıklar ve hata modeli

Domain NuGet bağımlılığı içermez. Application Domain'e, Infrastructure Application'a, Web Infrastructure'a bağımlıdır. Web, DI composition root'tur. İş verisine erişim `IFaturaRepository`, `IKuralRepository`, `IKullaniciRepository`, `IDenetimRepository`, `IRedRepository`, `IDenetimIsiRepository`, `IGonderimRepository` sözleşmeleri üzerinden yapılır. Beklenen iş reddi `Result<T>` ile ifade edilir. Beklenmeyen altyapı hataları loglanır; kullanıcıya bağlantı ayrıntıları veya SQL gösterilmez.

Public servis sözleşmeleri `Task`/`ValueTask` ve zorunlu `CancellationToken` kullanır. Raporlama geçersiz dönem için `Result<byte[]>.Failure` döndürür. Kriptografik bütünlük hatası ve iptal, iş kuralı reddinden farklıdır. QuestPDF/ClosedXML'in senkron dosya serileştirmesi öncesinde ve sonrasında iptal kontrol edilir; serileştirme kütüphanesinin tek çağrısının ortasında durdurma garantisi verilmez.

## Tarihler, versiyonlar, para

Kuralın fatura döneminin ilk gününde yürürlükte olması gerekir. Kural versiyonları ayın ilk gününde başlar; aynı kodun aralıkları çakışmaz. Kontrol edilmiş dönemler geriye dönük değiştirilemez; bu kullanım simülasyon ekranıyla karşılanır. Referans işlem ve tanı geçerliliği kalemin gerçek işlem tarihinde değerlendirilir. Üst sınır tarihi dahildir. Yaş, hizmet günündeki tamamlanmış yaş olarak hesaplanır.

Denetim zamanları UTC saklanır. Kurum takvimi Türkiye'dir. Arayüz `tr-TR`, `dd.MM.yyyy` ve TL gösterimi kullanır. HTML sayı girdisinin noktalı ondalığı ile Türkçe virgüllü girdi ayrı kültürlerle bağlanır. Tutarlar `decimal` ve Oracle `NUMBER(18,2)` kullanır; kuruş yuvarlaması `AwayFromZero` ile yapılır.

## Skor

Varsayılan skor formülü: `min(100, Σ(ağırlık × önem çarpanı × etkilenen tutar / fatura toplamı) × 100)`. Ağırlıklar 1–100 olduğu için bu formül yüksek skorları kolayca 100'e doyurur. `Scoring:NormalizeWeights=true` seçeneği ağırlığı 100'e böler. Her iki seçenek birim testlerinde kapsanır. Kullanılan yapılandırma her çalıştırmada `SKORLAMA_JSON` alanında saklanır.

Riskteki tutar, açık veya kabul edilmiş ENGELLEYICI/YUKSEK bulgu içeren **benzersiz kalemlerin** pozitif tutarlarıdır. Fatura düzeyinde böyle bir bulgu varsa tüm fatura riskte kabul edilir. Risk tutarı fatura toplamını aşmaz. Geçerli istisnalar katkı vermez. Sıfır toplamlı faturada açık bulgu varsa skor 100, yoksa 0'dır.

Tahmini önlenen tutar, fatura bazında ilk kontrol riskinden güncel riskin çıkarılmasıyla bulunur; negatif farklar 0 kabul edilir. İstisna gibi işlemler de bu değeri etkileyebilir. Bu değer gerçek tahsilat veya nedensel olarak önlenmiş SGK reddi değildir.

## Eşzamanlılık ve işlem bütünlüğü

Kontrol, fatura revizyonunu okuyup aynı revizyon üzerinde atomik olarak kaydedilir. Eşzamanlı düzeltme eski sonucun kaydedilmesini engeller. Hasta ilişkili değişiklikler aynı kurumdaki gönderilmemiş ilgili faturaların revizyonunu artırır ve onayını düşürür. Yeni kontrol eski bulguları silmez veya yeniden yazmaz. İstisna, kural kodu+versiyon+kalem+fatura revizyonu+son tarih ile sınırlıdır.

Onayda güncel kontrol ve çözülmüş engelleyici bulgular aranır. Süresi dolmuş istisna gönderime izin vermez. Gönderim anahtarı fatura ve revizyondan deterministik üretilir. Gönderim sürerken düzeltme engellenir. Kesilen gönderim bir dakika sonra aynı anahtarla tekrar denenebilir. Simülatör aynı anahtara aynı cevabı üretir; gerçek MEDULA idempotency garantisi iddia edilmez.

## Dönem işleri ve performans

Oracle'daki kalıcı kuyruk `FOR UPDATE SKIP LOCKED` ile alınır. İş kiraları ilerlemeyle uzatılır, süreç kesilirse süresi dolan iş tekrar alınabilir. İş sahibinin kurum, aktiflik ve rolü yeniden kontrol edilir. PL/SQL, durum/yürürlük/yetki uygunluğunu set tabanlı seçer, `BULK COLLECT LIMIT 1000` ve `FORALL` ile adayları yazar. Bulgusuz olduğu kanıtlanamayan fatura elenmez. Temiz faturaları pahalı kurallardan önce tahminle elemek gibi yanlış negatif üretebilecek bir optimizasyon kullanılmaz.

Bağlamlar dönem için toplu okunur, hasta geçmişi yalnızca gerekli hizmet gerçeklerini içerir. C# değerlendirmesi ve kalıcı sonuç yazımı sekiz sınırlı paralel işçiyle yapılır. Her faturanın sonucu, bulguları ve denetim kaydı tek transaction'dadır. İptal token'ları beklemelere ve veri tabanı komutlarına aktarılır.

## Red geri beslemesi

Sınıflandırılan kural kodu uzman girdisidir; yalnızca motorun tahminine bakılarak üretilmez. Aynı fatura/kalem/kural tekrarları tek gözlem sayılır. Eşleştirme gönderim anında kaydedilen kontrol çalıştırmasına ve redden önceki bulguya dayanır; sonradan yapılan kontroller modele sızmaz. Fatura genelindeki bulgu ilgili faturanın kalem redlerini kapsar. Sonucu bilinmeyen gönderimler metrik hesabına girmez. Paydası sıfır olan metrik `—` gösterilir. Sınıflandırılmamış redler kurala rastgele atanmaz; desen önerilerinde görünür.

Precision = TP/(TP+FP), recall = TP/(TP+FN), F1 = 2TP/(2TP+FP+FN). Kural ağırlıkları, kalibrasyon ekranında yetkili kural yöneticisince yeni versiyon oluşturularak kalibre edilir. En az 10 gözlem gerekir. Öneri, mevcut ağırlık × (0,5 + (2TP + 2)/(2TP + FP + FN + 4)) ile hesaplanır ve 1–100 aralığına sınırlandırılır. Bu, temsili bir ağırlık önerisidir; istatistiksel olarak doğrulanmış SGK red olasılığı modeli değildir. İleri yürürlük tarihi ve gerekçe zorunludur; eski sürümle gelen istek reddedilir. Kuralın önem düzeyi, parametreleri ve geçmiş bulgular korunur. Öneri kendiliğinden uygulanmaz.

Fatura genelinde bulgu üreten kurallarda hem tahmin hem red tarafının gözlem birimi fatura/kuraldır; diğerlerinde fatura/kalem/kuraldır. Böylece çok kalemli bir red, recall paydasında farklı bir birimle sayılmaz. Tekrarlanan gözlemde ilk olay zamanı esas alınır.

Simülatör reddi veya elle girilmiş red, yetkili kullanıcı tarafından mevcut kayıt üzerinden sınıflandırılabilir. Fatura/kurum/klinik yetkisi repository'de doğrulanır; isteğin eski kalem ve kural eşlemesi değişmişse güncelleme reddedilir. Farklı faturanın kalemi veya kalem tutarını aşan eşleme kabul edilmez. İşlem red tutarını ve tarihini değiştirmez, yeni red satırı oluşturmaz. Eski/yeni eşleme ve uzman gerekçesi aynı transaction içinde denetim izine yazılır; analiz yeni sınıflandırmayı kullanır.

## Güvenlik ve saklama

AES-256-GCM her şifrelemede rastgele nonce ve bütünlük etiketi kullanır. Başlangıç sırları kurulumda oluşturulur; parola ve anahtar repoda bulunmaz. Varsayılan hasta görünümü maskelidir. Maske kaldırma ayrı yetki ve denetim kaydı gerektirir, yanıt önbelleğe alınmaz. Denetim kayıtları hasta adı/kimlik düz metnini içermez. Oturum anahtarları AES ile korunur. Başarısız giriş sayacı, geçici hesap kilidi, giriş hız sınırı, HttpOnly/SameSite çerezi, anti-forgery ve CSP vardır.

Saklama süresi `Retention:Days` ile ayarlanır (varsayılan 3650 gün, hukuki süre iddiası değildir). Süresi dolan, aktif faturası olmayan hastanın ad/kimlik/doğum tarihi/cinsiyeti anonimleştirilir. Denetim izi silinmez. Anahtar ve Oracle volume yedekleri birlikte, erişim kontrollü saklanmalıdır. TLS, kurum IAM, gerçek elektronik imza ve resmî mevzuat yönetimi uygulamanın kapsamı dışındadır.

Fatura bulgu sayımı fatura/dönem ile daraltılır; `IX_BULGU_CALISTIRMA` ve `IX_CALISTIRMA_FATURA` indeksleri tekrar kontrol ve panel sorgularının geçmiş hacmiyle gereksiz tarama yapmasını önler. Ölçüm aşamaları `artifacts/verification/performance.json` içinde ayrı verilir.
