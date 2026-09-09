# Güvenlik politikası

## Desteklenen kapsam

Bu depo eğitim ve portföy amaçlıdır; yayımlanmış üretim sürümü veya geriye dönük güvenlik desteği taahhüdü yoktur. Yine de güvenlik bildirimleri ciddiyetle değerlendirilir.

## Açık bildirme

Bir güvenlik sorunu bulursanız gerçek plaka, kişi, bağlantı bilgisi veya çalışan istismar kodunu herkese açık issue içine koymayın. Depoda GitHub Private Vulnerability Reporting etkinse özel güvenlik bildirimi açın; değilse yalnızca hassas olmayan bir iletişim talebi issue'su oluşturun.

Bildirimde mümkünse etkilenen bileşeni, yeniden üretim koşullarını, olası etkiyi ve önerilen çözümü belirtin. Gerçek kişisel veri kullanmayın.

## Güvenli varsayımlar

- Python OCR servisi `127.0.0.1` üzerinde çalıştırılır ve varsayılan olarak loopback dışı istemcileri reddeder.
- Servis kimlik doğrulama sağlamaz; internete veya ortak ağa doğrudan açılmamalıdır.
- Görsel yüklemeleri MIME türü, bayt boyutu ve çözünürlük sınırından geçirilir.
- İlk yönetici için repoda varsayılan parola yoktur; bilgiler süreç ortam değişkenlerinden alınır.
- Yeni parolalar rastgele salt ve PBKDF2-HMAC-SHA256 ile saklanır.
- SQL bağlantısı `OTOPARK_DB_CONNECTION_STRING` üzerinden kaynak kod dışında tutulabilir.
- SQL komutları parametrelidir; giriş ve çıkış işlemleri transaction kullanır.

## Bilinen güvenlik sınırları

- Uygulama çalışma sırasında plaka görüntüleri ve OCR kayıtları üretebilir. Operatör saklama süresi, erişim kontrolü ve güvenli silme politikasından sorumludur.
- Masaüstü istemcisindeki rol denetimleri merkezi bir kimlik sağlayıcının yerine geçmez.
- Eski 64 karakterlik SHA-256 parola kayıtları geçiş amacıyla doğrulanabilir. Başarılı girişte PBKDF2'ye otomatik yükseltilir; bilinen varsayılan hesaplar ayrıca kaldırılmalıdır.
- OCR sonucu kesin kimlik doğrulaması değildir ve manuel onay gerektirebilir.

Ayrıntılı risk analizi için [docs/threat-model.md](docs/threat-model.md) dosyasına bakın.
