# Veri modeli ve akışlar

Bu sayfa uygulamanın temel akışlarını gösterir. Ayrıntılı davranışlar [mimari kararlar](../architecture-decisions.md), kullanıcı yetkileri [erişim matrisi](../yetki-matrisi.md) içindedir.

## Bileşenler

Web katmanı servisleri çağırır; veri erişimi ve dış sistem simülatörleri Infrastructure katmanındadır.

```mermaid
flowchart TB
    Kullanici[Yetkili kurum kullanıcısı] --> Web[ASP.NET Core MVC / Razor]
    Web --> Auth[Cookie / BCrypt / Rol politikaları]
    Web --> App[Application servisleri]
    App --> Domain[Domain / IFaturaKurali / Result]
    App --> Repository[Repository arayüzleri]
    Impl[Infrastructure / Dapper] -. uygular .-> Repository
    Impl --> Oracle[(Oracle XE 21c)]
    Worker[IHostedService dönem işçisi] --> App
    Worker --> Paket[PKG_ON_KONTROL]
    Paket --> Oracle
    Impl --> Simulator[Polly / MEDULA simülatörü]
    Web --> Rapor[QuestPDF / ClosedXML]
    Impl --> AES[AES-GCM / anahtar volume]
```

## Fatura kontrolü

Kontrol sonucu, okunan fatura revizyonu hâlâ güncelse kaydedilir. Eşzamanlı değişiklik eski sonucun kaydedilmesini engeller.

```mermaid
sequenceDiagram
    actor U as Kullanıcı
    participant W as MVC / Yetki / CSRF
    participant S as FaturaKontrolService
    participant R as IFaturaRepository
    participant D as Oracle
    participant E as KuralEngine
    U->>W: POST Kontrol Et
    W->>W: Oturum, rol ve anti-forgery doğrula
    W->>S: ValidateAsync(accessScope, invoiceId, token)
    S->>R: GetAsync(accessScope, invoiceId)
    R->>D: Kurum/klinik kapsamlı bağlam sorguları
    D-->>R: Fatura, kalemler, tanılar, belgeler, tarihsel gerçekler
    R-->>S: FaturaDenetimBaglami ve revizyon
    S->>E: Dönemde yürürlükteki kuralları değerlendir
    E-->>S: Bulgular, skor, benzersiz risk tutarı
    S->>R: SaveValidationAsync(sonuç, beklenen revizyon)
    R->>D: BEGIN / koşullu revizyon güncellemesi
    alt Revizyon veya yetki değişmiş
        R-->>S: Result.Failure
    else Güncel kayıt
        R->>D: Çalıştırma + bulgular + denetim izi / COMMIT
        R-->>S: Result.Success
    end
    S-->>W: İşlem sonucu
    W-->>U: Türkçe bulgu ve aksiyon paneli
```

## Veri ilişkileri

Şema ana varlıkları gösterir; bütün sütunları ve kısıtları içermez. Referans kodu ilişkilerinin bir kısmı mantıksaldır. Bilinmeyen veya eski kodların kural motorunca raporlanabilmesi için her işlem ve tanı girişine referans yabancı anahtarı uygulanmaz.

```mermaid
erDiagram
    KURUM ||--o{ KLINIK : kapsar
    KURUM ||--o{ KULLANICI : barindirir
    KULLANICI ||--o{ KULLANICI_ROL : atanir
    ROL ||--o{ KULLANICI_ROL : tanimlar
    KULLANICI ||--o{ KULLANICI_KLINIK_YETKI : yetkilidir
    KLINIK ||--o{ KULLANICI_KLINIK_YETKI : sinirlar
    HASTA ||--o{ BASVURU : basvurur
    KLINIK ||--o{ BASVURU : kabul_eder
    BASVURU ||--o{ BASVURU_TANI : tanilar
    BASVURU ||--o{ BELGE : belgeler
    BASVURU ||--o{ FATURA : faturalanir
    FATURA ||--o{ FATURA_KALEMI : icerir
    FATURA ||--o{ KURAL_CALISTIRMA : denetlenir
    KURAL_CALISTIRMA ||--o{ BULGU : uretir
    KURAL ||--o{ BULGU : versiyonla_eslesir
    FATURA_KALEMI |o--o{ BULGU : etkilenir
    BULGU ||--o{ BULGU_ISTISNA : gerekcelendirilir
    FATURA ||--o{ RED_KAYDI : reddedilir
    FATURA ||--o{ GONDERIM : gonderilir
    DENETIM_ISI ||--o{ DENETIM_ADAY : secer
    FATURA ||--o{ DENETIM_ADAY : adaydir
    KULLANICI ||--o{ DENETIM_IZI : iz_birakir
    SUT_ISLEM ||--o{ ISLEM_TANI_MATRIS : temsili_eslesme
    SUT_ISLEM ||--o{ BELGE_ZORUNLULUK : belge_ister
    SUT_ISLEM ||--o{ PAKET_ICERIK : paket_icerigi
    SUT_ISLEM ||--o{ ISLEM_TEKRAR_LIMIT : tekrar_siniri
```

## Rollerin başlıca işlemleri

Bu şema rollerin öne çıkan işlemlerini gösterir; tam yetki listesi değildir. Bütün roller kendi kurum ve klinik kapsamıyla sınırlıdır.

```mermaid
flowchart LR
    F[Fatura görevlisi] --> Liste([Yetkili faturaları listele])
    F --> Kontrol([Faturayı kontrol et])
    K[Kodlama uzmanı] --> Duzelt([Kaynağı doğrula ve düzelt])
    K --> Kimlik([Denetimli maske kaldırma])
    G[Gelir tahakkuk sorumlusu] --> Istisna([Gerekçeli istisna tanımla])
    G --> Onay([Simüle e-imza ile onayla])
    Onay --> Gonder([MEDULA simülatörüne gönder])
    G --> Red([Red geri beslemesi gir])
    Y[Yönetici] --> Toplu([Dönem kontrolü ve ilerleme])
    Y --> Rapor([PDF / Excel raporu al])
    KY[Kural yöneticisi] --> Versiyon([Versiyonlu kural güncelle])
    KY --> Sim([Geçmiş dönem simülasyonu])
    D[İç denetçi] --> Iz([Değiştirilemez denetim izini incele])
    D --> Rapor
    S[Sistem yöneticisi] --> Liste
    S --> Versiyon
    S --> Iz
```
