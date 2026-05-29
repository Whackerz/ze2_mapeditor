# ZE2 Map Editor

Standalone local map editor for legally owned Zombie Estate 2 map XML files. This project edits source level XMLs only and does not redistribute or modify game binaries or assets.

## Run

From this folder:

```powershell
dotnet run --project .\ZE2MapEditor\ZE2MapEditor.csproj
```

The editor opens `Zombie Estate 2\Levels` by default when that folder exists. Use **Open Map Folder** to choose another folder, such as `Zombie Estate 2\Data\Levels`.

## Open And Edit A Map

1. Choose a map folder.
2. Select a map prefix from the list, such as `Testmap`, `Estate`, or `DesertTown`.
3. Switch sectors with the sector selector. The editor loads `MapName0.xml`, `MapName1.xml`, and optional `MapName2.xml`.
4. Click a tile in the 32x32 grid to inspect it.
5. Use the inspector to edit floor, floor texture, walls, wall texture coordinates, wall flags, and tile props.
6. Use the tools tab to paint floor textures, fill floor rectangles, toggle floors, toggle wall edges, set wall types, place/remove tile props, or drag out room outlines.

If `Assets\MasterWall_Test_Desert.png` exists in your local editor project, the editor draws map textures from 16x16 atlas cells. XML texture coordinates are cell coordinates, so `floorTex X=23, Y=3` draws from pixel rectangle `368,48,16,16`. The public source repo intentionally does not include ZE2 game assets; copy your own legally owned atlas PNG into `ZE2MapEditor\Assets\` if you want the texture picker preview.

Use the map zoom control in the left panel to enlarge or shrink the 32x32 map view. Use the atlas zoom control in the Tools tab to enlarge or shrink the texture picker. In either view, mouse wheel scrolls vertically, Ctrl+wheel zooms toward the cursor, and Shift+wheel scrolls horizontally.

Use Ctrl+Z to undo and Ctrl+Y to redo unsaved editor changes. The Undo and Redo buttons do the same thing.

Special wall types are saved in ZE2's expected format: a fake, half, or window wall clears the normal solid wall flag and sets only its matching special flag. This matters for in-game collision, bullets, and path generation.

Keyboard shortcuts:

- `1`-`7`: select the matching tool from the Tools dropdown.
- `T`, `F`, `G`, `H`: select Top, Left, Bottom, or Right wall edge.
- `Shift+1`-`Shift+4`: select Normal, Fake, Half, or Window wall type.
- `F1`-`F10`: select the matching TPI type and switch to the TPI paint tool.
- Number and edge shortcuts are ignored while typing in text boxes, numeric fields, or dropdowns.

For paint tools, left-click places floors, walls, and TPIs. Right-click removes floors, walls, and TPIs. Dragging with either button paints a line/stroke with that action.

Most grid tools support click-and-drag painting. For toggle-style tools, the first tile in the drag decides whether the whole stroke paints the value on or off. For tile props, choose `ToggleTileProperty`, pick the TPI, then use TPI mode `Add`, `Remove`, or `ToggleStroke` to place or clear props with clicks or drag strokes. The `RoomOutline` tool previews a rectangle while dragging and writes wall edges around the selected rectangle on mouse release. The `FillFloorRect` tool previews the same rectangle shape and fills every tile inside with the selected floor texture.

The index order defaults to `y * 32 + x`, matching the observed ZE2 map layout. The dropdown can still switch between `x * 32 + y` and `y * 32 + x`, and the status bar shows hovered and selected raw XML indices for debugging.

## Save

**Save** writes the currently loaded source XML files back to the same prefix and creates timestamped `.bak` copies first.

**Save As New Prefix** writes the same sectors with a new prefix in the current folder, for example `Estate` to `Testmap` writes `Testmap0.xml`, `Testmap1.xml`, and `Testmap2.xml` when sector 2 exists.

The editor warns before overwriting files in `Data\Levels`. The recommended working folder is `Zombie Estate 2\Levels`.

## Export Generated Files

Use **Export Generated Files** after saving or while working from the current in-memory map. The exporter writes:

- `MapNameN_Ground.xml`
- `MapNameN_Walls.xml`
- `MapNameN_Ground.bin`
- `MapNameN_Walls.bin`
- `MapName_Path.txt` for sector 0

Existing generated files get timestamped `.bak` copies first. The exporter does not generate or modify wave files.

The path export follows ZE2's sector 0 path table shape: 1024 lines with 1024 direction values per line. It treats `NoPath` tiles as blocked, uses walls/half/window walls as movement blockers, treats fake walls as passable, and uses bounds-safe neighbor checks.

## New Blank Map

Use **New Blank Map** to create source XML files in the currently selected map folder. Enter a prefix and choose 1-3 sectors; the editor writes `Prefix0.xml`, `Prefix1.xml`, and optional `Prefix2.xml` with empty 32x32 sectors. If files already exist for that prefix, timestamped backups are created before overwrite.

## Source Files Vs Generated Files

Editable source files:

- `MapName0.xml`
- `MapName1.xml`
- optional `MapName2.xml`

Generated/support files:

- `MapName0_Ground.xml`
- `MapName0_Ground.bin`
- `MapName0_Walls.xml`
- `MapName0_Walls.bin`
- `MapName1_Ground.xml`
- `MapName1_Ground.bin`
- `MapName1_Walls.xml`
- `MapName1_Walls.bin`
- `MapName_Path.txt`
- wave XML files

The editor can now generate wall/ground XML, wall/ground `.bin` cache files, and the sector 0 path file. Wave files are still left untouched.

## Recommended ZE2 Workflow

1. Edit XML in this standalone editor.
2. Save to `Zombie Estate 2\Levels\MapName0.xml` and `MapName1.xml`.
3. Load the map in ZE2.
4. Use **Export Generated Files** in the standalone editor, or use Ctrl+X in the patched game editor to regenerate wall/ground files.
5. Use the standalone export path file, or use Ctrl+P in the patched game editor to regenerate the path file.
6. Test in-game.

## Validate

Run:

```powershell
dotnet run --project .\ZE2MapEditor.Validation\ZE2MapEditor.Validation.csproj
```

The validation command loads discovered source maps from `Data\Levels` and `Levels`, verifies each sector has 1024 tiles, round-trips XML to a temp folder, reloads it, checks wall flag normalization, validates generated export on a representative map, and checks generated/cache files are not detected as source prefixes.
