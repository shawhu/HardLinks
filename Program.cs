using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Diagnostics;

class RoundButton : Button
{
    const int Radius = 24; // <-- corner radius, change it here
    bool hover, pressed;

    public RoundButton()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        Cursor = Cursors.Hand;
    }

    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { pressed = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Parent!.BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var r = new Rectangle(0, 0, Width - 1, Height - 1);
        var d = Radius * 2;
        using var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        using var brush = new SolidBrush(!Enabled ? Color.Silver : pressed ? Color.FromArgb(0, 70, 140) : hover ? Color.FromArgb(30, 130, 230) : Color.FromArgb(0, 100, 190));
        g.FillPath(brush, path);
        using var font = new Font(Font, FontStyle.Bold);
        TextRenderer.DrawText(g, Text, font, ClientRectangle, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}

record FolderEntry(string FullPath, int Count);
class MainForm : Form
{
    const int InfoHeight = 120; // <-- height of lblInfo, change it here
    const float FontSize = 12F; // <-- form font size, change it here
    const float ButtonFontSize = 16F; // <-- button font size, change it here
    const float SlotFontSize = 8F; // <-- slot button font size, change it here
    const int Slots = 14; // <-- number of stored folders, keep it a multiple of Columns
    const int Columns = 7;
    const int GridHeight = 150; // <-- height of the folder button area, change it here
    Size FormSize = new Size(1800, 960);
    bool RandomFormPosition = false;
    Point FormPosition = new Point(-10, 0);

    static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "HardLinks.json");
    static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    readonly Label lblFiles = MakeZone("Drop files here");
    readonly Label lblFolder = MakeZone("Drop ONE target folder here");
    readonly TableLayoutPanel grid = new()
    {
        Dock = DockStyle.Fill,
        ColumnCount = Columns,
        RowCount = Slots / Columns,
        Margin = new Padding(8, 8, 8, 0)
    };
    readonly Button[] slotButtons = new Button[Slots];
    readonly ContextMenuStrip slotMenu = new();
    readonly RoundButton btnCreate = new()
    {
        Text = "Create hard links in the target folder",
        Dock = DockStyle.Fill,
        Enabled = false,
        Margin = new Padding(8, 24, 8, 24)
    };
    readonly Label lblInfo = new()
    {
        BackColor = Color.Black,
        ForeColor = Color.White,
        Text = "Info Panel",
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter,
        Margin = new Padding(0)
    };

    string[] files = [];
    string? folder;
    List<FolderEntry> folders = LoadFolders();
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

    public MainForm()
    {
        Text = $"HardLinks v{typeof(MainForm).Assembly.GetName().Version!.ToString(2)}";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        Font = new Font(Font.FontFamily, FontSize);
        btnCreate.Font = new Font(Font.FontFamily, ButtonFontSize);
        ClientSize = FormSize;
        if (!RandomFormPosition)
        {
            StartPosition = FormStartPosition.Manual;
            Location = FormPosition;
        }

        for (var c = 0; c < Columns; c++)
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / Columns));
        for (var r = 0; r < Slots / Columns; r++)
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / (Slots / Columns)));

        slotMenu.Items.Add(
            "Open in File Explorer",
            null,
            (s, e) => Process.Start(
                new ProcessStartInfo((string)slotMenu.SourceControl!.Tag!) { UseShellExecute = true }
            )
        );

        for (var i = 0; i < Slots; i++)
        {
            var b = new Button
            {
                Dock = DockStyle.Fill,
                Enabled = false,
                AutoEllipsis = true,
                Margin = new Padding(4),
                ContextMenuStrip = slotMenu,
                Font = new Font(Font.FontFamily, SlotFontSize)
            };
            b.Click += (s, e) => SetFolder((string)b.Tag!);
            slotButtons[i] = b;
            grid.Controls.Add(b, i % Columns, i / Columns);
        }
        RefreshSlots();

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, GridHeight));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 128));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, InfoHeight));
        layout.Controls.Add(lblFiles, 0, 0);
        layout.Controls.Add(grid, 0, 1);
        layout.Controls.Add(lblFolder, 0, 2);
        layout.Controls.Add(btnCreate, 0, 3);
        layout.Controls.Add(lblInfo, 0, 4);
        Controls.Add(layout);

        lblFiles.DragEnter += (s, e) => e.Effect = Paths(e)?.Any(File.Exists) == true ? DragDropEffects.Copy : DragDropEffects.None;
        lblFiles.DragDrop += (s, e) =>
        {
            files = Paths(e)!.Where(File.Exists).ToArray();
            lblFiles.Text = $"{files.Length} file(s) ready\n{string.Join("\n", files)}\n(drop again to replace)";
            SetInfo($"{files.Length} file(s) added.", Color.LightGreen);
            UpdateButton();
        };

        lblFolder.DragEnter += (s, e) => e.Effect = Paths(e) is [var p] && Directory.Exists(p) ? DragDropEffects.Copy : DragDropEffects.None;
        lblFolder.DragDrop += (s, e) => SetFolder(Paths(e)![0]);

        btnCreate.Click += (s, e) =>
        {
            var failures = new List<string>();
            var createdLinks = new List<string>();
            foreach (var file in files)
            {
                var name = Path.GetFileName(file);
                var linkPath = Path.Combine(folder!, name);
                if (CreateHardLink(linkPath, file, IntPtr.Zero))
                    createdLinks.Add(linkPath);
                else
                    failures.Add($"{name}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}");
            }

            var verificationFailures = createdLinks.Where(link => !File.Exists(link)).ToList();
            var verificationStatus = failures.Count == 0 && verificationFailures.Count == 0
                ? "[verified]"
                : "[failed to verify]";
            if (failures.Count == 0)
                SetInfo($"Created {files.Length} hard link(s) in {folder} {verificationStatus}", Color.LightGreen);
            else
                SetInfo($"Created {files.Length - failures.Count} of {files.Length}. First error - {failures[0]} {verificationStatus}", Color.Salmon);
        };
    }

    static List<FolderEntry> LoadFolders() =>
    File.Exists(ConfigPath) ? JsonSerializer.Deserialize<List<FolderEntry>>(File.ReadAllText(ConfigPath)) ?? [] : [];

    void SetFolder(string path)
    {
        folder = path;
        lblFolder.Text = path;
        SetInfo($"Target folder set: {path}", Color.LightGreen);
        var count = (folders.Find(f => string.Equals(f.FullPath, path, StringComparison.OrdinalIgnoreCase))?.Count ?? 0) + 1;
        folders.RemoveAll(f => string.Equals(f.FullPath, path, StringComparison.OrdinalIgnoreCase));
        if (folders.Count == Slots)
            folders.RemoveAt(Slots - 1);
        folders.Insert(0, new FolderEntry(path, count));
        folders = folders.OrderByDescending(f => f.Count).ToList();
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(folders, JsonOptions));
        RefreshSlots();
        UpdateButton();
    }

    void RefreshSlots()
    {
        for (var i = 0; i < Slots; i++)
        {
            var has = i < folders.Count;
            slotButtons[i].Tag = has ? folders[i].FullPath : null;
            slotButtons[i].Text = has ? new DirectoryInfo(folders[i].FullPath).Name : "";
            slotButtons[i].Enabled = has;
        }
    }

    static Label MakeZone(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        AllowDrop = true,
        BorderStyle = BorderStyle.FixedSingle,
        TextAlign = ContentAlignment.MiddleCenter,
        BackColor = Color.White,
        Margin = new Padding(8, 8, 8, 0)
    };

    static string[]? Paths(DragEventArgs e) => e.Data?.GetData(DataFormats.FileDrop) as string[];

    void SetInfo(string text, Color color)
    {
        lblInfo.ForeColor = color;
        lblInfo.Text = text;
    }

    void UpdateButton() => btnCreate.Enabled = files.Length > 0 && folder is not null;
}

class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}