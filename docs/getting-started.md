# Kurulum ve sorun giderme

Bu rehber uygulamayı Docker ile çalıştırır. Kodu değiştirmek için [geliştirme rehberini](../CONTRIBUTING.md), girişten sonraki adımlar için [kullanım rehberini](usage.md) izleyin.

## Gereksinimler

- Linux konteynerlerini çalıştırabilen Docker Engine veya Docker Desktop.
- Docker Compose 2.20 veya üzeri; kök Compose dosyası `include` kullanır.
- İlk derlemede imaj ve paketlerin indirilebilmesi için internet erişimi.

Bu kurulumda ana makineye .NET, Oracle veya Node.js yüklemeniz gerekmez. Komutları `compose.yaml` dosyasının bulunduğu dizinde çalıştırın. Aşağıdaki genel Docker komutları PowerShell ve Bash'te aynıdır.

```sh
docker version
docker compose version
docker compose up -d --build
docker compose ps -a
```

`initialize-secrets` tek sefer çalışıp `Exited (0)` durumuna geçer. Bu beklenen davranıştır. Oracle hazır olduktan sonra `web` başlar; `/health` kontrolü veritabanına da bağlanır.

Tarayıcıdan [sağlık kontrolünü](http://localhost:5187/health) açın. Hazır olduğunda yanıtın `status` alanı `healthy` olur. Sonra [giriş sayfasına](http://localhost:5187/Hesap/Login) gidin.

## İlk giriş

Kullanıcı adı `admin` olur. Parolayı almak için kullandığınız kabuğa uygun komutu seçin.

PowerShell:

```powershell
New-Item -ItemType Directory -Force secrets
docker compose cp web:/run/medula-secrets/admin-password secrets/docker-admin-password
Get-Content secrets/docker-admin-password
```

Bash:

```sh
mkdir -p secrets
docker compose cp web:/run/medula-secrets/admin-password secrets/docker-admin-password
cat secrets/docker-admin-password
```

`secrets/` Git dışında tutulur. Parolayı hata bildirimine veya ekran görüntüsüne eklemeyin. İlk kurulumdaki diğer demo hesapları aynı başlangıç parolasını kullanır; hesaplar ve yetkiler [erişim matrisinde](yetki-matrisi.md) açıklanır.

Veri yüklemesi güncel ay ve önceki iki ayı kapsar. Uygulamayı yeniden başlatmak mevcut faturaları sıfırlamaz veya aynı örnek verileri yeniden eklemez.

## Portlar ve ortamlar

| Ortam | Web | Oracle | Parola dosyası |
|---|---|---|---|
| Docker Compose | `localhost:5187` | `localhost:11522/XEPDB1` | `secrets/docker-admin-password`, yukarıdaki kopyalama sonrası |
| Yerel .NET geliştirme | `localhost:5186` | `localhost:11521/XEPDB1` | `secrets/admin-password` |

Bu iki ortamın veritabanları ve anahtarları ayrıdır. Bir ortamın parolasını diğerine kopyalamayın.

Varsayılan portlar doluysa, Compose kurulumu için yeni portları aynı terminalde ayarlayın. Örneğin PowerShell'de:

```powershell
$env:MEDULA_PORT = '5287'
$env:ORACLE_PORT = '12522'
docker compose up -d
```

Bash'te aynı değişkenler `export MEDULA_PORT=5287` ve `export ORACLE_PORT=12522` ile ayarlanır. Bu örnekte uygulama `http://localhost:5287` adresinde açılır. Ana makine portu değişse de konteynerler Oracle'a `oracle:1521/XEPDB1` üzerinden bağlanır.

Compose değişkenleri bir yerel `.env` dosyasında da tutulabilir. Varsayılanlarla çalışırken bu dosya gerekli değildir. Portları incelemek için `docker compose ps` kullanın; bağlantı sırları içerebileceğinden bütün yapılandırmayı bir issue'ya yapıştırmayın.

## Durdurma ve güncelleme

```sh
docker compose stop
docker compose start
```

Kaynak kodu güncelledikten sonra imajı yeniden oluşturun:

```sh
docker compose up -d --build
```

Veritabanı ve anahtarlar volume'larda tutulur. `stop` bu verileri silmez. Volume silmek mevcut kurulumu kaybettirir; AES anahtarı olmadan şifreli hasta alanları okunamaz. Temiz bir örnek ortam gerektiğinde mevcut volume'ları silmek yerine [ayrı test kurulumunu](testing.md#temiz-kurulum-ve-çevrimdışı-paket) kullanın.

## Sorun giderme

| Belirti | Kontrol |
|---|---|
| Docker sunucusuna bağlanılamıyor | Docker'ın çalıştığını ve Linux konteyner modunda olduğunu `docker version` ile doğrulayın. |
| Compose `include` alanını tanımıyor | `docker compose version` çıktısını kontrol edin; 2.20+ gerekir. |
| Tarayıcı bağlantıyı reddediyor | `docker compose ps -a` ile `web` durumuna ve yayımlanan porta bakın. İlk açılışta Oracle'ın hazırlanmasını bekleyin. |
| Sağlık yanıtı `not_ready` | Oracle bağlantısı kurulamıyor. Oracle ve web günlüklerini inceleyin. |
| `initialize-secrets` başarısız | `docker compose logs initialize-secrets` çıktısına bakın. Servisin sıfır çıkış koduyla tamamlanması gerekir. |
| Port kullanımda | Yukarıdaki ortam değişkenleriyle boş portlar seçin. |
| Parola kabul edilmiyor | Doğru ortamın parola dosyasını kullandığınızı doğrulayın. Çok sayıda başarısız denemeden sonra geçici hesap kilidi veya hız sınırı uygulanır. |
| Sayfa veya işlem görünmüyor | Hesabın rolünü ve klinik yetkisini [erişim matrisiyle](yetki-matrisi.md) karşılaştırın. |
| Fatura listesi boş | Kurulumda oluşan üç dönemden birini seçin. Dönem girdisi `YYYYMM` biçimindedir. |

Günlükleri son satırlarla sınırlamak için:

```sh
docker compose logs --tail 100 oracle
docker compose logs --tail 100 web
```

Hata bildirirken çalıştırdığınız komutu, işletim sistemini ve ilgili hata satırlarını ekleyin. Parolaları, anahtarları ve bağlantı bilgilerini çıkarın. Veritabanını veya anahtar deposunu silmek bir sorun giderme adımı değildir.
