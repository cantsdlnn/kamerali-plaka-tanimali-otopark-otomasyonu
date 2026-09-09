using System.Drawing.Imaging;
using Microsoft.Data.SqlClient;
using PlakaTanimaliOtoparkOtomasyonu.DataAccess;
using PlakaTanimaliOtoparkOtomasyonu.Helpers;
using PlakaTanimaliOtoparkOtomasyonu.Models;
using PlakaTanimaliOtoparkOtomasyonu.Services;

namespace PlakaTanimaliOtoparkOtomasyonu.Forms;

public class FrmAracGiris : Form
{
    private readonly Kullanici _aktifKullanici;
    private readonly ParkYeriRepository _parkYeriRepository = new();
    private readonly OtoparkServisi _otoparkServisi = new();
    private readonly KameraServisi _kameraServisi = new();
    private readonly PlakaOkumaServisi _plakaOkumaServisi = new();
    private readonly PlakaOnIslemeServisi _plakaOnIslemeServisi = new();

    private readonly PictureBox _picKamera = new();
    private readonly PictureBox _picRoiOnizleme = new();
    private readonly TextBox _txtPlaka = TemaYardimcisi.TextBoxHazirla("Plaka");
    private readonly ComboBox _cmbParkYerleri = TemaYardimcisi.ComboHazirla();
    private readonly TextBox _lblOcr = UIHelper.DurumKutusu("OCR sonucu bekleniyor.");
    private Image? _sonFoto;
    private string? _sonFotoYolu;
    private PlakaOkumaSonucu? _sonOkumaSonucu;
    private bool _roiSecimAktif;
    private bool _roiSurukleniyor;
    private PointF _roiBaslangic;
    private RectangleF? _geciciRoiOrani;

    public FrmAracGiris(Kullanici aktifKullanici)
    {
        _aktifKullanici = aktifKullanici;
        Text = "Arac Giris";
        BackColor = TemaYardimcisi.ArkaPlan;
        ForeColor = TemaYardimcisi.Metin;
        Font = new Font("Segoe UI", 10F);
        AutoScaleMode = AutoScaleMode.Font;
        MinimumSize = new Size(1100, 720);

        Controls.Add(EkranOlustur());
        Load += (_, _) => ParkYerleriniYukle();
        FormClosed += (_, _) => KaynaklariTemizle();
    }

