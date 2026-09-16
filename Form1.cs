using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AdaptiveUIFuzzyWinForms
{
    /// <summary>
    /// Adaptive UI / Auto-Dimming Controller.
    ///
    /// Zero-order Sugeno fuzzy system: crisp inputs -> triangular membership (fuzzification)
    /// -> min/max rule inference -> weighted average of crisp output singletons (defuzzification).
    /// Layout is a wide 3-column card design so everything fits without vertical scrolling.
    /// </summary>
    public class Form1 : Form
    {
        // ---- Palette ----
        private static readonly Color BgColor = Color.FromArgb(244, 246, 249);
        private static readonly Color CardBorder = Color.FromArgb(225, 227, 232);
        private static readonly Color TextDark = Color.FromArgb(30, 34, 44);
        private static readonly Color TextMuted = Color.FromArgb(110, 116, 130);
        private static readonly Color AccentInputs = Color.FromArgb(51, 65, 85);    // slate
        private static readonly Color AccentLight = Color.FromArgb(37, 99, 235);    // blue
        private static readonly Color AccentTime = Color.FromArgb(124, 58, 237);    // violet
        private static readonly Color AccentRules = Color.FromArgb(234, 88, 12);    // orange
        private static readonly Color AccentOutput = Color.FromArgb(5, 150, 105);   // emerald

        // ---- Win32 progress bar coloring ----
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        private const int PBM_SETBARCOLOR = 0x409;
        private const int PBM_SETBKCOLOR = 0x2001;
        private readonly List<(ProgressBar bar, Color color)> _coloredBars = new();

        // ---- Inputs ----
        private NumericUpDown numAmbientLight = null!;
        private NumericUpDown numBattery = null!;
        private NumericUpDown numTimeOfDay = null!;
        private NumericUpDown numActivity = null!;
        private Button btnCalculate = null!;

        // ---- Ambient Light membership ----
        private ProgressBar barLightDark = null!, barLightDim = null!, barLightBright = null!;
        private Label lblLightDark = null!, lblLightDim = null!, lblLightBright = null!;

        // ---- Battery membership ----
        private ProgressBar barBattLow = null!, barBattMed = null!, barBattHigh = null!;
        private Label lblBattLow = null!, lblBattMed = null!, lblBattHigh = null!;

        // ---- Time of Day membership ----
        private ProgressBar barTimeNight = null!, barTimeDay = null!, barTimeEvening = null!;
        private Label lblTimeNight = null!, lblTimeDay = null!, lblTimeEvening = null!;

        // ---- User Activity membership ----
        private ProgressBar barActIdle = null!, barActActive = null!, barActIntense = null!;
        private Label lblActIdle = null!, lblActActive = null!, lblActIntense = null!;

        // ---- Rules ----
        private ProgressBar barR1 = null!, barR2 = null!, barR3 = null!, barR4 = null!, barR5 = null!, barR6 = null!, barR7 = null!;
        private Label lblR1 = null!, lblR2 = null!, lblR3 = null!, lblR4 = null!, lblR5 = null!, lblR6 = null!, lblR7 = null!;

        // ---- Output ----
        private Label lblOutput = null!;
        private Panel meterPanel = null!;
        private double _meterValue = 10.0;

        // ---- Live phone preview ----
        private Panel phoneScreen = null!;
        private Label lblPhonePercent = null!;

        public Form1()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Adaptive UI / Auto-Dimming Fuzzy Controller";
            this.ClientSize = new Size(1280, 740);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9F);
            this.BackColor = BgColor;

            var lblTitle = new Label
            {
                Text = "Adaptive UI / Auto-Dimming Controller",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = TextDark,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(20, 12),
                Size = new Size(1240, 32)
            };
            this.Controls.Add(lblTitle);

            var lblSubtitle = new Label
            {
                Text = "Zero-order Sugeno fuzzy controller",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = TextMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(20, 44),
                Size = new Size(1240, 20)
            };
            this.Controls.Add(lblSubtitle);

            // ===================== ROW 1: INPUTS + LIVE PREVIEW =====================
            var cardInputs = CreateCard("Crisp Inputs", new Point(20, 75), new Size(760, 260), AccentInputs);

            AddLabel(cardInputs, "Ambient Light (lux):", 15, 45, 160);
            numAmbientLight = AddNumeric(cardInputs, 185, 43, 90, 0, 1000, 150);

            AddLabel(cardInputs, "Time of Day (0-23.9 hr):", 400, 45, 170);
            numTimeOfDay = AddNumeric(cardInputs, 580, 43, 90, 0, 23.9M, 21.5M, 1, 0.5M);

            AddLabel(cardInputs, "Battery Level (%):", 15, 80, 160);
            numBattery = AddNumeric(cardInputs, 185, 78, 90, 0, 100, 40);

            AddLabel(cardInputs, "User Activity (0-100):", 400, 80, 170);
            numActivity = AddNumeric(cardInputs, 580, 78, 90, 0, 100, 15);

            btnCalculate = new Button
            {
                Text = "Calculate",
                Location = new Point(280, 170),
                Size = new Size(200, 42),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = AccentInputs,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnCalculate.FlatAppearance.BorderSize = 0;
            btnCalculate.Click += BtnCalculate_Click;
            cardInputs.Controls.Add(btnCalculate);

            var cardPhone = CreateCard("Live Preview", new Point(800, 75), new Size(460, 260), AccentOutput);

            var phoneBody = new Panel
            {
                Location = new Point(150, 40),
                Size = new Size(160, 150),
                BackColor = Color.FromArgb(25, 25, 28)
            };
            cardPhone.Controls.Add(phoneBody);

            phoneScreen = new Panel
            {
                Location = new Point(8, 10),
                Size = new Size(144, 130),
                BackColor = Color.FromArgb(15, 20, 35)
            };
            phoneBody.Controls.Add(phoneScreen);

            lblPhonePercent = new Label
            {
                Text = "10%",
                Font = new Font("Segoe UI", 17F, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            phoneScreen.Controls.Add(lblPhonePercent);

            lblOutput = new Label
            {
                Text = "Screen Brightness: --",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = AccentOutput,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(15, 198),
                Size = new Size(430, 22)
            };
            cardPhone.Controls.Add(lblOutput);

            meterPanel = new Panel
            {
                Location = new Point(15, 224),
                Size = new Size(430, 16),
                BackColor = Color.White
            };
            meterPanel.Paint += MeterPanel_Paint;
            cardPhone.Controls.Add(meterPanel);

            // ===================== ROW 2: 3 COLUMNS (LIGHT&BATTERY / TIME&ACTIVITY / RULES) =====================
            int row2Y = 355, row2H = 360;
            int colW = 400, col1X = 20, col2X = 440, col3X = 860;

            var cardLightBattery = CreateCard("Ambient Light & Battery — Membership", new Point(col1X, row2Y), new Size(colW, row2H), AccentLight);
            (barLightDark, lblLightDark) = AddMembershipRow(cardLightBattery, "Light - Dark:", 45, AccentLight);
            (barLightDim, lblLightDim) = AddMembershipRow(cardLightBattery, "Light - Dim:", 80, AccentLight);
            (barLightBright, lblLightBright) = AddMembershipRow(cardLightBattery, "Light - Bright:", 115, AccentLight);
            (barBattLow, lblBattLow) = AddMembershipRow(cardLightBattery, "Battery - Low:", 170, AccentLight);
            (barBattMed, lblBattMed) = AddMembershipRow(cardLightBattery, "Battery - Medium:", 205, AccentLight);
            (barBattHigh, lblBattHigh) = AddMembershipRow(cardLightBattery, "Battery - High:", 240, AccentLight);

            var cardTimeActivity = CreateCard("Time of Day & User Activity — Membership", new Point(col2X, row2Y), new Size(colW, row2H), AccentTime);
            (barTimeNight, lblTimeNight) = AddMembershipRow(cardTimeActivity, "Time - Night:", 45, AccentTime);
            (barTimeDay, lblTimeDay) = AddMembershipRow(cardTimeActivity, "Time - Day:", 80, AccentTime);
            (barTimeEvening, lblTimeEvening) = AddMembershipRow(cardTimeActivity, "Time - Evening:", 115, AccentTime);
            (barActIdle, lblActIdle) = AddMembershipRow(cardTimeActivity, "Activity - Idle:", 170, AccentTime);
            (barActActive, lblActActive) = AddMembershipRow(cardTimeActivity, "Activity - Active:", 205, AccentTime);
            (barActIntense, lblActIntense) = AddMembershipRow(cardTimeActivity, "Activity - Intense:", 240, AccentTime);

            var cardRules = CreateCard("Rules (Sugeno min/max)", new Point(col3X, row2Y), new Size(colW, row2H), AccentRules);
            (barR1, lblR1) = AddRuleRow(cardRules, "R1 (Bright): Light Bright OR (Batt High & Day)", 45, AccentRules);
            (barR2, lblR2) = AddRuleRow(cardRules, "R2 (Bright): Time Day AND Activity Intense", 87, AccentRules);
            (barR3, lblR3) = AddRuleRow(cardRules, "R3 (Medium): Light Dim AND Battery Medium", 129, AccentRules);
            (barR4, lblR4) = AddRuleRow(cardRules, "R4 (Medium): Time Evening AND Activity Active", 171, AccentRules);
            (barR7, lblR7) = AddRuleRow(cardRules, "R7 (Medium): Battery High AND Light Dim", 213, AccentRules);
            (barR5, lblR5) = AddRuleRow(cardRules, "R5 (Dim): Light Dark OR Battery Low", 255, AccentRules);
            (barR6, lblR6) = AddRuleRow(cardRules, "R6 (Dim): Time Night AND Activity Idle", 297, AccentRules);

            this.Load += Form1_Load;
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            foreach (var (bar, color) in _coloredBars)
            {
                bar.Style = ProgressBarStyle.Continuous;
                SendMessage(bar.Handle, PBM_SETBARCOLOR, IntPtr.Zero, (IntPtr)ColorTranslator.ToWin32(color));
                SendMessage(bar.Handle, PBM_SETBKCOLOR, IntPtr.Zero, (IntPtr)ColorTranslator.ToWin32(Color.FromArgb(232, 234, 238)));
            }

            BtnCalculate_Click(sender, e);
        }

        // ===================== UI HELPERS =====================

        private Panel CreateCard(string title, Point location, Size size, Color accent)
        {
            var card = new Panel
            {
                Location = location,
                Size = size,
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            card.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, card.ClientRectangle,
                CardBorder, 1, ButtonBorderStyle.Solid, CardBorder, 1, ButtonBorderStyle.Solid,
                CardBorder, 1, ButtonBorderStyle.Solid, CardBorder, 1, ButtonBorderStyle.Solid);
            this.Controls.Add(card);

            var accentBar = new Panel { Location = new Point(0, 0), Size = new Size(size.Width, 4), BackColor = accent };
            card.Controls.Add(accentBar);

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = accent,
                Location = new Point(15, 14),
                Size = new Size(size.Width - 30, 22),
                AutoEllipsis = true,
                UseMnemonic = false
            };
            card.Controls.Add(lblTitle);

            return card;
        }

        private void AddLabel(Panel parent, string text, int x, int y, int width)
        {
            parent.Controls.Add(new Label { Text = text, Location = new Point(x, y), Size = new Size(width, 23), ForeColor = TextDark });
        }

        private NumericUpDown AddNumeric(Panel parent, int x, int y, int width, decimal min, decimal max, decimal value, int decimals = 0, decimal increment = 1M)
        {
            var num = new NumericUpDown
            {
                Location = new Point(x, y),
                Size = new Size(width, 23),
                Minimum = min,
                Maximum = max,
                Value = value,
                DecimalPlaces = decimals,
                Increment = increment
            };
            parent.Controls.Add(num);
            return num;
        }

        /// <summary>
        /// Caption + progress bar + value, all on one line. Used for the two narrower
        /// (400px) membership cards, where captions like "Battery - Medium:" are short.
        /// </summary>
        private (ProgressBar bar, Label valueLabel) AddMembershipRow(Panel parent, string caption, int y, Color barColor)
        {
            var lblCaption = new Label
            {
                Text = caption,
                Location = new Point(15, y),
                Size = new Size(135, 23),
                ForeColor = TextDark,
                AutoEllipsis = true
            };

            var bar = new ProgressBar
            {
                Location = new Point(155, y),
                Size = new Size(185, 20),
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };

            var lblValue = new Label
            {
                Text = "0.00",
                Location = new Point(345, y),
                Size = new Size(40, 23),
                ForeColor = TextDark,
                TextAlign = ContentAlignment.MiddleRight
            };

            parent.Controls.Add(lblCaption);
            parent.Controls.Add(bar);
            parent.Controls.Add(lblValue);
            _coloredBars.Add((bar, barColor));

            return (bar, lblValue);
        }

        /// <summary>
        /// Caption on its own line, with the progress bar + value directly below it.
        /// Used for the Rules card, where captions are too long to sit beside a bar
        /// in a 400px-wide column.
        /// </summary>
        private (ProgressBar bar, Label valueLabel) AddRuleRow(Panel parent, string caption, int y, Color barColor)
        {
            var lblCaption = new Label
            {
                Text = caption,
                Location = new Point(15, y),
                Size = new Size(370, 16),
                Font = new Font("Segoe UI", 8.25F),
                ForeColor = TextDark,
                AutoEllipsis = true
            };

            var bar = new ProgressBar
            {
                Location = new Point(15, y + 18),
                Size = new Size(300, 18),
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };

            var lblValue = new Label
            {
                Text = "0.00",
                Location = new Point(320, y + 17),
                Size = new Size(45, 18),
                ForeColor = TextDark,
                TextAlign = ContentAlignment.MiddleRight
            };

            parent.Controls.Add(lblCaption);
            parent.Controls.Add(bar);
            parent.Controls.Add(lblValue);
            _coloredBars.Add((bar, barColor));

            return (bar, lblValue);
        }

        private void SetMembership(ProgressBar bar, Label label, double value)
        {
            bar.Value = (int)Math.Round(Math.Clamp(value, 0.0, 1.0) * 100);
            label.Text = value.ToString("F2");
        }

        private static Color Interpolate(Color a, Color b, double t)
        {
            t = Math.Clamp(t, 0.0, 1.0);
            int r = (int)(a.R + (b.R - a.R) * t);
            int g = (int)(a.G + (b.G - a.G) * t);
            int bl = (int)(a.B + (b.B - a.B) * t);
            return Color.FromArgb(r, g, bl);
        }

        private void MeterPanel_Paint(object? sender, PaintEventArgs e)
        {
            var rect = meterPanel.ClientRectangle;
            using (var brush = new LinearGradientBrush(rect, Color.FromArgb(30, 40, 70), Color.FromArgb(255, 200, 90), LinearGradientMode.Horizontal))
            {
                e.Graphics.FillRectangle(brush, rect);
            }
            using (var pen = new Pen(CardBorder))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, rect.Width - 1, rect.Height - 1);
            }

            int markerX = (int)(rect.Width * (_meterValue / 100.0));
            markerX = Math.Clamp(markerX, 1, rect.Width - 2);
            using var whitePen = new Pen(Color.White, 3);
            e.Graphics.DrawLine(whitePen, markerX, 0, markerX, rect.Height);
            using var outlinePen = new Pen(Color.FromArgb(40, 40, 40), 1);
            e.Graphics.DrawLine(outlinePen, markerX, 0, markerX, rect.Height);
        }

        private void BtnCalculate_Click(object? sender, EventArgs e)
        {
            double light = (double)numAmbientLight.Value;
            double battery = (double)numBattery.Value;
            double time = (double)numTimeOfDay.Value;
            double activity = (double)numActivity.Value;

            // ---- 1. FUZZIFICATION ----
            double lightDark = TriangularMembership(light, -300, 0, 300);
            double lightDim = TriangularMembership(light, 100, 350, 600);
            double lightBright = TriangularMembership(light, 400, 1000, 1600);

            double battLow = TriangularMembership(battery, -50, 0, 40);
            double battMed = TriangularMembership(battery, 20, 50, 80);
            double battHigh = TriangularMembership(battery, 60, 100, 150);

            double timeNight = Math.Max(
                TriangularMembership(time, -4, 0, 6),
                TriangularMembership(time, 18, 24, 28));
            double timeDay = TriangularMembership(time, 6, 13, 20);
            double timeEvening = TriangularMembership(time, 16, 20, 24);

            double actIdle = TriangularMembership(activity, -30, 0, 30);
            double actActive = TriangularMembership(activity, 15, 50, 85);
            double actIntense = TriangularMembership(activity, 70, 100, 130);

            SetMembership(barLightDark, lblLightDark, lightDark);
            SetMembership(barLightDim, lblLightDim, lightDim);
            SetMembership(barLightBright, lblLightBright, lightBright);

            SetMembership(barBattLow, lblBattLow, battLow);
            SetMembership(barBattMed, lblBattMed, battMed);
            SetMembership(barBattHigh, lblBattHigh, battHigh);

            SetMembership(barTimeNight, lblTimeNight, timeNight);
            SetMembership(barTimeDay, lblTimeDay, timeDay);
            SetMembership(barTimeEvening, lblTimeEvening, timeEvening);

            SetMembership(barActIdle, lblActIdle, actIdle);
            SetMembership(barActActive, lblActActive, actActive);
            SetMembership(barActIntense, lblActIntense, actIntense);

            // ---- 2. RULE EVALUATION (AND -> Min, OR -> Max) ----
            double r1 = Math.Max(lightBright, Math.Min(battHigh, timeDay));      // -> Bright
            double r2 = Math.Min(timeDay, actIntense);                          // -> Bright
            double r3 = Math.Min(lightDim, battMed);                            // -> Medium
            double r4 = Math.Min(timeEvening, actActive);                       // -> Medium
            double r7 = Math.Min(battHigh, lightDim);                           // -> Medium
            double r5 = Math.Max(lightDark, battLow);                           // -> Dim
            double r6 = Math.Min(timeNight, actIdle);                           // -> Dim

            SetMembership(barR1, lblR1, r1);
            SetMembership(barR2, lblR2, r2);
            SetMembership(barR3, lblR3, r3);
            SetMembership(barR4, lblR4, r4);
            SetMembership(barR7, lblR7, r7);
            SetMembership(barR5, lblR5, r5);
            SetMembership(barR6, lblR6, r6);

            double brightStrength = Math.Max(r1, r2);
            double mediumStrength = Math.Max(r3, Math.Max(r4, r7));
            double dimStrength = Math.Max(r5, r6);

            // ---- 3. DEFUZZIFICATION (Sugeno weighted average of singleton outputs) ----
            const double cDim = 10.0;
            const double cMedium = 50.0;
            const double cBright = 100.0;

            double numerator = (dimStrength * cDim) + (mediumStrength * cMedium) + (brightStrength * cBright);
            double denominator = dimStrength + mediumStrength + brightStrength;

            double brightnessOutput = denominator > 0 ? numerator / denominator : 50.0;

            string category = brightnessOutput <= 25 ? "DIM" : brightnessOutput <= 65 ? "MEDIUM" : "BRIGHT";
            lblOutput.Text = $"{brightnessOutput:F2}% — {category}";

            _meterValue = brightnessOutput;
            meterPanel.Invalidate();

            phoneScreen.BackColor = Interpolate(Color.FromArgb(15, 20, 35), Color.FromArgb(255, 250, 235), brightnessOutput / 100.0);
            lblPhonePercent.Text = $"{brightnessOutput:F0}%";
            lblPhonePercent.ForeColor = brightnessOutput > 55 ? Color.FromArgb(30, 30, 30) : Color.White;
        }

        /// <summary>
        /// Triangular membership function. a = left foot, b = peak, c = right foot.
        /// Feet placed outside the variable's real range (e.g. a &lt; 0) create a
        /// "shoulder" that saturates at 1.0 at the extreme end of the range.
        /// </summary>
        private static double TriangularMembership(double x, double a, double b, double c)
        {
            if (x <= a || x >= c)
                return 0.0;

            if (x == b)
                return 1.0;

            if (x > a && x < b)
                return (x - a) / (b - a);

            return (c - x) / (c - b);
        }
    }
}
