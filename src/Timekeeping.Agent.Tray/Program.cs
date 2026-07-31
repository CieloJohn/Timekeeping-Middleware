using System.Net.Http.Json;
using Timekeeping.Contracts;

namespace Timekeeping.Agent.Tray;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new StatusForm());
    }
}

public sealed class StatusForm : Form
{
    private readonly NotifyIcon _tray;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(3) };
    private readonly Label _deviceLabel;
    private readonly Label _vpnLabel;
    private readonly Label _queueLabel;
    private readonly Label _syncLabel;
    private readonly Label _errorLabel;
    private readonly Button _openServiceBtn;
    private readonly string _statusUrl;

    public StatusForm()
    {
        _statusUrl = Environment.GetEnvironmentVariable("TIMEKEEPING_STATUS_URL")
                     ?? "http://127.0.0.1:17890/status";

        Text = "Timekeeping Agent";
        Width = 460;
        Height = 320;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 1,
            RowCount = 7
        };

        _deviceLabel = MakeLabel();
        _vpnLabel = MakeLabel();
        _queueLabel = MakeLabel();
        _syncLabel = MakeLabel();
        _errorLabel = MakeLabel();
        _errorLabel.ForeColor = Color.Firebrick;

        _openServiceBtn = new Button
        {
            Text = "Refresh now",
            AutoSize = true,
            Margin = new Padding(0, 12, 0, 0)
        };
        _openServiceBtn.Click += async (_, _) => await RefreshAsync();

        layout.Controls.Add(new Label { Text = "PVAO Timekeeping Agent", Font = new Font(Font, FontStyle.Bold), AutoSize = true });
        layout.Controls.Add(_deviceLabel);
        layout.Controls.Add(_vpnLabel);
        layout.Controls.Add(_queueLabel);
        layout.Controls.Add(_syncLabel);
        layout.Controls.Add(_errorLabel);
        layout.Controls.Add(_openServiceBtn);
        Controls.Add(layout);

        _tray = new NotifyIcon
        {
            Text = "Timekeeping Agent",
            Icon = SystemIcons.Application,
            Visible = true,
            ContextMenuStrip = new ContextMenuStrip()
        };
        _tray.ContextMenuStrip.Items.Add("Open", null, (_, _) =>
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        });
        _tray.ContextMenuStrip.Items.Add("Exit", null, (_, _) =>
        {
            _tray.Visible = false;
            Application.Exit();
        });
        _tray.DoubleClick += (_, _) =>
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        };

        _timer = new System.Windows.Forms.Timer { Interval = 3000 };
        _timer.Tick += async (_, _) => await RefreshAsync();
        _timer.Start();

        Resize += (_, _) =>
        {
            if (WindowState == FormWindowState.Minimized)
                Hide();
        };

        Shown += async (_, _) => await RefreshAsync();
    }

    private static Label MakeLabel() => new()
    {
        AutoSize = true,
        Margin = new Padding(0, 8, 0, 0),
        MaximumSize = new Size(410, 0)
    };

    private async Task RefreshAsync()
    {
        try
        {
            var status = await _http.GetFromJsonAsync<AgentStatusDto>(_statusUrl);
            if (status is null)
            {
                SetOffline("Agent status endpoint returned empty payload.");
                return;
            }

            _deviceLabel.Text = $"Device: {(status.DeviceConnected ? "Connected" : "Down")}  |  {status.DeviceStatus}  |  SN {status.DeviceSerialNumber}";
            _vpnLabel.Text = $"VPN / Ingest API: {(status.VpnOrApiReachable ? "Reachable" : "Unreachable")}";
            _queueLabel.Text = $"Queue — pending {status.PendingCount}, failed {status.FailedCount}, unmapped {status.UnmappedCount}";
            _syncLabel.Text = $"Sync: {status.SyncStatus}" +
                              (status.LastSuccessfulSyncUtc is null
                                  ? ""
                                  : $"  |  last OK {status.LastSuccessfulSyncUtc:u}");
            _errorLabel.Text = string.IsNullOrWhiteSpace(status.LastError) ? "" : $"Last error: {status.LastError}";

            _tray.Text = status.VpnOrApiReachable && status.DeviceConnected
                ? "Timekeeping Agent — OK"
                : "Timekeeping Agent — attention needed";
        }
        catch
        {
            SetOffline("Cannot reach local agent status (is the Windows service running?).");
        }
    }

    private void SetOffline(string message)
    {
        _deviceLabel.Text = "Device: unknown";
        _vpnLabel.Text = "VPN / Ingest API: unknown";
        _queueLabel.Text = "Queue: unknown";
        _syncLabel.Text = "Sync: agent offline";
        _errorLabel.Text = message;
        _tray.Text = "Timekeeping Agent — offline";
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        _timer.Stop();
        _tray.Visible = false;
        _http.Dispose();
        base.OnFormClosing(e);
    }
}
