using System.Drawing.Imaging;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using PlakaTanimaliOtoparkOtomasyonu.Helpers;

namespace PlakaTanimaliOtoparkOtomasyonu.Services;

public class PythonPlakaOkumaServisi
{
    public const string ServisCalismiyorMesaji =
        "Python plaka okuma servisi çalışmıyor. Lütfen PythonPlateReader klasöründe uvicorn app:app --reload komutunu başlatın.";

    private static readonly Uri Endpoint = new("http://127.0.0.1:8000/read-plate");
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(45)
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<PythonPlakaOkumaSonucu> OkuAsync(Image roiGoruntusu, CancellationToken cancellationToken = default)
    {
        var imagePath = Path.Combine(Path.GetTempPath(), $"plaka_python_{Guid.NewGuid():N}.png");

        try
        {
            using (var kaydedilecek = new Bitmap(roiGoruntusu))
            {
                kaydedilecek.Save(imagePath, ImageFormat.Png);
            }

            await using var stream = File.OpenRead(imagePath);
            using var content = new MultipartFormDataContent();
            using var imageContent = new StreamContent(stream);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(imageContent, "image", Path.GetFileName(imagePath));

            using var response = await HttpClient.PostAsync(Endpoint, content, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return PythonPlakaOkumaSonucu.Basarisiz($"Python OCR HTTP hatasi: {(int)response.StatusCode} {response.ReasonPhrase}");
            }

            var sonuc = JsonSerializer.Deserialize<PythonPlakaOkumaSonucu>(json, JsonOptions)
                        ?? PythonPlakaOkumaSonucu.Basarisiz("Python OCR bos JSON dondu.");

            sonuc.RawJson = json;
            sonuc.ServisCalisiyorMu = true;
            sonuc.Plate = PlakaDogrulamaYardimcisi.KarakterHatalariniDuzelt(sonuc.Plate);
            foreach (var candidate in sonuc.Candidates)
            {
                candidate.Text = PlakaDogrulamaYardimcisi.Temizle(candidate.Text);
                candidate.Plate = PlakaDogrulamaYardimcisi.KarakterHatalariniDuzelt(
                    string.IsNullOrWhiteSpace(candidate.Plate) ? candidate.Text : candidate.Plate);
                candidate.Valid = PlakaDogrulamaYardimcisi.TurkPlakasiMi(candidate.Plate);
            }

            sonuc.Success = sonuc.Success && PlakaDogrulamaYardimcisi.TurkPlakasiMi(sonuc.Plate);
            return sonuc;
        }
        catch (HttpRequestException)
        {
            return PythonPlakaOkumaSonucu.ServisKapali();
        }
        catch (TaskCanceledException)
        {
            return PythonPlakaOkumaSonucu.ServisKapali();
        }
        catch (Exception ex)
        {
            return PythonPlakaOkumaSonucu.Basarisiz($"Python OCR hatasi: {ex.Message}");
        }
        finally
        {
            try
            {
                if (File.Exists(imagePath))
                {
                    File.Delete(imagePath);
                }
            }
            catch
            {
                // Gecici ROI dosyasi silinemezse okuma akisini durdurmayiz.
            }
        }
    }
}

public class PythonPlakaOkumaSonucu
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("plate")]
    public string Plate { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public decimal Confidence { get; set; }

    [JsonPropertyName("method")]
    public string Method { get; set; } = "EasyOCR";

    [JsonPropertyName("source_method")]
    public string SourceMethod { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("candidates")]
    public List<PythonPlakaAdayi> Candidates { get; set; } = new();

    [JsonIgnore]
    public bool ServisCalisiyorMu { get; set; } = true;

    [JsonIgnore]
    public string RawJson { get; set; } = string.Empty;

    public static PythonPlakaOkumaSonucu ServisKapali()
    {
        return new PythonPlakaOkumaSonucu
        {
            Success = false,
            ServisCalisiyorMu = false,
            Message = PythonPlakaOkumaServisi.ServisCalismiyorMesaji
        };
    }

    public static PythonPlakaOkumaSonucu Basarisiz(string message)
    {
        return new PythonPlakaOkumaSonucu
        {
            Success = false,
            Message = message
        };
    }
}

public class PythonPlakaAdayi
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("plate")]
    public string Plate { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public decimal Confidence { get; set; }

    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("valid")]
    public bool Valid { get; set; }
}
