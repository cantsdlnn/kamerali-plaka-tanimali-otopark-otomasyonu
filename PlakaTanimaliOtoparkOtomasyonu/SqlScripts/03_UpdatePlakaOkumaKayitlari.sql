USE [PlakaTanimaliOtoparkDb];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.PlakaOkumaKayitlari', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PlakaOkumaKayitlari
    (
        PlakaOkumaKaydiId    INT IDENTITY(1,1) NOT NULL,
        OkunanMetin          NVARCHAR(100) NULL,
        DuzeltilmisPlaka     NVARCHAR(15) NULL,
        OkumaZamani          DATETIME2(7) NOT NULL
            CONSTRAINT DF_PlakaOkumaKayitlari_OkumaZamani DEFAULT (SYSDATETIME()),
        GuvenOrani           DECIMAL(5,2) NOT NULL
            CONSTRAINT DF_PlakaOkumaKayitlari_GuvenOrani DEFAULT (0),
        ResimYolu            NVARCHAR(300) NULL,
        IslemTuru            NVARCHAR(40) NOT NULL,
        BasariliMi           BIT NOT NULL
            CONSTRAINT DF_PlakaOkumaKayitlari_BasariliMi DEFAULT (0),
        Aciklama             NVARCHAR(500) NULL,

        CONSTRAINT PK_PlakaOkumaKayitlari
            PRIMARY KEY CLUSTERED (PlakaOkumaKaydiId)
    );
END;
GO

IF COL_LENGTH(N'dbo.PlakaOkumaKayitlari', N'OkunanMetin') IS NULL
BEGIN
    ALTER TABLE dbo.PlakaOkumaKayitlari
        ADD OkunanMetin NVARCHAR(100) NULL;
END;
GO

IF COL_LENGTH(N'dbo.PlakaOkumaKayitlari', N'DuzeltilmisPlaka') IS NULL
BEGIN
    ALTER TABLE dbo.PlakaOkumaKayitlari
        ADD DuzeltilmisPlaka NVARCHAR(15) NULL;
END;
GO

IF COL_LENGTH(N'dbo.PlakaOkumaKayitlari', N'OkumaZamani') IS NULL
BEGIN
    ALTER TABLE dbo.PlakaOkumaKayitlari
        ADD OkumaZamani DATETIME2(7) NOT NULL
            CONSTRAINT DF_PlakaOkumaKayitlari_OkumaZamani
            DEFAULT (SYSDATETIME()) WITH VALUES;
END;
GO

IF COL_LENGTH(N'dbo.PlakaOkumaKayitlari', N'GuvenOrani') IS NULL
BEGIN
    ALTER TABLE dbo.PlakaOkumaKayitlari
        ADD GuvenOrani DECIMAL(5,2) NOT NULL
            CONSTRAINT DF_PlakaOkumaKayitlari_GuvenOrani
            DEFAULT (0) WITH VALUES;
END;
GO

IF COL_LENGTH(N'dbo.PlakaOkumaKayitlari', N'ResimYolu') IS NULL
BEGIN
    ALTER TABLE dbo.PlakaOkumaKayitlari
        ADD ResimYolu NVARCHAR(300) NULL;
END;
GO

IF COL_LENGTH(N'dbo.PlakaOkumaKayitlari', N'IslemTuru') IS NULL
BEGIN
    EXEC sys.sp_executesql
        N'ALTER TABLE dbo.PlakaOkumaKayitlari
          ADD IslemTuru NVARCHAR(40) NULL;';

    EXEC sys.sp_executesql
        N'UPDATE dbo.PlakaOkumaKayitlari
          SET IslemTuru = N''EskiKayit''
          WHERE IslemTuru IS NULL OR LTRIM(RTRIM(IslemTuru)) = N'''';';

    EXEC sys.sp_executesql
        N'ALTER TABLE dbo.PlakaOkumaKayitlari
          ALTER COLUMN IslemTuru NVARCHAR(40) NOT NULL;';
END;
GO

IF COL_LENGTH(N'dbo.PlakaOkumaKayitlari', N'BasariliMi') IS NULL
BEGIN
    ALTER TABLE dbo.PlakaOkumaKayitlari
        ADD BasariliMi BIT NOT NULL
            CONSTRAINT DF_PlakaOkumaKayitlari_BasariliMi
            DEFAULT (0) WITH VALUES;
END;
GO

IF COL_LENGTH(N'dbo.PlakaOkumaKayitlari', N'Aciklama') IS NULL
BEGIN
    ALTER TABLE dbo.PlakaOkumaKayitlari
        ADD Aciklama NVARCHAR(500) NULL;
END;
GO
