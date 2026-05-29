using ZE2MapEditor;

if (args.Length >= 3 && args[0] == "--export")
{
    var folder = args[1];
    var prefix = args[2];
    var order = args.Length >= 4 && args[3].Equals("x", StringComparison.OrdinalIgnoreCase)
        ? TileIndexOrder.XThenY
        : TileIndexOrder.YThenX;
    var level = LevelMapStore.Load(folder, prefix);
    var result = GeneratedFileExporter.ExportAll(level, order, createBackups: true);
    LevelMapStore.Save(level, createBackups: true);
    Console.WriteLine($"Exported {result.Count} generated file(s) for {prefix}.");
    return 0;
}

var workspace = args.Length > 0 ? args[0] : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
var gameRoot = Path.Combine(workspace, "Zombie Estate 2");
var folders = new[]
{
    Path.Combine(gameRoot, "Data", "Levels"),
    Path.Combine(gameRoot, "Levels")
};

var failures = new List<string>();
var loadedSectors = 0;
var generatedExportValidated = false;

foreach (var folder in folders.Where(Directory.Exists))
{
    var prefixes = MapFolderScanner.FindPrefixes(folder);
    Console.WriteLine($"{folder}: {prefixes.Count} source map prefix(es)");

    if (prefixes.Any(prefix => prefix.Contains("_Ground", StringComparison.OrdinalIgnoreCase) || prefix.Contains("_Walls", StringComparison.OrdinalIgnoreCase)))
    {
        failures.Add($"Generated prefix leaked into scan for {folder}.");
    }

    foreach (var prefix in prefixes)
    {
        try
        {
            var level = LevelMapStore.Load(folder, prefix);
            foreach (var sector in level.Sectors)
            {
                loadedSectors++;
                if (sector.Tiles.Count != SectorMap.TileCount)
                {
                    failures.Add($"{prefix}{sector.Sector}.xml has {sector.Tiles.Count} tiles.");
                }
            }

            RoundTrip(level);
            if (!generatedExportValidated)
            {
                ValidateGeneratedExport(level);
                generatedExportValidated = true;
            }
        }
        catch (Exception ex)
        {
            failures.Add($"{prefix}: {ex.Message}");
        }
    }
}

if (loadedSectors == 0)
{
    failures.Add("No source map sectors were loaded.");
}

try
{
    ValidateWallNormalization();
    ValidateKnownBinPrefix(workspace);
}
catch (Exception ex)
{
    failures.Add($"Additional validation: {ex.Message}");
}

if (failures.Count > 0)
{
    Console.Error.WriteLine("Validation failed:");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine($"- {failure}");
    }

    return 1;
}

Console.WriteLine($"Validation passed. Loaded and round-tripped {loadedSectors} sector(s).");
return 0;

