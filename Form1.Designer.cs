using Guna.UI2.WinForms.Suite;
using System.Drawing;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;



namespace TimekeepingMiddleware
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            label1 = new Label();
            label7 = new Label();
            label14 = new Label();
            label15 = new Label();
            listView1 = new ListView();
            notifyIcon1 = new NotifyIcon(components);
            checkTimeTimer = new Timer(components);
            syncTimer = new Timer(components);
            label21 = new Label();
            label13 = new Label();
            pollingTimer = new Timer(components);
            minimize = new Button();
            exit = new Button();
            panel1 = new Panel();
            syncTime = new Button();
            label5 = new Label();
            panel12 = new Panel();
            labelClock = new Label();
            panel11 = new Panel();
            label6 = new Label();
            panel10 = new Panel();
            panel9 = new Panel();
            panel8 = new Panel();
            panel7 = new Panel();
            panel6 = new Panel();
            label19 = new Label();
            label23 = new Label();
            label22 = new Label();
            refresh = new Button();
            DatabaseUnreachable = new NotifyIcon(components);
            pictureBox1 = new PictureBox();
            panel2 = new Panel();
            panel3 = new Panel();
            MIDDLEWARE = new Panel();
            panel5 = new Panel();
            panel4 = new Panel();
            label2 = new Label();
            hideToggle = new CheckBox();
            label4 = new Label();
            SyncServerTimeToolTip = new ToolTip(components);
            popUpPanel = new Panel();
            closePopUp = new Button();
            panel13 = new Panel();
            popUpDesc = new Label();
            popUpTitle = new Label();
            popUpVisibilityTimer = new Timer(components);
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            MIDDLEWARE.SuspendLayout();
            popUpPanel.SuspendLayout();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.BackColor = Color.FromArgb(35, 59, 48);
            label1.Font = new Font("Segoe UI", 12F);
            label1.ForeColor = Color.Transparent;
            label1.Location = new Point(31, 29);
            label1.Name = "label1";
            label1.Size = new Size(114, 21);
            label1.TabIndex = 4;
            label1.Text = "Not Connected";
            // 
            // label7
            // 
            label7.AccessibleRole = AccessibleRole.TitleBar;
            label7.AutoSize = true;
            label7.Font = new Font("Microsoft Sans Serif", 14F, FontStyle.Bold);
            label7.ForeColor = Color.FromArgb(210, 210, 210);
            label7.Location = new Point(69, 48);
            label7.Name = "label7";
            label7.Size = new Size(216, 24);
            label7.TabIndex = 15;
            label7.Text = "TIMEKEEPING INFOS";
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.ForeColor = SystemColors.ButtonFace;
            label14.Location = new Point(533, 34);
            label14.Name = "label14";
            label14.Size = new Size(17, 15);
            label14.TabIndex = 22;
            label14.Text = "--";
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.BackColor = Color.FromArgb(35, 59, 48);
            label15.Font = new Font("Segoe UI", 12F);
            label15.ForeColor = SystemColors.ButtonFace;
            label15.Location = new Point(346, 30);
            label15.Name = "label15";
            label15.Size = new Size(114, 21);
            label15.TabIndex = 25;
            label15.Text = "Not Connected";
            // 
            // listView1
            // 
            listView1.BackColor = Color.FromArgb(32, 42, 32);
            listView1.BorderStyle = BorderStyle.None;
            listView1.Font = new Font("Verdana", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            listView1.ForeColor = SystemColors.ButtonFace;
            listView1.FullRowSelect = true;
            listView1.Location = new Point(16, 67);
            listView1.Name = "listView1";
            listView1.Size = new Size(645, 312);
            listView1.TabIndex = 29;
            listView1.UseCompatibleStateImageBehavior = false;
            listView1.View = View.Details;
            // 
            // notifyIcon1
            // 
            notifyIcon1.Icon = (Icon)resources.GetObject("notifyIcon1.Icon");
            notifyIcon1.Text = "Timekeeping Middleware ";
            notifyIcon1.Visible = true;
            notifyIcon1.MouseDoubleClick += notifyIcon1_MouseDoubleClick;
            // 
            // syncTimer
            // 
            syncTimer.Enabled = true;
            syncTimer.Interval = 1000;
            // 
            // label21
            // 
            label21.AutoSize = true;
            label21.Font = new Font("Segoe UI", 7F);
            label21.ForeColor = SystemColors.ScrollBar;
            label21.Location = new Point(20, 401);
            label21.Name = "label21";
            label21.Size = new Size(90, 12);
            label21.TabIndex = 44;
            label21.Text = "TRANSFER MODE : ";
            label21.TextAlign = ContentAlignment.BottomRight;
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Font = new Font("Segoe UI", 7F);
            label13.ForeColor = Color.FromArgb(190, 190, 190);
            label13.Location = new Point(530, 17);
            label13.Name = "label13";
            label13.Size = new Size(129, 12);
            label13.TabIndex = 42;
            label13.Text = "BIOMETRICS DEVICE SERIAL";
            label13.TextAlign = ContentAlignment.BottomRight;
            // 
            // minimize
            // 
            minimize.BackColor = Color.FromArgb(32, 42, 32);
            minimize.FlatAppearance.BorderSize = 0;
            minimize.FlatStyle = FlatStyle.Flat;
            minimize.Font = new Font("Segoe UI", 14F);
            minimize.ForeColor = SystemColors.ButtonFace;
            minimize.Location = new Point(675, 10);
            minimize.Name = "minimize";
            minimize.Size = new Size(35, 31);
            minimize.TabIndex = 40;
            minimize.Text = "―";
            minimize.UseVisualStyleBackColor = false;
            minimize.Click += minimize_Click;
            // 
            // exit
            // 
            exit.BackColor = Color.FromArgb(32, 42, 32);
            exit.FlatAppearance.BorderSize = 0;
            exit.FlatStyle = FlatStyle.Flat;
            exit.Font = new Font("Segoe UI", 14F);
            exit.ForeColor = SystemColors.ButtonFace;
            exit.Location = new Point(716, 10);
            exit.Name = "exit";
            exit.Size = new Size(35, 31);
            exit.TabIndex = 41;
            exit.Text = "⛌";
            exit.UseVisualStyleBackColor = false;
            exit.Click += exit_Click;
            // 
            // panel1
            // 
            panel1.Controls.Add(syncTime);
            panel1.Controls.Add(label5);
            panel1.Controls.Add(panel12);
            panel1.Controls.Add(labelClock);
            panel1.Controls.Add(panel11);
            panel1.Controls.Add(label6);
            panel1.Controls.Add(panel10);
            panel1.Controls.Add(panel9);
            panel1.Controls.Add(panel8);
            panel1.Controls.Add(panel7);
            panel1.Controls.Add(panel6);
            panel1.Controls.Add(label21);
            panel1.Controls.Add(label19);
            panel1.Controls.Add(label23);
            panel1.Controls.Add(label15);
            panel1.Controls.Add(label14);
            panel1.Controls.Add(label13);
            panel1.Controls.Add(label22);
            panel1.Controls.Add(label1);
            panel1.Controls.Add(refresh);
            panel1.Controls.Add(listView1);
            panel1.Location = new Point(45, 83);
            panel1.Name = "panel1";
            panel1.Size = new Size(677, 437);
            panel1.TabIndex = 42;
            // 
            // syncTime
            // 
            syncTime.AutoEllipsis = true;
            syncTime.BackColor = Color.FromArgb(32, 42, 32);
            syncTime.BackgroundImage = offSiteTimekeeping_NET8.Properties.Resources.sync_time;
            syncTime.BackgroundImageLayout = ImageLayout.Zoom;
            syncTime.FlatAppearance.BorderSize = 0;
            syncTime.FlatStyle = FlatStyle.Flat;
            syncTime.ForeColor = SystemColors.ButtonFace;
            syncTime.Location = new Point(625, 394);
            syncTime.Name = "syncTime";
            syncTime.Padding = new Padding(5);
            syncTime.Size = new Size(30, 29);
            syncTime.TabIndex = 56;
            SyncServerTimeToolTip.SetToolTip(syncTime, "Sync Time to Biometrics Device");
            syncTime.UseVisualStyleBackColor = false;
            syncTime.Click += button1_Click;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.BackColor = Color.FromArgb(52, 77, 52);
            label5.Font = new Font("Segoe UI", 7F);
            label5.ForeColor = Color.FromArgb(230, 230, 230);
            label5.Location = new Point(279, 16);
            label5.Name = "label5";
            label5.Size = new Size(13, 12);
            label5.TabIndex = 55;
            label5.Text = "--";
            label5.TextAlign = ContentAlignment.BottomRight;
            // 
            // panel12
            // 
            panel12.BackColor = Color.FromArgb(42, 62, 42);
            panel12.Location = new Point(188, 16);
            panel12.Name = "panel12";
            panel12.Size = new Size(8, 32);
            panel12.TabIndex = 52;
            // 
            // labelClock
            // 
            labelClock.AutoSize = true;
            labelClock.Font = new Font("Microsoft Sans Serif", 8F);
            labelClock.ForeColor = SystemColors.ButtonFace;
            labelClock.Location = new Point(202, 32);
            labelClock.Name = "labelClock";
            labelClock.Size = new Size(13, 13);
            labelClock.TabIndex = 50;
            labelClock.Text = "--";
            // 
            // panel11
            // 
            panel11.BackColor = Color.FromArgb(34, 59, 38);
            panel11.Location = new Point(640, 67);
            panel11.Name = "panel11";
            panel11.Size = new Size(10, 24);
            panel11.TabIndex = 54;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Segoe UI", 7F);
            label6.ForeColor = Color.FromArgb(190, 190, 190);
            label6.Location = new Point(202, 16);
            label6.Name = "label6";
            label6.Size = new Size(75, 12);
            label6.TabIndex = 51;
            label6.Text = "DATE AND TIME";
            label6.TextAlign = ContentAlignment.BottomRight;
            // 
            // panel10
            // 
            panel10.BackColor = Color.FromArgb(32, 42, 32);
            panel10.Location = new Point(656, 65);
            panel10.Name = "panel10";
            panel10.Size = new Size(11, 317);
            panel10.TabIndex = 49;
            // 
            // panel9
            // 
            panel9.BackColor = Color.FromArgb(32, 42, 32);
            panel9.Location = new Point(640, 91);
            panel9.Name = "panel9";
            panel9.Size = new Size(10, 291);
            panel9.TabIndex = 48;
            // 
            // panel8
            // 
            panel8.BackColor = Color.FromArgb(42, 62, 42);
            panel8.Location = new Point(516, 18);
            panel8.Name = "panel8";
            panel8.Size = new Size(8, 32);
            panel8.TabIndex = 49;
            // 
            // panel7
            // 
            panel7.BackColor = Color.FromArgb(42, 62, 42);
            panel7.Location = new Point(331, 18);
            panel7.Name = "panel7";
            panel7.Size = new Size(8, 32);
            panel7.TabIndex = 48;
            // 
            // panel6
            // 
            panel6.BackColor = Color.FromArgb(42, 62, 42);
            panel6.Location = new Point(20, 16);
            panel6.Name = "panel6";
            panel6.Size = new Size(8, 32);
            panel6.TabIndex = 47;
            // 
            // label19
            // 
            label19.AutoSize = true;
            label19.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            label19.ForeColor = SystemColors.ButtonFace;
            label19.Location = new Point(106, 399);
            label19.Name = "label19";
            label19.Size = new Size(17, 15);
            label19.TabIndex = 43;
            label19.Text = "--";
            label19.TextAlign = ContentAlignment.BottomRight;
            // 
            // label23
            // 
            label23.AutoSize = true;
            label23.Font = new Font("Segoe UI", 7F);
            label23.ForeColor = Color.FromArgb(190, 190, 190);
            label23.Location = new Point(344, 16);
            label23.Name = "label23";
            label23.Size = new Size(160, 12);
            label23.TabIndex = 44;
            label23.Text = "BIOMETRICS CONNECTION STATUS";
            // 
            // label22
            // 
            label22.AutoSize = true;
            label22.Font = new Font("Segoe UI", 7F);
            label22.ForeColor = Color.FromArgb(190, 190, 190);
            label22.Location = new Point(31, 14);
            label22.Name = "label22";
            label22.Size = new Size(137, 12);
            label22.TabIndex = 43;
            label22.Text = "SERVER CONNECTION STATUS";
            // 
            // refresh
            // 
            refresh.BackColor = Color.FromArgb(34, 59, 38);
            refresh.BackgroundImage = offSiteTimekeeping_NET8.Properties.Resources.refresh;
            refresh.BackgroundImageLayout = ImageLayout.Zoom;
            refresh.FlatAppearance.BorderSize = 0;
            refresh.FlatStyle = FlatStyle.Flat;
            refresh.ForeColor = SystemColors.ButtonFace;
            refresh.Location = new Point(609, 67);
            refresh.Name = "refresh";
            refresh.Size = new Size(27, 24);
            refresh.TabIndex = 47;
            refresh.UseVisualStyleBackColor = false;
            refresh.Click += refresh_Click;
            // 
            // DatabaseUnreachable
            // 
            DatabaseUnreachable.Text = "notifyIcon2";
            DatabaseUnreachable.Visible = true;
            // 
            // pictureBox1
            // 
            pictureBox1.BackgroundImage = (Image)resources.GetObject("pictureBox1.BackgroundImage");
            pictureBox1.BackgroundImageLayout = ImageLayout.Zoom;
            pictureBox1.Location = new Point(274, 153);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(527, 538);
            pictureBox1.TabIndex = 45;
            pictureBox1.TabStop = false;
            // 
            // panel2
            // 
            panel2.BackColor = Color.FromArgb(42, 62, 42);
            panel2.Location = new Point(59, 47);
            panel2.Name = "panel2";
            panel2.Size = new Size(10, 23);
            panel2.TabIndex = 46;
            // 
            // panel3
            // 
            panel3.BackColor = Color.FromArgb(42, 62, 42);
            panel3.Location = new Point(53, 47);
            panel3.Name = "panel3";
            panel3.Size = new Size(4, 23);
            panel3.TabIndex = 48;
            // 
            // MIDDLEWARE
            // 
            MIDDLEWARE.Controls.Add(panel5);
            MIDDLEWARE.Controls.Add(panel4);
            MIDDLEWARE.Location = new Point(2, 97);
            MIDDLEWARE.Name = "MIDDLEWARE";
            MIDDLEWARE.Size = new Size(40, 370);
            MIDDLEWARE.TabIndex = 49;
            MIDDLEWARE.Paint += middlewareVerticalText;
            // 
            // panel5
            // 
            panel5.BackColor = Color.FromArgb(42, 53, 42);
            panel5.Location = new Point(2, 305);
            panel5.Name = "panel5";
            panel5.Size = new Size(31, 11);
            panel5.TabIndex = 52;
            // 
            // panel4
            // 
            panel4.BackColor = Color.FromArgb(42, 53, 42);
            panel4.Location = new Point(2, 290);
            panel4.Name = "panel4";
            panel4.Size = new Size(31, 11);
            panel4.TabIndex = 51;
            // 
            // label2
            // 
            label2.AccessibleRole = AccessibleRole.TitleBar;
            label2.AutoSize = true;
            label2.Font = new Font("Bahnschrift Condensed", 9F, FontStyle.Bold);
            label2.ForeColor = Color.FromArgb(62, 81, 62);
            label2.Location = new Point(74, 34);
            label2.Name = "label2";
            label2.Size = new Size(59, 14);
            label2.TabIndex = 50;
            label2.Text = "PVAO MODULE";
            // 
            // hideToggle
            // 
            hideToggle.AutoSize = true;
            hideToggle.Font = new Font("Segoe UI", 8F);
            hideToggle.ForeColor = SystemColors.ScrollBar;
            hideToggle.Location = new Point(18, 536);
            hideToggle.Name = "hideToggle";
            hideToggle.Size = new Size(15, 14);
            hideToggle.TabIndex = 51;
            hideToggle.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.ForeColor = SystemColors.ScrollBar;
            label4.Location = new Point(36, 535);
            label4.Name = "label4";
            label4.Size = new Size(135, 15);
            label4.TabIndex = 53;
            label4.Text = "Always Minimize to Tray";
            // 
            // popUpPanel
            // 
            popUpPanel.BackColor = Color.FromArgb(42, 57, 42);
            popUpPanel.Controls.Add(closePopUp);
            popUpPanel.Controls.Add(panel13);
            popUpPanel.Controls.Add(popUpDesc);
            popUpPanel.Controls.Add(popUpTitle);
            popUpPanel.Location = new Point(209, 219);
            popUpPanel.Name = "popUpPanel";
            popUpPanel.Size = new Size(320, 170);
            popUpPanel.TabIndex = 57;
            popUpPanel.Visible = false;
            // 
            // closePopUp
            // 
            closePopUp.FlatAppearance.BorderSize = 0;
            closePopUp.FlatStyle = FlatStyle.Flat;
            closePopUp.Font = new Font("Segoe UI", 12F);
            closePopUp.ForeColor = SystemColors.ButtonFace;
            closePopUp.Location = new Point(281, 5);
            closePopUp.Name = "closePopUp";
            closePopUp.Size = new Size(31, 33);
            closePopUp.TabIndex = 6;
            closePopUp.Text = "⛌";
            closePopUp.UseVisualStyleBackColor = true;
            closePopUp.Click += closePopUp_Click;
            // 
            // panel13
            // 
            panel13.BackColor = Color.FromArgb(224, 224, 224);
            panel13.Location = new Point(17, 27);
            panel13.Name = "panel13";
            panel13.Size = new Size(10, 22);
            panel13.TabIndex = 2;
            // 
            // popUpDesc
            // 
            popUpDesc.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Italic);
            popUpDesc.ForeColor = SystemColors.Control;
            popUpDesc.Location = new Point(36, 68);
            popUpDesc.Name = "popUpDesc";
            popUpDesc.Size = new Size(249, 78);
            popUpDesc.TabIndex = 1;
            popUpDesc.Text = "Description of something";
            // 
            // popUpTitle
            // 
            popUpTitle.AutoSize = true;
            popUpTitle.Font = new Font("Microsoft Sans Serif", 14F, FontStyle.Bold);
            popUpTitle.ForeColor = SystemColors.ButtonHighlight;
            popUpTitle.Location = new Point(28, 27);
            popUpTitle.Name = "popUpTitle";
            popUpTitle.Size = new Size(66, 24);
            popUpTitle.TabIndex = 0;
            popUpTitle.Text = "TITLE";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            BackColor = Color.FromArgb(32, 42, 32);
            ClientSize = new Size(761, 570);
            Controls.Add(popUpPanel);
            Controls.Add(label4);
            Controls.Add(hideToggle);
            Controls.Add(label2);
            Controls.Add(panel3);
            Controls.Add(panel2);
            Controls.Add(label7);
            Controls.Add(exit);
            Controls.Add(minimize);
            Controls.Add(panel1);
            Controls.Add(pictureBox1);
            Controls.Add(MIDDLEWARE);
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form1";
            Text = "PVAO - TImekeeping Middleware";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            MIDDLEWARE.ResumeLayout(false);
            popUpPanel.ResumeLayout(false);
            popUpPanel.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Label label1;
        private Label label7;
        private Label label14;
        private Label label15;
        private ListView listView1;
        public NotifyIcon notifyIcon1;
        private Timer checkTimeTimer;
        private Timer syncTimer;
        private Label label13;
        private Label label21;
        private Timer pollingTimer;
        private Button minimize;
        private Button exit;
        private Panel panel1;
        private NotifyIcon DatabaseUnreachable;
        private Label label23;
        private Label label22;
        private Label label19;
        private PictureBox pictureBox1;
        private Button refresh;
        private Panel panel2;
        private Panel panel3;
        private Panel MIDDLEWARE;
        private Label label2;
        private Panel panel4;
        private Panel panel5;
        private Panel panel7;
        private Panel panel6;
        private Panel panel8;
        private CheckBox hideToggle;
        private Label label4;
        private Panel panel10;
        private Panel panel9;
        private Panel panel11;
        private Panel panel12;
        private Label labelClock;
        private Label label6;
        private Label label5;
        private Button syncTime;
        private ToolTip SyncServerTimeToolTip;
        private Panel popUpPanel;
        private Label popUpTitle;
        private Label popUpDesc;
        private Panel panel13;
        private Timer popUpVisibilityTimer;
        private Button closePopUp;
    }
}
