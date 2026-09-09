# Gereksinimler

## Amaç ve kapsam

Sistem, küçük/orta ölçekli bir otoparkın yerel bilgisayarında araç giriş-çıkışını plaka üzerinden takip eden bir portföy uygulamasıdır. İnsan operatör kararın son sahibidir; OCR sonucu otomatik ve kesin kimlik doğrulaması sayılmaz.

## Aktörler

- **Personel:** araç giriş/çıkışı, plaka doğrulama ve ödeme işlemlerini yürütür.
- **Yönetici:** personel yetkilerine ek olarak tarife ve yönetim ekranlarını kullanır.
- **Yerel OCR servisi:** kırpılmış görüntüyü işler ve aday plaka sonuçlarını döndürür.
- **SQL Server:** operasyon ve rapor verisini saklar.

## Fonksiyonel gereksinimler

| Kimlik | Gereksinim | Doğrulama yaklaşımı |
|---|---|---|
| FR-01 | Kamera veya dosyadan görüntü alınabilmeli | Manuel donanım testi |
| FR-02 | Seçili ROI alanı birden fazla ön işleme yöntemiyle hazırlanmalı | Görüntü entegrasyon testi |
| FR-03 | OCR adayları Türk plaka biçimine göre temizlenip doğrulanmalı | xUnit + pytest |
| FR-04 | Operatör OCR sonucunu onaylayabilmeli veya düzeltebilmeli | UI kabul testi |
| FR-05 | Araç girişi boş park yeriyle atomik kaydedilmeli | SQL entegrasyon testi |
| FR-06 | Aynı plaka için ikinci aktif kayıt engellenmeli | SQL entegrasyon testi |
| FR-07 | Çıkış ücreti tarife ve günlük üst sınıra göre hesaplanmalı | xUnit |
| FR-08 | Çıkış, ödeme ve park yerinin boşaltılması atomik olmalı | SQL entegrasyon testi |
| FR-09 | Yönetim paneli doluluk ve günlük özet sunmalı | UI + SQL kabul testi |
| FR-10 | İlk kullanıcı sabit parola olmadan güvenli biçimde oluşturulmalı | Kod inceleme + xUnit |

## Güvenlik ve gizlilik gereksinimleri

| Kimlik | Gereksinim |
|---|---|
| SR-01 | Kaynak depoda parola, token, gerçek plaka veya kişisel veri tutulmamalı |
| SR-02 | Yeni parolalar rastgele salt kullanılan yavaş bir parola türetme yöntemiyle saklanmalı |
| SR-03 | SQL girdileri parametrelenmeli ve kritik çoklu işlemler transaction kullanmalı |
| SR-04 | OCR API varsayılan olarak yalnızca loopback istemcilerine açık olmalı |
| SR-05 | Görsel türü, bayt büyüklüğü ve çözünürlüğü sınırlandırılmalı |
| SR-06 | Üretilen plaka görselleri Git geçmişine girmemeli |
| SR-07 | Gerçek kullanımda saklama süresi ve erişim yetkisi kurum tarafından tanımlanmalı |

## Kalite gereksinimleri

- Release derlemesi uyarısız tamamlanmalıdır.
- Saf iş kuralları ve güvenlik doğrulamaları deterministik birim testleriyle kapsanmalıdır.
- Kurulum adımları temiz bir Windows ortamında tekrar edilebilir olmalıdır.
- OCR motoru kullanılamadığında kullanıcı kontrollü yedek akış korunmalıdır.
- Ağ servisi başarısızlıkları masaüstü uygulamasını kontrolsüz biçimde sonlandırmamalıdır.

## Sistem gereksinimleri

- Windows 10/11, x64 önerilir
- .NET 8 Runtime/SDK
- Python 3.11
- SQL Server veya SQL Server Express
- EasyOCR kurulumu ve model indirmesi için ilk kurulumda internet
- Kamera akışı için OpenCV tarafından desteklenen web kamerası

## Kapsam dışı

- Bariyer donanımını doğrudan sürme
- Merkezi çok şubeli bulut mimarisi
- Resmî ödeme sağlayıcısı entegrasyonu
- Özel eğitilmiş plaka lokalizasyon modeli
- Üretim düzeyinde kimlik sağlayıcı veya denetim merkezi
