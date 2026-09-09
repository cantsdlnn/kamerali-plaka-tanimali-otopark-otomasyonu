using PlakaTanimaliOtoparkOtomasyonu.Helpers;

namespace PlakaTanimaliOtoparkOtomasyonu.Forms;

public class FrmYukleme : Form
{
    private readonly ProgressBar _progressBar = new();
    private readonly Label _lblYuzde = new();
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 25 };

    public FrmYukleme()
    {
        Text = "Yukleniyor";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(620, 340);
        BackColor = TemaYardimcisi.ArkaPlan;
        ForeColor = TemaYardimcisi.Metin;
        Font = new Font("Segoe UI", 10F);

        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(46),
            BackColor = TemaYardimcisi.ArkaPlan
        };

        var lblBaslik = new Label
        {
            Dock = DockStyle.Top,
            Height = 58,
            Text = "Kamerali Plaka Tanimali Otopark",
            Font = new Font("Segoe UI Semibold", 22F),
            ForeColor = Color.White
        };

        var lblAlt = new Label
        {
            Dock = DockStyle.Top,
            Height = 40,
            Text = "SQL Server, kamera ve servisler hazirlaniyor...",
            Font = new Font("Segoe UI", 11F),
            ForeColor = TemaYardimcisi.IkincilMetin
        };

        _progressBar.Dock = DockStyle.Top;
        _progressBar.Height = 18;
        _progressBar.Style = ProgressBarStyle.Continuous;
        _progressBar.Maximum = 100;

        _lblYuzde.Dock = DockStyle.Top;
        _lblYuzde.Height = 48;
        _lblYuzde.TextAlign = ContentAlignment.MiddleRight;
        _lblYuzde.ForeColor = TemaYardimcisi.IkincilMetin;
        _lblYuzde.Text = "%0";

        panel.Controls.Add(_lblYuzde);
        panel.Controls.Add(_progressBar);
        panel.Controls.Add(lblAlt);
        panel.Controls.Add(lblBaslik);
        Controls.Add(panel);

        _timer.Tick += Timer_Tick;
        Shown += (_, _) => _timer.Start();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        _progressBar.Value = Math.Min(_progressBar.Value + 4, 100);
        _lblYuzde.Text = $"%{_progressBar.Value}";

        if (_progressBar.Value >= 100)
        {
            _timer.Stop();
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
        }

        base.Dispose(disposing);
    }
}
