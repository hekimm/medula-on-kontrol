# Kurumsal rapor şablonu

PDF ve Excel raporları aynı form düzenini kullanır. Siyah hücre sınırları, açık pembe bölüm başlıkları (`#E6B8B7`), dönüşümlü açık gri satırlar ve üst bilgi kutusu kullanılır.

Üst bilgi, projeye ait M+ işaretini, rapor adını, doküman kodunu, düzenleme tarihini ve şablon revizyonunu içerir. PDF'de gerçek sayfa numarası ve toplam sayfa sayısı her sayfada gösterilir. Excel'de sayfa numaraları baskı alt bilgisindedir. Tarihler Türkiye saatine göre düzenlenir. Resmî kurum logosu veya kurum onayı kullanılmaz.

## PDF dönem raporu

`MOK.RP.01`, revizyon 01. A4 dikey sayfada şu bölümler bulunur:

- Özet göstergeler
- Klinik bazlı sonuçlar
- Kural bazlı bulgu dağılımı: en sık ilk 10 kural
- Dönemsel trend: gönderilen ve reddedilen fatura sayıları, red oranı
- Kontrol sonuçlarının değerlendirilmesi

Şablon sayfa uzunluğuna göre büyüyebilir. Devam sayfalarında üst bilgi ve ilgili tablonun başlıkları tekrarlanır. Veri olmayan tablolarda bu durum açıkça yazılır. Tanımsız oranlar sıfır yerine `—` gösterilir. Hedef ve sapma sütunları kullanılmaz.

## Excel çalışma kitabı

İlk sayfa **Dönem özeti**, PDF ile aynı bölüm ve rakamları kullanır. Standart dönem özeti A4 tek sayfaya ayarlanır; uzun listeler birden çok sayfaya devam eder.

İkinci sayfa **Bulgular**, `MOK.LS.01` kodlu ayrıntılı listedir. A3 yatay baskı alanı, tekrarlanan başlıklar, filtreler ve sabit üst satırlar bulunur. Uzun fatura/kalem kimlikleri Excel'in 15 haneli sayı hassasiyetinden etkilenmemesi için metindir. Tutarlar ve tarihler uygun veri türlerinde tutulur. Bulgu metinleri formül olarak çalıştırılmaz.

## Doğrulama

`RaporSablonTests` boş, standart ve 90 klinikli raporları kapsar. Uzun kimliklerin korunması, sayfalama ve dosya içeriği test edilir. Rapor verisi kullanıcının kurum ve klinik kapsamıyla sınırlıdır.
