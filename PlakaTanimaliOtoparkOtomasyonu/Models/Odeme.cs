namespace PlakaTanimaliOtoparkOtomasyonu.Models;

public class Odeme
{
    public int OdemeId { get; set; }
    public int ParkKaydiId { get; set; }
    public decimal Tutar { get; set; }
    public string OdemeTipi { get; set; } = "Nakit";
    public DateTime OdemeZamani { get; set; }
    public int KullaniciId { get; set; }
}
