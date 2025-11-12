using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace offSiteTimekeeping_NET8.Assets.customUserControl
{
    public partial class CustomIntervalPicker : UserControl
    {
        private TextBox minutesBox;
        private TextBox secondsBox;
        private Label colonLabel;
        private Label secondsLabel;
        private Label minuteLabel;
        private Button secondsUp, secondsDown, minuteUp, minuteDown;

        public CustomIntervalPicker()
        {
            this.Size = new Size(140, 80);
            this.BackColor = Color.FromArgb(32, 42, 32);

            Font font = new Font("Segoe UI", 10, FontStyle.Bold);

            minuteLabel = new Label
            {
                Text = "minutes",
                AutoSize = true,
                Font = new Font("Segoe UI", 7),
                ForeColor = Color.FromArgb(114, 114, 114),
                Location = new Point(5, 1)
            };
            minutesBox = CreateTimeBox(new Point(5, 30));
            minuteUp = CreateArrowButton(Properties.Resources.arrow_up, new Point(10, 12), () => AdjustTimeBox(minutesBox, 1, 0, 59));
            minuteDown = CreateArrowButton(Properties.Resources.arrow_down, new Point(10, 58), () => AdjustTimeBox(minutesBox, -1, 0, 59));

            colonLabel = new Label
            {
                Text = ":",
                AutoSize = true,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(47, 30)
            };
            secondsLabel = new Label
            {
                Text = "seconds",
                AutoSize = true,
                Font = new Font("Segoe UI", 7),
                ForeColor = Color.FromArgb(114, 114, 114),
                Location = new Point(63, 1)
            };

            secondsBox = CreateTimeBox(new Point(63, 30));
            secondsUp = CreateArrowButton(Properties.Resources.arrow_up, new Point(70, 12), () => AdjustTimeBox(secondsBox, 1, 0, 59));
            secondsDown = CreateArrowButton(Properties.Resources.arrow_down, new Point(70, 58), () => AdjustTimeBox(secondsBox, -1, 0, 59));

            ApplyInputRestrictions(minutesBox, 0, 59);
            ApplyInputRestrictions(secondsBox, 0, 59);

            this.Controls.AddRange(new Control[] {
                minuteLabel, minutesBox, minuteUp, minuteDown,
                colonLabel,
                secondsLabel, secondsBox, secondsUp, secondsDown
            });
        }

        private TextBox CreateTimeBox(Point location)
        {
            return new TextBox
            {
                Text = "00",
                Width = 35,
                Height = 30,
                Location = location,
                BackColor = this.BackColor,
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center
            };
        }

        private Button CreateArrowButton(Image img, Point location, Action onClick)
        {
            var btn = new Button
            {
                Size = new Size(24, 15),
                Location = location,
                BackColor = Color.FromArgb(45, 60, 45),
                FlatStyle = FlatStyle.Flat,
                BackgroundImage = img,
                BackgroundImageLayout = ImageLayout.Zoom,

            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private void ApplyInputRestrictions(TextBox textBox, int min, int max)
        {
            textBox.KeyPress += (s, e) =>
            {
                e.Handled = !char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar);
            };

            textBox.TextChanged += (s, e) =>
            {
                if (int.TryParse(textBox.Text, out int value))
                {
                    if (value > max) textBox.Text = max.ToString("D2");
                    else if (value < min) textBox.Text = min.ToString("D2");
                }
                else if (!string.IsNullOrEmpty(textBox.Text))
                {
                    textBox.Text = min.ToString("D2");
                }

                textBox.SelectionStart = textBox.Text.Length;
            };
        }

        private void AdjustTimeBox(TextBox box, int delta, int min, int max)
        {
            if (int.TryParse(box.Text, out int val))
            {
                val += delta;
                if (val > max) val = min;
                if (val < min) val = max;
                box.Text = val.ToString("D2");
            }
        }

        public void SetToZero()
        {
            minutesBox.Text = "00";
            secondsBox.Text = "00";
        }

        public TimeSpan SelectedInterval
        {
            get
            {
                int minutes = int.TryParse(minutesBox.Text, out int m) ? m : 0;
                int seconds = int.TryParse(secondsBox.Text, out int s) ? s : 0;
                return new TimeSpan(0, minutes, seconds);
            }
        }

        public int IntervalMilliseconds => (int)SelectedInterval.TotalMilliseconds;

    }
}
