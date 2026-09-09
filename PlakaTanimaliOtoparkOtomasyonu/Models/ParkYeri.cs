namespace PlakaTanimaliOtoparkOtomasyonu.Models;

public class ParkYeri
{
    public int ParkYeriId { get; set; }
    public string Kod { get; set; } = string.Empty;
    public string Kat { get; set; } = string.Empty;
    public string Bolge { get; set; } = string.Empty;
    public bool IsOccupied { get; set; }
    public bool AktifMi { get; set; }

    public override string ToString() => $"{Kod} - {Kat} / {Bolge}";
}
