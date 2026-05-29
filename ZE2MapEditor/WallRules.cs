namespace ZE2MapEditor;

public readonly record struct WallState(bool Normal, bool Fake, bool Half, bool Window)
{
    public bool Present => Normal || Fake || Half || Window;
}

public static class WallRules
{
    public static readonly TexCoord BlankWallTexture = new() { X = 31, Y = 31 };

    public static WallState FromEditor(bool present, bool fake, bool half, bool window)
    {
        if (!present)
        {
            return default;
        }

        if (window)
        {
            return new WallState(false, false, false, true);
        }

        if (half)
        {
            return new WallState(false, false, true, false);
        }

        if (fake)
        {
            return new WallState(false, true, false, false);
        }

        return new WallState(true, false, false, false);
    }

    public static WallState FromRaw(bool normal, bool fake, bool half, bool window)
    {
        if (window)
        {
            return new WallState(false, false, false, true);
        }

        if (half)
        {
            return new WallState(false, false, true, false);
        }

        if (fake)
        {
            return new WallState(false, true, false, false);
        }

        return normal ? new WallState(true, false, false, false) : default;
    }

    public static void Normalize(LevelMap level)
    {
        foreach (var sector in level.Sectors)
        {
            foreach (var tile in sector.Tiles)
            {
                Normalize(tile);
            }
        }
    }

    public static void Normalize(TileInfo tile)
    {
        ApplyLeft(tile, FromRaw(tile.leftWall, tile.leftWallFake, tile.leftWallHalf, tile.leftWallWindow));
        ApplyRight(tile, FromRaw(tile.rightWall, tile.rightWallFake, tile.rightWallHalf, tile.rightWallWindow));
        ApplyTop(tile, FromRaw(tile.topWall, tile.topWallFake, tile.topWallHalf, tile.topWallWindow));
        ApplyBottom(tile, FromRaw(tile.bottomWall, tile.bottomWallFake, tile.bottomWallHalf, tile.bottomWallWindow));
    }

    public static void AddBlankOppositeWalls(LevelMap level)
    {
        foreach (var sector in level.Sectors)
        {
            AddBlankOppositeWalls(sector);
        }
    }

    public static void AddBlankOppositeWalls(SectorMap sector)
    {
        for (var y = 0; y < SectorMap.Height; y++)
        {
            for (var x = 0; x < SectorMap.Width; x++)
            {
                var tile = sector.Tiles[MapIndexMapper.ToIndex(x, y, TileIndexOrder.YThenX)];
                if (x > 0 && HasLeftWall(tile))
                {
                    AddMissingWall(sector.Tiles[MapIndexMapper.ToIndex(x - 1, y, TileIndexOrder.YThenX)], WallEdge.Right);
                }

                if (x < SectorMap.Width - 1 && HasRightWall(tile))
                {
                    AddMissingWall(sector.Tiles[MapIndexMapper.ToIndex(x + 1, y, TileIndexOrder.YThenX)], WallEdge.Left);
                }

                if (y > 0 && HasTopWall(tile))
                {
                    AddMissingWall(sector.Tiles[MapIndexMapper.ToIndex(x, y - 1, TileIndexOrder.YThenX)], WallEdge.Bottom);
                }

                if (y < SectorMap.Height - 1 && HasBottomWall(tile))
                {
                    AddMissingWall(sector.Tiles[MapIndexMapper.ToIndex(x, y + 1, TileIndexOrder.YThenX)], WallEdge.Top);
                }
            }
        }
    }

    public static bool HasLeftWall(TileInfo tile)
    {
        return tile.leftWall || tile.leftWallHalf || tile.leftWallWindow;
    }

    public static bool HasRightWall(TileInfo tile)
    {
        return tile.rightWall || tile.rightWallHalf || tile.rightWallWindow;
    }

    public static bool HasTopWall(TileInfo tile)
    {
        return tile.topWall || tile.topWallHalf || tile.topWallWindow;
    }

    public static bool HasBottomWall(TileInfo tile)
    {
        return tile.bottomWall || tile.bottomWallHalf || tile.bottomWallWindow;
    }

    private static void AddMissingWall(TileInfo tile, WallEdge edge)
    {
        switch (edge)
        {
            case WallEdge.Left:
                if (!HasLeftWall(tile))
                {
                    ApplyLeft(tile, new WallState(true, false, false, false));
                    tile.leftWallTex = CloneTex(BlankWallTexture);
                }
                break;
            case WallEdge.Right:
                if (!HasRightWall(tile))
                {
                    ApplyRight(tile, new WallState(true, false, false, false));
                    tile.rightWallTex = CloneTex(BlankWallTexture);
                }
                break;
            case WallEdge.Top:
                if (!HasTopWall(tile))
                {
                    ApplyTop(tile, new WallState(true, false, false, false));
                    tile.topWallTex = CloneTex(BlankWallTexture);
                }
                break;
            case WallEdge.Bottom:
                if (!HasBottomWall(tile))
                {
                    ApplyBottom(tile, new WallState(true, false, false, false));
                    tile.bottomWallTex = CloneTex(BlankWallTexture);
                }
                break;
        }
    }

    private static TexCoord CloneTex(TexCoord tex)
    {
        return new TexCoord { X = tex.X, Y = tex.Y };
    }

    public static void ApplyLeft(TileInfo tile, WallState state)
    {
        tile.leftWall = state.Normal;
        tile.leftWallFake = state.Fake;
        tile.leftWallHalf = state.Half;
        tile.leftWallWindow = state.Window;
    }

    public static void ApplyRight(TileInfo tile, WallState state)
    {
        tile.rightWall = state.Normal;
        tile.rightWallFake = state.Fake;
        tile.rightWallHalf = state.Half;
        tile.rightWallWindow = state.Window;
    }

    public static void ApplyTop(TileInfo tile, WallState state)
    {
        tile.topWall = state.Normal;
        tile.topWallFake = state.Fake;
        tile.topWallHalf = state.Half;
        tile.topWallWindow = state.Window;
    }

    public static void ApplyBottom(TileInfo tile, WallState state)
    {
        tile.bottomWall = state.Normal;
        tile.bottomWallFake = state.Fake;
        tile.bottomWallHalf = state.Half;
        tile.bottomWallWindow = state.Window;
    }
}
