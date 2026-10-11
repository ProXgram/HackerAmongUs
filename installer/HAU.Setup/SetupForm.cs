using System.Diagnostics;
using System.Reflection;
using HAU.Setup.Core;
using HAU.Updates;

namespace HAU.Setup;

internal sealed class SetupForm : Form
{
    private static readonly Color Background = Color.FromArgb(9, 15, 27);
    private static readonly Color Card = Color.FromArgb(20, 31, 49);
    private static readonly Color Muted = Color.FromArgb(160, 178, 201);
    private static readonly Color Accent = Color.FromArgb(92, 218, 255);
    private readonly TextBox folder = new();
    private readonly Label status = new();
    private readonly Label locationHint = new();
    private readonly ProgressBar progress = new();
    private readonly Button browse = new();
    private readonly Button install = new();
    private readonly Button openFolder = new();
    private readonly Button cancel = new();
    private readonly Button details = new();
    private readonly ComboBox foundGames = new();
    private CancellationTokenSource? cancellation;
    private bool busy;
    private bool finished;
    private string lastError = "";

    internal static readonly SetupCatalog Catalog = new("1.0.2",
        new DownloadArtifact(new Uri("https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.788%2B5b766a3.zip"),
            "F4CC496BD098A0DF4164B81E3737297707F13A47C2478DBA2F60EEFAB784817A", "BepInEx loader"),
        new DownloadArtifact(new Uri("https://github.com/ProXgram/HackerAmongUs/releases/download/hau-v1.0.2/HAU-1.0.2-Plugin.zip"),
            "D76C8A983BA10634868ED7813F0423024707E36FA53A6DD6AB011D8E76BFF1DB", "Hacker Among Us"))
        { ExpectedPluginVersion = new Version(1, 0, 2, 0) };

    public SetupForm()
    {
        Text = "Hacker Among Us Setup";
        ClientSize = new Size(740, 595);
        MinimumSize = Size;
        MaximumSize = Size;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Background;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(30, 22, 30, 18), ColumnCount = 1, RowCount = 6 };
        foreach (var height in new float[] { 65, 95, 130, 110, 68, 63 })
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        Controls.Add(layout);

        var header = new Panel { Dock = DockStyle.Fill };
        var logo = new Label { Text = "HAU", Font = new Font("Segoe UI", 16, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, ForeColor = Background, BackColor = Accent, Location = Point.Empty, Size = new Size(70, 52) };
        header.Controls.Add(logo);
        header.Controls.Add(TextLabel("Hacker Among Us", 20, FontStyle.Bold, new Point(85, 0), new Size(450, 34)));
        header.Controls.Add(TextLabel($"HAU {Catalog.ModVersion}  ·  By Rifegul", 10, FontStyle.Regular, new Point(87, 35), new Size(450, 24), Muted));
        layout.Controls.Add(header, 0, 0);

        var introduction = new Panel { Dock = DockStyle.Fill };
        introduction.Controls.Add(TextLabel("Install HAU. Keep your settings.", 23, FontStyle.Bold, new Point(0, 7), new Size(670, 38)));
        introduction.Controls.Add(TextLabel("Choose Among Us, install HAU, then open your game normally.", 10.5f, FontStyle.Regular, new Point(0, 51), new Size(670, 28), Muted));
        layout.Controls.Add(introduction, 0, 1);

        var location = new Panel { Dock = DockStyle.Fill, BackColor = Card, Padding = new Padding(16), Margin = new Padding(0, 0, 0, 12) };
        location.Controls.Add(TextLabel("1   Choose your Among Us folder", 10, FontStyle.Bold, new Point(16, 12), new Size(350, 24)));
        folder.SetBounds(16, 43, 505, 31);
        folder.Font = new Font("Segoe UI", 10);
        folder.BackColor = Color.FromArgb(13, 19, 27);
        folder.ForeColor = Color.White;
        folder.BorderStyle = BorderStyle.FixedSingle;
        folder.TextChanged += (_, _) => { finished = false; openFolder.Enabled = false; install.Text = "Install HAU"; };
        location.Controls.Add(folder);
        StyleButton(browse, "Browse…", new Rectangle(533, 40, 128, 35), false);
        browse.Click += BrowseClick;
        location.Controls.Add(browse);
        locationHint.SetBounds(16, 83, 645, 24);
        locationHint.ForeColor = Muted;
        locationHint.Text = "Looking for Among Us…";
        location.Controls.Add(locationHint);
        layout.Controls.Add(location, 0, 2);

        var activity = new Panel { Dock = DockStyle.Fill };
        status.SetBounds(0, 4, 670, 46);
        status.Text = "2   Install HAU and open Among Us.\nYour earlier menu is backed up and saved settings are imported.";
        status.ForeColor = Muted;
        activity.Controls.Add(status);
        progress.SetBounds(0, 61, 678, 9);
        progress.Maximum = 100;
        activity.Controls.Add(progress);
        foundGames.SetBounds(0, 80, 678, 29);
        foundGames.DropDownStyle = ComboBoxStyle.DropDownList;
        foundGames.Visible = false;
        foundGames.SelectedIndexChanged += (_, _) => { if (foundGames.SelectedItem is string selected) folder.Text = selected; };
        activity.Controls.Add(foundGames);
        StyleButton(details, "Show details", new Rectangle(548, 79, 130, 30), false);
        details.Visible = false;
        details.Click += (_, _) => ShowText("Installation details", lastError);
        activity.Controls.Add(details);
        layout.Controls.Add(activity, 0, 3);

        var actions = new Panel { Dock = DockStyle.Fill };
        StyleButton(install, "Install HAU", new Rectangle(0, 0, 396, 49), true);
        install.Font = new Font("Segoe UI", 13, FontStyle.Bold);
        install.Click += InstallClick;
        actions.Controls.Add(install);
        StyleButton(openFolder, "Show game folder", new Rectangle(408, 0, 158, 49), false);
        openFolder.Enabled = false;
        openFolder.Click += (_, _) => OpenGameFolder();
        actions.Controls.Add(openFolder);
        StyleButton(cancel, "Close", new Rectangle(578, 0, 100, 49), false);
        cancel.Click += (_, _) => { if (busy) cancellation?.Cancel(); else Close(); };
        actions.Controls.Add(cancel);
        layout.Controls.Add(actions, 0, 4);

        var footer = new Panel { Dock = DockStyle.Fill };
        footer.Controls.Add(TextLabel("Windows 10/11 (64-bit)  ·  Steam / Microsoft Store / Xbox App", 9, FontStyle.Regular, new Point(0, 0), new Size(678, 22), Muted));
        footer.Controls.Add(TextLabel("For Among Us 2026.9.29. Internet connection required.", 9, FontStyle.Regular, new Point(0, 23), new Size(475, 22), Muted));
        var credits = new LinkLabel { Text = "Credits & licenses", LinkColor = Muted, ActiveLinkColor = Accent, VisitedLinkColor = Muted, Location = new Point(551, 23), Size = new Size(130, 25), Font = new Font("Segoe UI", 9) };
        credits.LinkClicked += (_, _) => ShowLicenses();
        footer.Controls.Add(credits);
        layout.Controls.Add(footer, 0, 5);
        Shown += async (_, _) => await DetectGameAsync();
        FormClosing += (_, e) => { if (busy) { e.Cancel = true; cancellation?.Cancel(); status.Text = "Stopping safely… Please wait."; } };
    }

