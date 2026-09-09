USE [PlakaTanimaliOtoparkDb];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -- Bilerek varsayilan kullanici/parola eklenmez. Bos bir veritabaninda ilk
    -- yonetici, uygulama baslatilmadan once ayarlanan OTOPARK_BOOTSTRAP_ADMIN_*
    -- ortam degiskenlerinden uretilir ve parola PBKDF2 ile ozetlenir.

    INSERT INTO dbo.ParkYerleri
        (Kod, Kat, Bolge, IsOccupied, AktifMi)
    SELECT
        kaynak.Kod,
        kaynak.Kat,
        kaynak.Bolge,
        0,
        1
    FROM
    (
        VALUES
            (N'A-01', N'Zemin', N'A Blok'),
            (N'A-02', N'Zemin', N'A Blok'),
            (N'A-03', N'Zemin', N'A Blok'),
            (N'A-04', N'Zemin', N'A Blok'),
            (N'A-05', N'Zemin', N'A Blok'),
            (N'A-06', N'Zemin', N'A Blok'),
            (N'A-07', N'Zemin', N'A Blok'),
            (N'A-08', N'Zemin', N'A Blok'),
            (N'A-09', N'Zemin', N'A Blok'),
            (N'A-10', N'Zemin', N'A Blok'),
            (N'A-11', N'Zemin', N'A Blok'),
            (N'A-12', N'Zemin', N'A Blok'),
            (N'A-13', N'Zemin', N'A Blok'),
            (N'A-14', N'Zemin', N'A Blok'),
            (N'A-15', N'Zemin', N'A Blok'),
            (N'A-16', N'Zemin', N'A Blok'),
            (N'A-17', N'Zemin', N'A Blok'),
            (N'A-18', N'Zemin', N'A Blok'),
            (N'A-19', N'Zemin', N'A Blok'),
            (N'A-20', N'Zemin', N'A Blok')
    ) AS kaynak (Kod, Kat, Bolge)
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.ParkYerleri AS mevcut
        WHERE mevcut.Kod = kaynak.Kod
    );

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.Tarifeler
        WHERE AktifMi = 1
    )
    BEGIN
        INSERT INTO dbo.Tarifeler
            (TarifeAdi, IlkSaatUcreti, SonrakiSaatUcreti, GunlukMaksimumUcret, AktifMi)
        VALUES
            (N'Standart Tarife', 50.00, 25.00, 250.00, 1);
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;
GO
