using Microsoft.Data.SqlClient;
using PlakaTanimaliOtoparkOtomasyonu.DataAccess;
using PlakaTanimaliOtoparkOtomasyonu.Helpers;
using PlakaTanimaliOtoparkOtomasyonu.Models;

namespace PlakaTanimaliOtoparkOtomasyonu.Forms;

public class FrmAnaPanel : Form
{
    private readonly Kullanici _aktifKullanici;
    private readonly Panel _icerikPanel = new();
    private readonly ParkKaydiRepository _parkKaydiRepository = new();
    private readonly List<RoundedButton> _menuButonlari = new();

    public FrmAnaPanel(Kullanici aktifKullanici)
    {
        _aktifKullanici = aktifKullanici;
        TemaYardimcisi.FormuHazirla(this, "Ana Panel");
        WindowState = FormWindowState.Maximized;

        var anaLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = TemaYardimcisi.ArkaPlan
        };
        anaLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
        anaLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        anaLayout.Controls.Add(SolMenuOlustur(), 0, 0);

        _icerikPanel.Dock = DockStyle.Fill;
        _icerikPanel.Padding = new Padding(18);
        _icerikPanel.BackColor = TemaYardimcisi.ArkaPlan;
        anaLayout.Controls.Add(_icerikPanel, 1, 0);

        Controls.Add(anaLayout);

