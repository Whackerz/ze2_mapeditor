namespace ZE2MapEditor;

public sealed class MainForm : Form
{
    private readonly TextBox folderText = new();
    private readonly ListBox mapList = new();
    private readonly ComboBox sectorCombo = new();
    private readonly ComboBox orderCombo = new();
    private readonly TileGridControl grid = new();
    private readonly Panel mapScrollPanel = new();
    private readonly Panel atlasScrollPanel = new();
    private readonly Label statusLabel = new();
    private readonly Label atlasLabel = new();
    private readonly Button newMapButton = new();
    private readonly Button undoButton = new();
    private readonly Button redoButton = new();
    private readonly Button saveButton = new();
    private readonly Button saveAsButton = new();
    private readonly Button exportButton = new();
    private readonly NumericUpDown mapZoom = ZoomBox();

    private readonly CheckBox floorCheck = new();
    private readonly NumericUpDown floorX = NumberBox();
    private readonly NumericUpDown floorY = NumberBox();
    private readonly ComboBox wallEdgeCombo = new();
    private readonly CheckBox wallCheck = new();
    private readonly CheckBox fakeCheck = new();
    private readonly CheckBox halfCheck = new();
    private readonly CheckBox windowCheck = new();
    private readonly NumericUpDown wallX = NumberBox();
    private readonly NumericUpDown wallY = NumberBox();
    private readonly CheckedListBox propList = new();

    private readonly ComboBox toolCombo = new();
    private readonly NumericUpDown paintFloorX = NumberBox();
    private readonly NumericUpDown paintFloorY = NumberBox();
    private readonly ComboBox paintWallEdgeCombo = new();
    private readonly ComboBox paintWallTypeCombo = new();
    private readonly ComboBox paintPropCombo = new();
    private readonly ComboBox paintPropModeCombo = new();
    private readonly AtlasPickerControl atlasPicker = new();
    private readonly Label selectedTextureLabel = new();
    private readonly NumericUpDown atlasZoom = ZoomBox();

    private LevelMap? level;
    private SectorMap? sector;
    private TextureAtlas? atlas;
    private bool loadingInspector;
    private bool dirty;
    private bool? strokePropState;
    private readonly Stack<LevelMap> undoHistory = new();
    private readonly Stack<LevelMap> redoHistory = new();
    private bool restoringHistory;

    public MainForm()
    {
        Text = "ZE2 Map Editor";
        KeyPreview = true;
        MinimumSize = new Size(1180, 760);
        Size = new Size(1380, 860);
        StartPosition = FormStartPosition.CenterScreen;

        BuildLayout();
        WireEvents();
        InitializeDefaults();
    }

    private static NumericUpDown NumberBox()
    {
        return new NumericUpDown
        {
            Minimum = 0,
            Maximum = 999,
            Width = 72
        };
    }

    private static NumericUpDown ZoomBox()
    {
        return new NumericUpDown
        {
            Minimum = 50,
            Maximum = 400,
            Increment = 25,
            Value = 100,
            Width = 72
        };
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
            Padding = new Padding(8)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        Controls.Add(root);

        root.Controls.Add(BuildLeftPanel(), 0, 0);
        root.Controls.Add(BuildMapPanel(), 1, 0);
        root.Controls.Add(BuildRightPanel(), 2, 0);
        root.SetColumnSpan(statusLabel, 3);
        statusLabel.Dock = DockStyle.Fill;
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        root.Controls.Add(statusLabel, 0, 1);
    }

    private Control BuildMapPanel()
    {
        mapScrollPanel.Dock = DockStyle.Fill;
        mapScrollPanel.AutoScroll = true;
        mapScrollPanel.BackColor = Color.FromArgb(20, 22, 25);
        mapScrollPanel.TabStop = true;
        grid.Location = new Point(8, 8);
        mapScrollPanel.Controls.Add(grid);
        return mapScrollPanel;
    }

