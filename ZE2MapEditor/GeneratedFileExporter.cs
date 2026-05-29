using System.Globalization;
using System.Numerics;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace ZE2MapEditor;

public sealed record GeneratedExportResult(IReadOnlyList<string> WrittenFiles)
{
    public int Count => WrittenFiles.Count;
}

public sealed class CustomLevelVertexDec
{
    public Vector3Value Position { get; set; } = new();
    public Vector2Value TextureCoordinate { get; set; } = new();
    public Vector2Value LightTextureCoordinate { get; set; } = new();
}

public sealed class Vector2Value
{
    public float X { get; set; }
    public float Y { get; set; }
}

public sealed class Vector3Value
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
}

public static class GeneratedFileExporter
{
    private const float TileSetSize = 512f;
    private const float TileSize = 16f;
    private const float Scale = 1f;
    private const float WallThicknessModifier = 0.075f;
    private static readonly XmlSerializer VertexXmlSerializer = new(typeof(CustomLevelVertexDec[]));

    public static GeneratedExportResult ExportAll(LevelMap level, TileIndexOrder order, bool createBackups)
    {
        WallRules.Normalize(level);
        WallRules.AddBlankOppositeWalls(level);

        Directory.CreateDirectory(level.FolderPath);
        var written = new List<string>();
        foreach (var sector in level.Sectors)
        {
            var groundVertices = BuildGroundVertices(level.Prefix, sector, order);
            var wallVertices = BuildWallVertices(level.Prefix, sector, order);
            var stem = Path.Combine(level.FolderPath, $"{level.Prefix}{sector.Sector}");

            WriteVertexXml(stem + "_Ground.xml", groundVertices, createBackups);
            WriteVertexXml(stem + "_Walls.xml", wallVertices, createBackups);
            WriteVertexBin(stem + "_Ground.bin", groundVertices, createBackups);
            WriteVertexBin(stem + "_Walls.bin", wallVertices, createBackups);

            written.Add(stem + "_Ground.xml");
            written.Add(stem + "_Walls.xml");
            written.Add(stem + "_Ground.bin");
            written.Add(stem + "_Walls.bin");
        }

        var mainSector = level.Sectors.FirstOrDefault(s => s.Sector == 0);
        if (mainSector is not null)
        {
            var pathFile = Path.Combine(level.FolderPath, $"{level.Prefix}_Path.txt");
            WritePathFile(pathFile, mainSector, order, createBackups);
            written.Add(pathFile);
        }

        return new GeneratedExportResult(written);
    }

    public static CustomLevelVertexDec[] LoadVertexXml(string path)
    {
        using var stream = File.OpenRead(path);
        return (CustomLevelVertexDec[]?)VertexXmlSerializer.Deserialize(stream)
            ?? throw new InvalidDataException($"Could not deserialize {path}.");
    }

    private static CustomLevelVertexDec[] BuildGroundVertices(string prefix, SectorMap sector, TileIndexOrder order)
    {
        var levelHeight = SectorHeight(prefix, sector.Sector);
        var groups = new List<CustomLevelVertexDec[]>(SectorMap.TileCount);
        for (var y = 0; y < SectorMap.Height; y++)
        {
            for (var x = 0; x < SectorMap.Width; x++)
            {
                var tile = TileAt(sector, order, x, y);
                groups.Add(tile.floor ? BuildFloor(x, y, tile.floorTex.X, tile.floorTex.Y, levelHeight, x, y) : Array.Empty<CustomLevelVertexDec>());
            }
        }

        return FlattenLikeZe2(groups);
    }

