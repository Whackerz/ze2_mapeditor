namespace ZE2MapEditor;

public sealed class PrefixPromptForm : Form
{
    private readonly TextBox prefixBox = new();

    public PrefixPromptForm(string currentPrefix)
    {
        Text = "Save As New Map Prefix";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(360, 120);

        var label = new Label { Text = "New map prefix", Location = new Point(12, 16), Width = 120 };
        prefixBox.Location = new Point(130, 12);
        prefixBox.Width = 210;
        prefixBox.Text = currentPrefix + "_Copy";

        var okButton = new Button { Text = "Save As", Location = new Point(184, 70), DialogResult = DialogResult.OK };
        var cancelButton = new Button { Text = "Cancel", Location = new Point(265, 70), DialogResult = DialogResult.Cancel };

        Controls.AddRange(new Control[] { label, prefixBox, okButton, cancelButton });
        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    public string Prefix => prefixBox.Text.Trim();

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
