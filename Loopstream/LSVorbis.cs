using System;
using System.IO;

namespace Loopstream
{
    public class LSVorbis : LSEncoder
    {
        private VorbisEncoderStream m_ves;

        public LSVorbis(LSSettings settings, LSPcmFeed pimp) : base()
        {
            logger = Logger.ogg;

            this.pimp = pimp;
            this.settings = settings;
            logger.a("creating VorbisEncoder object");

            if (Environment.Is64BitProcess)
            {
                LoadLibrary(Path.Combine(Program.tools, @"64\libvorbis.dll"));
            }
            else
            {
                LoadLibrary(Path.Combine(Program.tools, @"32\libvorbis.dll"));
            }

            if (settings.ogg.compression == LSSettings.LSCompression.q)
            {
                m_ves = new VorbisEncoderStream(
                    settings.ogg.channels == LSSettings.LSChannels.stereo ? 2 : 1,
                    settings.samplerate,
                    (settings.ogg.quality * 0.1f));
            }
            else
            {
                m_ves = new VorbisEncoderStream(
                    settings.ogg.channels == LSSettings.LSChannels.stereo ? 2 : 1,
                    settings.samplerate,
                    (int)settings.ogg.bitrate);
            }

            LSTag.NewTags += (tags) =>
            {
                var sep = tags.LastIndexOf(" - ");
                var meta = new System.Collections.Generic.Dictionary<string, string>();

                if (sep > 0)
                {
                    meta.Add("ARTIST", tags.Substring(0, sep));
                    meta.Add("TITLE", tags.Substring(sep + 3));
                }
                else
                {
                    meta.Add("TITLE", tags);
                }

                m_ves.Encoder.ChangeMetaData(meta);
            };

            /*
            proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = Program.tools + "oggenc2.exe";
            proc.StartInfo.WorkingDirectory = Program.tools.Trim('\\');
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.Arguments = string.Format(
                "-Q -R 44100 {0} {1} {2} " + //.................target params
                "-r -F 1 -B 16 -C 2 --raw-endianness 0 -", //...source params
                (settings.ogg.compression == LSSettings.LSCompression.cbr ? "-b" : "-q"),
                (settings.ogg.compression == LSSettings.LSCompression.cbr ? settings.ogg.bitrate : settings.ogg.quality),
                (settings.ogg.channels == LSSettings.LSChannels.stereo ? "" : "--downmix"));

            if (!File.Exists(proc.StartInfo.FileName))
            {
                System.Windows.Forms.MessageBox.Show(
                    "Could not start streaming due to a missing required file:\r\n\r\n" + proc.StartInfo.FileName +
                    "\r\n\r\nThis is usually because whoever made your loopstream.exe fucked up",
                    "Shit wont fly", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                Program.kill();
            }

            logger.a("starting oggenc");
            proc.Start();
            while (true)
            {
                logger.a("waiting for oggenc");
                try
                {
                    proc.Refresh();
                    if (proc.Modules.Count > 1) break;

                    logger.a("modules: " + proc.Modules.Count);
                    System.Threading.Thread.Sleep(10);
                }
                catch { }
            }
            */
            /*foreach (System.Diagnostics.ProcessModule mod in proc.Modules)
            {
                Console.WriteLine(mod.ModuleName + " // " + mod.FileName);
            }*/
            logger.a("oggenc running");
            pstdin = m_ves;
            pstdout = m_ves;
            dump = settings.recOgg;
            enc = settings.ogg;
            makeShouter();
        }

        public override void Dispose()
        {
            if (m_ves != null)
                m_ves.Dispose();

            base.Dispose();
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);
    }
}
