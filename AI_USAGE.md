# Yapay zekâ kullanım beyanı

Bu projeyi geliştirirken üretken yapay zekâ araçlarından yararlandım. Bunu saklanması gereken bir ayrıntı değil, doğru kullanıldığında mühendislik sürecini güçlendiren bir araç olarak görüyorum.

## Nerede kullandım?

- Alternatif mimari ve entegrasyon seçeneklerini araştırırken
- C# ile Python arasındaki HTTP sözleşmesini gözden geçirirken
- OCR ön işleme ve plaka doğrulama senaryoları üretirken
- Güvenlik tehditlerini ve hata durumlarını sistematik biçimde tararken
- Birim testlerindeki sınır değerlerini genişletirken
- Teknik dokümantasyonu daha anlaşılır ve tutarlı hâle getirirken

## Sorumluluğu nasıl korudum?

AI çıktısını nihai gerçek veya hazır çözüm olarak kabul etmedim. Önerileri mevcut kodla karşılaştırdım, anlamadığım değişikliği uygulamadım, bağımlılıkları doğruladım ve sonucu derleme/testlerle kontrol ettim. Bu repodaki mimari tercihlerin, veri akışının ve güvenlik sınırlarının sorumluluğu bana aittir.

## Bu projedeki “AI” ne anlama geliyor?

Plaka okuma tarafında EasyOCR gibi önceden eğitilmiş bir OCR motoru kullanılmaktadır. Bu repo özel bir sinir ağı eğitildiği veya özgün bir temel model geliştirildiği iddiasında değildir. Benim katkım; hazır bileşenleri yerel bir servis içinde bir araya getirmek, görüntü ön işleme seçeneklerini uygulamak, sonuçları Türk plaka kurallarıyla değerlendirmek ve masaüstü iş akışına güvenli biçimde bağlamaktır.

## Doğrulama kaydı

9 Eylül 2026 tarihinde:

- .NET çözümü Release modunda 0 uyarı ve 0 hatayla derlendi.
- 21 xUnit testi geçti.
- Python kaynakları derlendi ve 27 pytest testi geçti.
- Gerçek kamera, SQL Server ve EasyOCR modeliyle uçtan uca otomasyonun hâlen ayrı bir çalışma olduğu açıkça belgelendi.