    private static CustomLevelVertexDec[] BuildWallVertices(string prefix, SectorMap sector, TileIndexOrder order)
    {
        var levelHeight = SectorHeight(prefix, sector.Sector);
        var generatedTiles = BuildGeneratedTiles(sector, order, includeSurroundingWalls: true);
        var groups = new List<CustomLevelVertexDec[]>(SectorMap.TileCount);
        for (var y = 0; y < SectorMap.Height; y++)
        {
            for (var x = 0; x < SectorMap.Width; x++)
            {
                var tile = generatedTiles[x, y];
                var light = ComputeWallLightCoords(tile);
                var vertices = new List<CustomLevelVertexDec>(144);

                if (WallRules.FromRaw(tile.rightWall, tile.rightWallFake, tile.rightWallHalf, tile.rightWallWindow).Present)
                {
                    vertices.AddRange(AddWall(x, y, tile.rightWallTex.X, tile.rightWallTex.Y, 4.712389f, levelHeight, light));
                }

                if (WallRules.FromRaw(tile.bottomWall, tile.bottomWallFake, tile.bottomWallHalf, tile.bottomWallWindow).Present)
                {
                    vertices.AddRange(AddWall(x, y, tile.bottomWallTex.X, tile.bottomWallTex.Y, 3.1415927f, levelHeight, light));
                }

                if (WallRules.FromRaw(tile.leftWall, tile.leftWallFake, tile.leftWallHalf, tile.leftWallWindow).Present)
                {
                    vertices.AddRange(AddWall(x, y, tile.leftWallTex.X, tile.leftWallTex.Y, 1.5707964f, levelHeight, light));
                }

                if (WallRules.FromRaw(tile.topWall, tile.topWallFake, tile.topWallHalf, tile.topWallWindow).Present)
                {
                    vertices.AddRange(AddWall(x, y, tile.topWallTex.X, tile.topWallTex.Y, 0f, levelHeight, light));
                }

                groups.Add(vertices.ToArray());
            }
        }

        return FlattenLikeZe2(groups);
    }

    private static CustomLevelVertexDec[] FlattenLikeZe2(List<CustomLevelVertexDec[]> groups)
    {
        var total = groups.Sum(group => group.Length);
        var result = new CustomLevelVertexDec[total];
        var cursor = 0;
        for (var i = groups.Count - 1; i >= 0; i--)
        {
            Array.Copy(groups[i], 0, result, cursor, groups[i].Length);
            cursor += groups[i].Length;
        }

        return result;
    }

    private static CustomLevelVertexDec[] BuildFloor(int x, int y, int xCoord, int yCoord, float height, int lightX, int lightY)
    {
        var renderHeight = height != 0f ? height + 0.025f : height + 0.015f;
        var left = x;
        var right = x + Scale;
        var top = y;
        var bottom = y + Scale;
        var texLeft = xCoord * TileSize / TileSetSize;
        var texTop = yCoord * TileSize / TileSetSize;
        var texRight = (xCoord * TileSize + TileSize) / TileSetSize;
        var texBottom = (yCoord * TileSize + TileSize) / TileSetSize;
        var lightLeft = lightX * TileSize / TileSetSize;
        var lightTop = lightY * TileSize / TileSetSize;
        var lightRight = (lightX * TileSize + TileSize) / TileSetSize;
        var lightBottom = (lightY * TileSize + TileSize) / TileSetSize;

        return new[]
        {
            Vertex(left, renderHeight, top, texLeft, texTop, lightLeft, lightTop),
            Vertex(right, renderHeight, top, texRight, texTop, lightRight, lightTop),
            Vertex(right, renderHeight, bottom, texRight, texBottom, lightRight, lightBottom),
            Vertex(left, renderHeight, top, texLeft, texTop, lightLeft, lightTop),
            Vertex(right, renderHeight, bottom, texRight, texBottom, lightRight, lightBottom),
            Vertex(left, renderHeight, bottom, texLeft, texBottom, lightLeft, lightBottom)
        };
    }

