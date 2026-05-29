namespace ZE2MapEditor;

public sealed class NewMapPromptForm : Form
{
    private readonly TextBox prefixBox = new();
    private readonly NumericUpDown sectorCountBox = new();

    public NewMapPromptForm()
    {
        Text = "New Blank Map";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(360, 150);

        var prefixLabel = new Label { Text = "Map prefix", Location = new Point(12, 16), Width = 120 };
        prefixBox.Location = new Point(130, 12);
        prefixBox.Width = 210;
        prefixBox.Text = "NewMap";

        var sectorsLabel = new Label { Text = "Sectors", Location = new Point(12, 52), Width = 120 };
        sectorCountBox.Location = new Point(130, 48);
        sectorCountBox.Width = 72;
        sectorCountBox.Minimum = 1;
        sectorCountBox.Maximum = 3;
        sectorCountBox.Value = 2;

        var okButton = new Button { Text = "Create", Location = new Point(184, 100), DialogResult = DialogResult.OK };
        var cancelButton = new Button { Text = "Cancel", Location = new Point(265, 100), DialogResult = DialogResult.Cancel };

        Controls.AddRange(new Control[] { prefixLabel, prefixBox, sectorsLabel, sectorCountBox, okButton, cancelButton });
        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    public string Prefix => prefixBox.Text.Trim();
    public int SectorCount => (int)sectorCountBox.Value;

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (DialogResult != DialogResult.OK)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Prefix) || Prefix.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            MessageBox.Show(this, "Enter a valid file prefix.", "Invalid prefix", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.Cancel = true;
        }
    }
}
