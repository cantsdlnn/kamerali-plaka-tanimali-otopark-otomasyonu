namespace PlakaTanimaliOtoparkOtomasyonu.Models;

public class Kullanici
{
    public int KullaniciId { get; set; }
    public string KullaniciAdi { get; set; } = string.Empty;
    public string SifreHash { get; set; } = string.Empty;
    public string AdSoyad { get; set; } = string.Empty;
    public string Rol { get; set; } = "Personel";
    public bool AktifMi { get; set; }
    public DateTime OlusturmaTarihi { get; set; }

    public bool AdminMi => Rol.Equals("Admin", StringComparison.OrdinalIgnoreCase);
}
