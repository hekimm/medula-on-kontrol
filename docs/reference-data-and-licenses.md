# Referans veri ve bağımlılık kaynakları

## Klinik referans kapsamı

ICD-10 için yalnızca `J06.9`, `I10` ve `N40` örnek kodları kullanılır. Kaynak: [WHO ICD-10 2019 tarayıcısı](https://icd.who.int/browse10/2019/en?lang=en). Türkçe açıklamalar kısa temsili karşılıklardır; resmî Türkçe sınıflandırma dağıtımı değildir.

`TST001`–`TST012` kodları, puanları, fiyatları, paket ilişkileri, yaş/cinsiyet koşulları ve belge gereksinimleri sentetik test verileridir. Gerçek SUT kodu veya ödeme şartı değildir. Resmî metinler için [SGK duyuruları](https://www.sgk.gov.tr/duyuru/index/TumBirimler?page=1) kullanılmalıdır. Proje tam SUT/ICD listesi dağıtmaz.

## Sabitlenmiş bileşenler

NuGet doğrudan sürümleri `.csproj`, çözümlenmiş geçişli sürümler `packages.lock.json` dosyalarında saklanır. Docker restore `--locked-mode` kullanır. Arayüz CSS/JS dosyaları `wwwroot/vendor` içine alınmıştır; çalışma anında CDN bağlantısı yoktur.

| Bileşen | Sürüm | Kaynak / lisans notu |
|---|---|---|
| Bootstrap | 5.3.3 | [Bootstrap](https://github.com/twbs/bootstrap/tree/v5.3.3), MIT |
| ApexCharts | 3.54.1 | [Sürüm paketi](https://www.npmjs.com/package/apexcharts/v/3.54.1); bu paketteki MIT lisans metni vendor dizininde korunur. Yeni sürümlerin lisansı farklı olabilir. |
| FluentAssertions | 7.2.0 | [NuGet](https://www.nuget.org/packages/FluentAssertions/7.2.0), Apache-2.0; yeni ticari sürümlere otomatik yükseltilmez. |
| QuestPDF | 2025.7.4 | [QuestPDF lisans belgeleri](https://www.questpdf.com/license/); bireysel portföy kullanımına göre Community yapılandırılır. Kurumsal dağıtım için seçilen sürüm ve kullanımın lisans uygunluğu ayrıca değerlendirilir. |
| Oracle XE konteyneri | 21.3.0-slim-faststart | [İmaj geliştiricisinin kaynak deposu](https://github.com/gvenzl/oci-oracle-xe); imaj digest'i Compose içinde sabittir. Oracle lisans koşulları ayrıca geçerlidir. |
| .NET | 8 | [Microsoft destek yaşam döngüsü](https://devblogs.microsoft.com/dotnet/dotnet-8-9-end-of-support/): 10.11.2026 destek sonu; gerçek kurumsal işletimde yükseltme planı gerekir. |

Kaynak kodun [MIT lisansı](../LICENSE), bu bileşenlerin kendi lisanslarının yerine geçmez. Depoya dahil edilen Bootstrap ve ApexCharts lisans metinleri `src/MedulaOnKontrol.Web/wwwroot/vendor/` altında tutulur.
