using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ModbusMaster
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            long test = 1000;

            int count = sizeof(UInt64);
            Application.Run(new MasterForm());
        }
    }
}
