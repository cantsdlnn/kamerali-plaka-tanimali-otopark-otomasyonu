# Yerel doğrulama raporu

- Tarih: 2026-09-09
- Ortam: Windows 11, .NET SDK 8, Python 3.11
- Veri: doğrulama kuralları için sentetik plaka dizeleri; gerçek plaka görseli yok

| Kontrol | Sonuç |
|---|---|
| `dotnet test -c Release` | 21/21 xUnit testi başarılı |
| .NET Release derlemesi | 0 uyarı, 0 hata |
| `pytest tests -q` | 27/27 pytest vakası başarılı |
| `ruff check` / `ruff format --check` | Başarılı |
| `dotnet list package --vulnerable --include-transitive` | Bilinen NuGet açığı bulunmadı |
| `pip-audit` (sabitlenmiş üst düzey runtime paketleri) | Bilinen açık bulunmadı |

Kapsam; Türk plaka biçimi/karakter düzeltme, ücret hesabı, PBKDF2 ve eski hash yükseltme davranışı, MIME türü, yükleme boyutu ve yalnızca loopback erişim kurallarını içerir.

SQL Server transaction'ları, fiziksel kamera akışı ve EasyOCR modelinin gerçek görüntüler üzerindeki doğruluğu otomatik uçtan uca teste dahil değildir. Sonuçlar üretim uygunluğu veya OCR başarı oranı iddiası değildir.
