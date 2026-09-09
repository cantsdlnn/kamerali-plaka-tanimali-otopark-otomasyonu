namespace PlakaTanimaliOtoparkOtomasyonu.Models;

public class DashboardOzet
{
    public int ToplamKapasite { get; set; }
    public int DoluYer { get; set; }
    public int BosYer => Math.Max(ToplamKapasite - DoluYer, 0);
    public int GunlukGiris { get; set; }
    public int GunlukCikis { get; set; }
    public decimal GunlukKazanc { get; set; }
}
