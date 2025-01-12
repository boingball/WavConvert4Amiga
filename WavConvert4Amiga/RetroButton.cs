using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WavConvert4Amiga
{
    public class RetroButton : Button
    {
        public RetroButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.FromArgb(180, 190, 210);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (!Enabled)
            {
                // Disabled appearance
                using (var brush = new SolidBrush(Color.FromArgb(140, 150, 170))) // Darker gray
                {
                    e.Graphics.FillRectangle(brush, ClientRectangle);
                }
                // Draw text in darker color
                using (var brush = new SolidBrush(Color.FromArgb(100, 100, 100)))
                {
                    var textRect = ClientRectangle;
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        e.Graphics.DrawString(Text, Font, brush, textRect, sf);
                    }
                }
            }
            else
            {
                // Enabled appearance
                using (var brush = new SolidBrush(BackColor))
                {
                    e.Graphics.FillRectangle(brush, ClientRectangle);
                }

                // Draw text
                using (var brush = new SolidBrush(ForeColor))
                {
                    var textRect = ClientRectangle;
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        e.Graphics.DrawString(Text, Font, brush, textRect, sf);
                    }
                }
            }

            // Always draw 3D border
            ControlPaint.DrawBorder3D(e.Graphics, ClientRectangle, Border3DStyle.Raised);
        }

    }
}
