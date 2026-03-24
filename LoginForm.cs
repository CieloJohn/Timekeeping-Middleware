using Guna.UI2.WinForms;
using offSiteTimekeeping.Services;
using offSiteTimekeeping_NET8.Assets.customUserControl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
namespace TimekeepingMiddleware
{
    public partial class LoginForm : Form
    {
        public string IpAddress => biometricsIpTxtBox.Text;
        public int BiometricsPort => int.TryParse(portTxtBox.Text, out int val) ? val : 0;
        public int CommKey => int.TryParse(biometricsComKeyTxtBox.Text, out int val) ? val : 0;
        public string DataTransferMode => comboBox1.SelectedItem?.ToString();
        public bool LoginSuccessful { get; private set; } = false;

        private int currentStep = 0;
        //Form Handling
        public LoginForm()
        {
            try
            {
                InitializeComponent();

                comboBox1.DrawItem += comboBox1_DrawItem;
                steps.Add(panelStep1);
                steps.Add(panelStep2);
                ShowStep(0);
                showHideText();

                this.MouseDown += OnMouseDown;
                this.AllowDrop = true;
                this.DragEnter += LoginForm_DragEnter;
                this.DragDrop += LoginForm_DragDrop;
            }
            catch (Exception ex)
            {
                popUpMessage("NOTICE", $"{ex.InnerException?.Message ?? ex.Message} ||", 2);
                this.MouseDown += OnMouseDown;
                this.AllowDrop = true;
                this.DragEnter += LoginForm_DragEnter;
                this.DragDrop += LoginForm_DragDrop;
            }
        }


