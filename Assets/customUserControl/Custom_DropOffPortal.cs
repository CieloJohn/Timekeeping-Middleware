using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

namespace offSiteTimekeeping_NET8.Assets.customUserControl
{
    public partial class Custom_DropOffPortal : UserControl
    {
        public event Action<ConfigEncryptor.ConfigData> ConfigImported;

        private DashedPanel dropPanel;
        private Label dropLabel;
        private Button browseButton;

        private readonly string destinationPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TimekeepingMiddleware",
            "connection.json"
        );

        public Custom_DropOffPortal()
        {
            InitializeComponent();
            SetupUI();
        }

        private void SetupUI()
        {
            this.Size = new Size(300, 190);
            dropPanel = new DashedPanel
            {
                Size = new Size(200, 80),
                Location = new Point(10, 10),
                Padding = new Padding(8)
            };
            dropPanel.DragDrop += DropPanel_DragDrop;
            dropLabel = new Label
            {
                Text = "Drop server access file (.json) *",
                ForeColor = Color.FromArgb(75, 107, 75),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            dropPanel.Controls.Add(dropLabel);
            dropPanel.HoverChanged += (s, hovering) =>
            {
                dropLabel.Text = hovering ? "Release to import" : "Drag JSON file here";
            };

            browseButton = new Button
            {
                Text = "Browse...",
                ForeColor = Color.FromArgb(120, 185, 125),
                Size = new Size(90, 30),
                Location = new Point(15, 100),
                FlatStyle = FlatStyle.Flat,
                
            };
            browseButton.FlatAppearance.BorderColor = Color.FromArgb(110, 155, 115);
            browseButton.FlatAppearance.BorderSize = 1;
            browseButton.Click += BrowseButton_Click;

            this.Controls.Add(dropPanel);
            this.Controls.Add(browseButton);
        }

        private void DropPanel_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0) ImportFile(files[0]);
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "JSON Files|*.json",
                Title = "Select connection.json"
            };
            if (ofd.ShowDialog() == DialogResult.OK) ImportFile(ofd.FileName);
        }

        private void ImportFile(string sourcePath)
        {
            try
            {
                dropLabel.Text = $"Loaded: {Path.GetFileName(sourcePath)}";
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                File.Copy(sourcePath, destinationPath, overwrite: true);
            }
            catch
            {
                dropLabel.Text = "Error loading file";
            }
        }
    }
    public class DashedPanel : Panel
    {
        private bool _isDragOver = false;
        public event EventHandler<bool>? HoverChanged;

        public DashedPanel()
        {
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.BorderStyle = BorderStyle.None;
            this.AllowDrop = true;
            this.DragEnter += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files.Length > 0 && Path.GetExtension(files[0]).Equals(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        _isDragOver = true;
                        e.Effect = DragDropEffects.Copy;
                        Invalidate();
                        HoverChanged?.Invoke(this, true);
                        return;
                    }
                }
                _isDragOver = false;
                HoverChanged?.Invoke(this, false);
            };

            this.DragLeave += (s, e) =>
            {
                _isDragOver = false;
                Invalidate();
                HoverChanged?.Invoke(this, false);
            };

            this.DragDrop += (s, e) =>
            {
                _isDragOver = false;
                Invalidate();
                HoverChanged?.Invoke(this, false);
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Color borderColor = _isDragOver ? Color.FromArgb(75, 200, 75) : Color.FromArgb(65, 88, 65);
            using var dashedPen = new Pen(borderColor, 2);
            dashedPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var rect = new Rectangle(1, 1, this.ClientSize.Width - 3, this.ClientSize.Height - 3);
            e.Graphics.DrawRectangle(dashedPen, rect);
        }
    }
}
