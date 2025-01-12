using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


//Class to support Recording Indicator
namespace WavConvert4Amiga
    {
        public class RecordingIndicator : Control
        {
            private Timer blinkTimer;
            private bool isVisible = true;
            private string recordingType = "system";

            public RecordingIndicator()
            {
                SetStyle(ControlStyles.UserPaint |
                        ControlStyles.AllPaintingInWmPaint |
                        ControlStyles.OptimizedDoubleBuffer, true);

                BackColor = Color.Black;
                ForeColor = Color.FromArgb(255, 215, 0); // Gold color for text
                Font = new Font("Consolas", 12f, FontStyle.Bold);

                blinkTimer = new Timer();
                blinkTimer.Interval = 500; // Blink every 500ms
                blinkTimer.Tick += BlinkTimer_Tick;

                // Add these properties to ensure visibility
                this.BringToFront();
            }

            public string RecordingType
            {
                get => recordingType;
                set
                {
                    recordingType = value;
                    Invalidate();
                }
            }

            public void StartBlinking()
            {
                isVisible = true;
                blinkTimer.Start();
            }

            public void StopBlinking()
            {
                blinkTimer.Stop();
                isVisible = true;
                Invalidate();
            }

            private void BlinkTimer_Tick(object sender, EventArgs e)
            {
                isVisible = !isVisible;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                if (!isVisible) return;

                using (var brush = new SolidBrush(Color.Black))
                {
                    e.Graphics.FillRectangle(brush, ClientRectangle);
                }

                // Draw border
                using (var pen = new Pen(Color.FromArgb(255, 215, 0), 2))
                {
                    e.Graphics.DrawRectangle(pen,
                        new Rectangle(1, 1, Width - 3, Height - 3));
                }

                // Draw circle (recording indicator)
                using (var brush = new SolidBrush(Color.Red))
                {
                    e.Graphics.FillEllipse(brush, 10, (Height - 10) / 2, 10, 10);
                }

                // Draw text
                using (var brush = new SolidBrush(ForeColor))
                {
                    string text = $"RECORDING ({recordingType})";
                    var textSize = e.Graphics.MeasureString(text, Font);
                    e.Graphics.DrawString(text, Font, brush,
                        new PointF(30, (Height - textSize.Height) / 2));
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    blinkTimer?.Dispose();
                }
                base.Dispose(disposing);
            }
    }
}

