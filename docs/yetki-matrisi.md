# Rol ve erişim matrisi

Her işlemde oturumun kullanıcı/kurum kimliği kullanılır. Klinik kapsamı `KULLANICI_KLINIK_YETKI` üzerinden repository sorgusunda doğrulanır. Kullanıcı aktifliği ve roller her HTTP isteğinde veritabanından yenilenir. Sistem yöneticisi de kurum ve klinik kapsamını aşamaz.

| İşlem | Fatura Görevlisi | Kodlama Uzmanı | Gelir Tahakkuk Sorumlusu | Kural Yöneticisi | Yönetici | İç Denetçi | Sistem Yöneticisi |
|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| Panel/fatura görüntüleme | Var | Var | Var | Var | Var | Var | Var |
| Tekil kontrol | Var | Var | Var | Yok | Var | Yok | Var |
| Kalem/başvuru düzeltme | Var | Var | Var | Yok | Yok | Yok | Var |
| İstisna tanımlama | Yok | Yok | Var | Yok | Var | Yok | Var |
| E-imza simülasyonu ile onay | Yok | Yok | Var | Yok | Var | Yok | Var |
| Simülatöre gönderim | Yok | Yok | Var | Yok | Var | Yok | Var |
| Dönem kontrolü | Yok | Yok | Var | Yok | Var | Yok | Var |
| Kural versiyonu/parametre/simülasyon/kalibrasyon | Yok | Yok | Yok | Var | Yok | Yok | Var |
| Red analizi ve rapor görüntüleme | Yok | Yok | Var | Yok | Var | Var | Var |
| Red kaydı girişi | Yok | Yok | Var | Yok | Var | Yok | Var |
| PDF/Excel dışa aktarma | Yok | Yok | Var | Yok | Var | Var | Var |
| Denetim izi sorgulama | Yok | Yok | Yok | Yok | Yok | Var | Var |
| Hasta maskesini kaldırma | Yok | Var | Yok | Yok | Yok | Yok | Var |

Seed hesapları sırasıyla `fatura`, `kodlama`, `gelir`, `kural`, `yonetici`, `denetci`, `admin` adlarını kullanır. `fatura` ve `kodlama` yalnızca DAHILIYE kliniğine yetkilidir. Diğer beş hesap test kurumunun beş kliniğine atanır. `digerkurum` ikinci kurumu test eder. Kurulumda rastgele üretilen başlangıç parolası kod deposuna alınmaz.

`DENETIM_IZI` için UPDATE ve DELETE, satır bulunmasa bile statement trigger ile reddedilir. Veritabanı sahibinin trigger kaldırabilmesi Oracle yönetici yetkisinin doğal sonucudur; uygulama yetkisi değildir. Compose kurulumunda şema sahibi `MEDULA` ve çalışma hesabı `MEDULA_RUN` ayrıdır. Çalışma hesabına denetim izi için yalnızca SELECT/INSERT, diğer uygulama tabloları için gereken CRUD yetkileri verilir; sahibin trigger nesnesini değiştiremez. Yerel geliştirmede şema sahibi bağlantısı kullanılır. LDAP entegrasyonu için yalnızca `ILdapAuthenticator` sözleşmesi vardır; gerçek bir dizine bağlantı kurulmaz.
