using PlakaTanimaliOtoparkOtomasyonu.Models;

namespace PlakaTanimaliOtoparkOtomasyonu.DataAccess;

public class PlakaOkumaKaydiRepository : BaseRepository
{
    public void Ekle(PlakaOkumaKaydi kayit)
    {
        const string sql = """
            INSERT INTO dbo.PlakaOkumaKayitlari
                (OkunanMetin, DuzeltilmisPlaka, OkumaZamani, GuvenOrani, ResimYolu, IslemTuru, BasariliMi, Aciklama)
            VALUES
                (@OkunanMetin, @DuzeltilmisPlaka, SYSDATETIME(), @GuvenOrani, @ResimYolu, @IslemTuru, @BasariliMi, @Aciklama);
            """;

        KomutCalistir(
            sql,
            Parametre("@OkunanMetin", kayit.OkunanMetin),
            Parametre("@DuzeltilmisPlaka", kayit.DuzeltilmisPlaka),
            Parametre("@GuvenOrani", kayit.GuvenOrani),
            Parametre("@ResimYolu", kayit.ResimYolu),
            Parametre("@IslemTuru", kayit.IslemTuru),
            Parametre("@BasariliMi", kayit.BasariliMi),
            Parametre("@Aciklama", kayit.Aciklama));
    }
}
