# ADR-002: OCR hizmetini varsayılan olarak loopback sınırında tutmak

- Durum: Kabul edildi
- Tarih: 2026-09-09

## Bağlam

Python OCR servisi kimlik doğrulama sunmuyor ve işlediği plaka görselleri hassas olabilir. Masaüstü istemcisiyle aynı makinede çalışması amaçlanıyor.

## Karar

Servis yalnızca loopback istemcilerini kabul edecek. Uzak erişim varsayılan olarak kapalı olacak ve yalnızca `PLATE_READER_ALLOW_REMOTE` ile bilinçli biçimde açılabilecek. Yüklenen veri MIME türü, bayt boyutu ve çözünürlük sınırından geçmeden OCR modeline verilmeyecek.

## Sonuçlar

Yanlışlıkla internete açılma riski azalır ve kaynak tüketimi sınırlandırılır. Ortam değişkeniyle uzaktan erişim açılması tek başına üretim güvenliği sağlamaz; bu durumda TLS, kimlik doğrulama, rate limit, ağ segmentasyonu ve merkezi gözlem eklenmelidir.
