# Sentetik veri tohumlama

Tohumlama, uygulama başlangıcında `SentetikVeriSeeder.RunAsync` tarafından transaction içinde yapılır. Hasta adı ve geçersiz test kimliği uygulama seviyesinde AES-GCM ile şifrelendiği için bu alanlar açık metin SQL dump'ıyla dağıtılmaz.

- Kaynak: `src/MedulaOnKontrol.Infrastructure/Seeding/SentetikVeriSeeder.cs`.
- Kural tanımları: `data/kural-katalogu.json`.
- Varsayılan: güncel ay ve önceki iki ay; her ay 1.000 fatura × 5 kalem = 5.000 kalem.
- Ek izolasyon örneği: ikinci kurumda 2 fatura/10 kalem.
- Seed sürümü `TOHUMLAMA` tablosunda saklanır; yeniden başlatma verileri tekrar eklemez.
- `i % 20` ile kontrollü varyantlar; provizyon, tanı, işlem-tanı, işlem/paket/tekrar, tutar, belge, ilaç/malzeme ve dönem ihlalleri bulunur.
- Kimlikler `0` ile başlayan 11 haneli sentetik dizilerdir; geçerli T.C. kimlik numarası olamaz.
- Performans veri üretimi: `scripts/run-benchmark.ps1`; ayrı bir dönemde 10.000 fatura/50.000 kalem.

Salt SQL ile doğrulama için `S001__seed_verification.sql` kullanılabilir. SQLPlus oturumunda MEDULA şemasını seçin. Gerçek hasta veya SGK/MEDULA kimlik bilgisi girmeyin.