    private Control EkranOlustur()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(8),
            BackColor = TemaYardimcisi.ArkaPlan
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));

        var kameraKart = TemaYardimcisi.Kart();
        kameraKart.Dock = DockStyle.Fill;

        var kameraBaslik = TemaYardimcisi.Baslik("Kamera", 20);
        kameraBaslik.Dock = DockStyle.Top;

        _picKamera.Dock = DockStyle.Fill;
        _picKamera.BackColor = Color.Black;
        _picKamera.SizeMode = PictureBoxSizeMode.Zoom;
        _picKamera.BorderStyle = BorderStyle.FixedSingle;
        _picKamera.Paint += (_, e) => KameraServisi.PlakaCercevesiCiz(_picKamera, e.Graphics, _geciciRoiOrani, _roiSecimAktif);
        _picKamera.MouseDown += PicKamera_MouseDown;
        _picKamera.MouseMove += PicKamera_MouseMove;
        _picKamera.MouseUp += PicKamera_MouseUp;

        var kameraButonlari = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 112,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(2),
            BackColor = TemaYardimcisi.Panel
        };

        var btnKameraBaslat = TemaYardimcisi.Buton("Kamera Baslat");
        btnKameraBaslat.Width = 138;
        btnKameraBaslat.Click += (_, _) => KameraBaslat();

        var btnKameraDurdur = TemaYardimcisi.Buton("Durdur", TemaYardimcisi.Kirmizi);
        btnKameraDurdur.Width = 108;
        btnKameraDurdur.Click += (_, _) => _kameraServisi.Durdur();

        var btnFotoCek = TemaYardimcisi.Buton("Foto Cek", TemaYardimcisi.Yesil);
        btnFotoCek.Width = 116;
        btnFotoCek.Click += (_, _) => FotoCek();

        var btnPlakaOku = TemaYardimcisi.Buton("Plaka Oku", TemaYardimcisi.Sari);
        btnPlakaOku.Width = 120;
        btnPlakaOku.Click += async (sender, _) => await PlakaOkuAsync(sender as Control);

        var btnResimdenOku = TemaYardimcisi.Buton("Resimden Oku", TemaYardimcisi.PanelKoyu);
        btnResimdenOku.Width = 136;
        btnResimdenOku.Click += (_, _) => ResimdenPlakaOku();

        var btnRoiAyarla = TemaYardimcisi.Buton("ROI Ayarla", TemaYardimcisi.PanelKoyu);
        btnRoiAyarla.Width = 120;
        btnRoiAyarla.Click += (_, _) => RoiAyarlaBaslat();

        var btnRoiKaydet = TemaYardimcisi.Buton("ROI Kaydet", TemaYardimcisi.Yesil);
        btnRoiKaydet.Width = 120;
        btnRoiKaydet.Click += (_, _) => RoiKaydet();

        var btnRoiSifirla = TemaYardimcisi.Buton("ROI Sifirla", TemaYardimcisi.Kirmizi);
        btnRoiSifirla.Width = 120;
        btnRoiSifirla.Click += (_, _) => RoiSifirla();

        var btnOnizle = TemaYardimcisi.Buton("Kirpilani Onizle", TemaYardimcisi.PanelKoyu);
        btnOnizle.Width = 150;
        btnOnizle.Click += (_, _) => KirpilanAlaniOnizle();

        kameraButonlari.Controls.AddRange(new Control[]
        {
            btnKameraBaslat, btnKameraDurdur, btnFotoCek, btnPlakaOku, btnResimdenOku,
            btnRoiAyarla, btnRoiKaydet, btnRoiSifirla, btnOnizle
        });
        kameraKart.Controls.Add(_picKamera);
        kameraKart.Controls.Add(kameraButonlari);
        kameraKart.Controls.Add(kameraBaslik);

        var formKart = TemaYardimcisi.Kart();
        formKart.Dock = DockStyle.Fill;

        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = TemaYardimcisi.Panel
        };

        stack.Controls.Add(TemaYardimcisi.Baslik("Arac Giris", 20));
        stack.Controls.Add(TemaYardimcisi.Aciklama("Plaka otomatik okunabilir veya manuel duzeltilebilir."));
        stack.Controls.Add(Etiket("Plaka"));
        _txtPlaka.Width = 360;
        _txtPlaka.CharacterCasing = CharacterCasing.Upper;
        stack.Controls.Add(_txtPlaka);
        _lblOcr.Width = 360;
        _lblOcr.Height = 188;
        stack.Controls.Add(_lblOcr);
        stack.Controls.Add(Etiket("Kirpilan Alan Onizleme"));
        _picRoiOnizleme.Width = 360;
        _picRoiOnizleme.Height = 120;
        _picRoiOnizleme.BackColor = Color.Black;
        _picRoiOnizleme.SizeMode = PictureBoxSizeMode.Zoom;
        _picRoiOnizleme.BorderStyle = BorderStyle.FixedSingle;
        stack.Controls.Add(_picRoiOnizleme);
        stack.Controls.Add(Etiket("Bos Park Yeri"));
        _cmbParkYerleri.Width = 360;
        stack.Controls.Add(_cmbParkYerleri);

        var btnYenile = TemaYardimcisi.Buton("Bos Yerleri Yenile", TemaYardimcisi.PanelKoyu);
        btnYenile.Width = 360;
        btnYenile.Click += (_, _) => ParkYerleriniYukle();
        stack.Controls.Add(btnYenile);

        var btnKaydet = TemaYardimcisi.Buton("Girisi Kaydet", TemaYardimcisi.Yesil);
        btnKaydet.Width = 360;
        btnKaydet.Click += (_, _) => GirisKaydet();
        stack.Controls.Add(btnKaydet);

        formKart.Controls.Add(stack);
        layout.Controls.Add(kameraKart, 0, 0);
        layout.Controls.Add(formKart, 1, 0);
        return layout;
    }

    private void KameraBaslat()
    {
        try
        {
            _kameraServisi.Baslat(_picKamera);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Kamera", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void FotoCek()
    {
        try
        {
            _sonFoto?.Dispose();
            _sonFoto = _kameraServisi.FotoCek();
            if (_sonFoto is null)
            {
                MessageBox.Show("Once kamerayi baslatin veya goruntu geldiginden emin olun.", "Foto Cek", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _sonFotoYolu = FotoKaydet(_sonFoto);
            DurumYaz("Foto cekildi. Plaka okuma hazir.", TemaYardimcisi.IkincilMetin);
            KirpilanAlaniOnizle();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Foto cekilemedi: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task PlakaOkuAsync(Control? tetikleyenButon = null)
    {
        try
        {
            if (tetikleyenButon is not null)
            {
                tetikleyenButon.Enabled = false;
            }

            DurumYaz("Plaka okunuyor...\r\n2 saniye boyunca 15 kare yakalaniyor.", TemaYardimcisi.IkincilMetin);
            PlakaOkumaSonucu? sonuc = null;
            if (_kameraServisi.Calisiyor)
            {
                sonuc = await _plakaOkumaServisi.KameradanOkuAsync(_kameraServisi, "AracGirisKamera");
            }
            else if (_sonFoto is not null)
            {
                sonuc = await _plakaOkumaServisi.ResimdenOkuAsync(_sonFoto, "AracGirisFoto", _sonFotoYolu);
            }

            if (sonuc is null)
            {
                MessageBox.Show("Once kamerayi baslatin veya Resimden Oku butonuyla plaka fotografi secin.",
                    "Plaka Okuma", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DurumYaz("OCR icin kamera veya resim bekleniyor.", TemaYardimcisi.Sari);
                return;
            }

            SonucuFormaYaz(sonuc);
        }
        catch (Exception ex)
        {
            DurumYaz("OCR basarisiz.", TemaYardimcisi.Kirmizi);
            MessageBox.Show($"Plaka okunamadi: {ex.Message}", "OCR", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (tetikleyenButon is not null)
            {
                tetikleyenButon.Enabled = true;
            }
        }
    }

    private async void ResimdenPlakaOku()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Plaka fotografi sec",
            Filter = "Resim Dosyalari|*.jpg;*.jpeg;*.png;*.bmp|Tum Dosyalar|*.*"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            _kameraServisi.Durdur();
            using var secilen = Image.FromFile(dialog.FileName);
            _sonFoto?.Dispose();
            _sonFoto = new Bitmap(secilen);
            _sonFotoYolu = dialog.FileName;

            var eski = _picKamera.Image;
            _picKamera.Image = new Bitmap(_sonFoto);
            eski?.Dispose();
            _picKamera.Invalidate();
            KirpilanAlaniOnizle();

            DurumYaz("Secilen resim OCR ile okunuyor...", TemaYardimcisi.IkincilMetin);
            var sonuc = await _plakaOkumaServisi.ResimdenOkuAsync(_sonFoto, "AracGirisResim", dialog.FileName);
            SonucuFormaYaz(sonuc);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Resim okunamadi: {ex.Message}", "Resimden Plaka Oku", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SonucuFormaYaz(PlakaOkumaSonucu sonuc)
    {
        _sonOkumaSonucu = sonuc;
        PythonServisiUyarisiGoster(sonuc);
        var debugMetni = OcrDebugMetni(sonuc);

        if (PythonFormatDisiAdayiniSor(sonuc, debugMetni))
        {
            return;
        }

        if (YakinPlakaOnerisiniSor(sonuc))
        {
            _txtPlaka.Text = sonuc.DuzeltilmisPlaka;
            DurumYaz($"{debugMetni}\r\nOneri onaylandi: {sonuc.DuzeltilmisPlaka}", TemaYardimcisi.Yesil);
            return;
        }

        if (sonuc.BasariliMi && !string.IsNullOrWhiteSpace(sonuc.DuzeltilmisPlaka))
        {
            _txtPlaka.Text = sonuc.DuzeltilmisPlaka;
            DurumYaz($"{debugMetni}\r\nOCR: {sonuc.DuzeltilmisPlaka} - Guven %{sonuc.GuvenOrani:N0} - Aday {sonuc.Adaylar.Count}", TemaYardimcisi.Yesil);
            return;
        }

        if (PlakaDogrulamaYardimcisi.TurkPlakasiMi(sonuc.DuzeltilmisPlaka))
        {
            _txtPlaka.Text = sonuc.DuzeltilmisPlaka;
        }

        DurumYaz($"{debugMetni}\r\n{sonuc.Aciklama ?? "Plaka net okunamadi."}", TemaYardimcisi.Sari);
    }

    private bool YakinPlakaOnerisiniSor(PlakaOkumaSonucu sonuc)
    {
        if (string.IsNullOrWhiteSpace(sonuc.YakinPlakaOnerisi))
        {
            return false;
        }

        var cevap = MessageBox.Show(
            $"OCR sonucu dusuk guvenli. {sonuc.YakinPlakaOnerisi} plakasi olabilir mi?",
            "Plaka Onerisi",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (cevap != DialogResult.Yes)
        {
            return false;
        }

        sonuc.DuzeltilmisPlaka = sonuc.YakinPlakaOnerisi;
        sonuc.NihaiSonuc = sonuc.YakinPlakaOnerisi;
        sonuc.BasariliMi = true;
        sonuc.GuvenOrani = 100;
        return true;
    }

    private bool PythonFormatDisiAdayiniSor(PlakaOkumaSonucu sonuc, string debugMetni)
    {
        if (string.IsNullOrWhiteSpace(sonuc.PythonFormatDisiAday))
        {
            return false;
        }

        var mesaj = $"Python OCR plakayı okudu ancak sonuç Türk plaka formatına uymuyor: {sonuc.PythonFormatDisiAday}";
        var cevap = MessageBox.Show(
            $"{mesaj}\n\nBu adayi Plaka alanina yazmak ister misiniz?",
            "Python OCR",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (cevap == DialogResult.Yes)
        {
            _txtPlaka.Text = sonuc.PythonFormatDisiAday;
            DurumYaz($"{debugMetni}\r\n{mesaj}\r\nKullanici onayi ile TextBox'a yazildi.", TemaYardimcisi.Yesil);
        }
        else
        {
            DurumYaz($"{debugMetni}\r\n{mesaj}\r\nYedek OCR sonucu otomatik yazilmadi. Manuel giris bekleniyor.", TemaYardimcisi.Sari);
        }

        return true;
    }

    private static void PythonServisiUyarisiGoster(PlakaOkumaSonucu sonuc)
    {
        if (sonuc.PythonServisiCalisiyorMu)
        {
            return;
        }

        MessageBox.Show(
            PythonPlakaOkumaServisi.ServisCalismiyorMesaji,
            "Python OCR",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    private static string OcrDebugMetni(PlakaOkumaSonucu sonuc)
    {
        var python = string.IsNullOrWhiteSpace(sonuc.PythonSonucu) ? "-" : sonuc.PythonSonucu;
        var pythonAdaylari = string.IsNullOrWhiteSpace(sonuc.PythonAdaylari) ? "-" : sonuc.PythonAdaylari;
        var pythonSecilen = string.IsNullOrWhiteSpace(sonuc.PythonSecilenAday) ? "-" : sonuc.PythonSecilenAday;
        var pythonRed = string.IsNullOrWhiteSpace(sonuc.PythonReddedilmeSebebi) ? "-" : sonuc.PythonReddedilmeSebebi;
        var tesseract = string.IsNullOrWhiteSpace(sonuc.TesseractSonucu) ? "-" : sonuc.TesseractSonucu;
        var sablon = string.IsNullOrWhiteSpace(sonuc.SablonSonucu) ? "-" : sonuc.SablonSonucu;
        var nihai = PlakaDogrulamaYardimcisi.TurkPlakasiMi(sonuc.NihaiSonuc)
            ? sonuc.NihaiSonuc
            : PlakaDogrulamaYardimcisi.TurkPlakasiMi(sonuc.DuzeltilmisPlaka)
                ? sonuc.DuzeltilmisPlaka
                : "-";

        return $"Python: {python} (%{sonuc.PythonGuvenOrani:N0})\n" +
               $"Python adaylari: {pythonAdaylari}\n" +
               $"Python secilen: {pythonSecilen}\n" +
               $"Python red sebebi: {pythonRed}\n" +
               $"Regex modu: {(PlakaDogrulamaYardimcisi.DemoPlakaModuAktifMi() ? "Demo" : "Gercek")}\n" +
               $"Tesseract: {tesseract} (%{sonuc.TesseractGuvenOrani:N0})\n" +
               $"Sablon: {sablon} (%{sonuc.SablonGuvenOrani:N0})\n" +
               $"Secilen: {nihai}";
    }

    private void RoiAyarlaBaslat()
    {
        _roiSecimAktif = true;
        _geciciRoiOrani = null;
        DurumYaz("ROI secimi aktif. Plaka alanini mouse ile cizin, sonra ROI Kaydet'e basin.", TemaYardimcisi.IkincilMetin);
        _picKamera.Cursor = Cursors.Cross;
        _picKamera.Invalidate();
    }

    private void RoiKaydet()
    {
        if (_geciciRoiOrani.HasValue)
        {
            KameraServisi.PlakaCercevesiKaydet(_geciciRoiOrani.Value);
        }
        else
        {
            KameraServisi.PlakaCercevesiKaydet();
        }

        _roiSecimAktif = false;
        _roiSurukleniyor = false;
        _picKamera.Cursor = Cursors.Default;
        _geciciRoiOrani = null;
        DurumYaz("ROI kaydedildi. OCR bu alan uzerinden calisacak.", TemaYardimcisi.Yesil);
        _picKamera.Invalidate();
        KirpilanAlaniOnizle();
    }

    private void RoiSifirla()
    {
        KameraServisi.PlakaCercevesiSifirla();
        KameraServisi.PlakaCercevesiKaydet();
        _roiSecimAktif = false;
        _roiSurukleniyor = false;
        _geciciRoiOrani = null;
        _picKamera.Cursor = Cursors.Default;
        DurumYaz("ROI varsayilan orta cerceveye sifirlandi.", TemaYardimcisi.IkincilMetin);
        _picKamera.Invalidate();
        KirpilanAlaniOnizle();
    }

    private void PicKamera_MouseDown(object? sender, MouseEventArgs e)
    {
        if (!_roiSecimAktif || e.Button != MouseButtons.Left)
        {
            return;
        }

        if (KameraServisi.PictureBoxNoktasiniGoruntuOraninaCevir(_picKamera, e.Location, out _roiBaslangic))
        {
            _roiSurukleniyor = true;
            _geciciRoiOrani = new RectangleF(_roiBaslangic.X, _roiBaslangic.Y, 0.01F, 0.01F);
        }
    }

    private void PicKamera_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_roiSecimAktif || !_roiSurukleniyor)
        {
            return;
        }

        if (KameraServisi.PictureBoxNoktasiniGoruntuOraninaCevir(_picKamera, e.Location, out var bitis))
        {
            _geciciRoiOrani = KameraServisi.OranlardanDikdortgen(_roiBaslangic, bitis);
            _picKamera.Invalidate();
        }
    }

    private void PicKamera_MouseUp(object? sender, MouseEventArgs e)
    {
        if (!_roiSecimAktif || !_roiSurukleniyor)
        {
            return;
        }

        _roiSurukleniyor = false;
        if (_geciciRoiOrani is { Width: > 0.03F, Height: > 0.03F } roi)
        {
            KameraServisi.PlakaCercevesiAyarla(roi);
            DurumYaz("ROI secildi. Kalici yapmak icin ROI Kaydet'e basin.", TemaYardimcisi.IkincilMetin);
            KirpilanAlaniOnizle();
        }
        else
        {
            _geciciRoiOrani = null;
            DurumYaz("ROI cok kucuk. Plaka alanini biraz daha genis secin.", TemaYardimcisi.Sari);
        }

        _picKamera.Invalidate();
    }

    private void KirpilanAlaniOnizle()
    {
        try
        {
            using var kaynak = GecerliGoruntuAl();
            if (kaynak is null)
            {
                return;
            }

            using var kirpilmis = _plakaOnIslemeServisi.RoiKirp(kaynak);
            var eski = _picRoiOnizleme.Image;
            _picRoiOnizleme.Image = new Bitmap(kirpilmis);
            eski?.Dispose();
        }
        catch
        {
            // Onizleme yardimci bir gorseldir; hata olursa ana akisi durdurmayiz.
        }
    }

    private Image? GecerliGoruntuAl()
    {
        if (_kameraServisi.Calisiyor)
        {
            var kameraFoto = _kameraServisi.FotoCek();
            if (kameraFoto is not null)
            {
                return kameraFoto;
            }
        }

        if (_sonFoto is not null)
        {
            return new Bitmap(_sonFoto);
        }

        return _picKamera.Image is null ? null : new Bitmap(_picKamera.Image);
    }

    private void ParkYerleriniYukle()
    {
        try
        {
            var bosYerler = _parkYeriRepository.BosYerleriGetir();
            _cmbParkYerleri.DataSource = bosYerler;
            _cmbParkYerleri.DisplayMember = nameof(ParkYeri.Kod);
            _cmbParkYerleri.ValueMember = nameof(ParkYeri.ParkYeriId);

            if (bosYerler.Count > 0)
            {
                _cmbParkYerleri.SelectedIndex = 0;
            }
        }
        catch (SqlException ex)
        {
            MessageBox.Show($"Park yerleri yuklenemedi. SQL scriptlerini calistirdiginizdan emin olun.\n\n{ex.Message}",
                "Veritabani", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Park yerleri yuklenemedi: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void GirisKaydet()
    {
        try
        {
            var plaka = PlakaDogrulamaYardimcisi.Temizle(_txtPlaka.Text);
            if (!PlakaDogrulamaYardimcisi.TurkPlakasiMi(plaka))
            {
                MessageBox.Show("Gecerli bir Turk plakasi girin. Ornek: 34ABC123", "Plaka", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_cmbParkYerleri.SelectedItem is not ParkYeri parkYeri)
            {
                MessageBox.Show("Bos park yeri secilmedi.", "Park Yeri", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _plakaOkumaServisi.ManuelOkumaLogla(plaka, "AracGirisOnay", _sonOkumaSonucu?.ResimYolu ?? _sonFotoYolu, _sonOkumaSonucu?.OkunanMetin);
            var kayitId = _otoparkServisi.AracGirisKaydet(plaka, parkYeri.ParkYeriId, _aktifKullanici.KullaniciId);

            MessageBox.Show($"Arac girisi kaydedildi. Kayit No: {kayitId}", "Basarili", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _txtPlaka.Clear();
            DurumYaz("OCR sonucu bekleniyor.", TemaYardimcisi.IkincilMetin);
            _sonOkumaSonucu = null;
            ParkYerleriniYukle();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Giris kaydedilemedi: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static Label Etiket(string text)
    {
        return new Label
        {
            AutoSize = false,
            Width = 360,
            Height = 28,
            Text = text,
            ForeColor = TemaYardimcisi.IkincilMetin,
            Font = new Font("Segoe UI Semibold", 9F)
        };
    }

    private void DurumYaz(string mesaj, Color renk)
    {
        _lblOcr.ForeColor = renk;
        _lblOcr.Text = mesaj;
    }

    private static string FotoKaydet(Image image)
    {
        var klasor = Path.Combine(AppContext.BaseDirectory, "Resources");
        Directory.CreateDirectory(klasor);
        var yol = Path.Combine(klasor, $"plaka_giris_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");
        image.Save(yol, ImageFormat.Jpeg);
        return yol;
    }

    private void KaynaklariTemizle()
    {
        _kameraServisi.Dispose();
        _sonFoto?.Dispose();
    }
}