        if (_aktifKullanici.AdminMi)
        {
            DashboardYukle();
        }
        else
        {
            EkranAc(new FrmAracGiris(_aktifKullanici));
        }
    }

    private Control SolMenuOlustur()
    {
        var menu = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = TemaYardimcisi.PanelKoyu,
            Padding = new Padding(18)
        };

        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = TemaYardimcisi.PanelKoyu
        };

        stack.Controls.Add(new Label
        {
            AutoSize = false,
            Width = 210,
            Height = 104,
            Margin = new Padding(0, 0, 0, 10),
            Text = "PLAKA\nOTOPARK",
            Font = new Font("Segoe UI Semibold", 19F),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft
        });

        stack.Controls.Add(new Label
        {
            AutoSize = false,
            Width = 210,
            Height = 58,
            Margin = new Padding(0, 0, 0, 16),
            Text = $"{_aktifKullanici.AdSoyad}\n{_aktifKullanici.Rol}",
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = TemaYardimcisi.IkincilMetin,
            TextAlign = ContentAlignment.MiddleLeft
        });

        if (_aktifKullanici.AdminMi)
        {
            stack.Controls.Add(MenuButonu("Ana Panel", DashboardYukle));
            stack.Controls.Add(MenuButonu("Arac Giris", () => EkranAc(new FrmAracGiris(_aktifKullanici))));
            stack.Controls.Add(MenuButonu("Arac Cikis", () => EkranAc(new FrmAracCikis(_aktifKullanici))));
            stack.Controls.Add(MenuButonu("Park Yerleri", () => EkranAc(new FrmParkYerleri(false))));
            stack.Controls.Add(MenuButonu("Araclar", () => EkranAc(new FrmAraclar())));
            stack.Controls.Add(MenuButonu("Raporlar", () => EkranAc(new FrmRaporlar())));
            stack.Controls.Add(MenuButonu("Ayarlar", () => EkranAc(new FrmAyarlar())));
        }
        else
        {
            stack.Controls.Add(MenuButonu("Arac Giris", () => EkranAc(new FrmAracGiris(_aktifKullanici))));
            stack.Controls.Add(MenuButonu("Arac Cikis", () => EkranAc(new FrmAracCikis(_aktifKullanici))));
            stack.Controls.Add(MenuButonu("Aktif Kayitlar", () => EkranAc(new FrmParkYerleri(true))));
        }

        var btnCikis = MenuButonu("Cikis Yap", Close);
        btnCikis.NormalBackColor = TemaYardimcisi.Kirmizi;
        btnCikis.BackColor = TemaYardimcisi.Kirmizi;
        btnCikis.HoverBackColor = ControlPaint.Light(TemaYardimcisi.Kirmizi, 0.15F);
        stack.Controls.Add(btnCikis);

        menu.Controls.Add(stack);
        return menu;
    }

    private RoundedButton MenuButonu(string text, Action action)
    {
        var button = TemaYardimcisi.Buton(text, TemaYardimcisi.Panel);
        button.Width = 210;
        button.Height = 42;
        button.Margin = new Padding(0, 0, 0, 10);
        button.TextAlign = ContentAlignment.MiddleLeft;
        button.Click += (_, _) =>
        {
            MenuVurgula(button);
            action();
        };
        _menuButonlari.Add(button);
        return button;
    }

    private void MenuVurgula(RoundedButton secilen)
    {
        foreach (var button in _menuButonlari.Where(b => b.NormalBackColor != TemaYardimcisi.Kirmizi))
        {
            button.NormalBackColor = TemaYardimcisi.Panel;
            button.BackColor = TemaYardimcisi.Panel;
        }

        secilen.NormalBackColor = TemaYardimcisi.Mavi;
        secilen.BackColor = TemaYardimcisi.Mavi;
    }

    private void EkranAc(Form form)
    {
        _icerikPanel.Controls.Clear();
        form.TopLevel = false;
        form.FormBorderStyle = FormBorderStyle.None;
        form.Dock = DockStyle.Fill;
        _icerikPanel.Controls.Add(form);
        form.Show();
    }

    private void DashboardYukle()
    {
        _icerikPanel.Controls.Clear();

        var container = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8),
            BackColor = TemaYardimcisi.ArkaPlan
        };

        var baslik = TemaYardimcisi.Baslik("Ana Panel", 24);
        baslik.Dock = DockStyle.Top;
        container.Controls.Add(baslik);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 260,
            ColumnCount = 3,
            RowCount = 2,
            BackColor = TemaYardimcisi.ArkaPlan,
            Padding = new Padding(0, 24, 0, 0)
        };

        for (var i = 0; i < 3; i++)
        {
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        }

        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        try
        {
            var ozet = _parkKaydiRepository.DashboardOzetGetir();
            grid.Controls.Add(DashboardKarti("Toplam Kapasite", ozet.ToplamKapasite.ToString(), TemaYardimcisi.Mavi), 0, 0);
            grid.Controls.Add(DashboardKarti("Dolu Yer", ozet.DoluYer.ToString(), TemaYardimcisi.Kirmizi), 1, 0);
            grid.Controls.Add(DashboardKarti("Bos Yer", ozet.BosYer.ToString(), TemaYardimcisi.Yesil), 2, 0);
            grid.Controls.Add(DashboardKarti("Gunluk Giris", ozet.GunlukGiris.ToString(), TemaYardimcisi.Sari), 0, 1);
            grid.Controls.Add(DashboardKarti("Gunluk Cikis", ozet.GunlukCikis.ToString(), TemaYardimcisi.Mavi), 1, 1);
            grid.Controls.Add(DashboardKarti("Gunluk Kazanc", $"{ozet.GunlukKazanc:C2}", TemaYardimcisi.Yesil), 2, 1);
        }
        catch (SqlException ex)
        {
            grid.Controls.Add(HataKarti($"Veritabani baglantisi hazir degil.\n{ex.Message}"), 0, 0);
        }
        catch (Exception ex)
        {
            grid.Controls.Add(HataKarti($"Panel bilgileri yuklenemedi.\n{ex.Message}"), 0, 0);
        }

        container.Controls.Add(grid);
        _icerikPanel.Controls.Add(container);
    }

    private static Control DashboardKarti(string baslik, string deger, Color renk)
    {
        var panel = TemaYardimcisi.Kart();
        panel.Dock = DockStyle.Fill;

        var accent = new Panel { Dock = DockStyle.Left, Width = 6, BackColor = renk };
        var lblDeger = new Label
        {
            Dock = DockStyle.Top,
            Height = 52,
            Text = deger,
            Font = new Font("Segoe UI Semibold", 24F),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var lblBaslik = new Label
        {
            Dock = DockStyle.Top,
            Height = 32,
            Text = baslik,
            Font = new Font("Segoe UI", 10F),
            ForeColor = TemaYardimcisi.IkincilMetin,
            TextAlign = ContentAlignment.MiddleLeft
        };

        panel.Controls.Add(lblBaslik);
        panel.Controls.Add(lblDeger);
        panel.Controls.Add(accent);
        return panel;
    }

    private static Control HataKarti(string mesaj)
    {
        var panel = TemaYardimcisi.Kart();
        panel.Dock = DockStyle.Fill;
        panel.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = mesaj,
            ForeColor = TemaYardimcisi.IkincilMetin,
            TextAlign = ContentAlignment.MiddleCenter
        });
        return panel;
    }
}