    private Control BuildLeftPanel()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 16, ColumnCount = 1 };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var folderButton = new Button { Text = "Open Map Folder", Dock = DockStyle.Top, Height = 32 };
        folderButton.Click += (_, _) => PickFolder();
        folderText.ReadOnly = true;
        folderText.Dock = DockStyle.Top;
        mapList.Dock = DockStyle.Fill;

        sectorCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        orderCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        newMapButton.Text = "New Blank Map";
        undoButton.Text = "Undo";
        redoButton.Text = "Redo";
        saveButton.Text = "Save";
        saveAsButton.Text = "Save As New Prefix";
        exportButton.Text = "Export Generated Files";
        atlasLabel.AutoSize = false;
        atlasLabel.Height = 36;
        atlasLabel.TextAlign = ContentAlignment.MiddleLeft;
        foreach (var button in new[] { newMapButton, undoButton, redoButton, saveButton, saveAsButton, exportButton })
        {
            button.Height = 32;
            button.Dock = DockStyle.Top;
        }

        panel.Controls.Add(folderButton);
        panel.Controls.Add(folderText);
        panel.Controls.Add(mapList);
        panel.Controls.Add(new Label { Text = "Sector", Padding = new Padding(0, 8, 0, 2) });
        panel.Controls.Add(sectorCombo);
        panel.Controls.Add(new Label { Text = "Index order", Padding = new Padding(0, 8, 0, 2) });
        panel.Controls.Add(orderCombo);
        panel.Controls.Add(new Label { Text = "Map zoom %", Padding = new Padding(0, 8, 0, 2) });
        panel.Controls.Add(mapZoom);
        panel.Controls.Add(atlasLabel);
        panel.Controls.Add(newMapButton);
        panel.Controls.Add(undoButton);
        panel.Controls.Add(redoButton);
        panel.Controls.Add(saveButton);
        panel.Controls.Add(saveAsButton);
        panel.Controls.Add(exportButton);
        return panel;
    }

    private Control BuildRightPanel()
    {
        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(new TabPage("Inspector") { Controls = { BuildInspectorPanel() } });
        tabs.TabPages.Add(new TabPage("Tools") { Controls = { BuildToolsPanel() } });
        tabs.TabPages.Add(new TabPage("Help") { Controls = { BuildHelpPanel() } });
        return tabs;
    }

    private Control BuildInspectorPanel()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(8)
        };

        floorCheck.Text = "Floor";
        panel.Controls.Add(floorCheck);
        panel.Controls.Add(Row("Floor tex X/Y", floorX, floorY));

        wallEdgeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        panel.Controls.Add(Row("Wall edge", wallEdgeCombo));
        wallCheck.Text = "Wall present";
        fakeCheck.Text = "Fake";
        halfCheck.Text = "Half";
        windowCheck.Text = "Window";
        panel.Controls.Add(wallCheck);
        panel.Controls.Add(fakeCheck);
        panel.Controls.Add(halfCheck);
        panel.Controls.Add(windowCheck);
        panel.Controls.Add(Row("Wall tex X/Y", wallX, wallY));

        panel.Controls.Add(new Label { Text = "Tile props", AutoSize = true, Padding = new Padding(0, 10, 0, 2) });
        propList.CheckOnClick = true;
        propList.Height = 220;
        propList.Width = 285;
        panel.Controls.Add(propList);
        return panel;
    }

    private Control BuildToolsPanel()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(8)
        };

        toolCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        paintWallEdgeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        paintWallTypeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        paintPropCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        paintPropModeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        selectedTextureLabel.AutoSize = false;
        selectedTextureLabel.Width = 285;
        selectedTextureLabel.Height = 24;

        panel.Controls.Add(Row("Click tool", toolCombo));
        panel.Controls.Add(selectedTextureLabel);
        panel.Controls.Add(Row("Atlas zoom %", atlasZoom));
        panel.Controls.Add(Row("Paint floor tex X/Y", paintFloorX, paintFloorY));
        panel.Controls.Add(Row("Wall edge", paintWallEdgeCombo));
        panel.Controls.Add(Row("Wall type", paintWallTypeCombo));
        panel.Controls.Add(Row("TPI", paintPropCombo));
        panel.Controls.Add(Row("TPI mode", paintPropModeCombo));
        panel.Controls.Add(new Label { Text = "Texture atlas", AutoSize = true, Padding = new Padding(0, 8, 0, 2) });
        atlasScrollPanel.AutoScroll = true;
        atlasScrollPanel.Width = 300;
        atlasScrollPanel.Height = 360;
        atlasScrollPanel.BorderStyle = BorderStyle.FixedSingle;
        atlasScrollPanel.TabStop = true;
        atlasScrollPanel.Controls.Add(atlasPicker);
        panel.Controls.Add(atlasScrollPanel);
        panel.Controls.Add(new Label
        {
            Text = "Drag rectangle tools on the map. Wheel scrolls, Ctrl+wheel zooms.",
            AutoSize = false,
            Width = 285,
            Height = 54
        });
        return panel;
    }

    private Control BuildHelpPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            Padding = new Padding(10)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddHelpHeader(panel, "Tools");
        AddHelpRow(panel, "1", "Select");
        AddHelpRow(panel, "2", "Paint floor texture");
        AddHelpRow(panel, "3", "Place/remove floor");
        AddHelpRow(panel, "4", "Place/remove wall");
        AddHelpRow(panel, "5", "Place/remove TPI");
        AddHelpRow(panel, "6", "Room outline");
        AddHelpRow(panel, "7", "Fill floor rectangle");

        AddHelpHeader(panel, "Wall Edge");
        AddHelpRow(panel, "T", "Top");
        AddHelpRow(panel, "F", "Left");
        AddHelpRow(panel, "G", "Bottom");
        AddHelpRow(panel, "H", "Right");

        AddHelpHeader(panel, "Wall Type");
        AddHelpRow(panel, "Shift+1", "Normal");
        AddHelpRow(panel, "Shift+2", "Fake");
        AddHelpRow(panel, "Shift+3", "Half");
        AddHelpRow(panel, "Shift+4", "Window");

        AddHelpHeader(panel, "TPI");
        AddHelpRow(panel, "F1-F10", "Select TPI and switch to TPI paint");

        AddHelpHeader(panel, "Editing");
        AddHelpRow(panel, "Left click", "Place floors, walls, TPIs");
        AddHelpRow(panel, "Right click", "Remove floors, walls, TPIs");
        AddHelpRow(panel, "Drag", "Paint or remove a stroke");
        AddHelpRow(panel, "Ctrl+Z", "Undo");
        AddHelpRow(panel, "Ctrl+Y", "Redo");

        AddHelpHeader(panel, "View");
        AddHelpRow(panel, "Wheel", "Scroll vertically");
        AddHelpRow(panel, "Ctrl+Wheel", "Zoom toward cursor");
        AddHelpRow(panel, "Shift+Wheel", "Scroll horizontally");

        AddHelpHeader(panel, "Export");
        AddHelpRow(panel, "Button", "Export wall/ground XML, .bin caches, and path");

        return new Panel { Dock = DockStyle.Fill, AutoScroll = true, Controls = { panel } };
    }

    private static void AddHelpHeader(TableLayoutPanel panel, string text)
    {
        var label = new Label
        {
            Text = text,
            AutoSize = true,
            Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
            Padding = new Padding(0, 10, 0, 2)
        };
        panel.Controls.Add(label);
        panel.SetColumnSpan(label, 2);
    }

    private static void AddHelpRow(TableLayoutPanel panel, string keys, string action)
    {
        panel.Controls.Add(new Label { Text = keys, AutoSize = true, Padding = new Padding(0, 2, 8, 2) });
        panel.Controls.Add(new Label { Text = action, AutoSize = true, Padding = new Padding(0, 2, 0, 2) });
    }

    private static Control Row(string label, params Control[] controls)
    {
        var row = new FlowLayoutPanel { AutoSize = true, Width = 300, Height = 32 };
        row.Controls.Add(new Label { Text = label, Width = 110, TextAlign = ContentAlignment.MiddleLeft });
        foreach (var control in controls)
        {
            row.Controls.Add(control);
        }

        return row;
    }

    private void WireEvents()
    {
        mapList.SelectedIndexChanged += (_, _) => LoadSelectedPrefix();
        sectorCombo.SelectedIndexChanged += (_, _) => SelectSector();
        orderCombo.SelectedIndexChanged += (_, _) => UpdateIndexOrder();
        grid.TileSelected += SelectTile;
        grid.TilePaintRequested += PaintTileFromTool;
        grid.TileHovered += index => UpdateStatus(index);
        grid.RectangleCommitted += ApplyRectangleTool;
        newMapButton.Click += (_, _) => CreateNewBlankMap();
        undoButton.Click += (_, _) => Undo();
        redoButton.Click += (_, _) => Redo();
        saveButton.Click += (_, _) => SaveCurrent();
        saveAsButton.Click += (_, _) => SaveAsNewPrefix();
        exportButton.Click += (_, _) => ExportGeneratedFiles();
        atlasPicker.TextureSelected += SelectAtlasTexture;
        paintFloorX.ValueChanged += (_, _) => UpdateSelectedTextureLabel();
        paintFloorY.ValueChanged += (_, _) => UpdateSelectedTextureLabel();
        toolCombo.SelectedIndexChanged += (_, _) => UpdateToolMode();
        mapZoom.ValueChanged += (_, _) => UpdateMapZoom();
        atlasZoom.ValueChanged += (_, _) => UpdateAtlasZoom();
        mapScrollPanel.MouseWheel += (sender, e) => HandleViewportWheel((Control)sender!, mapScrollPanel, grid, mapZoom, e, new Point(8, 8));
        grid.MouseWheel += (sender, e) => HandleViewportWheel((Control)sender!, mapScrollPanel, grid, mapZoom, e, new Point(8, 8));
        atlasScrollPanel.MouseWheel += (sender, e) => HandleViewportWheel((Control)sender!, atlasScrollPanel, atlasPicker, atlasZoom, e, Point.Empty);
        atlasPicker.MouseWheel += (sender, e) => HandleViewportWheel((Control)sender!, atlasScrollPanel, atlasPicker, atlasZoom, e, Point.Empty);
        mapScrollPanel.MouseEnter += (_, _) => mapScrollPanel.Focus();
        grid.MouseEnter += (_, _) => mapScrollPanel.Focus();
        atlasScrollPanel.MouseEnter += (_, _) => atlasScrollPanel.Focus();
        atlasPicker.MouseEnter += (_, _) => atlasScrollPanel.Focus();

        floorCheck.CheckedChanged += (_, _) => ApplyInspector();
        floorX.ValueChanged += (_, _) => ApplyInspector();
        floorY.ValueChanged += (_, _) => ApplyInspector();
        wallEdgeCombo.SelectedIndexChanged += (_, _) => LoadInspector();
        wallCheck.CheckedChanged += (_, _) => ApplyInspector();
        fakeCheck.CheckedChanged += (_, _) => ApplyInspector();
        halfCheck.CheckedChanged += (_, _) => ApplyInspector();
        windowCheck.CheckedChanged += (_, _) => ApplyInspector();
        wallX.ValueChanged += (_, _) => ApplyInspector();
        wallY.ValueChanged += (_, _) => ApplyInspector();
        propList.ItemCheck += (_, e) =>
        {
            if (loadingInspector)
            {
                return;
            }

            BeginInvoke(new Action(ApplyInspector));
        };
        KeyDown += HandleKeyDown;
    }

    private void InitializeDefaults()
    {
        wallEdgeCombo.Items.AddRange(Enum.GetNames<WallEdge>());
        paintWallEdgeCombo.Items.AddRange(Enum.GetNames<WallEdge>());
        paintWallTypeCombo.Items.AddRange(Enum.GetNames<WallPaintType>());
        propList.Items.AddRange(Enum.GetNames<TilePropertyType>());
        paintPropCombo.Items.AddRange(Enum.GetNames<TilePropertyType>());
        paintPropModeCombo.Items.AddRange(Enum.GetNames<TilePropertyPaintMode>());
        toolCombo.Items.AddRange(Enum.GetNames<PaintTool>());
        orderCombo.Items.Add("x * 32 + y");
        orderCombo.Items.Add("y * 32 + x");

        wallEdgeCombo.SelectedIndex = 0;
        paintWallEdgeCombo.SelectedIndex = 0;
        paintWallTypeCombo.SelectedIndex = 0;
        paintPropCombo.SelectedIndex = 0;
        paintPropModeCombo.SelectedIndex = 0;
        toolCombo.SelectedIndex = 0;
        orderCombo.SelectedIndex = 1;
        UpdateToolMode();
        UpdateSelectedTextureLabel();
        UpdateMapZoom();
        UpdateAtlasZoom();
        UpdateHistoryButtons();

        LoadBundledAtlas();

        var defaultFolder = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Zombie Estate 2", "Levels");
        defaultFolder = Path.GetFullPath(defaultFolder);
        if (Directory.Exists(defaultFolder))
        {
            OpenFolder(defaultFolder);
        }
        else
        {
            UpdateStatus(-1);
        }
    }

    private void PickFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select a ZE2 map folder such as Zombie Estate 2\\Levels.",
            SelectedPath = Directory.Exists(folderText.Text) ? folderText.Text : Environment.CurrentDirectory,
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            OpenFolder(dialog.SelectedPath);
        }
    }

    private void LoadBundledAtlas()
    {
        var atlasPath = Path.Combine(AppContext.BaseDirectory, "Assets", "MasterWall_Test_Desert.png");
        if (!File.Exists(atlasPath))
        {
            atlasLabel.Text = "Atlas: not found";
            return;
        }

        atlas?.Dispose();
        atlas = TextureAtlas.Load(atlasPath);
        grid.Atlas = atlas;
        atlasPicker.Atlas = atlas;
        atlasPicker.UpdateSize();
        UpdateAtlasZoom();
        atlasLabel.Text = $"Atlas: {atlas.Columns}x{atlas.Rows} cells";
        atlasPicker.Invalidate();
    }

    private void SelectAtlasTexture(int x, int y)
    {
        paintFloorX.Value = x;
        paintFloorY.Value = y;
        UpdateSelectedTextureLabel();
    }

    private void OpenFolder(string path)
    {
        folderText.Text = path;
        mapList.Items.Clear();
        foreach (var prefix in MapFolderScanner.FindPrefixes(path))
        {
            mapList.Items.Add(prefix);
        }

        statusLabel.Text = mapList.Items.Count == 0 ? "No source map prefixes found." : $"Found {mapList.Items.Count} map prefix(es).";
        if (mapList.Items.Count > 0)
        {
            mapList.SelectedIndex = 0;
        }
    }

    private void LoadSelectedPrefix()
    {
        if (mapList.SelectedItem is not string prefix || string.IsNullOrWhiteSpace(folderText.Text))
        {
            return;
        }

        try
        {
            level = LevelMapStore.Load(folderText.Text, prefix);
            dirty = false;
            ClearHistory();
            sectorCombo.Items.Clear();
            foreach (var loadedSector in level.Sectors)
            {
                sectorCombo.Items.Add(loadedSector.Sector);
            }

            sectorCombo.SelectedIndex = 0;
            Text = $"ZE2 Map Editor - {prefix}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Load failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SelectSector()
    {
        if (level is null || sectorCombo.SelectedItem is not int sectorNumber)
        {
            return;
        }

        sector = level.Sectors.Single(s => s.Sector == sectorNumber);
        grid.Sector = sector;
        grid.SelectedIndex = -1;
        LoadInspector();
        grid.Invalidate();
    }

    private void UpdateIndexOrder()
    {
        grid.IndexOrder = orderCombo.SelectedIndex == 0 ? TileIndexOrder.XThenY : TileIndexOrder.YThenX;
        grid.SelectedIndex = -1;
        LoadInspector();
        grid.Invalidate();
        UpdateStatus(grid.HoverIndex);
    }

    private void SelectTile(int index)
    {
        LoadInspector();
        UpdateStatus(index);
    }

    private TileInfo? SelectedTile()
    {
        return sector is not null && grid.SelectedIndex >= 0 ? sector.Tiles[grid.SelectedIndex] : null;
    }

    private void LoadInspector()
    {
        loadingInspector = true;
        try
        {
            var tile = SelectedTile();
            var hasTile = tile is not null;
            foreach (var control in new Control[] { floorCheck, floorX, floorY, wallEdgeCombo, wallCheck, fakeCheck, halfCheck, windowCheck, wallX, wallY, propList })
            {
                control.Enabled = hasTile;
            }

            if (tile is null)
            {
                for (var i = 0; i < propList.Items.Count; i++)
                {
                    propList.SetItemChecked(i, false);
                }

                return;
            }

            floorCheck.Checked = tile.floor;
            floorX.Value = tile.floorTex.X;
            floorY.Value = tile.floorTex.Y;
            ReadWall(tile, CurrentInspectorEdge(), out var wall, out var fake, out var half, out var window, out var tex);
            wallCheck.Checked = wall || fake || half || window;
            fakeCheck.Checked = fake;
            halfCheck.Checked = half;
            windowCheck.Checked = window;
            wallX.Value = tex.X;
            wallY.Value = tex.Y;

            for (var i = 0; i < propList.Items.Count; i++)
            {
                var prop = Enum.Parse<TilePropertyType>((string)propList.Items[i]);
                propList.SetItemChecked(i, tile.tileProps.Contains(prop));
            }
        }
        finally
        {
            loadingInspector = false;
        }
    }

    private void ApplyInspector()
    {
        if (loadingInspector || restoringHistory)
        {
            return;
        }

        var tile = SelectedTile();
        if (tile is null)
        {
            return;
        }

        CaptureUndo();
        tile.floor = floorCheck.Checked;
        tile.floorTex.X = (int)floorX.Value;
        tile.floorTex.Y = (int)floorY.Value;
        WriteWall(tile, CurrentInspectorEdge(), wallCheck.Checked, fakeCheck.Checked, halfCheck.Checked, windowCheck.Checked, (int)wallX.Value, (int)wallY.Value);
        tile.tileProps.Clear();
        foreach (var checkedItem in propList.CheckedItems.Cast<string>())
        {
            tile.tileProps.Add(Enum.Parse<TilePropertyType>(checkedItem));
        }

        MarkDirty();
    }

    private void PaintTileFromTool(int index, PaintStrokePhase phase, MouseButtons button)
    {
        if (sector is null)
        {
            return;
        }

        var tile = sector.Tiles[index];
        var tool = Enum.Parse<PaintTool>((string)toolCombo.SelectedItem!);
        switch (tool)
        {
            case PaintTool.PaintFloorTexture:
                CaptureUndoForStroke(phase);
                tile.floor = button == MouseButtons.Left;
                if (tile.floor)
                {
                    tile.floorTex.X = (int)paintFloorX.Value;
                    tile.floorTex.Y = (int)paintFloorY.Value;
                }
                MarkDirty();
                break;
            case PaintTool.ToggleFloor:
                CaptureUndoForStroke(phase);
                tile.floor = button == MouseButtons.Left;
                MarkDirty();
                break;
            case PaintTool.ToggleWall:
                CaptureUndoForStroke(phase);
                var edge = Enum.Parse<WallEdge>((string)paintWallEdgeCombo.SelectedItem!);
                var wallType = Enum.Parse<WallPaintType>((string)paintWallTypeCombo.SelectedItem!);
                WriteWall(tile, edge, button == MouseButtons.Left, wallType == WallPaintType.Fake, wallType == WallPaintType.Half, wallType == WallPaintType.Window, (int)paintFloorX.Value, (int)paintFloorY.Value);
                MarkDirty();
                break;
            case PaintTool.ToggleTileProperty:
                CaptureUndoForStroke(phase);
                var prop = Enum.Parse<TilePropertyType>((string)paintPropCombo.SelectedItem!);
                PaintTileProperty(tile, prop, phase, button);
                MarkDirty();
                break;
        }

        LoadInspector();
    }

    private void PaintTileProperty(TileInfo tile, TilePropertyType prop, PaintStrokePhase phase, MouseButtons button)
    {
        if (button == MouseButtons.Left)
        {
            AddTileProperty(tile, prop);
            return;
        }

        if (button == MouseButtons.Right)
        {
            tile.tileProps.Remove(prop);
            return;
        }

        var mode = paintPropModeCombo.SelectedItem is string modeName
            ? Enum.Parse<TilePropertyPaintMode>(modeName)
            : TilePropertyPaintMode.Add;

        var shouldAdd = mode switch
        {
            TilePropertyPaintMode.Add => true,
            TilePropertyPaintMode.Remove => false,
            _ => ToggleStrokeState(tile, prop, phase)
        };

        if (shouldAdd && !tile.tileProps.Contains(prop))
        {
            AddTileProperty(tile, prop);
        }
        else if (!shouldAdd)
        {
            tile.tileProps.Remove(prop);
        }
    }

    private static void AddTileProperty(TileInfo tile, TilePropertyType prop)
    {
        if (!tile.tileProps.Contains(prop))
        {
            tile.tileProps.Add(prop);
        }
    }

    private bool ToggleStrokeState(TileInfo tile, TilePropertyType prop, PaintStrokePhase phase)
    {
        if (phase == PaintStrokePhase.Begin || strokePropState is null)
        {
            strokePropState = !tile.tileProps.Contains(prop);
        }

        return strokePropState.Value;
    }

    private void ApplyRectangleTool(Rectangle room, MouseButtons button)
    {
        var tool = toolCombo.SelectedItem is string name ? Enum.Parse<PaintTool>(name) : PaintTool.Select;
        switch (tool)
        {
            case PaintTool.RoomOutline:
                CreateRoomOutline(room, button);
                break;
            case PaintTool.FillFloorRect:
                FillFloorRectangle(room, button);
                break;
        }
    }

    private void CreateRoomOutline(Rectangle room, MouseButtons button)
    {
        if (sector is null || room.Width <= 0 || room.Height <= 0)
        {
            return;
        }

        var wallType = Enum.Parse<WallPaintType>((string)paintWallTypeCombo.SelectedItem!);
        var fake = wallType == WallPaintType.Fake;
        var half = wallType == WallPaintType.Half;
        var window = wallType == WallPaintType.Window;
        var texX = (int)paintFloorX.Value;
        var texY = (int)paintFloorY.Value;
        CaptureUndo();

        for (var x = room.Left; x < room.Right; x++)
        {
            WriteWall(TileAtCell(x, room.Top), WallEdge.Top, button == MouseButtons.Left, fake, half, window, texX, texY);
            WriteWall(TileAtCell(x, room.Bottom - 1), WallEdge.Bottom, button == MouseButtons.Left, fake, half, window, texX, texY);
        }

        for (var y = room.Top; y < room.Bottom; y++)
        {
            WriteWall(TileAtCell(room.Left, y), WallEdge.Left, button == MouseButtons.Left, fake, half, window, texX, texY);
            WriteWall(TileAtCell(room.Right - 1, y), WallEdge.Right, button == MouseButtons.Left, fake, half, window, texX, texY);
        }

        MarkDirty();
        LoadInspector();
    }

    private void FillFloorRectangle(Rectangle room, MouseButtons button)
    {
        if (sector is null || room.Width <= 0 || room.Height <= 0)
        {
            return;
        }

        var texX = (int)paintFloorX.Value;
        var texY = (int)paintFloorY.Value;
        CaptureUndo();
        for (var y = room.Top; y < room.Bottom; y++)
        {
            for (var x = room.Left; x < room.Right; x++)
            {
                var tile = TileAtCell(x, y);
                tile.floor = button == MouseButtons.Left;
                if (tile.floor)
                {
                    tile.floorTex.X = texX;
                    tile.floorTex.Y = texY;
                }
            }
        }

        MarkDirty();
        LoadInspector();
    }

    private TileInfo TileAtCell(int x, int y)
    {
        if (sector is null)
        {
            throw new InvalidOperationException("No sector loaded.");
        }

        return sector.Tiles[MapIndexMapper.ToIndex(x, y, grid.IndexOrder)];
    }

    private void UpdateToolMode()
    {
        var tool = toolCombo.SelectedItem is string name ? Enum.Parse<PaintTool>(name) : PaintTool.Select;
        grid.RectangleDragMode = tool is PaintTool.RoomOutline or PaintTool.FillFloorRect;
        strokePropState = null;
    }

    private void UpdateMapZoom()
    {
        grid.SetZoomPercent((int)mapZoom.Value);
        grid.Location = new Point(8, 8);
    }

    private void UpdateAtlasZoom()
    {
        atlasPicker.SetZoomPercent((int)atlasZoom.Value);
    }

    private static void HandleViewportWheel(Control sender, Panel viewport, Control content, NumericUpDown zoomBox, MouseEventArgs e, Point contentOrigin)
    {
        if ((ModifierKeys & Keys.Control) == Keys.Control)
        {
            ZoomViewportAtCursor(sender, viewport, content, zoomBox, e, contentOrigin);
        }
        else if ((ModifierKeys & Keys.Shift) == Keys.Shift)
        {
            ScrollViewport(viewport, verticalDelta: 0, horizontalDelta: -e.Delta);
        }
        else
        {
            ScrollViewport(viewport, verticalDelta: -e.Delta, horizontalDelta: 0);
        }

        if (e is HandledMouseEventArgs handled)
        {
            handled.Handled = true;
        }
    }

    private static void ZoomViewportAtCursor(Control sender, Panel viewport, Control content, NumericUpDown zoomBox, MouseEventArgs e, Point contentOrigin)
    {
        var screenPoint = sender.PointToScreen(e.Location);
        var viewportPoint = viewport.PointToClient(screenPoint);
        var contentPoint = content.PointToClient(screenPoint);
        var oldWidth = Math.Max(1, content.Width);
        var oldHeight = Math.Max(1, content.Height);
        var anchorX = Math.Clamp(contentPoint.X / (double)oldWidth, 0, 1);
        var anchorY = Math.Clamp(contentPoint.Y / (double)oldHeight, 0, 1);

        var next = zoomBox.Value + (e.Delta > 0 ? zoomBox.Increment : -zoomBox.Increment);
        var clamped = Math.Clamp(next, zoomBox.Minimum, zoomBox.Maximum);
        if (clamped == zoomBox.Value)
        {
            return;
        }

        zoomBox.Value = clamped;

        var targetX = (int)Math.Round(anchorX * content.Width + contentOrigin.X - viewportPoint.X);
        var targetY = (int)Math.Round(anchorY * content.Height + contentOrigin.Y - viewportPoint.Y);
        SetScrollPosition(viewport, targetX, targetY);
    }

    private static void ScrollViewport(ScrollableControl viewport, int verticalDelta, int horizontalDelta)
    {
        const int divisor = 2;
        var nextX = viewport.HorizontalScroll.Value + horizontalDelta / divisor;
        var nextY = viewport.VerticalScroll.Value + verticalDelta / divisor;

        SetScrollPosition(viewport, nextX, nextY);
    }

    private static void SetScrollPosition(ScrollableControl viewport, int x, int y)
    {
        var nextX = ClampScroll(viewport.HorizontalScroll, x);
        var nextY = ClampScroll(viewport.VerticalScroll, y);
        viewport.AutoScrollPosition = new Point(nextX, nextY);
    }

    private static int ClampScroll(ScrollProperties scroll, int value)
    {
        var max = Math.Max(scroll.Minimum, scroll.Maximum - scroll.LargeChange + 1);
        return Math.Clamp(value, scroll.Minimum, max);
    }

    private WallEdge CurrentInspectorEdge()
    {
        return Enum.Parse<WallEdge>((string)wallEdgeCombo.SelectedItem!);
    }

    private static void ReadWall(TileInfo tile, WallEdge edge, out bool wall, out bool fake, out bool half, out bool window, out TexCoord tex)
    {
        switch (edge)
        {
            case WallEdge.Left:
                wall = tile.leftWall; fake = tile.leftWallFake; half = tile.leftWallHalf; window = tile.leftWallWindow; tex = tile.leftWallTex; break;
            case WallEdge.Right:
                wall = tile.rightWall; fake = tile.rightWallFake; half = tile.rightWallHalf; window = tile.rightWallWindow; tex = tile.rightWallTex; break;
            case WallEdge.Top:
                wall = tile.topWall; fake = tile.topWallFake; half = tile.topWallHalf; window = tile.topWallWindow; tex = tile.topWallTex; break;
            default:
                wall = tile.bottomWall; fake = tile.bottomWallFake; half = tile.bottomWallHalf; window = tile.bottomWallWindow; tex = tile.bottomWallTex; break;
        }
    }

    private static void WriteWall(TileInfo tile, WallEdge edge, bool wall, bool fake, bool half, bool window, int texX, int texY)
    {
        var tex = new TexCoord { X = texX, Y = texY };
        var state = WallRules.FromEditor(wall, fake, half, window);
        switch (edge)
        {
            case WallEdge.Left:
                WallRules.ApplyLeft(tile, state); tile.leftWallTex = tex; break;
            case WallEdge.Right:
                WallRules.ApplyRight(tile, state); tile.rightWallTex = tex; break;
            case WallEdge.Top:
                WallRules.ApplyTop(tile, state); tile.topWallTex = tex; break;
            case WallEdge.Bottom:
                WallRules.ApplyBottom(tile, state); tile.bottomWallTex = tex; break;
        }
    }

    private void SaveCurrent()
    {
        if (level is null)
        {
            return;
        }

        if (IsDataLevelsFolder(level.FolderPath))
        {
            var result = MessageBox.Show(this, "You are saving into Data\\Levels. This will overwrite source map XMLs after making backups. Continue?", "Confirm Data\\Levels write", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
            {
                return;
            }
        }

        try
        {
            LevelMapStore.Save(level, createBackups: true);
            dirty = false;
            UpdateStatus(grid.SelectedIndex);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveAsNewPrefix()
    {
        if (level is null)
        {
            return;
        }

        using var dialog = new PrefixPromptForm(level.Prefix);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            var newPrefix = dialog.Prefix;
            level = LevelMapStore.SaveAs(level, newPrefix, level.FolderPath);
            dirty = false;
            OpenFolder(level.FolderPath);
            mapList.SelectedItem = newPrefix;
            UpdateStatus(grid.SelectedIndex);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Save As failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExportGeneratedFiles()
    {
        if (level is null)
        {
            return;
        }

        if (IsDataLevelsFolder(level.FolderPath))
        {
            var result = MessageBox.Show(this, "You are exporting generated files into Data\\Levels. Existing generated XML, .bin, and path files will be backed up before overwrite. Continue?", "Confirm Data\\Levels export", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
            {
                return;
            }
        }

        try
        {
            UseWaitCursor = true;
            statusLabel.Text = "Exporting generated files...";
            statusLabel.Refresh();
            var result = GeneratedFileExporter.ExportAll(level, grid.IndexOrder, createBackups: true);
            LevelMapStore.Save(level, createBackups: true);
            dirty = false;
            grid.Invalidate();
            LoadInspector();
            UpdateStatus(grid.SelectedIndex);
            MessageBox.Show(this, $"Exported {result.Count} generated file(s) and saved source XML with hidden opposite walls. This includes wall/ground XML, wall/ground .bin files, and the sector 0 path file.", "Generated export complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Generated export failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void CreateNewBlankMap()
    {
        if (string.IsNullOrWhiteSpace(folderText.Text) || !Directory.Exists(folderText.Text))
        {
            PickFolder();
            if (string.IsNullOrWhiteSpace(folderText.Text) || !Directory.Exists(folderText.Text))
            {
                return;
            }
        }

        using var dialog = new NewMapPromptForm();
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var existingFiles = Enumerable.Range(0, 3)
            .Select(sector => Path.Combine(folderText.Text, $"{dialog.Prefix}{sector}.xml"))
            .Where(File.Exists)
            .ToList();

        if (existingFiles.Count > 0)
        {
            var result = MessageBox.Show(this, "Source XML files already exist for this prefix. The editor will create backups before overwriting them. Continue?", "Overwrite existing map?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
            {
                return;
            }
        }

        try
        {
            var newPrefix = dialog.Prefix;
            level = LevelMapStore.CreateBlank(folderText.Text, newPrefix, dialog.SectorCount);
            LevelMapStore.Save(level, createBackups: true);
            dirty = false;
            ClearHistory();
            OpenFolder(level.FolderPath);
            mapList.SelectedItem = newPrefix;
            UpdateStatus(grid.SelectedIndex);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "New map failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.Z)
        {
            Undo();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.Control && e.KeyCode == Keys.Y)
        {
            Redo();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (!e.Control && !e.Alt && !IsTextInputFocused())
        {
            if (TrySelectTpiShortcut(e.KeyCode) ||
                TrySelectWallTypeShortcut(e.KeyCode, e.Shift) ||
                TrySelectWallEdgeShortcut(e.KeyCode) ||
                (!e.Shift && TrySelectToolShortcut(e.KeyCode)))
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }
    }

    private bool TrySelectToolShortcut(Keys key)
    {
        if (key < Keys.D1 || key > Keys.D9)
        {
            return false;
        }

        var index = key - Keys.D1;
        if (index >= toolCombo.Items.Count)
        {
            return false;
        }

        toolCombo.SelectedIndex = index;
        UpdateStatus(grid.SelectedIndex);
        return true;
    }

    private bool TrySelectWallEdgeShortcut(Keys key)
    {
        var edge = key switch
        {
            Keys.T => WallEdge.Top,
            Keys.F => WallEdge.Left,
            Keys.G => WallEdge.Bottom,
            Keys.H => WallEdge.Right,
            _ => (WallEdge?)null
        };

        if (edge is null)
        {
            return false;
        }

        SetComboToEnum(wallEdgeCombo, edge.Value);
        SetComboToEnum(paintWallEdgeCombo, edge.Value);
        UpdateStatus(grid.SelectedIndex);
        return true;
    }

    private bool TrySelectWallTypeShortcut(Keys key, bool shift)
    {
        if (!shift || key < Keys.D1 || key > Keys.D4)
        {
            return false;
        }

        var type = key switch
        {
            Keys.D1 => WallPaintType.Normal,
            Keys.D2 => WallPaintType.Fake,
            Keys.D3 => WallPaintType.Half,
            Keys.D4 => WallPaintType.Window,
            _ => WallPaintType.Normal
        };

        SetComboToEnum(paintWallTypeCombo, type);
        UpdateStatus(grid.SelectedIndex);
        return true;
    }

    private bool TrySelectTpiShortcut(Keys key)
    {
        if (key < Keys.F1 || key > Keys.F10)
        {
            return false;
        }

        var index = key - Keys.F1;
        if (index >= paintPropCombo.Items.Count)
        {
            return false;
        }

        paintPropCombo.SelectedIndex = index;
        SetComboToEnum(toolCombo, PaintTool.ToggleTileProperty);
        UpdateStatus(grid.SelectedIndex);
        return true;
    }

    private static void SetComboToEnum<TEnum>(ComboBox combo, TEnum value)
        where TEnum : struct, Enum
    {
        var name = value.ToString();
        for (var i = 0; i < combo.Items.Count; i++)
        {
            if (string.Equals((string?)combo.Items[i], name, StringComparison.Ordinal))
            {
                combo.SelectedIndex = i;
                return;
            }
        }
    }

    private bool IsTextInputFocused()
    {
        return IsTextInputFocused(this);
    }

    private static bool IsTextInputFocused(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            if (child.ContainsFocus && (child is TextBoxBase || child is NumericUpDown || child is ComboBox))
            {
                return true;
            }

            if (child.ContainsFocus && IsTextInputFocused(child))
            {
                return true;
            }
        }

        return false;
    }

    private void CaptureUndoForStroke(PaintStrokePhase phase)
    {
        if (phase == PaintStrokePhase.Begin)
        {
            CaptureUndo();
        }
    }

    private void CaptureUndo()
    {
        if (restoringHistory || level is null)
        {
            return;
        }

        undoHistory.Push(LevelMapStore.Clone(level));
        redoHistory.Clear();
        UpdateHistoryButtons();
    }

    private void Undo()
    {
        if (level is null || undoHistory.Count == 0)
        {
            return;
        }

        redoHistory.Push(LevelMapStore.Clone(level));
        RestoreFromHistory(undoHistory.Pop());
    }

    private void Redo()
    {
        if (level is null || redoHistory.Count == 0)
        {
            return;
        }

        undoHistory.Push(LevelMapStore.Clone(level));
        RestoreFromHistory(redoHistory.Pop());
    }

    private void RestoreFromHistory(LevelMap snapshot)
    {
        restoringHistory = true;
        try
        {
            var selectedSector = sector?.Sector ?? snapshot.Sectors.First().Sector;
            level = LevelMapStore.Clone(snapshot);
            sector = level.Sectors.FirstOrDefault(s => s.Sector == selectedSector) ?? level.Sectors.First();
            grid.Sector = sector;
            grid.SelectedIndex = -1;
            dirty = true;
            LoadInspector();
            grid.Invalidate();
            UpdateStatus(grid.SelectedIndex);
        }
        finally
        {
            restoringHistory = false;
        }

        UpdateHistoryButtons();
    }

    private void ClearHistory()
    {
        undoHistory.Clear();
        redoHistory.Clear();
        UpdateHistoryButtons();
    }

    private void UpdateHistoryButtons()
    {
        undoButton.Enabled = undoHistory.Count > 0;
        redoButton.Enabled = redoHistory.Count > 0;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (dirty)
        {
            var result = MessageBox.Show(this, "There are unsaved XML changes. Close anyway?", "Unsaved changes", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
            {
                e.Cancel = true;
                return;
            }
        }

        base.OnFormClosing(e);
    }

    private static bool IsDataLevelsFolder(string path)
    {
        var normalized = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return normalized.EndsWith(Path.Combine("Data", "Levels"), StringComparison.OrdinalIgnoreCase);
    }

    private void MarkDirty()
    {
        dirty = true;
        grid.Invalidate();
        UpdateStatus(grid.SelectedIndex);
    }

    private void UpdateSelectedTextureLabel()
    {
        selectedTextureLabel.Text = $"Selected texture: X={(int)paintFloorX.Value}, Y={(int)paintFloorY.Value}";
        atlasPicker.SetSelected((int)paintFloorX.Value, (int)paintFloorY.Value);
    }

    private void UpdateStatus(int hoverOrSelected)
    {
        var selected = grid.SelectedIndex >= 0 ? $"Selected index: {grid.SelectedIndex}" : "Selected index: none";
        var hover = hoverOrSelected >= 0 ? $"Hover index: {hoverOrSelected}" : "Hover index: none";
        var order = orderCombo.SelectedIndex == 0 ? "x * 32 + y" : "y * 32 + x";
        var map = level is null ? "No map loaded" : $"{level.Prefix} sector {sector?.Sector}";
        statusLabel.Text = $"{map} | {selected} | {hover} | order {order} | {(dirty ? "Unsaved changes" : "Saved")}";
    }
}
