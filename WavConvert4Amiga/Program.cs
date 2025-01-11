using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WavConvert4Amiga
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                // Enable visual styles
                Application.EnableVisualStyles();

                // Set default text rendering - use true instead of false to enable ClearType
                Application.SetCompatibleTextRenderingDefault(true);

                // Run the application with proper exception handling
                using (var mainForm = new MainForm())
                {
                    Application.Run(mainForm);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Application Error: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                              "Error",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Error);
            }
        }
    }
}
 
