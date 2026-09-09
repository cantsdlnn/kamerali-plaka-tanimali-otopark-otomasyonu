# Python Plate Reader

FastAPI, EasyOCR ve OpenCV kullanan yerel plaka okuma servisidir. C# Windows Forms uygulaması, kırpılmış ROI görüntüsünü `multipart/form-data` ile bu servise gönderir.

## Kurulum

```powershell
py -3.11 -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install --upgrade pip
pip install -r requirements.txt
```

## Çalıştırma

```powershell
uvicorn app:app --reload
```

Servis varsayılan olarak `http://127.0.0.1:8000` adresinde çalışır.

## Uç noktalar

- `GET /health`: Sağlık kontrolü
- `POST /read-plate`: `image` alanında bir görsel alır ve OCR sonucunu JSON olarak döndürür

PowerShell ile örnek istek:

```powershell
curl.exe -X POST "http://127.0.0.1:8000/read-plate" -F "image=@C:\ornek-plaka.png"
```

Örnek başarılı cevap:

```json
{
  "success": true,
  "plate": "34ABC123",
  "confidence": 0.82,
  "method": "EasyOCR",
  "source_method": "adaptive_threshold",
  "candidates": []
}
```

Genel proje kurulumu ve mimari açıklaması için kök dizindeki `README.md` dosyasına bakın.
