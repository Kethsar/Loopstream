﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LoopStream
{
    static class Program
    {
        public static bool debug = false;
        public static NotifyIcon ni;
        public static string tools;
        public static bool SIGNMODE;
        public static System.IO.StreamWriter log;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            //Console.WriteLine(LSSettings.version().ToString("x"));
            //Program.kill();

            SIGNMODE = false;
            if (args.Length > 0)
            {
                if (args[0] == "sign")
                {
                    SIGNMODE = true;
                }
            }

            //AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            if (!debug)
            {
                //log = new System.IO.StreamWriter("resolve.log", false, System.Text.Encoding.UTF8);
                //log.AutoFlush = true;
                AppDomain.CurrentDomain.AssemblyResolve += (sender, dargs) =>
                {
                    //log.WriteLine(DateTime.UtcNow.Ticks + "  " + dargs.Name + " // " + dargs.RequestingAssembly);
                    String resourceName = "LoopStream.lib." +
                        //new AssemblyName(dargs.Name).Name + ".dll";
                        dargs.Name.Substring(0, dargs.Name.IndexOf(", ")) + ".dll";
                    
                    using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                    {
                        if (stream == null) return null;
                        Byte[] assemblyData = new Byte[stream.Length];
                        stream.Read(assemblyData, 0, assemblyData.Length);
                        return Assembly.Load(assemblyData);
                    }
                };
            }

            if (!debug)
            {
                IconExtractor ie = new IconExtractor(Application.ExecutablePath);
                icon = ie.GetIcon(0);
                ie.Dispose();
            }
            else icon = new System.Drawing.Icon(@"..\..\res\loopstream.ico");
            tools = "LoopStreamTools\\";

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Home());
        }

        public static System.Drawing.Icon icon;

        [Obsolete()]
        static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string ns = "LoopStream";
            // Project -> LoopStream Properties -> Resources -> Add existing file -> *.dll
            // No further actions required. Also next line is workaround for win7 custom themes
            if (args.Name.Contains("PresentationFramework")) return null;
            string dllname = args.Name.Contains(',')
                ? args.Name.Substring(0, args.Name.IndexOf(','))
                : args.Name.Replace(".dll", "");

            dllname = dllname.Replace(".", "_");
            if (dllname.EndsWith("_resources"))return null;
            System.Resources.ResourceManager rm = new System.Resources.ResourceManager(ns + ".Properties.Resources",
                                                                                       System.Reflection.Assembly.GetExecutingAssembly());
            byte[] bytes = (byte[])rm.GetObject(dllname);
            return System.Reflection.Assembly.Load(bytes);
        }

        public static void kill()
        {
            if (ni != null) ni.Dispose();
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
    }
}
