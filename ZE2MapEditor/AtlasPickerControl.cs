using System.Drawing.Drawing2D;

namespace ZE2MapEditor;

public sealed class AtlasPickerControl : Control
{
    private int displayCellSize = TextureAtlas.CellSize;

    public AtlasPickerControl()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(28, 30, 34);
        Size = new Size(512, 512);
        MinimumSize = Size;
        SetStyle(ControlStyles.ResizeRedraw, true);
    }

    public TextureAtlas? Atlas { get; set; }
    public int SelectedX { get; private set; }
    public int SelectedY { get; private set; }
    public event Action<int, int>? TextureSelected;

    public void SetZoomPercent(int percent)
    {
        displayCellSize = Math.Clamp(TextureAtlas.CellSize * percent / 100, 8, 96);
        UpdateSize();
        Invalidate();
    }

    public void SetSelected(int x, int y)
    {
        SelectedX = Math.Max(0, x);
        SelectedY = Math.Max(0, y);
        Invalidate();
    }

    public void UpdateSize()
    {
        if (Atlas is null)
        {
            return;
        }

        Size = new Size(Atlas.Columns * displayCellSize, Atlas.Rows * displayCellSize);
        MinimumSize = Size;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (Atlas is null)
        {
            return;
        }

        var x = e.X / displayCellSize;
        var y = e.Y / displayCellSize;
        if (!Atlas.TrySourceRect(x, y, out _))
        {
            return;
        }

        SelectedX = x;
        SelectedY = y;
        TextureSelected?.Invoke(x, y);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;

        using var backBrush = new SolidBrush(BackColor);
        e.Graphics.FillRectangle(backBrush, ClientRectangle);

        if (Atlas is null)
        {
            TextRenderer.DrawText(e.Graphics, "Atlas not loaded", Font, ClientRectangle, Color.Gainsboro, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        var atlasWidth = Atlas.Columns * displayCellSize;
        var atlasHeight = Atlas.Rows * displayCellSize;
        e.Graphics.DrawImage(Atlas.Bitmap, new Rectangle(0, 0, atlasWidth, atlasHeight));

        using var gridPen = new Pen(Color.FromArgb(70, 255, 255, 255), 1);
        for (var x = 0; x <= Atlas.Columns; x++)
        {
            e.Graphics.DrawLine(gridPen, x * displayCellSize, 0, x * displayCellSize, atlasHeight);
        }

        for (var y = 0; y <= Atlas.Rows; y++)
        {
            e.Graphics.DrawLine(gridPen, 0, y * displayCellSize, atlasWidth, y * displayCellSize);
        }

        using var selectedPen = new Pen(Color.Gold, 3);
        e.Graphics.DrawRectangle(selectedPen, SelectedX * displayCellSize + 1, SelectedY * displayCellSize + 1, displayCellSize - 2, displayCellSize - 2);
    }
}
