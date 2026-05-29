using System.Drawing.Drawing2D;

namespace ZE2MapEditor;

public sealed class TileGridControl : Control
{
    private readonly Dictionary<TilePropertyType, Brush> propertyBrushes = new()
    {
        [TilePropertyType.NoPath] = new SolidBrush(Color.FromArgb(130, 210, 40, 40)),
        [TilePropertyType.NoSpawn] = new SolidBrush(Color.FromArgb(120, 40, 70, 200)),
        [TilePropertyType.PlayerOneSpawn] = new SolidBrush(Color.FromArgb(180, 40, 200, 80)),
        [TilePropertyType.PlayerTwoSpawn] = new SolidBrush(Color.FromArgb(180, 70, 160, 230)),
        [TilePropertyType.PlayerThreeSpawn] = new SolidBrush(Color.FromArgb(180, 240, 200, 50)),
        [TilePropertyType.PlayerFourSpawn] = new SolidBrush(Color.FromArgb(180, 190, 90, 220)),
        [TilePropertyType.ShopKeep] = new SolidBrush(Color.FromArgb(170, 20, 160, 150)),
        [TilePropertyType.CarePackageSpawn] = new SolidBrush(Color.FromArgb(170, 240, 130, 40)),
        [TilePropertyType.BossArea] = new SolidBrush(Color.FromArgb(170, 150, 30, 30)),
        [TilePropertyType.DemonFire] = new SolidBrush(Color.FromArgb(170, 245, 80, 20))
    };

