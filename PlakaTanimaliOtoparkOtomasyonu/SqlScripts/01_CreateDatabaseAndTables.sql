SET NOCOUNT ON;
GO

IF DB_ID(N'PlakaTanimaliOtoparkDb') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE [PlakaTanimaliOtoparkDb] COLLATE Turkish_CI_AS;');
END;
GO

USE [PlakaTanimaliOtoparkDb];
GO

IF OBJECT_ID(N'dbo.Kullanicilar', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Kullanicilar
    (
        KullaniciId       INT IDENTITY(1,1) NOT NULL,
        KullaniciAdi      NVARCHAR(50) NOT NULL,
        SifreHash         NVARCHAR(128) NOT NULL,
        AdSoyad           NVARCHAR(100) NOT NULL,
        Rol               NVARCHAR(20) NOT NULL,
        AktifMi           BIT NOT NULL
            CONSTRAINT DF_Kullanicilar_AktifMi DEFAULT (1),
        OlusturmaTarihi   DATETIME2(7) NOT NULL
            CONSTRAINT DF_Kullanicilar_OlusturmaTarihi DEFAULT (SYSDATETIME()),

        CONSTRAINT PK_Kullanicilar
            PRIMARY KEY CLUSTERED (KullaniciId),
        CONSTRAINT UQ_Kullanicilar_KullaniciAdi
            UNIQUE (KullaniciAdi),
        CONSTRAINT CK_Kullanicilar_Rol
            CHECK (Rol IN (N'Admin', N'Personel'))
    );
END;
GO

IF OBJECT_ID(N'dbo.Araclar', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Araclar
    (
        AracId          INT IDENTITY(1,1) NOT NULL,
        Plaka           NVARCHAR(15) NOT NULL,
        Marka           NVARCHAR(50) NULL,
        Model           NVARCHAR(50) NULL,
        Renk            NVARCHAR(30) NULL,
        SahipAdi        NVARCHAR(100) NULL,
        Telefon         NVARCHAR(25) NULL,
        KayitTarihi     DATETIME2(7) NOT NULL
            CONSTRAINT DF_Araclar_KayitTarihi DEFAULT (SYSDATETIME()),

        CONSTRAINT PK_Araclar
            PRIMARY KEY CLUSTERED (AracId),
        CONSTRAINT UQ_Araclar_Plaka
            UNIQUE (Plaka)
    );
END;
GO

IF OBJECT_ID(N'dbo.ParkYerleri', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ParkYerleri
    (
        ParkYeriId      INT IDENTITY(1,1) NOT NULL,
        Kod             NVARCHAR(10) NOT NULL,
        Kat             NVARCHAR(20) NOT NULL,
        Bolge           NVARCHAR(20) NOT NULL,
        IsOccupied      BIT NOT NULL
            CONSTRAINT DF_ParkYerleri_IsOccupied DEFAULT (0),
        AktifMi         BIT NOT NULL
            CONSTRAINT DF_ParkYerleri_AktifMi DEFAULT (1),

        CONSTRAINT PK_ParkYerleri
            PRIMARY KEY CLUSTERED (ParkYeriId),
        CONSTRAINT UQ_ParkYerleri_Kod
            UNIQUE (Kod)
    );
END;
GO

IF OBJECT_ID(N'dbo.Tarifeler', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tarifeler
    (
        TarifeId               INT IDENTITY(1,1) NOT NULL,
        TarifeAdi              NVARCHAR(80) NOT NULL,
        IlkSaatUcreti          DECIMAL(18,2) NOT NULL,
        SonrakiSaatUcreti      DECIMAL(18,2) NOT NULL,
        GunlukMaksimumUcret    DECIMAL(18,2) NOT NULL,
        AktifMi                BIT NOT NULL
            CONSTRAINT DF_Tarifeler_AktifMi DEFAULT (1),
        GuncellemeTarihi       DATETIME2(7) NOT NULL
            CONSTRAINT DF_Tarifeler_GuncellemeTarihi DEFAULT (SYSDATETIME()),

        CONSTRAINT PK_Tarifeler
            PRIMARY KEY CLUSTERED (TarifeId)
    );
END;
GO

IF OBJECT_ID(N'dbo.ParkKayitlari', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ParkKayitlari
    (
        ParkKaydiId          INT IDENTITY(1,1) NOT NULL,
        AracId               INT NOT NULL,
        ParkYeriId           INT NOT NULL,
        GirisZamani          DATETIME2(7) NOT NULL,
        CikisZamani          DATETIME2(7) NULL,
        Status               NVARCHAR(20) NOT NULL,
        TotalFee             DECIMAL(18,2) NULL,
        CreatedByUserId      INT NOT NULL,
        CompletedByUserId    INT NULL,
        OlusturmaTarihi      DATETIME2(7) NOT NULL
            CONSTRAINT DF_ParkKayitlari_OlusturmaTarihi DEFAULT (SYSDATETIME()),

        CONSTRAINT PK_ParkKayitlari
            PRIMARY KEY CLUSTERED (ParkKaydiId),
        CONSTRAINT CK_ParkKayitlari_Status
            CHECK (Status IN (N'Active', N'Completed')),
        CONSTRAINT FK_ParkKayitlari_Araclar
            FOREIGN KEY (AracId) REFERENCES dbo.Araclar (AracId),
        CONSTRAINT FK_ParkKayitlari_ParkYerleri
            FOREIGN KEY (ParkYeriId) REFERENCES dbo.ParkYerleri (ParkYeriId),
        CONSTRAINT FK_ParkKayitlari_CreatedBy
            FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Kullanicilar (KullaniciId),
        CONSTRAINT FK_ParkKayitlari_CompletedBy
            FOREIGN KEY (CompletedByUserId) REFERENCES dbo.Kullanicilar (KullaniciId)
    );
END;
GO

IF OBJECT_ID(N'dbo.Odemeler', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Odemeler
    (
        OdemeId          INT IDENTITY(1,1) NOT NULL,
        ParkKaydiId      INT NOT NULL,
        Tutar            DECIMAL(18,2) NOT NULL,
        OdemeTipi        NVARCHAR(30) NOT NULL,
        OdemeZamani      DATETIME2(7) NOT NULL
            CONSTRAINT DF_Odemeler_OdemeZamani DEFAULT (SYSDATETIME()),
        KullaniciId      INT NOT NULL,

        CONSTRAINT PK_Odemeler
            PRIMARY KEY CLUSTERED (OdemeId),
        CONSTRAINT FK_Odemeler_ParkKayitlari
            FOREIGN KEY (ParkKaydiId) REFERENCES dbo.ParkKayitlari (ParkKaydiId),
        CONSTRAINT FK_Odemeler_Kullanicilar
            FOREIGN KEY (KullaniciId) REFERENCES dbo.Kullanicilar (KullaniciId)
    );
END;
GO

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

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ParkKayitlari')
      AND name = N'IX_ParkKayitlari_GirisZamani'
)
BEGIN
    CREATE INDEX IX_ParkKayitlari_GirisZamani
        ON dbo.ParkKayitlari (GirisZamani);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ParkKayitlari')
      AND name = N'IX_ParkKayitlari_Status_Plaka'
)
BEGIN
    CREATE INDEX IX_ParkKayitlari_Status_Plaka
        ON dbo.ParkKayitlari (Status, AracId)
        INCLUDE (ParkYeriId, GirisZamani, CikisZamani, TotalFee);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Odemeler')
      AND name = N'IX_Odemeler_OdemeZamani'
)
BEGIN
    CREATE INDEX IX_Odemeler_OdemeZamani
        ON dbo.Odemeler (OdemeZamani);
END;
GO
