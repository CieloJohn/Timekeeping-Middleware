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
    public partial class CustomTimePicker : UserControl
    {
        private TextBox hourBox;
        private TextBox minuteBox;
        private Label hourLabel;
        private Label minuteLabel;
        private Label amPmLabel;
        private ComboBox amPmCombo;
        private Button hourUp, hourDown, minuteUp, minuteDown;

        public CustomTimePicker()
        {
            InitializeControls();
        }

        private void InitializeControls()
        {
            this.BackColor = Color.FromArgb(32, 42, 32);
            this.Size = new Size(230, 50);

            Font labelFont = new Font("Segoe UI", 7);
            Font boxFont = new Font("Segoe UI", 10, FontStyle.Bold);

            hourLabel = new Label
            {
                Text = "hour",
                ForeColor = Color.FromArgb(114, 114, 114),
                Font = labelFont,
                Location = new Point(8, 0),
                AutoSize = true
            };

            hourBox = CreateTimeBox("12", new Point(5, 15));
            hourUp = CreateArrowButton(Properties.Resources.arrow_up, new Point(hourBox.Right + 2, 13), () => AdjustTimeBox(hourBox, 1, 1, 12));
            hourDown = CreateArrowButton(Properties.Resources.arrow_down, new Point(hourBox.Right + 2, 28), () => AdjustTimeBox(hourBox, -1, 1, 12));

            minuteLabel = new Label
            {
                Text = "minutes",
                ForeColor = Color.FromArgb(114, 114, 114),
                Font = labelFont,
                Location = new Point(63, 0),
                AutoSize = true
            };

            minuteBox = CreateTimeBox("00", new Point(63, 15));
            minuteUp = CreateArrowButton(Properties.Resources.arrow_up, new Point(minuteBox.Right + 2, 13), () => AdjustTimeBox(minuteBox, 1, 0, 59));
            minuteDown = CreateArrowButton(Properties.Resources.arrow_down, new Point(minuteBox.Right + 2, 28), () => AdjustTimeBox(minuteBox, -1, 0, 59));

            amPmLabel = new Label
            {
                Text = "AM/PM",
                ForeColor = Color.FromArgb(114, 114, 114),
                Font = labelFont,
                Location = new Point(125, 0),
                AutoSize = true
            };

            amPmCombo = new ComboBox
            {
                Location = new Point(120, 15),
                Size = new Size(60, 25),
                Font = boxFont,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(32, 43, 32),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };

            amPmCombo.DrawMode = DrawMode.OwnerDrawFixed;
            amPmCombo.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;
                e.DrawBackground();
                using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(32, 43, 32)))
                using (SolidBrush textBrush = new SolidBrush(Color.White))
                {
                    e.Graphics.FillRectangle(bgBrush, e.Bounds);
                    e.Graphics.DrawString(amPmCombo.Items[e.Index].ToString(), boxFont, textBrush, e.Bounds);
                }
                e.DrawFocusRectangle();
            };

            amPmCombo.Items.AddRange(new[] { "AM", "PM" });
            amPmCombo.SelectedIndex = DateTime.Now.Hour < 12 ? 0 : 1;

            this.Controls.AddRange(new Control[] {
                hourLabel, hourBox, hourUp, hourDown,
                minuteLabel, minuteBox, minuteUp, minuteDown,
                amPmLabel, amPmCombo
            });
        }

        private TextBox CreateTimeBox(string text, Point location)
        {
            var box = new TextBox
            {
                Text = text,
                Width = 35,
                Height = 25,
                Location = location,
                BackColor = Color.FromArgb(32, 42, 32),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center
            };

            box.KeyPress += (s, e) =>
            {
                e.Handled = !char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar);
            };

            return box;
        }

        private Button CreateArrowButton(Image img, Point location, Action onClick)
        {
            var btn = new Button
            {
                Size = new Size(15, 15),
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

        public TimeSpan SelectedTime
        {
            get
            {
                int hour = int.TryParse(hourBox.Text, out int h) ? h : 0;
                int minute = int.TryParse(minuteBox.Text, out int m) ? m : 0;

                string ampm = amPmCombo.SelectedItem?.ToString() ?? "AM";
                if (ampm == "PM" && hour < 12) hour += 12;
                if (ampm == "AM" && hour == 12) hour = 0;

                return new TimeSpan(hour, minute, 0);
            }
            set
            {
                int h = value.Hours;
                amPmCombo.SelectedItem = h < 12 ? "AM" : "PM";
                hourBox.Text = (h % 12 == 0 ? 12 : h % 12).ToString("D2");
                minuteBox.Text = value.Minutes.ToString("D2");
            }
        }

        public bool HasValidTime =>
            int.TryParse(hourBox.Text, out _) &&
            int.TryParse(minuteBox.Text, out _) &&
            amPmCombo.SelectedItem != null;
    }
}
