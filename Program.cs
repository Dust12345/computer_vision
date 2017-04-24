#region
using System;
using System.Windows.Forms;
using GLab.Core;
#endregion

namespace Frame.VrAibo
{
    internal static class Program
    {
        /// <summary>
        ///   The entry point of the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //GLabController.Instance.RegisterPlugin(new ComputerVision());
            GLabController.Instance.RegisterPlugin(new StereoVision());
            Application.Run(GLabController.Instance.Workspace);
        }
    }
}