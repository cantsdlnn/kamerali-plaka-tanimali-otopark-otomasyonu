namespace PlakaTanimaliOtoparkOtomasyonu.Models;

public class Tarife
{
    public int TarifeId { get; set; }
    public string TarifeAdi { get; set; } = string.Empty;
    public decimal IlkSaatUcreti { get; set; }
    public decimal SonrakiSaatUcreti { get; set; }
    public decimal GunlukMaksimumUcret { get; set; }
    public bool AktifMi { get; set; }
    public DateTime GuncellemeTarihi { get; set; }
}