    private static Label TextLabel(string text, float size, FontStyle style, Point location, Size bounds, Color? color = null) => new()
    { Text = text, Font = new Font("Segoe UI", size, style), Location = location, Size = bounds, ForeColor = color ?? Color.White };

    private static void StyleButton(Button button, string text, Rectangle bounds, bool primary)
    {
        button.Text = text;
        button.Bounds = bounds;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        button.FlatAppearance.BorderColor = Color.FromArgb(69, 86, 105);
        button.BackColor = primary ? Accent : Card;
        button.ForeColor = primary ? Background : Color.White;
        button.Cursor = Cursors.Hand;
        button.UseVisualStyleBackColor = false;
    }

    private async Task DetectGameAsync()
    {
        try
        {
            var choices = await Task.Run(InstallerService.FindGameDirectories);
            if (IsDisposed) return;
            if (choices.Count > 0)
            {
                folder.Text = choices[0];
                locationHint.Text = "Among Us found. Close the game before installing.";
                locationHint.ForeColor = Accent;
                if (choices.Count > 1)
                {
                    foundGames.Items.AddRange(choices.Cast<object>().ToArray());
                    foundGames.SelectedIndex = 0;
                    foundGames.Visible = true;
                    locationHint.Text = "More than one game found. Choose the copy you want below.";
                }
            }
            else
            {
                locationHint.Text = "Click Browse and select Among Us.exe in your game folder.";
                status.Text = "Steam: Manage → Browse local files.\nXbox App / Microsoft Store: Manage → Files → Browse.";
            }
        }
        catch
        {
            locationHint.Text = "Click Browse and select Among Us.exe in your game folder.";
        }
    }

    private void BrowseClick(object? sender, EventArgs e)
    {
        using var picker = new OpenFileDialog { Title = "Select Among Us.exe", Filter = "Among Us|Among Us.exe", CheckFileExists = true, Multiselect = false };
        if (Directory.Exists(folder.Text)) picker.InitialDirectory = folder.Text;
        if (picker.ShowDialog(this) != DialogResult.OK) return;
        folder.Text = Path.GetDirectoryName(picker.FileName) ?? "";
        locationHint.ForeColor = Muted;
        locationHint.Text = "Game folder selected. Close Among Us before installing.";
        status.ForeColor = Muted;
        status.Text = "Ready to install HAU. We’ll download everything you need.";
    }

