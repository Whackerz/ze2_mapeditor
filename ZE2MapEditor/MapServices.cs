using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace ZE2MapEditor;

public static class MapIndexMapper
{
    public static int ToIndex(int x, int y, TileIndexOrder order)
    {
        return order == TileIndexOrder.XThenY
            ? x * SectorMap.Height + y
            : y * SectorMap.Width + x;
    }
}

public static class MapFolderScanner
{
    private static readonly Regex SourceSectorRegex = new(@"^(?<prefix>.+?)(?<sector>[0-2])\.xml$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static IReadOnlyList<string> FindPrefixes(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            return Array.Empty<string>();
        }

        return Directory.EnumerateFiles(folderPath, "*0.xml")
            .Select(Path.GetFileName)
            .Where(name => name is not null && !name.Contains("_Ground", StringComparison.OrdinalIgnoreCase) && !name.Contains("_Walls", StringComparison.OrdinalIgnoreCase))
            .Select(name => SourceSectorRegex.Match(name!))
            .Where(match => match.Success && match.Groups["sector"].Value == "0")
            .Select(match => match.Groups["prefix"].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(prefix => prefix, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<int> FindSectors(string folderPath, string prefix)
    {
        return Enumerable.Range(0, 3)
            .Where(sector => File.Exists(Path.Combine(folderPath, $"{prefix}{sector}.xml")))
            .ToList();
    }
}

public static class TileXmlSerializer
{
    private static readonly XmlSerializer Serializer = new(typeof(TileInfoDocument));

    public static SectorMap LoadSector(string path, int sector)
    {
        using var stream = File.OpenRead(path);
        var document = (TileInfoDocument?)Serializer.Deserialize(stream)
            ?? throw new InvalidDataException($"Could not deserialize {path}.");

        if (document.Tiles.Count != SectorMap.TileCount)
        {
            throw new InvalidDataException($"{Path.GetFileName(path)} has {document.Tiles.Count} tiles; expected {SectorMap.TileCount}.");
        }

        foreach (var tile in document.Tiles)
        {
            tile.leftWallTex ??= new TexCoord();
            tile.rightWallTex ??= new TexCoord();
            tile.topWallTex ??= new TexCoord();
            tile.bottomWallTex ??= new TexCoord();
            tile.floorTex ??= new TexCoord();
            tile.tileProps ??= new List<TilePropertyType>();
        }

        return new SectorMap(sector, path, document.Tiles);
    }

    public static void SaveSector(SectorMap sector, string path)
    {
        var document = new TileInfoDocument { Tiles = sector.Tiles };
        var settings = new XmlWriterSettings
        {
            Indent = true,
            OmitXmlDeclaration = false
        };

        var namespaces = new XmlSerializerNamespaces();
        namespaces.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
        namespaces.Add("xsd", "http://www.w3.org/2001/XMLSchema");

        using var writer = XmlWriter.Create(path, settings);
        Serializer.Serialize(writer, document, namespaces);
    }
}

public static class LevelMapStore
{
    public static LevelMap Load(string folderPath, string prefix)
    {
        var sectors = MapFolderScanner.FindSectors(folderPath, prefix)
            .Select(sector => TileXmlSerializer.LoadSector(Path.Combine(folderPath, $"{prefix}{sector}.xml"), sector))
            .ToList();

        if (sectors.Count == 0)
        {
            throw new FileNotFoundException($"No source sectors found for prefix '{prefix}'.");
        }

        return new LevelMap(folderPath, prefix, sectors);
    }

    public static void Save(LevelMap level, bool createBackups)
    {
        WallRules.Normalize(level);
        WallRules.AddBlankOppositeWalls(level);

        foreach (var sector in level.Sectors)
        {
            var path = Path.Combine(level.FolderPath, $"{level.Prefix}{sector.Sector}.xml");
            if (createBackups && File.Exists(path))
            {
                File.Copy(path, BackupPath(path), overwrite: false);
            }

            TileXmlSerializer.SaveSector(sector, path);
            sector.Path = path;
        }
    }

    public static LevelMap SaveAs(LevelMap level, string newPrefix, string destinationFolder)
    {
        var clone = new LevelMap(destinationFolder, newPrefix, level.Sectors);
        Directory.CreateDirectory(destinationFolder);
        Save(clone, createBackups: true);
        return clone;
    }

    public static LevelMap CreateBlank(string folderPath, string prefix, int sectorCount)
    {
        var sectors = Enumerable.Range(0, sectorCount)
            .Select(sector => new SectorMap(sector, Path.Combine(folderPath, $"{prefix}{sector}.xml"), CreateBlankTiles()))
            .ToList();

        return new LevelMap(folderPath, prefix, sectors);
    }

    public static LevelMap Clone(LevelMap level)
    {
        var sectors = level.Sectors
            .Select(sector => new SectorMap(sector.Sector, sector.Path, sector.Tiles.Select(CloneTile).ToList()))
            .ToList();

        return new LevelMap(level.FolderPath, level.Prefix, sectors);
    }

    private static List<TileInfo> CreateBlankTiles()
    {
        return Enumerable.Range(0, SectorMap.TileCount)
            .Select(_ => new TileInfo())
            .ToList();
    }

    private static TileInfo CloneTile(TileInfo tile)
    {
        return new TileInfo
        {
            leftWall = tile.leftWall,
            rightWall = tile.rightWall,
            topWall = tile.topWall,
            bottomWall = tile.bottomWall,
            floor = tile.floor,
            leftWallFake = tile.leftWallFake,
            rightWallFake = tile.rightWallFake,
            topWallFake = tile.topWallFake,
            bottomWallFake = tile.bottomWallFake,
            leftWallHalf = tile.leftWallHalf,
            rightWallHalf = tile.rightWallHalf,
            topWallHalf = tile.topWallHalf,
            bottomWallHalf = tile.bottomWallHalf,
            leftWallWindow = tile.leftWallWindow,
            rightWallWindow = tile.rightWallWindow,
            topWallWindow = tile.topWallWindow,
            bottomWallWindow = tile.bottomWallWindow,
            leftWallTex = CloneTex(tile.leftWallTex),
            rightWallTex = CloneTex(tile.rightWallTex),
            topWallTex = CloneTex(tile.topWallTex),
            bottomWallTex = CloneTex(tile.bottomWallTex),
            floorTex = CloneTex(tile.floorTex),
            tileProps = tile.tileProps.ToList()
        };
    }

    private static TexCoord CloneTex(TexCoord tex)
    {
        return new TexCoord { X = tex.X, Y = tex.Y };
    }

    private static string BackupPath(string path)
    {
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        return $"{path}.{stamp}.bak";
    }
}
