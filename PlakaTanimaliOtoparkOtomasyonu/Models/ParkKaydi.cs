namespace PlakaTanimaliOtoparkOtomasyonu.Models;

public class ParkKaydi
{
    public int ParkKaydiId { get; set; }
    public int AracId { get; set; }
    public int ParkYeriId { get; set; }
    public string Plaka { get; set; } = string.Empty;
    public string ParkYeriKod { get; set; } = string.Empty;
    public DateTime GirisZamani { get; set; }
    public DateTime? CikisZamani { get; set; }
    public string Status { get; set; } = "Active";
    public decimal? TotalFee { get; set; }
    public int CreatedByUserId { get; set; }
    public int? CompletedByUserId { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
}