static void RoundTrip(LevelMap level)
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "ZE2MapEditorValidation", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(tempRoot);
    try
    {
        var saved = LevelMapStore.SaveAs(level, level.Prefix, tempRoot);
        var reloaded = LevelMapStore.Load(tempRoot, saved.Prefix);
        if (reloaded.Sectors.Count != level.Sectors.Count)
        {
            throw new InvalidDataException($"Round-trip sector count changed for {level.Prefix}.");
        }

        foreach (var sector in reloaded.Sectors)
        {
            if (sector.Tiles.Count != SectorMap.TileCount)
            {
                throw new InvalidDataException($"Round-trip tile count changed for {level.Prefix}{sector.Sector}.");
            }
        }
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void ValidateGeneratedExport(LevelMap level)
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "ZE2MapEditorGeneratedValidation", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(tempRoot);
    try
    {
        var saved = LevelMapStore.SaveAs(level, level.Prefix, tempRoot);
        var result = GeneratedFileExporter.ExportAll(saved, TileIndexOrder.YThenX, createBackups: true);
        if (result.Count == 0)
        {
            throw new InvalidDataException("Generated export wrote no files.");
        }

        foreach (var sector in saved.Sectors)
        {
            var groundXml = Path.Combine(tempRoot, $"{saved.Prefix}{sector.Sector}_Ground.xml");
            var wallsXml = Path.Combine(tempRoot, $"{saved.Prefix}{sector.Sector}_Walls.xml");
            var groundBin = Path.Combine(tempRoot, $"{saved.Prefix}{sector.Sector}_Ground.bin");
            var wallsBin = Path.Combine(tempRoot, $"{saved.Prefix}{sector.Sector}_Walls.bin");

            if (!File.Exists(groundXml) || !File.Exists(wallsXml) || !File.Exists(groundBin) || !File.Exists(wallsBin))
            {
                throw new FileNotFoundException($"Generated files missing for {saved.Prefix}{sector.Sector}.");
            }

            _ = GeneratedFileExporter.LoadVertexXml(groundXml);
            _ = GeneratedFileExporter.LoadVertexXml(wallsXml);
        }

        var pathFile = Path.Combine(tempRoot, $"{saved.Prefix}_Path.txt");
        var pathBytes = File.ReadAllBytes(pathFile);
        if (pathBytes.Length >= 3 && pathBytes[0] == 0xEF && pathBytes[1] == 0xBB && pathBytes[2] == 0xBF)
        {
            throw new InvalidDataException("Path file was written with a UTF-8 BOM.");
        }

        var lines = File.ReadAllLines(pathFile);
        if (lines.Length != SectorMap.TileCount)
        {
            throw new InvalidDataException($"Path file has {lines.Length} lines; expected {SectorMap.TileCount}.");
        }

        var values = lines[0].Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (values.Length != SectorMap.TileCount)
        {
            throw new InvalidDataException($"Path line has {values.Length} values; expected {SectorMap.TileCount}.");
        }
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void ValidateWallNormalization()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "ZE2MapEditorWallValidation", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(tempRoot);
    try
    {
        var level = LevelMapStore.CreateBlank(tempRoot, "WallFlagTest", 1);
        var tile = level.Sectors[0].Tiles[0];
        tile.leftWall = true;
        tile.leftWallFake = true;
        tile.rightWall = true;
        tile.rightWallHalf = true;
        tile.topWall = true;
        tile.topWallWindow = true;

        LevelMapStore.Save(level, createBackups: true);
        var reloaded = LevelMapStore.Load(tempRoot, "WallFlagTest");
        var savedTile = reloaded.Sectors[0].Tiles[0];
        if (savedTile.leftWall || !savedTile.leftWallFake)
        {
            throw new InvalidDataException("Fake wall saved with normal wall still enabled.");
        }

        if (savedTile.rightWall || !savedTile.rightWallHalf)
        {
            throw new InvalidDataException("Half wall saved with normal wall still enabled.");
        }

        if (savedTile.topWall || !savedTile.topWallWindow)
        {
            throw new InvalidDataException("Window wall saved with normal wall still enabled.");
        }
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void ValidateKnownBinPrefix(string workspace)
{
    var levels = Path.Combine(workspace, "Zombie Estate 2", "Levels");
    var sourceXml = Path.Combine(levels, "FirstMap0.xml");
    var expectedBin = Path.Combine(levels, "FirstMap0_Ground.bin.20260527_191527.bak");
    if (!File.Exists(sourceXml) || !File.Exists(expectedBin))
    {
        return;
    }

    var tempRoot = Path.Combine(Path.GetTempPath(), "ZE2MapEditorBinValidation", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(tempRoot);
    try
    {
        File.Copy(sourceXml, Path.Combine(tempRoot, "FirstMap0.xml"));
        var level = LevelMapStore.Load(tempRoot, "FirstMap");
        GeneratedFileExporter.ExportAll(level, TileIndexOrder.YThenX, createBackups: true);
        var actual = File.ReadAllBytes(Path.Combine(tempRoot, "FirstMap0_Ground.bin"));
        var expected = File.ReadAllBytes(expectedBin);
        var sampleLength = Math.Min(256, Math.Min(actual.Length, expected.Length));
        for (var i = 0; i < sampleLength; i++)
        {
            if (actual[i] != expected[i])
            {
                throw new InvalidDataException($"Generated bin prefix differs from known protobuf-net output at byte {i}: got {actual[i]:X2}, expected {expected[i]:X2}.");
            }
        }
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}
