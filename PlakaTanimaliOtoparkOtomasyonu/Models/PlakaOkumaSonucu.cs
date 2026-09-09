namespace PlakaTanimaliOtoparkOtomasyonu.Models;

public class PlakaOkumaSonucu
{
    public string OkunanMetin { get; set; } = string.Empty;
    public string DuzeltilmisPlaka { get; set; } = string.Empty;
    public decimal GuvenOrani { get; set; }
    public bool BasariliMi { get; set; }
    public string? ResimYolu { get; set; }
    public string IslemTuru { get; set; } = "Kamera";
    public string? Aciklama { get; set; }
    public string PythonSonucu { get; set; } = string.Empty;
    public decimal PythonGuvenOrani { get; set; }
    public string PythonAdaylari { get; set; } = string.Empty;
    public string PythonSecilenAday { get; set; } = string.Empty;
    public string PythonFormatDisiAday { get; set; } = string.Empty;
    public decimal PythonFormatDisiGuvenOrani { get; set; }
    public string PythonReddedilmeSebebi { get; set; } = string.Empty;
    public string PythonMesaji { get; set; } = string.Empty;
    public bool PythonServisiCalisiyorMu { get; set; } = true;
    public string TesseractSonucu { get; set; } = string.Empty;
    public decimal TesseractGuvenOrani { get; set; }
    public string SablonSonucu { get; set; } = string.Empty;
    public decimal SablonGuvenOrani { get; set; }
    public string NihaiSonuc { get; set; } = string.Empty;
    public string? YakinPlakaOnerisi { get; set; }
    public int YakinPlakaMesafesi { get; set; }
    public List<PlakaOkumaAdayi> Adaylar { get; } = new();

    public bool ManuelDuzeltmeGerekli => !BasariliMi || GuvenOrani < 70;
}

public class PlakaOkumaAdayi
{
    public string HamMetin { get; set; } = string.Empty;
    public string TemizMetin { get; set; } = string.Empty;
    public string DuzeltilmisPlaka { get; set; } = string.Empty;
    public decimal GuvenOrani { get; set; }
    public bool GecerliMi { get; set; }
    public int KareNo { get; set; }
    public string OnIslemeTuru { get; set; } = string.Empty;
}
