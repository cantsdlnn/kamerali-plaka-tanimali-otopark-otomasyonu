using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace PlakaTanimaliOtoparkOtomasyonu.Helpers;

/// <summary>
/// Parolalari surumlenmis, kendini aciklayan bir PBKDF2 formatiyla saklar.
/// Eski SHA-256 kayitlari yalnizca basarili giris sonrasinda PBKDF2'ye tasimak
/// icin dogrulanabilir; yeni kayitlar hicbir zaman eski formatta uretilmez.
/// </summary>
public static class ParolaYardimcisi
{
    private const string Algoritma = "PBKDF2-SHA256";
    private const int IterasyonSayisi = 600_000;
    private const int MinimumKabulEdilenIterasyon = 10_000;
    private const int MaksimumKabulEdilenIterasyon = 2_000_000;
    private const int TuzUzunlugu = 16;
    private const int OzetUzunlugu = 32;

    public static string Hashle(string sifre)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sifre);

        var salt = RandomNumberGenerator.GetBytes(TuzUzunlugu);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            sifre,
            salt,
            IterasyonSayisi,
            HashAlgorithmName.SHA256,
            OzetUzunlugu);

        return string.Join(
            '$',
            Algoritma,
            IterasyonSayisi.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public static bool Dogrula(string sifre, string? saklananHash)
    {
        if (string.IsNullOrEmpty(sifre) || string.IsNullOrWhiteSpace(saklananHash))
        {
            return false;
        }

        return Pbkdf2Dogrula(sifre, saklananHash) || LegacySha256Dogrula(sifre, saklananHash);
    }

    public static bool YenidenHashlenmeli(string? saklananHash)
    {
        if (string.IsNullOrWhiteSpace(saklananHash))
        {
            return true;
        }

        if (LegacySha256Mi(saklananHash))
        {
            return true;
        }

        var parcalar = saklananHash.Split('$');
        return parcalar.Length != 4
            || !string.Equals(parcalar[0], Algoritma, StringComparison.Ordinal)
            || !int.TryParse(parcalar[1], NumberStyles.None, CultureInfo.InvariantCulture, out var iterasyon)
            || iterasyon < IterasyonSayisi;
    }

    public static bool GucluParolaMi(string? sifre)
    {
        return !string.IsNullOrWhiteSpace(sifre)
            && sifre.Length >= 12
            && sifre.Any(char.IsLetter)
            && sifre.Any(char.IsDigit);
    }

    private static bool Pbkdf2Dogrula(string sifre, string saklananHash)
    {
        var parcalar = saklananHash.Split('$');
        if (parcalar.Length != 4
            || !string.Equals(parcalar[0], Algoritma, StringComparison.Ordinal)
            || !int.TryParse(parcalar[1], NumberStyles.None, CultureInfo.InvariantCulture, out var iterasyon)
            || iterasyon < MinimumKabulEdilenIterasyon
            || iterasyon > MaksimumKabulEdilenIterasyon)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parcalar[2]);
            var beklenen = Convert.FromBase64String(parcalar[3]);
            if (salt.Length < TuzUzunlugu || beklenen.Length != OzetUzunlugu)
            {
                return false;
            }

            var gercek = Rfc2898DeriveBytes.Pbkdf2(
                sifre,
                salt,
                iterasyon,
                HashAlgorithmName.SHA256,
                beklenen.Length);

            return CryptographicOperations.FixedTimeEquals(gercek, beklenen);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool LegacySha256Dogrula(string sifre, string saklananHash)
    {
        if (!LegacySha256Mi(saklananHash))
        {
            return false;
        }

        var gercek = SHA256.HashData(Encoding.Unicode.GetBytes(sifre));
        var beklenen = Convert.FromHexString(saklananHash);
        return CryptographicOperations.FixedTimeEquals(gercek, beklenen);
    }

    private static bool LegacySha256Mi(string value)
    {
        return value.Length == 64 && value.All(Uri.IsHexDigit);
    }
}
