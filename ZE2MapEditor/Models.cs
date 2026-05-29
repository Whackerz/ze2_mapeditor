using System.Xml.Serialization;

namespace ZE2MapEditor;

[XmlRoot("ArrayOfTileInfo")]
public sealed class TileInfoDocument
{
    [XmlElement("TileInfo")]
    public List<TileInfo> Tiles { get; set; } = new();
}

public sealed class TileInfo
{
    public bool leftWall { get; set; }
    public bool rightWall { get; set; }
    public bool topWall { get; set; }
    public bool bottomWall { get; set; }
    public bool floor { get; set; }
    public bool leftWallFake { get; set; }
    public bool rightWallFake { get; set; }
    public bool topWallFake { get; set; }
    public bool bottomWallFake { get; set; }
    public bool leftWallHalf { get; set; }
    public bool rightWallHalf { get; set; }
    public bool topWallHalf { get; set; }
    public bool bottomWallHalf { get; set; }
    public bool leftWallWindow { get; set; }
    public bool rightWallWindow { get; set; }
    public bool topWallWindow { get; set; }
    public bool bottomWallWindow { get; set; }
    public TexCoord leftWallTex { get; set; } = new();
    public TexCoord rightWallTex { get; set; } = new();
    public TexCoord topWallTex { get; set; } = new();
    public TexCoord bottomWallTex { get; set; } = new();
    public TexCoord floorTex { get; set; } = new();

    [XmlArray("tileProps")]
    [XmlArrayItem("TilePropertyType")]
    public List<TilePropertyType> tileProps { get; set; } = new();
}

public sealed class TexCoord
{
    public int X { get; set; }
    public int Y { get; set; }
}

public enum TilePropertyType
{
    NoPath,
    CarePackageSpawn,
    ShopKeep,
    NoSpawn,
    BossArea,
    DemonFire,
    PlayerOneSpawn,
    PlayerTwoSpawn,
    PlayerThreeSpawn,
    PlayerFourSpawn
}

public sealed class SectorMap
{
    public const int Width = 32;
    public const int Height = 32;
    public const int TileCount = Width * Height;

    public SectorMap(int sector, string path, List<TileInfo> tiles)
    {
        Sector = sector;
        Path = path;
        Tiles = tiles;
    }

    public int Sector { get; }
    public string Path { get; set; }
    public List<TileInfo> Tiles { get; }
}

public sealed class LevelMap
{
    public LevelMap(string folderPath, string prefix, IReadOnlyList<SectorMap> sectors)
    {
        FolderPath = folderPath;
        Prefix = prefix;
        Sectors = sectors.OrderBy(s => s.Sector).ToList();
    }

    public string FolderPath { get; set; }
    public string Prefix { get; set; }
    public List<SectorMap> Sectors { get; }
}

public enum TileIndexOrder
{
    XThenY,
    YThenX
}

public enum WallEdge
{
    Left,
    Right,
    Top,
    Bottom
}

public enum WallPaintType
{
    Normal,
    Fake,
    Half,
    Window
}

public enum PaintTool
{
    Select,
    PaintFloorTexture,
    ToggleFloor,
    ToggleWall,
    ToggleTileProperty,
    RoomOutline,
    FillFloorRect
}

public enum PaintStrokePhase
{
    Begin,
    Continue
}

public enum TilePropertyPaintMode
{
    Add,
    Remove,
    ToggleStroke
}
