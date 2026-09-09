# Değişiklik günlüğü

Bu proje [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) biçimini izler.

## [Unreleased]

### Added

- Plaka doğrulama, ücret hesaplama ve parola güvenliği için 21 xUnit testi
- Python çekirdek doğrulamaları için 27 pytest testi
- Windows .NET ve Python doğrulama işlerinden oluşan GitHub Actions iş akışı
- Dependabot yapılandırması
- Güvenlik politikası, tehdit modeli, gereksinimler, mimari açıklaması ve ADR'ler
- Açık AI kullanım beyanı

### Changed

- Parola saklama yöntemi PBKDF2-HMAC-SHA256 ve rastgele salt kullanacak şekilde yükseltildi
- Eski SHA-256 kayıtları başarılı girişte otomatik yeniden hash'leniyor
- İlk yönetici hesabı varsayılan parola yerine ortam değişkenlerinden oluşturuluyor
- SQL bağlantı cümlesi için ortam değişkeni önceliği eklendi
- OCR servisi loopback varsayımı ve kontrollü uzaktan erişim seçeneğiyle sınırlandı
- Python OpenCV bağımlılığı yalnızca headless paket kullanacak şekilde düzeltildi

### Security

- Görsel MIME türü, yükleme boyutu ve piksel sınırı doğrulamaları eklendi
- Çıkış plaka görüntüleri `.gitignore` kapsamına alındı
- SQL seed betiğindeki varsayılan demo hesapları kaldırıldı
