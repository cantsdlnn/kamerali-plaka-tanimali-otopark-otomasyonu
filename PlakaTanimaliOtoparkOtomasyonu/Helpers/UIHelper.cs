namespace PlakaTanimaliOtoparkOtomasyonu.Helpers;

public static class UIHelper
{
    public static readonly Color ArkaPlan = Color.FromArgb(15, 23, 42);
    public static readonly Color Panel = Color.FromArgb(30, 41, 59);
    public static readonly Color Kart = Color.FromArgb(39, 52, 73);
    public static readonly Color PanelKoyu = Color.FromArgb(17, 24, 39);
    public static readonly Color Cizgi = Color.FromArgb(51, 65, 85);
    public static readonly Color Metin = Color.FromArgb(248, 250, 252);
    public static readonly Color IkincilMetin = Color.FromArgb(203, 213, 225);
    public static readonly Color Mavi = Color.FromArgb(37, 99, 235);
    public static readonly Color Yesil = Color.FromArgb(22, 163, 74);
    public static readonly Color Sari = Color.FromArgb(234, 179, 8);
    public static readonly Color Kirmizi = Color.FromArgb(220, 38, 38);

    public static void FormHazirla(Form form, string baslik, Size? minimumSize = null)
    {
        form.Text = baslik;
        form.BackColor = ArkaPlan;
        form.ForeColor = Metin;
        form.Font = new Font("Segoe UI", 10F);
        form.AutoScaleMode = AutoScaleMode.Font;
        form.StartPosition = FormStartPosition.CenterScreen;
        form.MinimumSize = minimumSize ?? new Size(1100, 720);
    }

    public static Panel KartPanel(int padding = 18)
    {
        return new Panel
        {
            BackColor = Kart,
            Padding = new Padding(padding),
            Margin = new Padding(8)
        };
    }

    public static Label Baslik(string text, int size = 22)
    {
        return new Label
        {
            AutoSize = false,
            Height = Math.Max(42, size + 20),
            Dock = DockStyle.Top,
            Text = text,
            ForeColor = Metin,
            Font = new Font("Segoe UI Semibold", size, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 0, 0, 10)
        };
    }

    public static Label Aciklama(string text)
    {
        return new Label
        {
            AutoSize = false,
            Height = 34,
            Text = text,
            ForeColor = IkincilMetin,
            Font = new Font("Segoe UI", 10F),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 0, 0, 12)
        };
    }

    public static Label Etiket(string text, int width)
    {
        return new Label
        {
            AutoSize = false,
            Width = width,
            Height = 26,
            Text = text,
            ForeColor = IkincilMetin,
            Font = new Font("Segoe UI Semibold", 9.5F),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 8, 0, 4)
        };
    }

    public static TextBox TextBox(string? placeholder = null)
    {
        return new TextBox
        {
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = PanelKoyu,
            ForeColor = Metin,
            Font = new Font("Segoe UI", 11F),
            PlaceholderText = placeholder ?? string.Empty,
            Height = 38,
            Margin = new Padding(0, 0, 0, 8)
        };
    }

    public static ComboBox ComboBox()
    {
        return new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = PanelKoyu,
            ForeColor = Metin,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10.5F),
            Height = 38,
            Margin = new Padding(0, 0, 0, 8)
        };
    }

    public static TextBox DurumKutusu(string text = "")
    {
        return new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            ScrollBars = ScrollBars.Vertical,
            BackColor = PanelKoyu,
            ForeColor = IkincilMetin,
            Font = new Font("Consolas", 9.5F),
            Text = text,
            Margin = new Padding(0, 0, 0, 10)
        };
    }

    public static RoundedButton Buton(string text, Color? renk = null)
    {
        var normal = renk ?? Mavi;
        return new RoundedButton
        {
            Text = text,
            NormalBackColor = normal,
            BackColor = normal,
            HoverBackColor = ControlPaint.Light(normal, 0.12F),
            BorderRadius = 8,
            Height = 42,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Regular),
            Margin = new Padding(0, 0, 8, 8)
        };
    }

    public static void GridHazirla(DataGridView grid)
    {
        grid.BackgroundColor = ArkaPlan;
        grid.BorderStyle = BorderStyle.None;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        grid.ColumnHeadersDefaultCellStyle.BackColor = PanelKoyu;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Metin;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = PanelKoyu;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Metin;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10F);
        grid.ColumnHeadersHeight = 38;
        grid.DefaultCellStyle.BackColor = Panel;
        grid.DefaultCellStyle.ForeColor = Metin;
        grid.DefaultCellStyle.SelectionBackColor = Mavi;
        grid.DefaultCellStyle.SelectionForeColor = Color.White;
        grid.DefaultCellStyle.Font = new Font("Segoe UI", 10F);
        grid.DefaultCellStyle.Padding = new Padding(6, 3, 6, 3);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(36, 49, 69);
        grid.GridColor = Cizgi;
        grid.RowHeadersVisible = false;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.ReadOnly = true;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.RowTemplate.Height = 34;
    }

    public static TableLayoutPanel IkiKolonLayout(float solYuzde, int padding = 12)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(padding),
            BackColor = ArkaPlan
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, solYuzde));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F - solYuzde));
        return layout;
    }
}
