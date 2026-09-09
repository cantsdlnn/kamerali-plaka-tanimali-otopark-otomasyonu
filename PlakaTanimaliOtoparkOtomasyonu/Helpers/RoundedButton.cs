using System.Drawing.Drawing2D;

namespace PlakaTanimaliOtoparkOtomasyonu.Helpers;

public class RoundedButton : Button
{
    public int BorderRadius { get; set; } = 10;
    public Color HoverBackColor { get; set; } = Color.FromArgb(55, 125, 245);
    public Color NormalBackColor { get; set; } = Color.FromArgb(37, 99, 235);

    public RoundedButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = NormalBackColor;
        ForeColor = Color.White;
        Cursor = Cursors.Hand;
        Height = 42;
        Font = new Font("Segoe UI Semibold", 10F, FontStyle.Regular);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        BackColor = HoverBackColor;
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        BackColor = NormalBackColor;
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Region = new Region(GetRoundPath(ClientRectangle, BorderRadius));
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = GetRoundPath(ClientRectangle, BorderRadius);
        using var brush = new SolidBrush(BackColor);
        pevent.Graphics.FillPath(brush, path);
        TextRenderer.DrawText(pevent.Graphics, Text, Font, ClientRectangle, ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private static GraphicsPath GetRoundPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var diameter = Math.Max(radius * 2, 1);
        var arc = new Rectangle(rect.Location, new Size(diameter, diameter));

        path.AddArc(arc, 180, 90);
        arc.X = rect.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = rect.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = rect.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }
}
