namespace PlakaTanimaliOtoparkOtomasyonu.Models;

public class PlakaOkumaKaydi
{
    public int PlakaOkumaKaydiId { get; set; }
    public string? OkunanMetin { get; set; }
    public string? DuzeltilmisPlaka { get; set; }
    public DateTime OkumaZamani { get; set; }
    public decimal GuvenOrani { get; set; }
    public string? ResimYolu { get; set; }
    public string IslemTuru { get; set; } = "Kamera";
    public bool BasariliMi { get; set; }
    public string? Aciklama { get; set; }
}
