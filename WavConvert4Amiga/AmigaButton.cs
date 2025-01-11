using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WavConvert4Amiga
{
    public class AmigaButton : Button
    {
        public AmigaButton()
        {
            BackColor = Color.FromArgb(180, 190, 210); // Light blue-grey
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
            Font = new Font("Consolas", 9f); // Monospace font
        }
    }
}
