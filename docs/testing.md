# Test ve paketleme

Komutlar proje kökünde çalıştırılır. Geliştirme ortamı ve ilk restore adımları [katkı rehberindedir](../CONTRIBUTING.md#geliştirme-ortamı). Birim testleri Oracle gerektirmez; entegrasyon testleri hazırlanmış bir geliştirme veritabanı, tarayıcı testleri çalışan uygulama gerektirir.

## Birim testleri ve kapsam

```powershell
dotnet test tests/MedulaOnKontrol.UnitTests --collect:"XPlat Code Coverage" --results-directory artifacts/test-results
python scripts/verify_coverage.py
```

Kapsam denetimi kural motoru ve kural uygulamalarının bütün kaynak satırlarının çalıştırılmasını ister. Bu oran projenin tamamının kapsam oranı değildir. Sonuç `artifacts/verification/coverage.json` dosyasına yazılır. Farklı test çıktı dizinini denetlemek için ilk argümanda o dizini verin.

## Oracle entegrasyon testleri

Geliştirme Oracle'ı ve ilk veri yüklemesi hazır olduğunda:

```powershell
dotnet test tests/MedulaOnKontrol.IntegrationTests
```

Testler `localhost:11521/XEPDB1` üzerindeki geliştirme veritabanını ve `secrets/` altındaki yerel anahtarları kullanır. SQLite veya bellek içi veritabanı ikamesi yoktur. Sentetik kayıtlar eklenir; yalnızca bu amaçla hazırlanmış veritabanında çalıştırın.

Başka bir Oracle test veritabanı için `MEDULA_TEST_CONNECTION` ortam değişkeni kullanılabilir. Şema ve örnek referanslar hazır olmalı; hasta alanları için kullanılan `secrets/aes-key` aynı kuruluma ait olmalıdır. Bağlantı dizesini kaynak dosyasına yazmayın.

## Tarayıcı testleri

Yerel uygulamayı başlatıp [sağlık kontrolünü](http://localhost:5186/health) doğrulayın. Ardından:

```powershell
npm ci
npx playwright install chromium
npm run test:ui
```

Varsayılan adres `http://localhost:5186`, parola dosyası `secrets/admin-password` olur. Compose örneğini sınamak için önce [parolayı kopyalayın](getting-started.md#ilk-giriş), sonra PowerShell'de:

```powershell
$env:MEDULA_URL = 'http://localhost:5187'
$env:MEDULA_ADMIN_PASSWORD_FILE = 'secrets/docker-admin-password'
npm run test:ui
```

Bu değişkenler yalnızca tarayıcı testlerinin hedefini değiştirir. Oracle entegrasyon testlerinin bağlantısını değiştirmez. Yerel varsayılanlara dönmek için yeni bir terminal açabilirsiniz.

Testler fatura kontrolü, istisna, red sınıflandırması, rapor indirme, rol sınırları ve klavye kullanımını kapsar. axe taraması otomatik erişilebilirlik kontrolüdür; elle ekran okuyucu değerlendirmesinin yerini tutmaz.

| Çıktı | Dizin |
|---|---|
| Playwright JSON ve erişilebilirlik sonuçları | `artifacts/verification/` |
| Test ekran görüntüleri | `artifacts/screenshots/` |
| Başarısız test izleri | `artifacts/ui-results/` |
| Birim testleri ve kapsam dosyaları | `artifacts/test-results/` |
| Ana test betiğinin Oracle sonuçları | `artifacts/integration-results/` |

Bu dizinler Git'e dahil edilmez. `docs/screenshots/` yalnızca README ve kullanım rehberinin kullandığı örnekleri içerir.

## Bütün kontroller

Geliştirme uygulaması, Oracle ve tarayıcı bağımlılıkları hazırken:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-workspace.ps1
```

Betik isimlendirme, birim testi, kural kapsamı, Oracle ve tarayıcı kontrollerini bu sırayla çalıştırır. Bir aşama başarısız olursa sonraki aşamaya geçmez. Hata çıktısında ilk başarısız komutu bulun; bütün zinciri yeniden çalıştırmadan önce o komutu tek başına doğrulayın.

Yalnızca isimlendirme ve kaynak biçimi için:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-naming.ps1
```

## Performans ölçümü

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/run-benchmark.ps1
```

Betik ayrı bir döneme 10.000 fatura ve 50.000 kalem ekler. Oracle'dan aday seçimi, veri okuma, kural değerlendirme ve sonuç yazımını ölçer. Veri üretimi süreye dahil değildir. Sonuç `artifacts/verification/performance.json` dosyasına yazılır.

Hedef, başarısız fatura olmadan 60 saniyenin altında tamamlamaktır. Süre donanıma, Oracle'a ve mevcut veri hacmine bağlıdır. Sonucu paylaşırken donanımı, yazılım sürümlerini ve test koşullarını belirtin.

## Temiz kurulum ve çevrimdışı paket

Boş bir veritabanıyla başlangıç akışını sınamak için:

```powershell
docker compose build
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/install-acceptance.ps1 -ProjectName medula-test
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/record-runtime-verification.ps1 -ProjectName medula-test
```

Bu kurulum kendi volume'larını ve 5190/11525 portlarını kullanır. Aynı proje adını tekrar kullanmak önceki veriyi korur. Yeni bir kurulum sınamak için kullanılmamış bir proje adı seçin. Mevcut örnek ortamın verisini silmeniz gerekmez.

İnternete bağlı makinede çevrimdışı paket hazırlamak için:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/export-offline.ps1
```

`artifacts/offline/` altında imaj arşivi, Compose dosyası, Oracle başlangıç betiği ve SHA-256 özeti oluşur. Dizin, oluşturulmuş imajları içerir; kaynak deposuna eklenmez.

Bu dizini hedef makineye taşıyın. Arşivin SHA-256 değerini `SHA256.txt` ile karşılaştırdıktan sonra hedefte bu dizinden çalıştırın:

```sh
docker load -i medula-images.tar
docker compose up -d --no-build --pull never
```

Hedef makinede de Linux konteyner desteği olan Docker gerekir. Başlangıç parolası hedef kurulumda üretilir; [giriş adımları](getting-started.md#ilk-giriş) aynıdır.