    private static CustomLevelVertexDec[] AddWall(int x, int y, int xTexCoord, int yTexCoord, float rotation, float height, TexCoord[] wallLights)
    {
        var offset = rotation == 0f || Math.Abs(rotation - 3.1415927f) < 0.00001f ? 0.001f : 0f;
        var thickness = Scale * WallThicknessModifier;
        var local = new[]
        {
            new Vector3(0f, Scale + offset, thickness),
            new Vector3(0f, 0f, thickness),
            new Vector3(Scale, Scale + offset, thickness),
            new Vector3(Scale, 0f, thickness),
            new Vector3(0f, Scale + offset, 0f),
            new Vector3(Scale, Scale + offset, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(Scale, 0f, 0f)
        };

        var lightIndex = 0;
        var translation = new Vector3(x, height, y);
        if (Math.Abs(rotation - 3.1415927f) < 0.00001f)
        {
            translation = new Vector3(x + 1, height, y + 1);
            lightIndex = 2;
        }
        else if (Math.Abs(rotation - 1.5707964f) < 0.00001f)
        {
            translation = new Vector3(x, height, y + 1);
            lightIndex = 1;
        }
        else if (Math.Abs(rotation - 4.712389f) < 0.00001f)
        {
            translation = new Vector3(x + 1, height, y);
            lightIndex = 3;
        }

        var matrix = Matrix4x4.CreateRotationY(rotation) * Matrix4x4.CreateTranslation(translation);
        var p = local.Select(point => Vector3.Transform(point, matrix)).ToArray();
        var texLeft = xTexCoord * TileSize / TileSetSize;
        var texTop = yTexCoord * TileSize / TileSetSize;
        var texRight = (xTexCoord * TileSize + TileSize) / TileSetSize;
        var texBottom = (yTexCoord * TileSize + TileSize) / TileSetSize;
        var lightCoord = wallLights[lightIndex];
        var lightLeft = lightCoord.X * TileSize / TileSetSize;
        var lightTop = lightCoord.Y * TileSize / TileSetSize;
        var lightRight = (lightCoord.X * TileSize + TileSize) / TileSetSize;
        var lightBottom = (lightCoord.Y * TileSize + TileSize) / TileSetSize;
        var onePixel = 1f / TileSetSize;

        return new[]
        {
            Vertex(p[0], texLeft, texTop, lightLeft, lightTop),
            Vertex(p[1], texLeft, texBottom, lightLeft, lightBottom),
            Vertex(p[2], texRight, texTop, lightRight, lightTop),
            Vertex(p[1], texLeft, texBottom, lightLeft, lightBottom),
            Vertex(p[3], texRight, texBottom, lightRight, lightBottom),
            Vertex(p[2], texRight, texTop, lightRight, lightTop),
            Vertex(p[4], texLeft, texTop, lightLeft, lightTop),
            Vertex(p[5], texRight, texTop, lightRight, lightTop),
            Vertex(p[6], texLeft, texBottom, lightLeft, lightBottom),
            Vertex(p[6], texLeft, texBottom, lightLeft, lightBottom),
            Vertex(p[5], texRight, texTop, lightRight, lightTop),
            Vertex(p[7], texRight, texBottom, lightRight, lightBottom),
            Vertex(p[0], texLeft, texTop, -1f, 0f),
            Vertex(p[5], texLeft + onePixel, texTop + onePixel, -1f, 0f),
            Vertex(p[4], texLeft, texTop + onePixel, -1f, 0f),
            Vertex(p[0], texLeft, texTop, -1f, 0f),
            Vertex(p[2], texLeft + onePixel, texTop, -1f, 0f),
            Vertex(p[5], texLeft, texTop + onePixel, -1f, 0f),
            Vertex(p[1], texLeft, texTop, -1f, 0f),
            Vertex(p[6], texLeft, texTop + onePixel, -1f, 0f),
            Vertex(p[7], texLeft + onePixel, texTop + onePixel, -1f, 0f),
            Vertex(p[1], texLeft, texTop, -1f, 0f),
            Vertex(p[7], texLeft + onePixel, texTop + onePixel, -1f, 0f),
            Vertex(p[3], texLeft + onePixel, texTop, -1f, 0f),
            Vertex(p[0], texLeft, texTop, -1f, 0f),
            Vertex(p[6], texLeft + onePixel, texTop + onePixel, -1f, 0f),
            Vertex(p[1], texLeft, texTop + onePixel, -1f, 0f),
            Vertex(p[4], texLeft + onePixel, texTop, -1f, 0f),
            Vertex(p[6], texLeft + onePixel, texTop + onePixel, -1f, 0f),
            Vertex(p[0], texLeft, texTop, -1f, 0f),
            Vertex(p[2], texLeft, texTop, -1f, 0f),
            Vertex(p[3], texLeft, texTop + onePixel, -1f, 0f),
            Vertex(p[7], texLeft + onePixel, texTop + onePixel, -1f, 0f),
            Vertex(p[5], texLeft + onePixel, texTop, -1f, 0f),
            Vertex(p[2], texLeft, texTop, -1f, 0f),
            Vertex(p[7], texLeft + onePixel, texTop + onePixel, -1f, 0f)
        };
    }

    private static TexCoord[] ComputeWallLightCoords(TileInfo tile)
    {
        var hasFloor = tile.floor && !IsBlankFloor(tile.floorTex);
        var wallLights = Enumerable.Range(0, 4)
            .Select(_ => hasFloor ? new TexCoord { X = 2, Y = 2 } : new TexCoord())
            .ToArray();

        var point = new TexCoord { X = 9, Y = 5 };
        var point2 = new TexCoord { X = 9, Y = 2 };
        var point3 = new TexCoord { X = 13, Y = 4 };
        var point4 = new TexCoord { X = 9, Y = 4 };
        var point5 = new TexCoord { X = 9, Y = 1 };
        var point6 = new TexCoord { X = 13, Y = 3 };
        var top = HasTopWallLight(tile);
        var bottom = HasBottomWallLight(tile);
        var left = HasLeftWallLight(tile);
        var right = HasRightWallLight(tile);

        if (bottom && right)
        {
            wallLights[2] = hasFloor ? point2 : point5;
            wallLights[3] = hasFloor ? point : point4;
        }

        if (bottom && left)
        {
            wallLights[2] = hasFloor ? point : point4;
            wallLights[1] = hasFloor ? point2 : point5;
        }

        if (bottom && right && left)
        {
            wallLights[2] = hasFloor ? point3 : point6;
        }

        if (top && right)
        {
            wallLights[0] = hasFloor ? point : point4;
            wallLights[3] = hasFloor ? point2 : point5;
        }

        if (top && left)
        {
            wallLights[0] = hasFloor ? point2 : point5;
            wallLights[1] = hasFloor ? point : point4;
        }

        if (top && right && left)
        {
            wallLights[0] = hasFloor ? point3 : point6;
        }

        return wallLights;
    }

    private static bool IsBlankFloor(TexCoord tex)
    {
        return (tex.X == 31 && tex.Y == 31) || (tex.X == 63 && tex.Y == 63);
    }

    private static CustomLevelVertexDec Vertex(float x, float y, float z, float texX, float texY, float lightX, float lightY)
    {
        return new CustomLevelVertexDec
        {
            Position = new Vector3Value { X = x, Y = y, Z = z },
            TextureCoordinate = new Vector2Value { X = texX, Y = texY },
            LightTextureCoordinate = new Vector2Value { X = lightX, Y = lightY }
        };
    }

    private static CustomLevelVertexDec Vertex(Vector3 position, float texX, float texY, float lightX, float lightY)
    {
        return Vertex(position.X, position.Y, position.Z, texX, texY, lightX, lightY);
    }

    private static void WriteVertexXml(string path, CustomLevelVertexDec[] vertices, bool createBackups)
    {
        BackupIfNeeded(path, createBackups);
        var settings = new XmlWriterSettings
        {
            Indent = true,
            OmitXmlDeclaration = false
        };

        var namespaces = new XmlSerializerNamespaces();
        namespaces.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
        namespaces.Add("xsd", "http://www.w3.org/2001/XMLSchema");

        using var writer = XmlWriter.Create(path, settings);
        VertexXmlSerializer.Serialize(writer, vertices, namespaces);
    }

    private static void WriteVertexBin(string path, IReadOnlyList<CustomLevelVertexDec> vertices, bool createBackups)
    {
        BackupIfNeeded(path, createBackups);
        using var stream = File.Create(path);
        foreach (var vertex in vertices)
        {
            using var message = new MemoryStream();
            WriteVector3Message(message, 1, vertex.Position);
            WriteVector2Message(message, 2, vertex.TextureCoordinate);
            WriteVector2Message(message, 3, vertex.LightTextureCoordinate);
            WriteTag(stream, 1, 2);
            WriteVarInt(stream, (ulong)message.Length);
            message.Position = 0;
            message.CopyTo(stream);
        }
    }

    private static void WriteVector3Message(Stream stream, int field, Vector3Value value)
    {
        using var message = new MemoryStream();
        WriteFixed32(message, 1, value.X);
        WriteFixed32(message, 2, value.Y);
        WriteFixed32(message, 3, value.Z);
        WriteTag(stream, field, 2);
        WriteVarInt(stream, (ulong)message.Length);
        message.Position = 0;
        message.CopyTo(stream);
    }

    private static void WriteVector2Message(Stream stream, int field, Vector2Value value)
    {
        using var message = new MemoryStream();
        WriteFixed32(message, 1, value.X);
        WriteFixed32(message, 2, value.Y);
        WriteTag(stream, field, 2);
        WriteVarInt(stream, (ulong)message.Length);
        message.Position = 0;
        message.CopyTo(stream);
    }

    private static void WriteFixed32(Stream stream, int field, float value)
    {
        if (value == 0f)
        {
            return;
        }

        WriteTag(stream, field, 5);
        var bytes = BitConverter.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static void WriteTag(Stream stream, int field, int wireType)
    {
        WriteVarInt(stream, (ulong)((field << 3) | wireType));
    }

    private static void WriteVarInt(Stream stream, ulong value)
    {
        while (value >= 0x80)
        {
            stream.WriteByte((byte)(value | 0x80));
            value >>= 7;
        }

        stream.WriteByte((byte)value);
    }

    private static void WritePathFile(string path, SectorMap sector, TileIndexOrder order, bool createBackups)
    {
        BackupIfNeeded(path, createBackups);
        var directions = BuildPathDirections(sector, order);
        using var writer = new StreamWriter(path, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        for (var x = 0; x < SectorMap.Width; x++)
        {
            for (var y = 0; y < SectorMap.Height; y++)
            {
                var sourceIndex = PathIndex(x, y);
                var line = new StringBuilder(SectorMap.TileCount * 3);
                for (var targetX = 0; targetX < SectorMap.Width; targetX++)
                {
                    for (var targetY = 0; targetY < SectorMap.Height; targetY++)
                    {
                        line.Append(directions[sourceIndex, PathIndex(targetX, targetY)].ToString(CultureInfo.InvariantCulture));
                        line.Append(',');
                    }
                }

                writer.WriteLine(line.ToString());
            }
        }
    }

    private static int[,] BuildPathDirections(SectorMap sector, TileIndexOrder order)
    {
        var directions = new int[SectorMap.TileCount, SectorMap.TileCount];
        for (var source = 0; source < SectorMap.TileCount; source++)
        {
            for (var target = 0; target < SectorMap.TileCount; target++)
            {
                directions[source, target] = -1;
            }
        }

        var generatedTiles = BuildGeneratedTiles(sector, order, includeSurroundingWalls: true);
        var blocked = BuildBlockedMap(generatedTiles);
        for (var targetX = 0; targetX < SectorMap.Width; targetX++)
        {
            for (var targetY = 0; targetY < SectorMap.Height; targetY++)
            {
                if (blocked[targetX, targetY])
                {
                    continue;
                }

                var targetIndex = PathIndex(targetX, targetY);
                directions[targetIndex, targetIndex] = 10;
                var parent = RunDijkstraTowardTarget(generatedTiles, blocked, targetX, targetY);
                for (var sourceX = 0; sourceX < SectorMap.Width; sourceX++)
                {
                    for (var sourceY = 0; sourceY < SectorMap.Height; sourceY++)
                    {
                        if (blocked[sourceX, sourceY])
                        {
                            continue;
                        }

                        var sourceIndex = PathIndex(sourceX, sourceY);
                        var parentIndex = parent[sourceIndex];
                        if (parentIndex >= 0)
                        {
                            var (parentX, parentY) = FromPathIndex(parentIndex);
                            directions[sourceIndex, targetIndex] = DirectionFromTo(sourceX, sourceY, parentX, parentY);
                        }
                    }
                }
            }
        }

        return directions;
    }

    private static int[] RunDijkstraTowardTarget(TileInfo[,] tiles, bool[,] blocked, int targetX, int targetY)
    {
        var distances = Enumerable.Repeat(float.PositiveInfinity, SectorMap.TileCount).ToArray();
        var parents = Enumerable.Repeat(-1, SectorMap.TileCount).ToArray();
        var queue = new PriorityQueue<int, float>();
        var targetIndex = PathIndex(targetX, targetY);
        distances[targetIndex] = 0f;
        queue.Enqueue(targetIndex, 0f);

        while (queue.Count > 0)
        {
            queue.TryDequeue(out var currentIndex, out var queuedDistance);
            if (queuedDistance > distances[currentIndex])
            {
                continue;
            }

            var (x, y) = FromPathIndex(currentIndex);
            for (var angle = 0; angle < 8; angle++)
            {
                if (!TryNeighbor(tiles, blocked, x, y, angle, out var nx, out var ny))
                {
                    continue;
                }

                var neighborIndex = PathIndex(nx, ny);
                var nextDistance = distances[currentIndex] + (angle % 2 == 1 ? 1.41f : 1f);
                if (nextDistance >= distances[neighborIndex])
                {
                    continue;
                }

                distances[neighborIndex] = nextDistance;
                parents[neighborIndex] = currentIndex;
                queue.Enqueue(neighborIndex, nextDistance);
            }
        }

        return parents;
    }

    private static bool TryNeighbor(TileInfo[,] tiles, bool[,] blocked, int x, int y, int angle, out int nx, out int ny)
    {
        nx = x;
        ny = y;
        var center = tiles[x, y];
        switch (angle)
        {
            case 0:
                ny = y - 1;
                return InBounds(nx, ny) && !blocked[nx, ny] && !HasTopWall(center);
            case 1:
                nx = x + 1;
                ny = y - 1;
                return InBounds(nx, ny) && InBounds(x, y - 1) && InBounds(x + 1, y) && !blocked[nx, ny]
                    && !HasTopWall(center)
                    && !HasRightWall(tiles[x, y - 1])
                    && !HasRightWall(center)
                    && !HasTopWall(tiles[x + 1, y]);
            case 2:
                nx = x + 1;
                return InBounds(nx, ny) && !blocked[nx, ny] && !HasRightWall(center);
            case 3:
                nx = x + 1;
                ny = y + 1;
                return InBounds(nx, ny) && InBounds(x, y + 1) && InBounds(x + 1, y) && !blocked[nx, ny]
                    && !HasBottomWall(center)
                    && !HasRightWall(tiles[x, y + 1])
                    && !HasRightWall(center)
                    && !HasBottomWall(tiles[x + 1, y]);
            case 4:
                ny = y + 1;
                return InBounds(nx, ny) && !blocked[nx, ny] && !HasBottomWall(center);
            case 5:
                nx = x - 1;
                ny = y + 1;
                return InBounds(nx, ny) && InBounds(x, y + 1) && InBounds(x - 1, y) && !blocked[nx, ny]
                    && !HasBottomWall(center)
                    && !HasLeftWall(tiles[x, y + 1])
                    && !HasLeftWall(center)
                    && !HasBottomWall(tiles[x - 1, y]);
            case 6:
                nx = x - 1;
                return InBounds(nx, ny) && !blocked[nx, ny] && !HasLeftWall(center);
            case 7:
                nx = x - 1;
                ny = y - 1;
                return InBounds(nx, ny) && InBounds(x, y - 1) && InBounds(x - 1, y) && !blocked[nx, ny]
                    && !HasTopWall(center)
                    && !HasLeftWall(tiles[x, y - 1])
                    && !HasLeftWall(center)
                    && !HasTopWall(tiles[x - 1, y]);
            default:
                return false;
        }
    }

    private static bool[,] BuildBlockedMap(TileInfo[,] generatedTiles)
    {
        var blocked = new bool[SectorMap.Width, SectorMap.Height];
        for (var y = 0; y < SectorMap.Height; y++)
        {
            for (var x = 0; x < SectorMap.Width; x++)
            {
                blocked[x, y] = generatedTiles[x, y].tileProps.Contains(TilePropertyType.NoPath);
            }
        }

        return blocked;
    }

    private static TileInfo[,] BuildGeneratedTiles(SectorMap sector, TileIndexOrder order, bool includeSurroundingWalls)
    {
        var tiles = new TileInfo[SectorMap.Width, SectorMap.Height];
        for (var y = 0; y < SectorMap.Height; y++)
        {
            for (var x = 0; x < SectorMap.Width; x++)
            {
                tiles[x, y] = CloneTile(TileAt(sector, order, x, y));
            }
        }

        if (includeSurroundingWalls)
        {
            AddSurroundingWalls(tiles);
        }

        return tiles;
    }

    private static void AddSurroundingWalls(TileInfo[,] tiles)
    {
        var blankTex = new TexCoord { X = 63, Y = 63 };
        for (var i = 0; i < SectorMap.Width; i++)
        {
            ApplyGeneratedWall(tiles[i, 0], WallEdge.Top, blankTex);
            ApplyGeneratedWall(tiles[i, SectorMap.Height - 1], WallEdge.Bottom, blankTex);
            ApplyGeneratedWall(tiles[0, i], WallEdge.Left, blankTex);
            ApplyGeneratedWall(tiles[SectorMap.Width - 1, i], WallEdge.Right, blankTex);
        }
    }

    private static void ApplyGeneratedWall(TileInfo tile, WallEdge edge, TexCoord tex)
    {
        var clonedTex = new TexCoord { X = tex.X, Y = tex.Y };
        switch (edge)
        {
            case WallEdge.Left:
                var left = new WallState(!tile.leftWall, false, false, false);
                WallRules.ApplyLeft(tile, left);
                tile.leftWallTex = clonedTex;
                break;
            case WallEdge.Right:
                var right = new WallState(!tile.rightWall, false, false, false);
                WallRules.ApplyRight(tile, right);
                tile.rightWallTex = clonedTex;
                break;
            case WallEdge.Top:
                var top = new WallState(!tile.topWall, false, false, false);
                WallRules.ApplyTop(tile, top);
                tile.topWallTex = clonedTex;
                break;
            case WallEdge.Bottom:
                var bottom = new WallState(!tile.bottomWall, false, false, false);
                WallRules.ApplyBottom(tile, bottom);
                tile.bottomWallTex = clonedTex;
                break;
        }
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

    private static int DirectionFromTo(int x, int y, int nextX, int nextY)
    {
        if (nextX == x + 1 && nextY == y)
        {
            return 0;
        }

        if (nextX == x + 1 && nextY == y - 1)
        {
            return 1;
        }

        if (nextX == x && nextY == y - 1)
        {
            return 2;
        }

        if (nextX == x - 1 && nextY == y - 1)
        {
            return 3;
        }

        if (nextX == x - 1 && nextY == y)
        {
            return 4;
        }

        if (nextX == x - 1 && nextY == y + 1)
        {
            return 5;
        }

        if (nextX == x && nextY == y + 1)
        {
            return 6;
        }

        if (nextX == x + 1 && nextY == y + 1)
        {
            return 7;
        }

        return nextX == x && nextY == y ? 10 : -1;
    }

    private static TileInfo TileAt(SectorMap sector, TileIndexOrder order, int x, int y)
    {
        return sector.Tiles[MapIndexMapper.ToIndex(x, y, order)];
    }

    private static bool InBounds(int x, int y)
    {
        return x >= 0 && x < SectorMap.Width && y >= 0 && y < SectorMap.Height;
    }

    private static int PathIndex(int x, int y)
    {
        return x * SectorMap.Height + y;
    }

    private static (int X, int Y) FromPathIndex(int index)
    {
        return (index / SectorMap.Height, index % SectorMap.Height);
    }

    private static bool HasTopWall(TileInfo tile)
    {
        return tile.topWall || tile.topWallHalf || tile.topWallWindow;
    }

    private static bool HasBottomWall(TileInfo tile)
    {
        return tile.bottomWall || tile.bottomWallHalf || tile.bottomWallWindow;
    }

    private static bool HasLeftWall(TileInfo tile)
    {
        return tile.leftWall || tile.leftWallHalf || tile.leftWallWindow;
    }

    private static bool HasRightWall(TileInfo tile)
    {
        return tile.rightWall || tile.rightWallHalf || tile.rightWallWindow;
    }

    private static bool HasTopWallLight(TileInfo tile)
    {
        return tile.topWall || tile.topWallWindow;
    }

    private static bool HasBottomWallLight(TileInfo tile)
    {
        return tile.bottomWall || tile.bottomWallWindow;
    }

    private static bool HasLeftWallLight(TileInfo tile)
    {
        return tile.leftWall || tile.leftWallWindow;
    }

    private static bool HasRightWallLight(TileInfo tile)
    {
        return tile.rightWall || tile.rightWallWindow;
    }

    private static float SectorHeight(string prefix, int sector)
    {
        return MainSectorRendersOnTop(prefix) ? -sector : sector;
    }

    private static bool MainSectorRendersOnTop(string prefix)
    {
        return prefix.Equals("Mall", StringComparison.OrdinalIgnoreCase)
            || prefix.Equals("Skyscraper", StringComparison.OrdinalIgnoreCase)
            || prefix.Equals("Office", StringComparison.OrdinalIgnoreCase);
    }

    private static void BackupIfNeeded(string path, bool createBackups)
    {
        if (!createBackups || !File.Exists(path))
        {
            return;
        }

        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        File.Copy(path, $"{path}.{stamp}.bak", overwrite: false);
    }
}
