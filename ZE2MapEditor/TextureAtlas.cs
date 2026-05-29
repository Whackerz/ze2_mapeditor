namespace ZE2MapEditor;

public sealed class TextureAtlas : IDisposable
{
    public const int CellSize = 16;

    private TextureAtlas(Bitmap bitmap, string path)
    {
        Bitmap = bitmap;
        Path = path;
    }

    public Bitmap Bitmap { get; }
    public string Path { get; }
    public int Columns => Bitmap.Width / CellSize;
    public int Rows => Bitmap.Height / CellSize;

    public static TextureAtlas Load(string path)
    {
        using var stream = File.OpenRead(path);
        using var source = new Bitmap(stream);
        return new TextureAtlas(new Bitmap(source), path);
    }

    public bool TrySourceRect(TexCoord coord, out Rectangle source)
    {
        return TrySourceRect(coord.X, coord.Y, out source);
    }

    public bool TrySourceRect(int x, int y, out Rectangle source)
    {
        source = Rectangle.Empty;
        if (x < 0 || y < 0 || x >= Columns || y >= Rows)
        {
            return false;
        }

        source = new Rectangle(x * CellSize, y * CellSize, CellSize, CellSize);
        return true;
    }

    public void Dispose()
    {
        Bitmap.Dispose();
    }
}
