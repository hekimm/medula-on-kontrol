# İsimlendirme kuralları

İş kavramları Türkçe, teknik katman ve sorumluluk adları İngilizcedir. C# tanımlayıcıları ASCII kullanır; kullanıcı metinleri Türkçe karakterlerle yazılır. Ürün adı `MedulaOnKontrol` olarak sabittir.

| Öğe | Kural | Örnek |
|---|---|---|
| Proje | Ürün + katman | `MedulaOnKontrol.Application` |
| Tip ve dosya | PascalCase; dosya adı üst düzey tip ile aynı | `FaturaKalemi.cs` |
| Arayüz | I + PascalCase | `IDenetimIsiRepository` |
| Namespace | Proje ve klasör yolu | `MedulaOnKontrol.Application.Contracts` |
| Partial sınıf | Tip + iş sorumluluğu | `MedulaRepository.Faturalar.cs` |
| Özellik, metot, enum üyesi | PascalCase | `ToplamTutar`, `BasvuruTip.Ayaktan` |
| Parametre ve yerel değişken | camelCase | `faturaRepository`, `bulguSayisi` |
| Özel alan | _camelCase | `_handlers` |
| Sabit | PascalCase; yerel sabitler dahil | `FilterPredicate` |
| Record birincil parametresi | Oluşturduğu özellik gibi PascalCase | `UserId` |
| Genel tip parametresi | T veya T + kavram | `T`, `TResult` |
| Asenkron metot | Fiil + sorumluluk + Async | `CreatePdfAsync`, `ListBulgularAsync` |
| Python | Dosya, fonksiyon ve değişkende snake_case | `verify_naming.py` |
| PowerShell / shell dosyası | Fiil + sorumluluk, kebab-case | `start-local.ps1` |
| PowerShell değişkeni / parametresi | camelCase / PascalCase | `$workspaceRoot`, `$ProjectName` |
| JavaScript değişkeni | camelCase | `clinicChartElement` |
| HTML id ve CSS sınıfı | kebab-case | `clinic-chart` |
| Belge, veri ve görsel dosyası | kebab-case | `kural-katalogu.json` |
| Ortam değişkeni | UPPER_SNAKE_CASE | `MEDULA_SECRET_DIR` |

`Id`, `Name`, `Status`, `Version`, `Revision`, `CreatedAt`, `CancellationToken`, `PageNumber` ve `PageSize` ortak teknik terimlerdir. Sorumluluk sonekleri `Repository`, `Service`, `Controller`, `Request`, `Validator`, `Options`, `Worker`, `Seeder` olarak kullanılır. Validator adı istek tipini içerir: `DuzeltmeRequestValidator`.

`i`, `j`, `id` ve `_` kısa ve açık bağlamlarda kullanılabilir. Diğer adlar temsil ettikleri işi anlatmalıdır. Framework dosyaları ve API adları kendi kurallarını izler: `Program.cs`, `GlobalUsings.cs`, `_Layout.cshtml`, `appsettings.json`, `Controller.User`. Depo giriş belgeleri `README.md`, `CONTRIBUTING.md` ve `LICENSE` yerleşik adlarını kullanır.

Public servisler `Task`/`ValueTask` ve zorunlu `CancellationToken` kullanır. Saf kural hesaplamaları senkrondur. MVC action adları URL sözleşmesi olduğu için `Async` soneki almaz; asenkron testler alır.

Oracle nesneleri `UPPER_SNAKE_CASE`, migration dosyaları `V001__initial_schema.sql` biçimindedir. Uygulanmış SQL dosyaları, enum sayıları, JSON/form alanları, kural kodları, roller ve konfigürasyon anahtarları uyumluluk sözleşmesidir. Değişiklik gerekiyorsa veri geçişi veya eşleme birlikte ele alınır.

Denetim: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-naming.ps1`. C# bildirimleri Roslyn ile; dosya yolları, Python bildirimleri ve Razor kimlikleri ayrıca kontrol edilir. Standart, `scripts/test-workspace.ps1` içinde de çalışır. Bağımlılıklar, gizli anahtarlar ve üretilen çıktılar denetime dahil değildir.

Metin dosyaları UTF-8 kullanır. PowerShell dosyaları UTF-8 BOM / CRLF, Linux betikleri LF ile kaydedilir. Ayrıntılar `.editorconfig` ve `.gitattributes` içindedir.
