# Geliştirme ve katkı

Bir sorunu düzeltiyorsanız önce mevcut davranışı tekrar üretin. Değişikliği tek bir konu etrafında tutun; ilgisiz yeniden adlandırma veya biçim değişikliklerini ayrı çalışın.

## Geliştirme ortamı

Yerel geliştirme betikleri Windows PowerShell içindir. .NET SDK sürümü [global.json](global.json) içinde sabittir. Docker'ın Linux konteynerlerini çalıştırabilmesi gerekir. Testler ayrıca Python 3.9+ ve Node.js 18+ kullanır.

Fork'unuzu klonlayıp proje kökünde çalışın:

```powershell
dotnet --version
docker version
python --version
node --version
dotnet restore MedulaOnKontrol.sln --locked-mode
dotnet build MedulaOnKontrol.sln --no-restore
```

Yalnızca kural motoru üzerinde çalışırken veritabanı gerekmez:

```powershell
dotnet test tests/MedulaOnKontrol.UnitTests
```

Web veya veri erişimi için geliştirme Oracle'ını başlatın:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/start-oracle.ps1
docker logs --tail 50 medula-oracle
```

Oracle hazır olduktan sonra:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/start-local.ps1
Invoke-RestMethod http://localhost:5186/health
Get-Content secrets/admin-password
```

`start-local.ps1` uygulamayı `artifacts/local-web` altına yayımlar ve arka planda başlatır. Kaynak değişiklikleri otomatik yüklenmez; görmek için betiği yeniden çalıştırın. Betik yalnızca bu dizinden başlatılmış, 5186 portundaki uygulamayı yeniden başlatır. Başka bir süreç o portu kullanıyorsa hata verir.

Compose örneği 5187/11522, yerel geliştirme 5186/11521 portlarını kullanır. İki ortamın anahtarları ve verileri ayrıdır. Linux veya macOS üzerinde uygulamayı denemek için [Compose kurulumunu](docs/getting-started.md) kullanabilirsiniz; yerel Windows betikleri bu kabuklarda doğrudan çalışmaz.

## Nerede değişiklik yapılır?

| Değişiklik | Başlangıç noktası | Kontrol |
|---|---|---|
| İş kuralı veya hesaplama | `Application/Rules`, `Application/Services` | İlgili birim testi ve kural kapsamı |
| Sorgu, revizyon, transaction | `Infrastructure/Persistence` | Oracle entegrasyon testleri |
| Sayfa, form veya kullanıcı akışı | `Web/Controllers`, `Web/Views`, `Web/wwwroot` | Playwright ve elle ilgili ekran |
| Başlangıç kural tanımı | `scripts/generate_catalog.py` | Üretilen katalog ve kural sabitleri |
| Şema değişikliği | `db/migrations` | Mevcut ve boş Oracle kurulumları |
| Kullanım veya kurulum açıklaması | `README.md`, `docs/` | Belge bağlantıları ve anlatılan komutlar |

Tablodaki katman yolları `src/MedulaOnKontrol.` proje adlarına göredir. Bağımlılık yönleri ve kayıt sözleşmeleri [mimari belgede](docs/architecture-decisions.md), dosya ve kod adları [isimlendirme kurallarında](docs/naming-conventions.md) açıklanır.

## Testleri çalıştırma

Geliştirme uygulaması ve Oracle hazırken:

```powershell
npm ci
npx playwright install chromium
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-workspace.ps1
```

Bu komut isimlendirme, birim, kural kapsamı, Oracle ve arayüz kontrollerini sırayla çalıştırır. Oracle testleri sentetik kayıt ekler; kişisel veya üretim veritabanına yönlendirmeyin. Ayrı test komutları ve ortam değişkenleri [test belgesindedir](docs/testing.md).

Bir davranışı değiştiriyorsanız önce onu gösteren testi ekleyin veya güncelleyin. Yalnızca belge değişikliğinde bütün entegrasyon ortamını çalıştırmak gerekmez; `python scripts/verify_naming.py` dosya adlarını ve yerel Markdown bağlantılarını denetler.

## Üretilen kaynaklar ve bağımlılıklar

`python scripts/generate_catalog.py`, başlangıç JSON kataloğunu, katalog belgesini ve C# kural sabitlerini üretir. Bunlar depoya dahildir; üreticiyi değiştirdiğinizde ilgili çıktıları birlikte inceleyin.

`scripts/generate_schema.py` başlangıç şemasının üreticisidir. Mevcut `V001` ve `V002` dosyalarını yeniden yazdığı için normal geliştirme akışında çalıştırmayın. Şema değişikliklerini yeni bir migration dosyasıyla ekleyin; daha önce uygulanmış SQL dosyalarını değiştirmeyin.

NuGet `packages.lock.json` dosyaları ve npm `package-lock.json` depoda tutulur. Bir paket sürümünü değiştirirken kilit dosyasını da güncelleyin; bağımlılık değişikliğinin nedenini açıklayın. `wwwroot/vendor` uygulamanın CDN olmadan açılması için gereklidir ve lisans metinleriyle birlikte korunur.

## Pull request

Başlık değişikliğin sonucunu anlatsın. Açıklamada sorunu, nasıl düzelttiğinizi ve çalıştırdığınız testleri yazın. Arayüz değişiyorsa ilgili ekran görüntüsünü PR'a ekleyin; testin ürettiği bütün görselleri kaynak depoya kopyalamayın.

Göndermeden önce:

```sh
git status --short
git diff --check
git diff
```

Yeni dosyalar `git diff` çıktısında görünmez. Eklemeyi düşündüğünüz dosyaları ayrı inceleyin; staging sonrasında `git diff --cached` ile gönderilecek içeriği kontrol edin.

`artifacts/`, `bin/`, `obj/`, `node_modules/`, günlükler, `.env` ve `secrets/` Git dışındadır. `docs/screenshots/` yalnızca rehberlerin kullandığı iki örneği içerir. Kilit dosyaları, veri kataloğu ve üçüncü taraf lisansları gereksiz çıktı değildir; depoda kalır.

Hata bildiriminde tekrar üretme adımlarını, beklenen ve gerçekleşen davranışı, işletim sistemi ile ilgili hata satırlarını paylaşın. Parola, anahtar, bağlantı bilgisi veya gerçek hasta verisi eklemeyin. Kaynak katkıları projenin [MIT lisansı](LICENSE) kapsamında değerlendirilir.