    private async void InstallClick(object? sender, EventArgs e)
    {
        if (busy) return;
        var directory = folder.Text.Trim().Trim('"');
        var problem = InstallerService.ValidateGameDirectory(directory);
        if (problem is not null) { status.ForeColor = Color.FromArgb(255, 194, 108); status.Text = problem; return; }
        cancellation = new CancellationTokenSource();
        details.Visible = false;
        lastError = "";
        SetBusy(true);
        progress.Value = 0;
        status.ForeColor = Muted;
        var reporter = new Progress<InstallProgress>(update =>
        {
            if (!busy || IsDisposed) return;
            progress.Value = Math.Clamp(update.Percent, 0, 100);
            status.Text = update.Message;
        });
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            status.Text = "Checking for a compatible mod release…";
            var selectedCatalog = await ResolveCatalogAsync(directory, http, cancellation.Token);
            var service = new InstallerService(selectedCatalog, http);
            await Task.Run(() => service.InstallAsync(directory, reporter, cancellation.Token));
            finished = true;
            progress.Value = 100;
            status.ForeColor = Accent;
            status.Text = "Installed! Open Among Us normally.\nUse your menu key (Delete by default).";
            locationHint.Text = $"Version {selectedCatalog.ModVersion} is ready. Your settings are preserved.";
            locationHint.ForeColor = Accent;
            install.Text = "Reinstall HAU";
        }
        catch (OperationCanceledException)
        {
            status.ForeColor = Muted;
            status.Text = "Cancelled. Any installation changes have been restored.";
        }
        catch (Exception ex)
        {
            progress.Value = 0;
            status.ForeColor = Color.FromArgb(255, 194, 108);
            status.Text = FriendlyError(ex);
            lastError = ex.Message;
            details.Visible = true;
            foundGames.Visible = false;
            install.Text = "Try again";
        }
        finally
        {
            SetBusy(false);
            cancellation.Dispose();
            cancellation = null;
        }
    }

    private static async Task<SetupCatalog> ResolveCatalogAsync(string directory, HttpClient http, CancellationToken ct)
    {
        var gameVersion = InstallerService.GetGameVersion(directory);
        var selected = Catalog;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(15));
            using var response = await http.GetAsync(UpdateTrust.ManifestUrl, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength > UpdateManifestVerifier.MaximumEnvelopeBytes)
                throw new InvalidDataException("The update information is larger than expected.");
            using var input = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var output = new MemoryStream();
            var buffer = new byte[4096];
            int count;
            while ((count = await input.ReadAsync(buffer, timeout.Token)) > 0)
            {
                if (output.Length + count > UpdateManifestVerifier.MaximumEnvelopeBytes)
                    throw new InvalidDataException("The update information is larger than expected.");
                output.Write(buffer, 0, count);
            }
            var manifest = new UpdateManifestVerifier(UpdateTrust.PublicKeyPem).Verify(output.ToArray());
            if (manifest.SupportsGameVersion(gameVersion) && manifest.IsNewerThan(Catalog.ModVersion))
                selected = AutoUpdateService.CreateCatalog(manifest, Catalog.Loader);
        }
        catch (HttpRequestException) {   }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { }
        var current = Path.Combine(directory, "BepInEx", "plugins", "HAU.dll");
        if (File.Exists(current) && InstallerService.GetInstalledModVersion(directory) > UpdateManifestVerifier.ParseNumericVersion(selected.ModVersion))
            throw new InvalidOperationException("A newer mod is already installed. Download the latest setup before replacing it.");
        return selected;
    }

    private void SetBusy(bool value)
    {
        busy = value;
        folder.Enabled = browse.Enabled = install.Enabled = foundGames.Enabled = !value;
        openFolder.Enabled = !value && finished;
        cancel.Text = value ? "Cancel" : "Close";
        install.Text = value ? "Installing…" : finished ? "Reinstall HAU" : "Install HAU";
    }

    private static string FriendlyError(Exception error) => error switch
    {
        HttpRequestException => "The download failed. Check your internet connection and try again.",
        UnauthorizedAccessException => "Windows won’t let us write to that folder. Choose the game folder from Steam or Xbox App.",
        _ => error.Message.Length > 165 ? "Installation could not finish. Click Show details for the reason and recovery steps." : error.Message
    };

    private void OpenGameFolder()
    {
        if (!Directory.Exists(folder.Text)) return;
        try { Process.Start(new ProcessStartInfo(folder.Text) { UseShellExecute = true }); }
        catch (Exception ex) { status.Text = FriendlyError(ex); }
    }

    private void ShowLicenses()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var sections = new List<string>();
        foreach (var name in assembly.GetManifestResourceNames().Order())
        {
            using var stream = assembly.GetManifestResourceStream(name);
            if (stream is null) continue;
            using var reader = new StreamReader(stream);
            sections.Add(name + "\r\n\r\n" + reader.ReadToEnd());
        }
        ShowText("Credits & licenses", string.Join("\r\n\r\n", sections));
    }

    private void ShowText(string title, string text)
    {
        using var dialog = new Form { Text = title, Size = new Size(720, 520), StartPosition = FormStartPosition.CenterParent, MinimizeBox = false };
        var box = new TextBox { Text = text.Replace("\r\n", "\n").Replace("\n", "\r\n"), Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Both, WordWrap = false, Font = new Font("Consolas", 9) };
        dialog.Controls.Add(box);
        dialog.Shown += (_, _) => box.Select(0, 0);
        dialog.ShowDialog(this);
    }
}
