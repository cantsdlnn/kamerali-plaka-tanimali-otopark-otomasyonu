namespace PlakaTanimaliOtoparkOtomasyonu.Models;

public class CikisHesapSonucu
{
    public ParkKaydi? ParkKaydi { get; set; }
    public TimeSpan Sure { get; set; }
    public int UcretlendirilenSaat { get; set; }
    public decimal Ucret { get; set; }
}
