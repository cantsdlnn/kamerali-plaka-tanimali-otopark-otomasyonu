namespace PlakaTanimaliOtoparkOtomasyonu.Models;

public class Arac
{
    public int AracId { get; set; }
    public string Plaka { get; set; } = string.Empty;
    public string? Marka { get; set; }
    public string? Model { get; set; }
    public string? Renk { get; set; }
    public string? SahipAdi { get; set; }
    public string? Telefon { get; set; }
    public DateTime KayitTarihi { get; set; }
}