    public TileGridControl()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(28, 30, 34);
        MinimumSize = new Size(512, 512);
        SetStyle(ControlStyles.ResizeRedraw, true);
    }

    public SectorMap? Sector { get; set; }
    public TextureAtlas? Atlas { get; set; }
    public TileIndexOrder IndexOrder { get; set; }
    public bool RectangleDragMode { get; set; }
    public int CellSizePixels { get; private set; } = 16;
    public int SelectedIndex { get; set; } = -1;
    public int HoverIndex { get; private set; } = -1;
    public event Action<int>? TileSelected;
    public event Action<int, PaintStrokePhase, MouseButtons>? TilePaintRequested;
    public event Action<int>? TileHovered;
    public event Action<Rectangle, MouseButtons>? RectangleCommitted;

    private bool isPainting;
    private int lastPaintIndex = -1;
    private MouseButtons activePaintButton = MouseButtons.None;
    private Point? roomStartCell;
    private Point? roomCurrentCell;

    public void SetZoomPercent(int percent)
    {
        CellSizePixels = Math.Clamp(TextureAtlas.CellSize * percent / 100, 8, 96);
        var size = CellSizePixels * SectorMap.Width + 12;
        Size = new Size(size, size);
        MinimumSize = Size;
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var index = HitTest(e.Location);
        if (index == HoverIndex)
        {
            return;
        }

        HoverIndex = index;
        TileHovered?.Invoke(index);
        if ((e.Button == MouseButtons.Left || e.Button == MouseButtons.Right) && isPainting && !RectangleDragMode && index >= 0 && index != lastPaintIndex)
        {
            lastPaintIndex = index;
            SelectedIndex = index;
            TilePaintRequested?.Invoke(index, PaintStrokePhase.Continue, activePaintButton);
        }
        else if ((e.Button == MouseButtons.Left || e.Button == MouseButtons.Right) && RectangleDragMode && roomStartCell.HasValue)
        {
            roomCurrentCell = HitCell(e.Location);
        }

        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        HoverIndex = -1;
        TileHovered?.Invoke(-1);
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        var index = HitTest(e.Location);
        if (index < 0)
        {
            return;
        }

        if (e.Button is not (MouseButtons.Left or MouseButtons.Right))
        {
            return;
        }

        if (RectangleDragMode)
        {
            roomStartCell = HitCell(e.Location);
            roomCurrentCell = roomStartCell;
            activePaintButton = e.Button;
            Capture = true;
            Invalidate();
            return;
        }

        isPainting = true;
        lastPaintIndex = index;
        activePaintButton = e.Button;
        Capture = true;
        SelectedIndex = index;
        TileSelected?.Invoke(index);
        TilePaintRequested?.Invoke(index, PaintStrokePhase.Begin, activePaintButton);
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (RectangleDragMode && roomStartCell.HasValue && roomCurrentCell.HasValue)
        {
            RectangleCommitted?.Invoke(NormalizeCells(roomStartCell.Value, roomCurrentCell.Value), activePaintButton);
        }

        isPainting = false;
        lastPaintIndex = -1;
        activePaintButton = MouseButtons.None;
        roomStartCell = null;
        roomCurrentCell = null;
        Capture = false;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.None;
        e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;

        using var backBrush = new SolidBrush(BackColor);
        e.Graphics.FillRectangle(backBrush, ClientRectangle);

        var rect = GridRect();
        if (Sector is null)
        {
            TextRenderer.DrawText(e.Graphics, "Open a ZE2 map folder and select a map.", Font, rect, Color.Gainsboro, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        var cell = rect.Width / (float)SectorMap.Width;
        for (var y = 0; y < SectorMap.Height; y++)
        {
            for (var x = 0; x < SectorMap.Width; x++)
            {
                var index = MapIndexMapper.ToIndex(x, y, IndexOrder);
                var tileRect = new RectangleF(rect.Left + x * cell, rect.Top + y * cell, cell, cell);
                DrawTile(e.Graphics, Sector.Tiles[index], tileRect, index);
            }
        }

        using var gridPen = new Pen(Color.FromArgb(80, 92, 96), 1);
        for (var i = 0; i <= SectorMap.Width; i++)
        {
            var pos = rect.Left + i * cell;
            e.Graphics.DrawLine(gridPen, pos, rect.Top, pos, rect.Bottom);
        }

        for (var i = 0; i <= SectorMap.Height; i++)
        {
            var pos = rect.Top + i * cell;
            e.Graphics.DrawLine(gridPen, rect.Left, pos, rect.Right, pos);
        }

        if (roomStartCell.HasValue && roomCurrentCell.HasValue)
        {
            DrawRoomPreview(e.Graphics, rect, cell, NormalizeCells(roomStartCell.Value, roomCurrentCell.Value));
        }
    }

    private void DrawTile(Graphics graphics, TileInfo tile, RectangleF rect, int index)
    {
        if (tile.floor && Atlas is not null && Atlas.TrySourceRect(tile.floorTex, out var floorSource))
        {
            graphics.DrawImage(Atlas.Bitmap, rect, floorSource, GraphicsUnit.Pixel);
        }
        else
        {
            using var floorBrush = new SolidBrush(tile.floor ? Color.FromArgb(66, 78, 66) : Color.FromArgb(43, 43, 47));
            graphics.FillRectangle(floorBrush, rect);
        }

        foreach (var prop in tile.tileProps.Distinct())
        {
            if (propertyBrushes.TryGetValue(prop, out var brush))
            {
                graphics.FillRectangle(brush, rect);
                break;
            }
        }

        DrawWall(graphics, rect, WallEdge.Left, tile.leftWall, tile.leftWallFake, tile.leftWallHalf, tile.leftWallWindow, tile.leftWallTex);
        DrawWall(graphics, rect, WallEdge.Right, tile.rightWall, tile.rightWallFake, tile.rightWallHalf, tile.rightWallWindow, tile.rightWallTex);
        DrawWall(graphics, rect, WallEdge.Top, tile.topWall, tile.topWallFake, tile.topWallHalf, tile.topWallWindow, tile.topWallTex);
        DrawWall(graphics, rect, WallEdge.Bottom, tile.bottomWall, tile.bottomWallFake, tile.bottomWallHalf, tile.bottomWallWindow, tile.bottomWallTex);

        if (index == HoverIndex)
        {
            using var hoverPen = new Pen(Color.White, 2);
            graphics.DrawRectangle(hoverPen, rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2);
        }

        if (index == SelectedIndex)
        {
            using var selectedPen = new Pen(Color.Gold, 3);
            graphics.DrawRectangle(selectedPen, rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4);
        }
    }

    private void DrawWall(Graphics graphics, RectangleF rect, WallEdge edge, bool wall, bool fake, bool half, bool window, TexCoord tex)
    {
        if (!wall && !fake && !half && !window)
        {
            return;
        }

        var thickness = half ? 3f : 5f;
        var strip = edge switch
        {
            WallEdge.Left => new RectangleF(rect.Left, rect.Top, thickness, rect.Height),
            WallEdge.Right => new RectangleF(rect.Right - thickness, rect.Top, thickness, rect.Height),
            WallEdge.Top => new RectangleF(rect.Left, rect.Top, rect.Width, thickness),
            _ => new RectangleF(rect.Left, rect.Bottom - thickness, rect.Width, thickness)
        };

        if (Atlas is not null && Atlas.TrySourceRect(tex, out var source))
        {
            graphics.DrawImage(Atlas.Bitmap, strip, source, GraphicsUnit.Pixel);
        }

        var color = window ? Color.DeepSkyBlue : fake ? Color.Silver : half ? Color.Khaki : Color.WhiteSmoke;
        using var pen = new Pen(color, half ? 2 : 4);
        if (fake)
        {
            pen.DashStyle = DashStyle.Dash;
        }

        var left = rect.Left + 1;
        var right = rect.Right - 1;
        var top = rect.Top + 1;
        var bottom = rect.Bottom - 1;
        switch (edge)
        {
            case WallEdge.Left:
                graphics.DrawLine(pen, left, top, left, bottom);
                break;
            case WallEdge.Right:
                graphics.DrawLine(pen, right, top, right, bottom);
                break;
            case WallEdge.Top:
                graphics.DrawLine(pen, left, top, right, top);
                break;
            case WallEdge.Bottom:
                graphics.DrawLine(pen, left, bottom, right, bottom);
                break;
        }
    }

    private int HitTest(Point point)
    {
        var cell = HitCell(point);
        return cell.HasValue ? MapIndexMapper.ToIndex(cell.Value.X, cell.Value.Y, IndexOrder) : -1;
    }

    private Point? HitCell(Point point)
    {
        var rect = GridRect();
        if (Sector is null || !rect.Contains(point))
        {
            return null;
        }

        var cell = rect.Width / (float)SectorMap.Width;
        var x = Math.Clamp((int)((point.X - rect.Left) / cell), 0, SectorMap.Width - 1);
        var y = Math.Clamp((int)((point.Y - rect.Top) / cell), 0, SectorMap.Height - 1);
        return new Point(x, y);
    }

    private static Rectangle NormalizeCells(Point a, Point b)
    {
        var left = Math.Min(a.X, b.X);
        var top = Math.Min(a.Y, b.Y);
        var right = Math.Max(a.X, b.X);
        var bottom = Math.Max(a.Y, b.Y);
        return Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }

    private static void DrawRoomPreview(Graphics graphics, Rectangle gridRect, float cell, Rectangle room)
    {
        var preview = new RectangleF(
            gridRect.Left + room.Left * cell,
            gridRect.Top + room.Top * cell,
            room.Width * cell,
            room.Height * cell);
        using var fill = new SolidBrush(Color.FromArgb(45, 255, 215, 0));
        using var pen = new Pen(Color.Gold, 3) { DashStyle = DashStyle.Dash };
        graphics.FillRectangle(fill, preview);
        graphics.DrawRectangle(pen, preview.X, preview.Y, preview.Width, preview.Height);
    }

    private Rectangle GridRect()
    {
        var size = CellSizePixels * SectorMap.Width;
        size -= size % SectorMap.Width;
        var left = (ClientSize.Width - size) / 2;
        var top = (ClientSize.Height - size) / 2;
        return new Rectangle(left, top, size, size);
    }
}
