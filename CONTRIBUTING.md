# Katkı rehberi

Katkılar küçük, doğrulanabilir ve dokümante edilmiş değişiklikler hâlinde yapılmalıdır.

## Geliştirme ortamı

1. Windows üzerinde .NET 8 SDK, Python 3.11 ve isteğe bağlı SQL Server kurun.
2. `.NET` bağımlılıklarını kilitli dosyalarla geri yükleyin:

   ```powershell
   dotnet restore .\PlakaTanimaliOtoparkOtomasyonu.sln --locked-mode
   ```

3. Python test ortamını hazırlayın:

   ```powershell
   cd PythonPlateReader
   py -3.11 -m venv .venv
   .\.venv\Scripts\Activate.ps1
   pip install -r requirements-dev.txt
   ```

Tam OCR servisini çalıştıracaksanız ayrıca `requirements.txt` dosyasını kurun. Bu kurulum PyTorch nedeniyle büyüktür.

## Değişiklik kuralları

- Gerçek plaka, kişi adı, telefon, veritabanı veya kamera görüntüsü commit etmeyin.
- Yeni davranış için birim testi ekleyin veya neden eklenemediğini pull request açıklamasında belirtin.
- SQL sorgularında kullanıcı girdisini string birleştirmeyle kullanmayın.
- OCR servisinin local-only varsayımını zayıflatan değişikliklerde tehdit modelini güncelleyin.
- AI desteği kullanıldıysa sonuçların nasıl doğrulandığını şeffaf biçimde açıklayın.
- Büyük model/binary dosyalarını gerekçe ve lisans kaydı olmadan eklemeyin.

## Göndermeden önce

```powershell
dotnet build .\PlakaTanimaliOtoparkOtomasyonu.sln -c Release
dotnet test .\PlakaTanimaliOtoparkOtomasyonu.sln -c Release --no-build

cd PythonPlateReader
python -m compileall -q app.py core.py tests
python -m pytest tests -q
```

README, changelog ve ilgili ADR/dokümanları davranış değişikliğiyle birlikte güncelleyin.