        private void ShowStep(int stepIndex)
        {
            foreach (Panel p in steps)
                p.Visible = false;

            steps[stepIndex].Visible = true;
            BackButton.Visible = stepIndex > 0;

            switch (stepIndex)
            {
                case 1:
                    Program.DataTransferMode = DataTransferMode;
                    Program.ScheduledTime = scheduledTimePicker.SelectedTime;
                    Program.IntervalTime = intervalInput.IntervalMilliseconds;

                    NextConnectButton.Text = "CONNECT";
                    NextConnectButton.Font = new Font("Segoe UI", 9);
                    NextConnectButton.Location = new Point(158, 440);

                    BackButton.Location = new Point(121, 439);
                    break;
                case 0:
                    NextConnectButton.Text = "➠";
                    NextConnectButton.Location = new Point(140, 440);
                    break;
            }
        }
        private void NextConnectButton_Click(object sender, EventArgs e)
        {
            bool biometricsCredentialsFilled =
                //ConfigEncryptor.IsBiometricsCredentialsNull();
                !string.IsNullOrWhiteSpace(Program.BiometricsIp) &&
                Program.BiometricsPort > 0 &&
                Program.BiometricsCommKey >= 0;

            if (currentStep == 0)
            {
                if (!biometricsCredentialsFilled)
                {
                    Program.BiometricsIp = IpAddress;
                    Program.BiometricsPort = BiometricsPort;
                    Program.BiometricsCommKey = CommKey;
                }
                currentStep++;
                ShowStep(currentStep);
            }

            else if (currentStep == 1)
            {
                Program.DataTransferMode = DataTransferMode;
                Program.ScheduledTime = scheduledTimePicker.SelectedTime;
                Program.IntervalTime = intervalInput.IntervalMilliseconds;

                bool dataTransferConfigured =
                  comboBox1.SelectedItem != null &&
                  (Program.IntervalTime > 0 || Program.ScheduledTime > TimeSpan.Zero);

                if (!biometricsCredentialsFilled || !dataTransferConfigured)
                {
                    popUpMessage("NOTICE ••", "Incomplete credenntials. Inputs must not be empty or 0 or ar 12:00 AM ||", 2);
                    return;
                }
                else
                {
                    if (!TestBiometricsConnection(Program.BiometricsIp, Program.BiometricsCommKey, Program.BiometricsPort))
                    {
                        popUpMessage("ERROR ••", "Failed to connect to the biometrics device. Please check IP or CommKey.||", 1);
                        return;
                    }
                    if (!DatabaseServices.TestDatabaseConnection())
                    {
                        popUpMessage("ERROR ••", "Database Cannot Be Reached ||", 1);
                        return;
                    }
                    else
                    {
                        LoginSuccessful = true;
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                }
            }
        }

        private void BackButton_Click(object sender, EventArgs e)
        {
            if (currentStep > 0)
            {
                currentStep--;
                ShowStep(currentStep);
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            importServerAccess.Visible = true;
            NextConnectButton.Enabled = false;
            BackButton.Enabled = false;
        }

        private void exitImportPanel_Click(object sender, EventArgs e)
        {
            importServerAccess.Visible = false;
            showCredentialsError.Visible = false;
            NextConnectButton.Enabled = true;
            BackButton.Enabled = true;
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem is not string selected)
            {
                intervalInput.Visible = false;
                setIntervalLabel.Visible = false;
                scheduledTimePicker.Visible = false;
                setTimeLabel.Visible = false;
                return;
            }

            bool isInterval = selected == "Fixed Interval";
            bool isSpecificTime = selected == "Specific Time";

            intervalInput.Visible = selected == "Fixed Interval";
            setIntervalLabel.Visible = selected == "Fixed Interval";
            scheduledTimePicker.Visible = isSpecificTime;
            setTimeLabel.Visible = isSpecificTime;

            if (isInterval)
            {
                scheduledTimePicker.SelectedTime = TimeSpan.Zero;
                Program.ScheduledTime = TimeSpan.Zero;
            }
            else if (isSpecificTime)
            {
                intervalInput.SetToZero();
                Program.IntervalTime = 0;
            }
        }

        private void DecryptButton_Click(object sender, EventArgs e)
        {
            try
            {

                bool connString = !string.IsNullOrWhiteSpace(ConfigEncryptor.GetConnectionString());
                bool biometricsConfig = ConfigEncryptor.IsBiometricsCredentialsNull();
                if (connString && biometricsConfig)
                {
                    popUpMessage("SUCCESS ◉•", "Connection string and biometrics credentials saved ||", 3);
                    currentStep = 1;
                    ShowStep(1);
                    NextConnectButton.Enabled = true;
                    BackButton.Visible = false;
                    closeCredentialsNotice.Visible = false;
                    proceedToTransferProcedureConfig.Visible = true;
                    NextConnectButton.Location = new Point(140, 440);


                    var config = ConfigEncryptor.LoadConfig();

                    Program.BiometricsIp = config.BiometricsIp;
                    Program.BiometricsPort = config.BiometricsPort;
                    Program.BiometricsCommKey = config.CommKey;
                }
                else if (connString && !biometricsConfig)
                {
                    popUpMessage("SUCCESS ◉•", "Connection string saved only ||", 3);
                    closeCredentialsNotice.Visible = false;
                    proceedToTransferProcedureConfig.Visible = true;
                    NextConnectButton.Enabled = true;
                    BackButton.Enabled = true;
                }
                else
                {
                    popUpMessage("ERROR ••", "Error decrypting: Connection configuration empty||", 1);
                    return;
                }
            }
            catch (Exception ex)
            {
                popUpMessage("ERROR ••", "Error decrypting: " + ex.Message + " ||", 1);
                return;
            }
        }
        protected void EnableMinimizeToTray(NotifyIcon trayIcon)
        {
            this.Resize += (s, e) =>
            {
                if (this.WindowState == FormWindowState.Minimized)
                {
                    this.Hide();
                    trayIcon.Visible = true;
                    trayIcon.ShowBalloonTip(1000, "Timekeeping Middleware", "Running in background", ToolTipIcon.Info);
                }
            };

            this.FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    this.Hide();
                    trayIcon.Visible = true;
                    trayIcon.ShowBalloonTip(1000, "Timekeeping Middleware", "Running in background", ToolTipIcon.Info);
                }
            };
        }
        private void closeCredentialsNotice_Click(object sender, EventArgs e)
        {
            showCredentialsError.Visible = false;
            visibilityTimer.Stop();
        }
        private void proceedToTransferProcedureConfig_Click(object sender, EventArgs e)
        {
            showCredentialsError.Visible = false;
            importServerAccess.Visible = false;
            visibilityTimer.Stop();
        }
        private bool TestBiometricsConnection(string ip, int comKey, int port)
        {
            try
            {
                zkemkeeper.CZKEM zk = new zkemkeeper.CZKEM();
                return (zk.SetCommPassword(comKey) && zk.Connect_Net(ip, port));
            }
            catch (Exception ex)
            {
                popUpMessage("NOTICE", ex.Message + " ||", 1);
                return false;
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private System.Windows.Forms.Button BackButton;
        private Label label9;
        private Label label10;
        private Panel panelStep2;
        private Label setTimeLabel;
        private System.Windows.Forms.Button NextConnectButton;
        private const int HT_CAPTION = 0x2;
        private Guna2Panel titlePanel;

        private Guna2PictureBox guna2PictureBox1;
        private Guna2Button showHideComKey;
        private Custom_DropOffPortal custom_DropOffPortal1;
        private Panel importServerAccess;
        private PictureBox pictureBox1;
        private Button DecryptButton;
        private Guna2Button exitImportPanel;
        private Label label3;
        private Panel pvaoModuleTagBorder;
        private Guna2Button proceedToTransferProcedureConfig;

        private System.Windows.Forms.Timer visibilityTimer;


        private void InitializeComponent()
        {
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges1 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges2 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges3 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges4 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges5 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges6 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges7 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges8 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges9 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges10 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges15 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges16 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges11 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges12 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges13 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges14 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges17 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges18 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges19 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges20 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges21 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges22 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges23 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges24 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges25 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges26 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges27 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges28 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LoginForm));
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges29 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges30 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges31 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges32 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges33 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges34 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges35 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges36 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges37 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges38 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges39 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges40 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            button1 = new Button();
            button2 = new Button();
            label6 = new Label();
            label8 = new Label();
            BackButton = new Button();
            label9 = new Label();
            label10 = new Label();
            panelStep2 = new Panel();
            guna2Panel6 = new Guna2Panel();
            guna2Panel7 = new Guna2Panel();
            guna2Panel8 = new Guna2Panel();
            guna2Panel9 = new Guna2Panel();
            comboBox1 = new Guna2ComboBox();
            scheduledTimePicker = new CustomTimePicker();
            setTimeLabel = new Label();
            intervalInput = new CustomIntervalPicker();
            setIntervalLabel = new Label();
            NextConnectButton = new Button();
            showCredentialsError = new Guna2Panel();
            proceedToTransferProcedureConfig = new Guna2Button();
            closeCredentialsNotice = new Guna2Button();
            credentialsNotice = new Guna2HtmlLabel();
            panel2 = new Panel();
            label12 = new Label();
            guna2Panel2 = new Guna2Panel();
            label7 = new Label();
            label4 = new Label();
            panelStep1 = new Panel();
            guna2Panel5 = new Guna2Panel();
            guna2Panel4 = new Guna2Panel();
            guna2Panel3 = new Guna2Panel();
            guna2Panel1 = new Guna2Panel();
            label1 = new Label();
            label2 = new Label();
            portTxtBox = new Guna2TextBox();
            showHideComKey = new Guna2Button();
            biometricsIpTxtBox = new Guna2TextBox();
            biometricsComKeyTxtBox = new Guna2TextBox();
            titlePanel = new Guna2Panel();
            panel1 = new Panel();
            moduleVertical = new Panel();
            label13 = new Label();
            label11 = new Label();
            guna2PictureBox1 = new Guna2PictureBox();
            custom_DropOffPortal1 = new Custom_DropOffPortal();
            importServerAccess = new Panel();
            label3 = new Label();
            DecryptButton = new Button();
            exitImportPanel = new Guna2Button();
            pictureBox1 = new PictureBox();
            pvaoModuleTagBorder = new Panel();
            panelStep2.SuspendLayout();
            showCredentialsError.SuspendLayout();
            panelStep1.SuspendLayout();
            titlePanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)guna2PictureBox1).BeginInit();
            importServerAccess.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // button1
            // 
            button1.FlatAppearance.BorderSize = 0;
            button1.FlatStyle = FlatStyle.Flat;
            button1.Font = new Font("Segoe UI", 12F);
            button1.ForeColor = SystemColors.ButtonFace;
            button1.Location = new Point(297, 10);
            button1.Name = "button1";
            button1.Size = new Size(31, 33);
            button1.TabIndex = 5;
            button1.Text = "―";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // button2
            // 
            button2.FlatAppearance.BorderSize = 0;
            button2.FlatStyle = FlatStyle.Flat;
            button2.Font = new Font("Segoe UI", 12F);
            button2.ForeColor = SystemColors.ButtonFace;
            button2.Location = new Point(326, 8);
            button2.Name = "button2";
            button2.Size = new Size(31, 33);
            button2.TabIndex = 6;
            button2.Text = "⛌";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Microsoft Sans Serif", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label6.ForeColor = SystemColors.ButtonFace;
            label6.Location = new Point(58, 35);
            label6.Margin = new Padding(0);
            label6.Name = "label6";
            label6.Size = new Size(188, 29);
            label6.TabIndex = 26;
            label6.Text = "TIMEKEEPING";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.BackColor = Color.FromArgb(30, 40, 35);
            label8.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label8.ForeColor = SystemColors.ButtonFace;
            label8.Location = new Point(64, 63);
            label8.Name = "label8";
            label8.RightToLeft = RightToLeft.Yes;
            label8.Size = new Size(162, 15);
            label8.TabIndex = 29;
            label8.Text = "M   I   D   D   L   E   W   A   R   E";
            // 
            // BackButton
            // 
            BackButton.FlatStyle = FlatStyle.Flat;
            BackButton.Font = new Font("Segoe UI", 12F);
            BackButton.ForeColor = SystemColors.ButtonFace;
            BackButton.Location = new Point(113, 440);
            BackButton.Name = "BackButton";
            BackButton.Size = new Size(31, 30);
            BackButton.TabIndex = 31;
            BackButton.Text = "⇐";
            BackButton.UseVisualStyleBackColor = true;
            BackButton.Visible = false;
            BackButton.Click += BackButton_Click;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Font = new Font("Segoe UI", 7F);
            label9.ForeColor = SystemColors.ScrollBar;
            label9.Location = new Point(29, 62);
            label9.Name = "label9";
            label9.Size = new Size(82, 12);
            label9.TabIndex = 33;
            label9.Text = "TRANSFER MODE";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Font = new Font("Segoe UI", 11F);
            label10.ForeColor = SystemColors.ButtonFace;
            label10.Location = new Point(48, 21);
            label10.Name = "label10";
            label10.Size = new Size(168, 20);
            label10.TabIndex = 32;
            label10.Text = "Data Transfer Procedure";
            // 
            // panelStep2
            // 
            panelStep2.Controls.Add(guna2Panel6);
            panelStep2.Controls.Add(guna2Panel7);
            panelStep2.Controls.Add(guna2Panel8);
            panelStep2.Controls.Add(guna2Panel9);
            panelStep2.Controls.Add(comboBox1);
            panelStep2.Controls.Add(label9);
            panelStep2.Controls.Add(label10);
            panelStep2.Controls.Add(scheduledTimePicker);
            panelStep2.Controls.Add(setTimeLabel);
            panelStep2.Controls.Add(intervalInput);
            panelStep2.Controls.Add(setIntervalLabel);
            panelStep2.Location = new Point(59, 165);
            panelStep2.Name = "panelStep2";
            panelStep2.Size = new Size(247, 289);
            panelStep2.TabIndex = 34;
            panelStep2.Visible = false;
            panelStep2.Paint += PanelSteps_Paint;
            // 
            // guna2Panel6
            // 
            guna2Panel6.BackColor = Color.FromArgb(53, 69, 53);
            guna2Panel6.CustomizableEdges = customizableEdges1;
            guna2Panel6.ForeColor = Color.FromArgb(32, 42, 32);
            guna2Panel6.Location = new Point(36, 34);
            guna2Panel6.Name = "guna2Panel6";
            guna2Panel6.ShadowDecoration.CustomizableEdges = customizableEdges2;
            guna2Panel6.Size = new Size(10, 10);
            guna2Panel6.TabIndex = 51;
            // 
            // guna2Panel7
            // 
            guna2Panel7.BackColor = Color.FromArgb(53, 69, 53);
            guna2Panel7.CustomizableEdges = customizableEdges3;
            guna2Panel7.ForeColor = Color.FromArgb(32, 42, 32);
            guna2Panel7.Location = new Point(23, 34);
            guna2Panel7.Name = "guna2Panel7";
            guna2Panel7.ShadowDecoration.CustomizableEdges = customizableEdges4;
            guna2Panel7.Size = new Size(10, 10);
            guna2Panel7.TabIndex = 50;
            // 
            // guna2Panel8
            // 
            guna2Panel8.BackColor = Color.FromArgb(53, 69, 53);
            guna2Panel8.CustomizableEdges = customizableEdges5;
            guna2Panel8.ForeColor = Color.FromArgb(32, 42, 32);
            guna2Panel8.Location = new Point(36, 20);
            guna2Panel8.Name = "guna2Panel8";
            guna2Panel8.ShadowDecoration.CustomizableEdges = customizableEdges6;
            guna2Panel8.Size = new Size(10, 10);
            guna2Panel8.TabIndex = 49;
            // 
            // guna2Panel9
            // 
            guna2Panel9.BackColor = Color.FromArgb(53, 69, 53);
            guna2Panel9.CustomizableEdges = customizableEdges7;
            guna2Panel9.ForeColor = Color.FromArgb(32, 42, 32);
            guna2Panel9.Location = new Point(23, 20);
            guna2Panel9.Name = "guna2Panel9";
            guna2Panel9.ShadowDecoration.CustomizableEdges = customizableEdges8;
            guna2Panel9.Size = new Size(10, 10);
            guna2Panel9.TabIndex = 48;
            // 
            // comboBox1
            // 
            comboBox1.BackColor = Color.FromArgb(32, 42, 32);
            comboBox1.BorderColor = Color.FromArgb(93, 104, 93);
            comboBox1.CustomizableEdges = customizableEdges9;
            comboBox1.DrawMode = DrawMode.OwnerDrawFixed;
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox1.FillColor = Color.FromArgb(32, 42, 32);
            comboBox1.FocusedColor = Color.FromArgb(75, 128, 90);
            comboBox1.FocusedState.BorderColor = Color.FromArgb(75, 128, 90);
            comboBox1.Font = new Font("Segoe UI", 10F);
            comboBox1.ForeColor = SystemColors.ButtonFace;
            comboBox1.HoverState.BorderColor = Color.FromArgb(75, 128, 90);
            comboBox1.ItemHeight = 30;
            comboBox1.Items.AddRange(new object[] { "Fixed Interval", "Specific Time" });
            comboBox1.Location = new Point(29, 82);
            comboBox1.Name = "comboBox1";
            comboBox1.ShadowDecoration.CustomizableEdges = customizableEdges10;
            comboBox1.Size = new Size(188, 36);
            comboBox1.TabIndex = 40;
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            // 
            // scheduledTimePicker
            // 
            scheduledTimePicker.BackColor = Color.FromArgb(32, 42, 32);
            scheduledTimePicker.Location = new Point(33, 164);
            scheduledTimePicker.Name = "scheduledTimePicker";
            scheduledTimePicker.SelectedTime = TimeSpan.Parse("00:00:00");
            scheduledTimePicker.Size = new Size(183, 50);
            scheduledTimePicker.TabIndex = 42;
            scheduledTimePicker.Visible = false;
            // 
            // setTimeLabel
            // 
            setTimeLabel.AutoSize = true;
            setTimeLabel.Font = new Font("Segoe UI", 7F);
            setTimeLabel.ForeColor = SystemColors.ScrollBar;
            setTimeLabel.Location = new Point(99, 140);
            setTimeLabel.Name = "setTimeLabel";
            setTimeLabel.Size = new Size(45, 12);
            setTimeLabel.TabIndex = 37;
            setTimeLabel.Text = "SET TIME";
            setTimeLabel.Visible = false;
            // 
            // intervalInput
            // 
            intervalInput.BackColor = Color.FromArgb(32, 42, 32);
            intervalInput.Location = new Point(72, 151);
            intervalInput.Name = "intervalInput";
            intervalInput.Size = new Size(103, 80);
            intervalInput.TabIndex = 41;
            intervalInput.Visible = false;
            // 
            // setIntervalLabel
            // 
            setIntervalLabel.AutoSize = true;
            setIntervalLabel.Font = new Font("Segoe UI", 7F);
            setIntervalLabel.ForeColor = SystemColors.ScrollBar;
            setIntervalLabel.Location = new Point(91, 133);
            setIntervalLabel.Name = "setIntervalLabel";
            setIntervalLabel.Size = new Size(65, 12);
            setIntervalLabel.TabIndex = 38;
            setIntervalLabel.Text = "SET INTERVAL";
            setIntervalLabel.Visible = false;
            // 
            // NextConnectButton
            // 
            NextConnectButton.FlatStyle = FlatStyle.Flat;
            NextConnectButton.Font = new Font("Segoe UI", 12F);
            NextConnectButton.ForeColor = SystemColors.ButtonFace;
            NextConnectButton.Location = new Point(150, 440);
            NextConnectButton.Name = "NextConnectButton";
            NextConnectButton.Size = new Size(81, 30);
            NextConnectButton.TabIndex = 35;
            NextConnectButton.Text = "➠ ";
            NextConnectButton.UseVisualStyleBackColor = true;
            NextConnectButton.Click += NextConnectButton_Click;
            // 
            // showCredentialsError
            // 
            showCredentialsError.BackColor = Color.FromArgb(32, 167, 199);
            showCredentialsError.BorderStyle = System.Drawing.Drawing2D.DashStyle.Dash;
            showCredentialsError.Controls.Add(proceedToTransferProcedureConfig);
            showCredentialsError.Controls.Add(closeCredentialsNotice);
            showCredentialsError.Controls.Add(credentialsNotice);
            showCredentialsError.Controls.Add(panel2);
            showCredentialsError.Controls.Add(label12);
            showCredentialsError.CustomizableEdges = customizableEdges15;
            showCredentialsError.Location = new Point(24, 220);
            showCredentialsError.Name = "showCredentialsError";
            showCredentialsError.ShadowDecoration.CustomizableEdges = customizableEdges16;
            showCredentialsError.Size = new Size(310, 115);
            showCredentialsError.TabIndex = 70;
            showCredentialsError.Visible = false;
            // 
            // proceedToTransferProcedureConfig
            // 
            proceedToTransferProcedureConfig.BackColor = Color.Transparent;
            proceedToTransferProcedureConfig.CustomizableEdges = customizableEdges11;
            proceedToTransferProcedureConfig.DisabledState.BorderColor = Color.DarkGray;
            proceedToTransferProcedureConfig.DisabledState.CustomBorderColor = Color.DarkGray;
            proceedToTransferProcedureConfig.FillColor = Color.Transparent;
            proceedToTransferProcedureConfig.Font = new Font("Segoe UI", 9F);
            proceedToTransferProcedureConfig.ForeColor = Color.FromArgb(10, 60, 90);
            proceedToTransferProcedureConfig.Location = new Point(278, 0);
            proceedToTransferProcedureConfig.Name = "proceedToTransferProcedureConfig";
            proceedToTransferProcedureConfig.ShadowDecoration.CustomizableEdges = customizableEdges12;
            proceedToTransferProcedureConfig.Size = new Size(32, 28);
            proceedToTransferProcedureConfig.TabIndex = 51;
            proceedToTransferProcedureConfig.Text = "⛌";
            proceedToTransferProcedureConfig.Visible = false;
            proceedToTransferProcedureConfig.Click += proceedToTransferProcedureConfig_Click;
            // 
            // closeCredentialsNotice
            // 
            closeCredentialsNotice.BackColor = Color.Transparent;
            closeCredentialsNotice.CustomizableEdges = customizableEdges13;
            closeCredentialsNotice.DisabledState.BorderColor = Color.DarkGray;
            closeCredentialsNotice.DisabledState.CustomBorderColor = Color.DarkGray;
            closeCredentialsNotice.FillColor = Color.Transparent;
            closeCredentialsNotice.Font = new Font("Segoe UI", 9F);
            closeCredentialsNotice.ForeColor = Color.FromArgb(10, 60, 90);
            closeCredentialsNotice.Location = new Point(278, 0);
            closeCredentialsNotice.Name = "closeCredentialsNotice";
            closeCredentialsNotice.ShadowDecoration.CustomizableEdges = customizableEdges14;
            closeCredentialsNotice.Size = new Size(32, 28);
            closeCredentialsNotice.TabIndex = 1;
            closeCredentialsNotice.Text = "⛌";
            closeCredentialsNotice.Click += closeCredentialsNotice_Click;
            // 
            // credentialsNotice
            // 
            credentialsNotice.AutoSize = false;
            credentialsNotice.BackColor = Color.Transparent;
            credentialsNotice.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Italic);
            credentialsNotice.ForeColor = Color.FromArgb(10, 70, 90);
            credentialsNotice.Location = new Point(25, 47);
            credentialsNotice.MaximumSize = new Size(265, 50);
            credentialsNotice.Name = "credentialsNotice";
            credentialsNotice.Size = new Size(265, 50);
            credentialsNotice.TabIndex = 0;
            credentialsNotice.Text = "Filler text";
            // 
            // panel2
            // 
            panel2.BackColor = Color.FromArgb(10, 90, 130);
            panel2.Location = new Point(14, 19);
            panel2.Name = "panel2";
            panel2.Size = new Size(10, 17);
            panel2.TabIndex = 50;
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.BackColor = Color.Transparent;
            label12.Font = new Font("Microsoft Sans Serif", 14F, FontStyle.Bold);
            label12.ForeColor = Color.FromArgb(10, 90, 120);
            label12.Location = new Point(25, 17);
            label12.Margin = new Padding(0);
            label12.Name = "label12";
            label12.Size = new Size(106, 24);
            label12.TabIndex = 49;
            label12.Text = "ERROR ••";
            // 
            // guna2Panel2
            // 
            guna2Panel2.BackColor = Color.FromArgb(68, 79, 68);
            guna2Panel2.CustomizableEdges = customizableEdges17;
            guna2Panel2.ForeColor = Color.FromArgb(32, 42, 32);
            guna2Panel2.Location = new Point(14, 85);
            guna2Panel2.Name = "guna2Panel2";
            guna2Panel2.ShadowDecoration.CustomizableEdges = customizableEdges18;
            guna2Panel2.Size = new Size(24, 132);
            guna2Panel2.TabIndex = 43;
            guna2Panel2.Paint += biometricsVerticalText;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Font = new Font("Segoe UI", 7F);
            label7.ForeColor = SystemColors.ScrollBar;
            label7.Location = new Point(44, 172);
            label7.Name = "label7";
            label7.Size = new Size(57, 12);
            label7.TabIndex = 28;
            label7.Text = "COMM KEY";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 7F);
            label4.ForeColor = SystemColors.ScrollBar;
            label4.Location = new Point(44, 78);
            label4.Name = "label4";
            label4.Size = new Size(58, 12);
            label4.TabIndex = 25;
            label4.Text = "IP ADDRESS";
            // 
            // panelStep1
            // 
            panelStep1.Controls.Add(guna2Panel5);
            panelStep1.Controls.Add(guna2Panel4);
            panelStep1.Controls.Add(guna2Panel3);
            panelStep1.Controls.Add(guna2Panel1);
            panelStep1.Controls.Add(label1);
            panelStep1.Controls.Add(label2);
            panelStep1.Controls.Add(portTxtBox);
            panelStep1.Controls.Add(showHideComKey);
            panelStep1.Controls.Add(guna2Panel2);
            panelStep1.Controls.Add(label7);
            panelStep1.Controls.Add(label4);
            panelStep1.Controls.Add(biometricsIpTxtBox);
            panelStep1.Controls.Add(biometricsComKeyTxtBox);
            panelStep1.Location = new Point(59, 165);
            panelStep1.Name = "panelStep1";
            panelStep1.Size = new Size(247, 290);
            panelStep1.TabIndex = 2;
            panelStep1.Paint += PanelSteps_Paint;
            // 
            // guna2Panel5
            // 
            guna2Panel5.BackColor = Color.FromArgb(53, 69, 53);
            guna2Panel5.CustomizableEdges = customizableEdges19;
            guna2Panel5.ForeColor = Color.FromArgb(32, 42, 32);
            guna2Panel5.Location = new Point(39, 47);
            guna2Panel5.Name = "guna2Panel5";
            guna2Panel5.ShadowDecoration.CustomizableEdges = customizableEdges20;
            guna2Panel5.Size = new Size(10, 10);
            guna2Panel5.TabIndex = 47;
            // 
            // guna2Panel4
            // 
            guna2Panel4.BackColor = Color.FromArgb(53, 69, 53);
            guna2Panel4.CustomizableEdges = customizableEdges21;
            guna2Panel4.ForeColor = Color.FromArgb(32, 42, 32);
            guna2Panel4.Location = new Point(26, 47);
            guna2Panel4.Name = "guna2Panel4";
            guna2Panel4.ShadowDecoration.CustomizableEdges = customizableEdges22;
            guna2Panel4.Size = new Size(10, 10);
            guna2Panel4.TabIndex = 46;
            // 
            // guna2Panel3
            // 
            guna2Panel3.BackColor = Color.FromArgb(53, 69, 53);
            guna2Panel3.CustomizableEdges = customizableEdges23;
            guna2Panel3.ForeColor = Color.FromArgb(32, 42, 32);
            guna2Panel3.Location = new Point(39, 33);
            guna2Panel3.Name = "guna2Panel3";
            guna2Panel3.ShadowDecoration.CustomizableEdges = customizableEdges24;
            guna2Panel3.Size = new Size(10, 10);
            guna2Panel3.TabIndex = 45;
            // 
            // guna2Panel1
            // 
            guna2Panel1.BackColor = Color.FromArgb(53, 69, 53);
            guna2Panel1.CustomizableEdges = customizableEdges25;
            guna2Panel1.ForeColor = Color.FromArgb(32, 42, 32);
            guna2Panel1.Location = new Point(26, 33);
            guna2Panel1.Name = "guna2Panel1";
            guna2Panel1.ShadowDecoration.CustomizableEdges = customizableEdges26;
            guna2Panel1.Size = new Size(10, 10);
            guna2Panel1.TabIndex = 44;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.BackColor = Color.FromArgb(32, 42, 32);
            label1.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.Gainsboro;
            label1.Location = new Point(53, 34);
            label1.Name = "label1";
            label1.Size = new Size(170, 20);
            label1.TabIndex = 53;
            label1.Text = "Input Device Credentials";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 7F);
            label2.ForeColor = SystemColors.ScrollBar;
            label2.Location = new Point(44, 124);
            label2.Name = "label2";
            label2.Size = new Size(30, 12);
            label2.TabIndex = 50;
            label2.Text = "PORT";
            // 
            // portTxtBox
            // 
            portTxtBox.BorderColor = Color.FromArgb(93, 104, 93);
            portTxtBox.BorderRadius = 2;
            portTxtBox.CustomizableEdges = customizableEdges27;
            portTxtBox.DefaultText = "";
            portTxtBox.DisabledState.BorderColor = Color.FromArgb(190, 190, 190);
            portTxtBox.DisabledState.FillColor = Color.FromArgb(226, 226, 226);
            portTxtBox.DisabledState.ForeColor = Color.FromArgb(138, 138, 138);
            portTxtBox.DisabledState.PlaceholderForeColor = Color.FromArgb(138, 138, 138);
            portTxtBox.FillColor = Color.FromArgb(32, 42, 32);
            portTxtBox.FocusedState.BorderColor = Color.FromArgb(75, 128, 90);
            portTxtBox.Font = new Font("Segoe UI", 8F);
            portTxtBox.ForeColor = SystemColors.ButtonFace;
            portTxtBox.HoverState.BorderColor = Color.FromArgb(75, 128, 90);
            portTxtBox.Location = new Point(43, 137);
            portTxtBox.Margin = new Padding(9, 10, 9, 10);
            portTxtBox.Name = "portTxtBox";
            portTxtBox.PlaceholderText = "";
            portTxtBox.SelectedText = "";
            portTxtBox.ShadowDecoration.CustomizableEdges = customizableEdges28;
            portTxtBox.Size = new Size(182, 30);
            portTxtBox.TabIndex = 51;
            // 
            // showHideComKey
            // 
            showHideComKey.BackgroundImage = (Image)resources.GetObject("showHideComKey.BackgroundImage");
            showHideComKey.BackgroundImageLayout = ImageLayout.Zoom;
            showHideComKey.CustomizableEdges = customizableEdges29;
            showHideComKey.DisabledState.BorderColor = Color.DarkGray;
            showHideComKey.DisabledState.CustomBorderColor = Color.DarkGray;
            showHideComKey.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            showHideComKey.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            showHideComKey.FillColor = Color.Transparent;
            showHideComKey.Font = new Font("Segoe UI", 9F);
            showHideComKey.ForeColor = Color.White;
            showHideComKey.Location = new Point(195, 191);
            showHideComKey.Name = "showHideComKey";
            showHideComKey.ShadowDecoration.CustomizableEdges = customizableEdges30;
            showHideComKey.Size = new Size(21, 21);
            showHideComKey.TabIndex = 49;
            // 
            // biometricsIpTxtBox
            // 
            biometricsIpTxtBox.BorderColor = Color.FromArgb(93, 104, 93);
            biometricsIpTxtBox.BorderRadius = 2;
            biometricsIpTxtBox.CustomizableEdges = customizableEdges31;
            biometricsIpTxtBox.DefaultText = "";
            biometricsIpTxtBox.DisabledState.BorderColor = Color.FromArgb(190, 190, 190);
            biometricsIpTxtBox.DisabledState.FillColor = Color.FromArgb(226, 226, 226);
            biometricsIpTxtBox.DisabledState.ForeColor = Color.FromArgb(138, 138, 138);
            biometricsIpTxtBox.DisabledState.PlaceholderForeColor = Color.FromArgb(138, 138, 138);
            biometricsIpTxtBox.FillColor = Color.FromArgb(32, 42, 32);
            biometricsIpTxtBox.FocusedState.BorderColor = Color.FromArgb(75, 128, 90);
            biometricsIpTxtBox.Font = new Font("Segoe UI", 8F);
            biometricsIpTxtBox.ForeColor = SystemColors.ButtonFace;
            biometricsIpTxtBox.HoverState.BorderColor = Color.FromArgb(75, 128, 90);
            biometricsIpTxtBox.Location = new Point(43, 90);
            biometricsIpTxtBox.Margin = new Padding(9, 10, 9, 10);
            biometricsIpTxtBox.Name = "biometricsIpTxtBox";
            biometricsIpTxtBox.PlaceholderText = "";
            biometricsIpTxtBox.SelectedText = "";
            biometricsIpTxtBox.ShadowDecoration.CustomizableEdges = customizableEdges32;
            biometricsIpTxtBox.Size = new Size(182, 30);
            biometricsIpTxtBox.TabIndex = 47;
            // 
            // biometricsComKeyTxtBox
            // 
            biometricsComKeyTxtBox.BorderColor = Color.FromArgb(93, 104, 93);
            biometricsComKeyTxtBox.BorderRadius = 2;
            biometricsComKeyTxtBox.CustomizableEdges = customizableEdges33;
            biometricsComKeyTxtBox.DefaultText = "";
            biometricsComKeyTxtBox.DisabledState.BorderColor = Color.FromArgb(190, 190, 190);
            biometricsComKeyTxtBox.DisabledState.FillColor = Color.FromArgb(226, 226, 226);
            biometricsComKeyTxtBox.DisabledState.ForeColor = Color.FromArgb(138, 138, 138);
            biometricsComKeyTxtBox.DisabledState.PlaceholderForeColor = Color.FromArgb(138, 138, 138);
            biometricsComKeyTxtBox.FillColor = Color.FromArgb(32, 42, 32);
            biometricsComKeyTxtBox.FocusedState.BorderColor = Color.FromArgb(75, 128, 90);
            biometricsComKeyTxtBox.Font = new Font("Segoe UI", 8F);
            biometricsComKeyTxtBox.ForeColor = SystemColors.ButtonFace;
            biometricsComKeyTxtBox.HoverState.BorderColor = Color.FromArgb(75, 128, 90);
            biometricsComKeyTxtBox.Location = new Point(43, 187);
            biometricsComKeyTxtBox.Margin = new Padding(9, 10, 9, 10);
            biometricsComKeyTxtBox.Name = "biometricsComKeyTxtBox";
            biometricsComKeyTxtBox.PlaceholderText = "";
            biometricsComKeyTxtBox.SelectedText = "";
            biometricsComKeyTxtBox.ShadowDecoration.CustomizableEdges = customizableEdges34;
            biometricsComKeyTxtBox.Size = new Size(182, 30);
            biometricsComKeyTxtBox.TabIndex = 48;
            biometricsComKeyTxtBox.UseSystemPasswordChar = true;
            // 
            // titlePanel
            // 
            titlePanel.BorderStyle = System.Drawing.Drawing2D.DashStyle.DashDotDot;
            titlePanel.Controls.Add(panel1);
            titlePanel.Controls.Add(label8);
            titlePanel.Controls.Add(label6);
            titlePanel.Controls.Add(moduleVertical);
            titlePanel.CustomizableEdges = customizableEdges35;
            titlePanel.Location = new Point(44, 57);
            titlePanel.Name = "titlePanel";
            titlePanel.ShadowDecoration.CustomizableEdges = customizableEdges36;
            titlePanel.Size = new Size(277, 117);
            titlePanel.TabIndex = 43;
            // 
            // panel1
            // 
            panel1.BackColor = Color.FromArgb(42, 62, 42);
            panel1.Location = new Point(28, 16);
            panel1.Name = "panel1";
            panel1.Size = new Size(4, 74);
            panel1.TabIndex = 50;
            // 
            // moduleVertical
            // 
            moduleVertical.BackColor = Color.FromArgb(42, 62, 42);
            moduleVertical.Location = new Point(38, 16);
            moduleVertical.Name = "moduleVertical";
            moduleVertical.Size = new Size(20, 74);
            moduleVertical.TabIndex = 49;
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Font = new Font("Bahnschrift Condensed", 14F, FontStyle.Bold);
            label13.ForeColor = Color.FromArgb(52, 63, 50);
            label13.Location = new Point(11, 12);
            label13.Margin = new Padding(0);
            label13.Name = "label13";
            label13.Size = new Size(94, 23);
            label13.TabIndex = 51;
            label13.Text = "PVAO MODULE";
            label13.TextAlign = ContentAlignment.MiddleLeft;
            label13.MouseDown += OnMouseDown;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Font = new Font("Bahnschrift Condensed", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label11.ForeColor = Color.FromArgb(62, 92, 62);
            label11.Location = new Point(214, 82);
            label11.Name = "label11";
            label11.Size = new Size(61, 13);
            label11.TabIndex = 50;
            label11.Text = "__ZKEMKEEPER";
            // 
            // guna2PictureBox1
            // 
            guna2PictureBox1.CustomizableEdges = customizableEdges37;
            guna2PictureBox1.Image = (Image)resources.GetObject("guna2PictureBox1.Image");
            guna2PictureBox1.ImageRotate = 0F;
            guna2PictureBox1.Location = new Point(82, 271);
            guna2PictureBox1.Name = "guna2PictureBox1";
            guna2PictureBox1.ShadowDecoration.CustomizableEdges = customizableEdges38;
            guna2PictureBox1.Size = new Size(413, 388);
            guna2PictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            guna2PictureBox1.TabIndex = 45;
            guna2PictureBox1.TabStop = false;
            guna2PictureBox1.MouseDown += OnMouseDown;
            // 
            // custom_DropOffPortal1
            // 
            custom_DropOffPortal1.Location = new Point(14, 82);
            custom_DropOffPortal1.Name = "custom_DropOffPortal1";
            custom_DropOffPortal1.Size = new Size(220, 142);
            custom_DropOffPortal1.TabIndex = 53;
            // 
            // importServerAccess
            // 
            importServerAccess.BackColor = Color.FromArgb(32, 42, 32);
            importServerAccess.Controls.Add(label3);
            importServerAccess.Controls.Add(DecryptButton);
            importServerAccess.Controls.Add(exitImportPanel);
            importServerAccess.Controls.Add(custom_DropOffPortal1);
            importServerAccess.Location = new Point(58, 165);
            importServerAccess.Name = "importServerAccess";
            importServerAccess.Size = new Size(249, 305);
            importServerAccess.TabIndex = 54;
            importServerAccess.TabStop = true;
            importServerAccess.Visible = false;
            importServerAccess.Paint += PanelSteps_Paint;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Microsoft Sans Serif", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label3.ForeColor = Color.FromArgb(59, 73, 57);
            label3.Location = new Point(16, 40);
            label3.Margin = new Padding(0);
            label3.Name = "label3";
            label3.Size = new Size(113, 29);
            label3.TabIndex = 51;
            label3.Text = "IMPORT";
            // 
            // DecryptButton
            // 
            DecryptButton.BackColor = Color.FromArgb(52, 82, 65);
            DecryptButton.FlatAppearance.BorderColor = Color.FromArgb(120, 205, 125);
            DecryptButton.FlatStyle = FlatStyle.Flat;
            DecryptButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            DecryptButton.ForeColor = Color.FromArgb(120, 205, 125);
            DecryptButton.Location = new Point(128, 183);
            DecryptButton.Name = "DecryptButton";
            DecryptButton.Size = new Size(90, 30);
            DecryptButton.TabIndex = 54;
            DecryptButton.Text = "Connect >>";
            DecryptButton.UseVisualStyleBackColor = false;
            DecryptButton.Click += DecryptButton_Click;
            // 
            // exitImportPanel
            // 
            exitImportPanel.BackColor = Color.FromArgb(32, 42, 32);
            exitImportPanel.CustomizableEdges = customizableEdges39;
            exitImportPanel.DisabledState.BorderColor = Color.DarkGray;
            exitImportPanel.DisabledState.CustomBorderColor = Color.DarkGray;
            exitImportPanel.FillColor = Color.FromArgb(32, 47, 32);
            exitImportPanel.Font = new Font("Segoe UI", 9F);
            exitImportPanel.ForeColor = Color.White;
            exitImportPanel.Location = new Point(203, 13);
            exitImportPanel.Name = "exitImportPanel";
            exitImportPanel.ShadowDecoration.CustomizableEdges = customizableEdges40;
            exitImportPanel.Size = new Size(32, 28);
            exitImportPanel.TabIndex = 52;
            exitImportPanel.Text = "⛌";
            exitImportPanel.Click += exitImportPanel_Click;
            // 
            // pictureBox1
            // 
            pictureBox1.BackgroundImage = offSiteTimekeeping_NET8.Properties.Resources.importServerAccess;
            pictureBox1.BackgroundImageLayout = ImageLayout.Zoom;
            pictureBox1.Location = new Point(271, 416);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(20, 20);
            pictureBox1.TabIndex = 52;
            pictureBox1.TabStop = false;
            pictureBox1.Click += pictureBox1_Click;
            // 
            // pvaoModuleTagBorder
            // 
            pvaoModuleTagBorder.Location = new Point(8, 8);
            pvaoModuleTagBorder.Name = "pvaoModuleTagBorder";
            pvaoModuleTagBorder.Size = new Size(100, 33);
            pvaoModuleTagBorder.TabIndex = 71;
            pvaoModuleTagBorder.Paint += PanelSteps_Paint2;
            // 
            // LoginForm
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            BackColor = Color.FromArgb(32, 42, 32);
            ClientSize = new Size(364, 557);
            Controls.Add(label13);
            Controls.Add(pvaoModuleTagBorder);
            Controls.Add(showCredentialsError);
            Controls.Add(importServerAccess);
            Controls.Add(pictureBox1);
            Controls.Add(label11);
            Controls.Add(NextConnectButton);
            Controls.Add(BackButton);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(panelStep2);
            Controls.Add(panelStep1);
            Controls.Add(titlePanel);
            Controls.Add(guna2PictureBox1);
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "LoginForm";
            panelStep2.ResumeLayout(false);
            panelStep2.PerformLayout();
            showCredentialsError.ResumeLayout(false);
            showCredentialsError.PerformLayout();
            panelStep1.ResumeLayout(false);
            panelStep1.PerformLayout();
            titlePanel.ResumeLayout(false);
            titlePanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)guna2PictureBox1).EndInit();
            importServerAccess.ResumeLayout(false);
            importServerAccess.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }


        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private Label label6;
        private Label label8;
        public Label setIntervalLabel;
        public Guna.UI2.WinForms.Guna2ComboBox comboBox1;
        private Guna.UI2.WinForms.Guna2Panel showCredentialsError;
        private Guna.UI2.WinForms.Guna2HtmlLabel credentialsNotice;
        private Guna.UI2.WinForms.Guna2Button closeCredentialsNotice;
        private Guna.UI2.WinForms.Guna2Panel guna2Panel2;
        private Label label7;
        private Label label4;
        private Panel panelStep1;
        public Guna2TextBox biometricsIpTxtBox;
        public Guna2TextBox biometricsComKeyTxtBox;
        private List<Panel> steps = new List<Panel>();
        public CustomIntervalPicker IntervalTime => intervalInput;
        private Panel moduleVertical;
        private Label label11;
        private Panel panel1;
        private Panel panel2;
        private Label label12;
        private Label label13;
        private CustomTimePicker scheduledTimePicker;
        private CustomIntervalPicker intervalInput;
        private Label label2;
        public Guna2TextBox portTxtBox;
        private Label label1;
        private Guna2Panel guna2Panel1;
        private Guna2Panel guna2Panel3;
        private Guna2Panel guna2Panel5;
        private Guna2Panel guna2Panel4;
        private Guna2Panel guna2Panel6;
        private Guna2Panel guna2Panel7;
        private Guna2Panel guna2Panel8;
        private Guna2Panel guna2Panel9;


        //Custom UI Stuffs
        Image showImage = offSiteTimekeeping_NET8.Properties.Resources.show_password_3;
        Image hideImage = offSiteTimekeeping_NET8.Properties.Resources.hide_password_3;

        private void button1_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ConfigEncryptor.ClearConfig();
            this.Close();
        }
        private void showHideText()
        {
            showHideComKey.MouseDown += (s, e) =>
            {
                biometricsComKeyTxtBox.UseSystemPasswordChar = false;
                showHideComKey.BackgroundImage = hideImage;
            };

            showHideComKey.MouseUp += (s, e) =>
            {
                biometricsComKeyTxtBox.UseSystemPasswordChar = true;
                showHideComKey.BackgroundImage = showImage;
            };
        }

        private void popUpMessage(string title, string message, int mode)
        {
            switch (mode)
            {
                //RED
                case 1:
                    showCredentialsError.BackColor = Color.FromArgb(130, 50, 50);
                    label12.ForeColor = Color.FromArgb(190, 135, 135);
                    panel2.BackColor = Color.FromArgb(182, 120, 120);
                    credentialsNotice.ForeColor = Color.FromArgb(210, 150, 150);
                    closeCredentialsNotice.ForeColor = Color.FromArgb(190, 135, 135);
                    break;
                //YELLOW
                case 2:
                    showCredentialsError.BackColor = Color.FromArgb(190, 210, 50);
                    label12.ForeColor = Color.FromArgb(50, 50, 10);
                    panel2.BackColor = Color.FromArgb(50, 50, 10);
                    credentialsNotice.ForeColor = Color.FromArgb(70, 70, 20);
                    closeCredentialsNotice.ForeColor = Color.FromArgb(50, 50, 10);
                    break;
                //BLUE
                case 3:
                    showCredentialsError.BackColor = Color.FromArgb(32, 167, 199);
                    label12.ForeColor = Color.FromArgb(10, 90, 120);
                    panel2.BackColor = Color.FromArgb(10, 90, 130);
                    credentialsNotice.ForeColor = Color.FromArgb(10, 70, 90);
                    closeCredentialsNotice.ForeColor = Color.FromArgb(10, 90, 120);
                    break;

                    //case 3:
                    //    showCredentialsError.BackColor = Color.FromArgb(40, 60, 40);
                    //    label12.ForeColor = Color.FromArgb(90, 120, 90);
                    //    panel2.BackColor = Color.FromArgb(57, 82, 57);
                    //    credentialsNotice.ForeColor = Color.FromArgb(160, 190, 160);
                    //    closeCredentialsNotice.ForeColor = Color.FromArgb(90, 120, 90);
                    //    break;
            }

            showCredentialsError.BringToFront();
            showCredentialsError.Visible = true;
            label12.Text = title;
            credentialsNotice.Text = message;

            visibilityTimer = new System.Windows.Forms.Timer();
            visibilityTimer.Interval = 5000;
            visibilityTimer.Tick += (s, e) =>
            {
                showCredentialsError.Visible = false;
                visibilityTimer.Stop();
                visibilityTimer.Dispose();
            };


        }
        private void comboBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            e.DrawBackground();
            e.Graphics.DrawString(comboBox1.Items[e.Index].ToString(),
                                  comboBox1.Font,
                                  Brushes.Black,
                                  e.Bounds);
            e.DrawFocusRectangle();
        }
        private void biometricsVerticalText(object sender, PaintEventArgs e)
        {
            string text = "B I O M E T R I C S";
            Font font = new Font("Microsoft Sans Serif", 9, FontStyle.Bold);
            Brush brush = new SolidBrush(Color.FromArgb(255, 32, 42, 32));

            e.Graphics.TranslateTransform(5, guna2Panel2.Height - 8);
            e.Graphics.RotateTransform(-90);
            e.Graphics.DrawString(text, font, brush, 0, 0);
            e.Graphics.ResetTransform();
        }

        private void PanelSteps_Paint(object sender, PaintEventArgs e)
        {
            Control panel = sender as Control;

            using (Pen dashedPen = new Pen(Color.FromArgb(65, 88, 65), 1))
            {
                dashedPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.DrawRectangle(dashedPen, 0, 0, panel.Width - 1, panel.Height - 1);
            }
        }
        private void PanelSteps_Paint2(object sender, PaintEventArgs e)
        {
            Control panel = sender as Control;

            using (Pen dashedPen = new Pen(Color.FromArgb(52, 63, 50), 1))
            {
                dashedPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.DrawRectangle(dashedPen, 0, 0, panel.Width - 1, panel.Height - 1);
            }
        }
        private void LoginForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0 && Path.GetExtension(files[0]).Equals(".json", StringComparison.OrdinalIgnoreCase))
                {
                    e.Effect = DragDropEffects.Copy;
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            }
        }

        private void LoginForm_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files.Length > 0)
            {
                string sourcePath = files[0];
                string destinationPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "TimekeepingMiddleware",
                    "connection.json"
                );

                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                    File.Copy(sourcePath, destinationPath, overwrite: true);

                    MessageBox.Show("Connection file imported successfully!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error importing file: " + ex.Message);
                }
            }
        }
    }
}
