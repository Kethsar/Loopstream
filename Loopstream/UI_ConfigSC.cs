﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using System.Text.RegularExpressions;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Loopstream
{
    public partial class ConfigSC : Form
    {
        public ConfigSC(LSSettings settings, Rectangle bounds)
        {
            this.settings = settings;
            this.bounds = bounds;
            this.Location = new Point(
                bounds.Left + bounds.Width / 2 - this.Width / 2,
                bounds.Top + bounds.Height / 2 - this.Height / 2);

            InitializeComponent();
            pTabs.BringToFront();
        }

        Timer tPop;
        Control popTop;
        bool disregardEvents;
        public LSSettings settings; //, apply;
        Rectangle bounds;
        bool doRegexTests;
        string welcomeText;

        private void gCancel_Click(object sender, EventArgs e)
        {
            apply(false);
        }

        private void gSave_Click(object sender, EventArgs e)
        {
            /*StringBuilder sb = new StringBuilder();
            sb.AppendLine(((LSDevice)gOutS.SelectedItem).id + "\t" + gOutS.SelectedItem);
            sb.AppendLine(((LSDevice)gOneS.SelectedItem).id + "\t" + gOneS.SelectedItem);
            sb.AppendLine(((LSDevice)gTwoS.SelectedItem).id + "\t" + gTwoS.SelectedItem);
            System.IO.File.WriteAllText("Loopstream.ini", sb.ToString());*/
            apply(true);
        }

        void apply(bool save)
        {
            unFX();
            if (settings.devRec == null ||
                settings.devOut == null)
            {
                MessageBox.Show("Sorry, you need to select all three audio devices.");
                return;
            }
            int[] samplerates = {
                8000, 11025, 16000, 22050, 32000,
                44100, 48000, 88200, 96000
            };
            bool sampleOk = false;
            foreach (int sr in samplerates)
            {
                if (settings.samplerate == sr)
                {
                    sampleOk = true;
                }
            }
            if (!sampleOk)
            {
                string msg = "";
                foreach (int i in samplerates)
                {
                    msg += i.ToString("#,##0")
                        .Replace(',', '\'')
                        .Replace('.', '\'') + "\n";
                }
                if (DialogResult.Yes ==
                    MessageBox.Show("Your samplerate is most likely fucked up.\n\n" +
                    "You probably want 44'100.  Wanna cancel and fix it?\n\n" +
                    "Here are some choices:\n\n" +
                    msg.Trim('\n'), "bad samplerate",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning))
                {
                    return;
                }
            }



            string smp = "";
            string fail = "";
            string dbg = "";

            if (settings.devRec is LSDevice)
                try
                {
                    if (((LSDevice)settings.devRec).wf == null)
                    {
                        ((LSDevice)settings.devRec).test();
                    }
                    NAudio.Wave.WaveFormat wf = ((LSDevice)settings.devRec).wf;
                    if (wf.SampleRate != settings.samplerate)
                    {
                        smp += "music=" + wf.SampleRate + ", ";
                    }
                    if (Program.debug)
                    {
                        dbg += "music=" + LSDevice.stringer(wf) + "\n\n";
                    }
                }
                catch
                {
                    fail += "music, ";
                }

            if (settings.devMic != null && ((LSDevice)settings.devMic).mm != null)
            {
                try
                {
                    if (((LSDevice)settings.devMic).wf == null)
                    {
                        ((LSDevice)settings.devMic).test();
                    }
                    NAudio.Wave.WaveFormat wf = ((LSDevice)settings.devMic).wf;
                    if (wf.SampleRate != settings.samplerate)
                    {
                        smp += "mic=" + wf.SampleRate + ", ";
                    }
                    if (Program.debug)
                    {
                        dbg += "mic=" + LSDevice.stringer(wf) + "\n\n";
                    }
                }
                catch
                {
                    fail += "mic, ";
                }
            }
            if (!string.IsNullOrEmpty(fail))
            {
                MessageBox.Show("I am sorry to say that some devices (" + fail.Trim(',', ' ') + ") fail to open.\nThis will probably cause issues :(",
                    "Critical error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            if (!string.IsNullOrEmpty(smp))
            {
                if (DialogResult.Yes == MessageBox.Show(
                    "Some of your devices (" + smp.Trim(',', ' ') + ")\n" + 
                    "have a different samplerate than what you\n" +
                    "specified in the settings (" + settings.samplerate + " Hz).\n" +
                    "\n" +
                    "This will still work, but there will be:\n" +
                    "    - slightly reduced sound quality\n" +
                    "    - more latency / audio delay\n" +
                    "    - more load on your computer\n\n" +
                    "Do you have time to fix this for\n" +
                    "the ultimate r/a/dio experience?",
                    "HEY! LISTEN!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning))
                {
                    samplerateTutorial();
                }
            }
            if (Program.debug)
            {
                if (DialogResult.No == MessageBox.Show("-!- DEBUG (no=clipboard)\n\n" + dbg, "DEBUG", MessageBoxButtons.YesNo, MessageBoxIcon.Information))
                {
                    Clipboard.Clear();
                    Clipboard.SetText(dbg);
                }
            }
            if (save)
            {
                settings.save();
            }
            this.Close();
        }

        string resolveBinary(string exe)
        {
            if (System.IO.File.Exists(exe))
            {
                return System.IO.Path.GetFullPath(exe);
            }
            var vals = Environment.GetEnvironmentVariable("PATH");
            foreach (string path in vals.Split(';'))
            {
                var fp = System.IO.Path.Combine(path, exe);
                if (System.IO.File.Exists(fp))
                {
                    return fp;
                }
            }
            return null;
        }

        void samplerateTutorial()
        {
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.Arguments = "shell32.dll,Control_RunDLL mmsys.cpl,,playback";
            proc.StartInfo.FileName = "rundll32.exe";
            proc.Start();
            MessageBox.Show("I have taken the liberty of opening\nyour soundcard control panel.\n\n" +
                "Here's what you'll do:\n\n" +
                "1. Rightclick the first playback device\n" +
                "2. Select «Properties»\n" +
                "3. Open the «Advanced» tab\n" +
                "4. Change «Default Format» to this:\n" +
                "        16 bit, " + settings.samplerate + " Hz (whatever)\n" +
                "5. Press OK\n\n" +
                "Now do that with all your playback devices,\nthen press OK on both the soundcard\ncontrol window and this message.");
            
            proc = new System.Diagnostics.Process();
            proc.StartInfo.Arguments = "shell32.dll,Control_RunDLL mmsys.cpl,,recording";
            proc.StartInfo.FileName = "rundll32.exe";
            proc.Start();
            MessageBox.Show("Sorry, but you're not finished just yet.\n" +
                "Don't worry, you're getting good at this!\n\n" +
                "1. Rightclick the first recording device\n" +
                "2. Select «Properties»\n" +
                "3. Open the «Advanced» tab\n" +
                "4. Change «Default Format» to this:\n" +
                "        2 channel, 16 bit, " + settings.samplerate + " Hz (whatever)\n" +
                "5. Press OK\n\n" +
                "==== if you can't see a «2 channel»\n" +
                "==== option, then «1 channel» is OK!\n\n" +
                "Same procedure like last year, do that on all\nof your recording devices, then hit OK.\n\n" +
                "Thanks, now you're ready to fly!");

            reDevice();
            initDevs();
            string bads = "";
            foreach (LSAudioSrc idev in settings.devs)
            {
                if (!(idev is LSDevice))
                    continue;
                
                var dev = (LSDevice)idev;

                if (dev != null &&
                    dev.wf != null &&
                    dev.wf.SampleRate != settings.samplerate)
                {
                    bads += (dev.mm.DataFlow == DataFlow.All ||
                             dev.mm.DataFlow == DataFlow.Render ?
                             "Music" : "Microphone") + " source:   " +
                             dev.name + "\n";
                }
            }
            if (!string.IsNullOrEmpty(bads))
            {
                MessageBox.Show("The following audio devices are still not using the correct samplerate (" + settings.samplerate + "):\n\n" + bads);
            }
        }

        private void ConfigSC_Load(object sender, EventArgs e)
        {
            welcomeText = gDescText.Text;
            this.Icon = Program.icon;
            disregardEvents = true;
            Timer t = new Timer();
            t.Tick += t_Tick;
            t.Interval = 100;
            t.Start();

            tPop = new Timer();
            tPop.Interval = tt.InitialDelay;
            tPop.Tick += tPop_Tick;
        }
        TLabel[] tabHeader;
        void t_Tick(object sender, EventArgs e)
        {
            ((Timer)sender).Stop();
            initForm();

            tabHeader = new TLabel[] { hSoundcard, hServer, hEncoders, hTags, hTriggers };
            pTabs.BringToFront();
            for (int a = 0; a < tabHeader.Length; a++)
            {
                tabHeader[a].MouseDown += tabHeader_MouseDown;
            }
            tabHeader_MouseDown(tabHeader[0], new MouseEventArgs(System.Windows.Forms.MouseButtons.Left, 1, 20, 10, 0));

            if (!System.IO.File.Exists(Program.beta > 0 ? "Loopstream-beta.ini" : "Loopstream.ini"))
            {
                if (DialogResult.Yes == MessageBox.Show("Since this is your first time,  wanna use the setup wizard?\n\nThis will start your default web browser,\nand you might get a firewall warning", "Loopstream for Dummies", MessageBoxButtons.YesNo))
                {
                    new System.Threading.Thread(new System.Threading.ThreadStart(wizard)).Start();
                }
            }

            Timer asdf = new Timer();
            asdf.Interval = 100;
            asdf.Start();
            asdf.Tick += new EventHandler(readServerSharekey_tick);

            gHost.ContextMenu = new ContextMenu();
            gUser.ContextMenu = new ContextMenu();
            gPass.ContextMenu = new ContextMenu();
            gMount.ContextMenu = new ContextMenu();
            gHost.ContextMenuStrip = new ContextMenuStrip();
            gUser.ContextMenuStrip = new ContextMenuStrip();
            gPass.ContextMenuStrip = new ContextMenuStrip();
            gMount.ContextMenuStrip = new ContextMenuStrip();
            gHost.ShortcutsEnabled = true;
            gUser.ShortcutsEnabled = true;
            gPass.ShortcutsEnabled = true;
            gMount.ShortcutsEnabled = true;
        }

        bool readServerSharekey = false;
        void readServerSharekey_tick(object sender, EventArgs e)
        {
            if (readServerSharekey)
            {
                readServerSharekey = false;
                bool first = true;
                while (true)
                {
                    try
                    {
                        if (Clipboard.ContainsText())
                        {
                            string key = Clipboard.GetText();
                            if (!string.IsNullOrEmpty(key) &&
                                key.Contains("cs#"))
                            {
                                try
                                {
                                    int i = key.IndexOf("cs#");
                                    int j = key.IndexOf("#", i + 4);
                                    if (i < 0 || j < 0) throw new Exception();
                                    i += 3;
                                    key = key.Substring(i, j - i);
                                }
                                catch
                                {
                                    MessageBox.Show(
                                        "Padding error\n\n" +
                                        "Sorry, but the share-key is invalid.");
                                    throw new Exception();
                                }
                                try
                                {
                                    key = Z.gzd(key);
                                }
                                catch
                                {
                                    MessageBox.Show(
                                        "Decoder error\n\n" +
                                        "Sorry, but the share-key is invalid.");
                                    throw new Exception();
                                }
                                string[] lines = key.Split('\n');
                                /*
                                    +(gHost.ReadOnly ? "1" : "0")
                                    + (gUser.ReadOnly ? "1" : "0")
                                    + (gPass.ReadOnly ? "1" : "0")
                                    + (gMount.ReadOnly ? "1" : "0")
                                    + " " + gServerSel.Text
                                    + "\n" + settings.host
                                    + "\n" + settings.port
                                    + "\n" + settings.user
                                    + "\n" + settings.pass
                                    + "\n" + settings.mount;
                                */
                                settings.host = lines[1];
                                settings.port = Convert.ToInt32(lines[2]);
                                settings.user = lines[3];
                                settings.pass = lines[4];
                                settings.mount = lines[5];
                                visualizeServerSettings();

                                gUnhide.Checked = true;
                                gServerSel.Text = lines[0].Substring(5);
                                if (lines[0][0] != '1') toggleReadOnly(gHost, null); else gHost.Text = "";
                                if (lines[0][1] != '1') toggleReadOnly(gUser, null); else gUser.Text = "";
                                if (lines[0][2] != '1') toggleReadOnly(gPass, null); else gPass.Text = "";
                                if (lines[0][3] != '1') toggleReadOnly(gMount, null); else gMount.Text = "";
                                MessageBox.Show("Loaded server settings «" + gServerSel.Text + "»");
                                if (gPass.Left > 0) gPass.Focus();
                                return;

                                // ##cs#H4sIAAAAAAAEADMwMDRQyC0tScwrUUgpUfDxDHPlgnCLElMy8/Xyi9K5LAwMDLiK80uLklO5kjMS89JTFUoyMou5cjLLUgGemTG0QAAAAA==##
                            }
                        }
                    }
                    catch { }
                    if (first)
                    {
                        if (DialogResult.Yes != MessageBox.Show(
                            "Do you have a share-key to automatically fill in server details?",
                            "Server Settings Share-Key", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                        {
                            return;
                        }
                    }
                    else
                    {
                        if (DialogResult.Retry != MessageBox.Show(
                            "Could not find the radio server settings in your clipboard.",
                            "Server Settings Share-Key", MessageBoxButtons.RetryCancel, MessageBoxIcon.Exclamation))
                        {
                            return;
                        }
                    }
                    first = false;
                }
            }
        }

        long lastTAB = 0;
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.F1))
            {
                new System.Threading.Thread(new System.Threading.ThreadStart(wizard)).Start();
                return true;
            }
            if (keyData == (Keys.Tab) ||
                keyData == (Keys.Tab | Keys.Shift))
            {
                lastTAB = DateTime.UtcNow.Ticks;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        void tabHandler(object sender, EventArgs e)
        {
            long now = DateTime.UtcNow.Ticks;
            //this.Text = (now - lastTAB).ToString();
            if (now - lastTAB < 10000 * 20)
            {
                ((TextBox)sender).SelectAll();
            }
        }

        void hookFocus(Control.ControlCollection cc)
        {
            foreach (Control c in cc)
            {
                hookFocus(c.Controls);
                if (c.GetType() == typeof(TextBox))
                {
                    c.GotFocus += tabHandler;
                }
            }
        }

        void initDevs()
        {
            var ov = disregardEvents;
            disregardEvents = true;

            gMusS.Items.Clear();
            gMicS.Items.Clear();
            gOutS.Items.Clear();
            populate(true, gOutS, gMusS);
            populate(false, gMicS);
            gMusS.Items.Add(settings.wavetailer);

            LSDevice nil = new LSDevice();
            nil.name = "(disabled)";
            gMicS.Items.Insert(0, nil);

            gMusS.SelectedItem = settings.devRec;
            gMicS.SelectedItem = settings.devMic;
            gOutS.SelectedItem = settings.devOut;

            if (settings.devMic == null ||
                ((LSDevice)settings.devMic).mm == null)
                gMicS.SelectedIndex = 0;

            freshenChanselButtons();
            disregardEvents = ov;
        }

        void freshenChanselButtons()
        {
            freshenChansel(gMusB, settings.devRec, settings.chRec);
            freshenChansel(gMicB, settings.devMic, settings.chMic);
            freshenChansel(gOutB, settings.devOut, settings.chOut);
            gMicB.Enabled = settings.devMic != null && ((LSDevice)settings.devMic).wf != null;
            gOutB.Enabled = settings.devOut != null && ((LSDevice)settings.devOut).wf != null;
        }

        void freshenChansel(Control ctl, LSAudioSrc asrc, int[] chans)
        {
            if (asrc is LSWavetail)
            {
                ctl.Text = "Configure";
                ctl.Enabled = true;
                return;
            }

            var dev = (LSDevice)asrc;
            
            if (dev == null || dev.wf == null)
            {
                ctl.Enabled = false;
                ctl.Text = "Channels";
                return;
            }
            ctl.Enabled = true;
            
            if (chans.Length == 0)
            {
                ctl.Text = "? the fuck";
                return;
            }

            if (dev.wf.Channels == 1)
            {
                ctl.Text = "Mono";
                return;
            }
            
            string chlist = "";
            if (dev.wf.Channels == 2)
            {
                if (chans.Length == 2)
                    chlist = "Stereo";

                else if (chans.Contains(0))
                    chlist = "Mono (L)";

                else if (chans.Contains(1))
                    chlist = "Mono (R)";

                else throw new Exception("invalid num chans " + chlist.Length);
            }

            if (dev.wf.Channels > 2)
            {
                foreach (var n in chans)
                    chlist += (chlist == "" ? "" : " and ") + (n + 1);
            }

            ctl.Text = chlist;
        }

        void initForm()
        {
            disregardEvents = true;
            initDevs();

            gRate.Text = settings.samplerate.ToString();
            gReverbP.Text = settings.reverbP.ToString();
            gReverbS.Text = settings.reverbS.ToString();

            gMp3Enable.Checked = settings.mp3.enabled;
            gMp3Bitrate.Checked = settings.mp3.compression == LSSettings.LSCompression.cbr;
            gMp3Quality.Checked = settings.mp3.compression == LSSettings.LSCompression.q;
            gMp3BitrateV.Text = settings.mp3.bitrate.ToString();
            gMp3QualityV.Text = settings.mp3.quality.ToString();
            gMp3Mono.Checked = settings.mp3.channels == LSSettings.LSChannels.mono;
            gMp3Stereo.Checked = settings.mp3.channels == LSSettings.LSChannels.stereo;
            
            gOggEnable.Checked = settings.ogg.enabled;
            gOggBitrate.Checked = settings.ogg.compression == LSSettings.LSCompression.cbr;
            gOggQuality.Checked = settings.ogg.compression == LSSettings.LSCompression.q;
            gOggBitrateV.Text = settings.ogg.bitrate.ToString();
            gOggQualityV.Text = settings.ogg.quality.ToString();
            gOggMono.Checked = settings.ogg.channels == LSSettings.LSChannels.mono;
            gOggStereo.Checked = settings.ogg.channels == LSSettings.LSChannels.stereo;

            gOpusEnable.Checked = settings.opus.enabled;
            gOpusQualityV.Text = settings.opus.quality.ToString();
            gOpusMono.Checked = settings.opus.channels == LSSettings.LSChannels.mono;
            gOpusStereo.Checked = settings.opus.channels == LSSettings.LSChannels.stereo;

            visualizeServerSettings();

            doRegexTests = false;
            gTestDevs.Checked = settings.testDevs;
            gUnavail.Checked = settings.showUnavail;
            gSplash.Checked = settings.splash;
            gVU.Checked = settings.vu;
            gKillMic.Checked = settings.killmic;
            gRecPCM.Checked = settings.recPCM;
            gRecMP3.Checked = settings.recMp3;
            gRecOGG.Checked = settings.recOgg;
            gRecOpus.Checked = settings.recOpus;
            gAutoconn.Checked = settings.autoconn;
            gAutohide.Checked = settings.autohide;

            //gCPoor_DEPRECATED.Checked = settings.warn_poor_DEPRECATED;
            //gCDrop_DEPRECATED.Checked = settings.warn_drop_DEPRECATED;
            //gLPoor_DEPRECATED.Text = Math.Round(settings.lim_poor_DEPRECATED * 100) + "";
            //gLDrop_DEPRECATED.Text = Math.Round(settings.lim_drop_DEPRECATED * 100) + "";

            gTagAuto.Checked = settings.tagAuto;
            gReader.Items.Clear();
            gReader.Items.Add(LSSettings.LSMeta.Reader.WindowCaption);
            gReader.Items.Add(LSSettings.LSMeta.Reader.File);
            gReader.Items.Add(LSSettings.LSMeta.Reader.Website);
            gReader.Items.Add(LSSettings.LSMeta.Reader.ProcessMemory);
            
            gTarget.Visible = false;
            gMake32.Visible = false;
            loadMetaReader(true);


            if (settings.pass == "hackme")
            {
                gUnhide.Checked = true;
            }
            hookFocus(this.Controls);


            var event_types = Enum.GetValues(typeof(LSSettings.LSTrigger.EventType));
            foreach (var et in event_types)
            {
                gEvType.Items.Add(et);
            }
            loadTriggers();
            

            disregardEvents = false;
        }

        void visualizeServerSettings()
        {
            gHost.Text = settings.host + ":" + settings.port;
            gUser.Text = settings.user;
            gPass.Text = settings.pass;
            gMount.Text = settings.mount;
            gShout.Checked = settings.relay == LSSettings.LSRelay.shout;
            gIce.Checked = settings.relay == LSSettings.LSRelay.ice;
            gSiren.Checked = settings.relay == LSSettings.LSRelay.siren;
            gTitle.Text = settings.title;
            gDescription.Text = settings.description;
            gGenre.Text = settings.genre;
            gURL.Text = settings.url;
            gPublic.Checked = settings.pubstream;

            gServerSel.Items.Clear();
            LSSettings.LSServerPreset activePreset = null;
            settings.serverPresets.Sort((x, y) => x.presetName.CompareTo(y.presetName));
            foreach (LSSettings.LSServerPreset preset in settings.serverPresets)
            {
                gServerSel.Items.Add(preset);
                if (preset.Matches(settings))
                {
                    activePreset = preset;
                }
            }
            gServerSel.SelectedItem = activePreset;
            if (activePreset == null)
            {
                gServerSel.Text = "(unsaved)";
            }
        }

        void reDevice()
        {
            //Splesh spl = new Splesh();
            //spl.Show();
            //Application.DoEvents();
            var p = new Progress(panel3, label31);
            gMusS.Enabled = gMicS.Enabled = gOutS.Enabled = false;
            
            settings.init();
            settings.runTests(p, true);
            label31.Visible = false;

            gMusS.Enabled = gMicS.Enabled = gOutS.Enabled = true;
            //spl.gtfo();
        }

        void tabHeader_MouseDown(object sender, MouseEventArgs e)
        {
            int i = Array.IndexOf(tabHeader, sender);
            TLabel ass = tabHeader[i];
            if (i > 0 && e.X < 8) i--;
            if (i < tabHeader.Length - 1 && e.X > ass.Width - 8) i++;
            //ass = tabHeader[i];
            setTab(i);
        }

        void setTab(int i)
        {
            TLabel ass = tabHeader[i];
            foreach (TLabel l in tabHeader)
            {
                l.ForeColor = SystemColors.ControlDark;
                l.ShadeBack = false;
                //l.SendToBack();
            }
            label5.BringToFront();
            ass.BringToFront();
            ass.ShadeBack = true;
            ass.ForeColor = SystemColors.ControlText;
            //tabPage[i].BringToFront();
            tc.SelectedIndex = i;
        }

        void populate(bool playback, params ComboBox[] lb)
        {
            foreach (ComboBox l in lb)
            {
                l.Items.Clear();
            }
            foreach (LSAudioSrc idev in settings.devs)
            {
                if (!(idev is LSDevice))
                    continue;

                var lsd = (LSDevice)idev;

                if ((playback && lsd.isPlay) || (!playback && lsd.isRec))
                {
                    foreach (ComboBox l in lb)
                    {
                        if (lsd.wf != null || settings.showUnavail || !settings.testDevs)
                        {
                            l.Items.Add(lsd);
                        }
                    }
                }
            }
        }

        void indicateOK(TextBox tb)
        {
            tb.BackColor = SystemColors.Window;
            tb.ForeColor = SystemColors.WindowText;
        }

        void indicateError(TextBox tb)
        {
            tb.BackColor = Color.Firebrick;
            tb.ForeColor = Color.Gold;
        }

        int getValue(TextBox tb)
        {
            int v;
            if (Int32.TryParse(tb.Text, out v))
            {
                indicateOK(tb);
                return v;
            }
            else
            {
                indicateError(tb);
                return -1;
            }
        }

        private void gHost_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (gHost.Text.Length < 2)
                {
                    settings.host = "";
                    settings.port = 3;
                }
                else
                {
                    settings.host = gHost.Text.Replace(" ", "").Split(':')[0];
                    settings.port = Convert.ToInt32(gHost.Text.Split(':')[1]);
                }
                gHost.BackColor = SystemColors.Window;
                gHost.ForeColor = SystemColors.WindowText;
            }
            catch
            {
                gHost.BackColor = Color.Firebrick;
                gHost.ForeColor = Color.Gold;
            }
        }

        private void gUser_TextChanged(object sender, EventArgs e)
        {
            if (gUser.Text.Length > 0)
            {
                settings.user = gUser.Text;
                gUser.BackColor = SystemColors.Window;
                gUser.ForeColor = SystemColors.WindowText;
            }
            else
            {
                gUser.BackColor = Color.Firebrick;
                gUser.ForeColor = Color.Gold;
            }
        }

        private void gPass_TextChanged(object sender, EventArgs e)
        {
            if (gPass.Text.Length > 0)
            {
                settings.pass = gPass.Text;
                gPass.BackColor = SystemColors.Window;
                gPass.ForeColor = SystemColors.WindowText;
            }
            else
            {
                gPass.BackColor = Color.Firebrick;
                gPass.ForeColor = Color.Gold;
            }
        }

        private void gMount_TextChanged(object sender, EventArgs e)
        {
            if (gMount.Text.Length > 0)
            {
                settings.mount = gMount.Text.TrimStart('/');
                if (settings.mount.EndsWith(".mp3") ||
                    settings.mount.EndsWith(".ogg") ||
                    settings.mount.EndsWith(".opus") ||
                    settings.mount.EndsWith(".aac"))
                {
                    settings.mount =
                        settings.mount.Substring(0,
                        settings.mount.LastIndexOf('.'));
                }
                gMount.BackColor = SystemColors.Window;
                gMount.ForeColor = SystemColors.WindowText;
            }
            else
            {
                gMount.BackColor = Color.Firebrick;
                gMount.ForeColor = Color.Gold;
            }
        }

        private void gShout_CheckedChanged(object sender, EventArgs e)
        {
            if (gShout.Checked) settings.relay = LSSettings.LSRelay.shout;
        }

        private void gIce_CheckedChanged(object sender, EventArgs e)
        {
            if (gIce.Checked) settings.relay = LSSettings.LSRelay.ice;
        }

        private void gSiren_CheckedChanged(object sender, EventArgs e)
        {
            if (gSiren.Checked) settings.relay = LSSettings.LSRelay.siren;
        }

        private void gOutS_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (disregardEvents)
                return;

            settings.devOut = (LSAudioSrc)gOutS.SelectedItem;
            freshenChanselButtons();
            playFX((LSDevice)settings.devOut);
        }

        private void gOneS_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (disregardEvents)
                return;

            settings.devRec = (LSAudioSrc)gMusS.SelectedItem;
            //if (Program.debug) MessageBox.Show(LSDevice.stringer(settings.devRec.wf));
            freshenChanselButtons();
            if (settings.devRec is LSDevice)
                playFX((LSDevice)settings.devRec);
        }

        private void gTwoS_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (disregardEvents)
                return;

            settings.devMic = (LSAudioSrc)gMicS.SelectedItem;
            freshenChanselButtons();
            //if (Program.debug) MessageBox.Show(LSDevice.stringer(settings.devMic.wf));
        }

        private void gTestDevs_CheckedChanged(object sender, EventArgs e)
        {
            settings.testDevs = gTestDevs.Checked;
        }
        
        private void gSplash_CheckedChanged(object sender, EventArgs e)
        {
            settings.splash = gSplash.Checked;
        }

        private void gRecPCM_CheckedChanged(object sender, EventArgs e)
        {
            settings.recPCM = gRecPCM.Checked;
        }

        private void gRecMP3_CheckedChanged(object sender, EventArgs e)
        {
            settings.recMp3 = gRecMP3.Checked;
        }

        private void gRecOGG_CheckedChanged(object sender, EventArgs e)
        {
            settings.recOgg = gRecOGG.Checked;
        }

        Timer unFxTimer = null;
        //NAudio.Wave.Mp3FileReader fx_mp3 = null;
        NAudio.Wave.WaveFileReader fx_wav = null;
        NAudio.Wave.WasapiOut fx_out = null;
        System.IO.Stream fx_stream = null;
        void playFX(LSDevice dev)
        {
            if (unFxTimer == null)
            {
                unFxTimer = new Timer();
                unFxTimer.Interval = 3000;
                unFxTimer.Tick += delegate(object oa, EventArgs ob)
                {
                    unFX();
                };
            }
            try
            {
                if (disregardEvents) return;
                unFX();
                fx_stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Loopstream.res.sc.wav");
                //fx_mp3 = new NAudio.Wave.Mp3FileReader(fx_stream);
                fx_wav = new NAudio.Wave.WaveFileReader(fx_stream);
                fx_out = new NAudio.Wave.WasapiOut(dev.mm, NAudio.CoreAudioApi.AudioClientShareMode.Shared, false, 100);
                fx_out.Init(fx_wav);
                fx_out.Play();
                unFxTimer.Start();
            }
            catch { }
        }
        void unFX()
        {
            try
            {
                if (fx_out != null) fx_out.Stop();
                if (fx_out != null) fx_out.Dispose();
                if (fx_wav != null) fx_wav.Dispose();
                if (fx_stream != null) fx_stream.Dispose();
                if (unFxTimer != null)
                    unFxTimer.Stop();
            }
            catch { }
        }

        private void gMp3Enable_CheckedChanged(object sender, EventArgs e)
        {
            settings.mp3.enabled = gMp3Enable.Checked;
        }

        private void gMp3Bitrate_CheckedChanged(object sender, EventArgs e)
        {
            if (gMp3Bitrate.Checked) settings.mp3.compression = LSSettings.LSCompression.cbr;
        }

        private void gMp3Quality_CheckedChanged(object sender, EventArgs e)
        {
            if (gMp3Quality.Checked) settings.mp3.compression = LSSettings.LSCompression.q;
        }

        private void gMp3Mono_CheckedChanged(object sender, EventArgs e)
        {
            if (gMp3Mono.Checked) settings.mp3.channels = LSSettings.LSChannels.mono;
        }

        private void gMp3Stereo_CheckedChanged(object sender, EventArgs e)
        {
            if (gMp3Stereo.Checked) settings.mp3.channels = LSSettings.LSChannels.stereo;
        }

        private void gMp3BitrateV_TextChanged(object sender, EventArgs e)
        {
            int n = getValue(gMp3BitrateV);
            if (n >= 0) settings.mp3.bitrate = n;
        }

        private void gMp3QualityV_TextChanged(object sender, EventArgs e)
        {
            int n = getValue(gMp3QualityV);
            if (n >= 0) settings.mp3.quality = n;
        }

        private void gOggEnable_CheckedChanged(object sender, EventArgs e)
        {
            settings.ogg.enabled = gOggEnable.Checked;
        }

        private void gOggBitrate_CheckedChanged(object sender, EventArgs e)
        {
            if (gOggBitrate.Checked) settings.ogg.compression = LSSettings.LSCompression.cbr;
        }

        private void gOggQuality_CheckedChanged(object sender, EventArgs e)
        {
            if (gOggQuality.Checked) settings.ogg.compression = LSSettings.LSCompression.q;
        }

        private void gOggMono_CheckedChanged(object sender, EventArgs e)
        {
            if (gOggMono.Checked) settings.ogg.channels = LSSettings.LSChannels.mono;
        }

        private void gOggStereo_CheckedChanged(object sender, EventArgs e)
        {
            if (gOggStereo.Checked) settings.ogg.channels = LSSettings.LSChannels.stereo;
        }

        private void gOggBitrateV_TextChanged(object sender, EventArgs e)
        {
            int n = getValue(gOggBitrateV);
            if (n >= 0) settings.ogg.bitrate = n;
        }

        private void gOggQualityV_TextChanged(object sender, EventArgs e)
        {
            int n = getValue(gOggQualityV);
            if (n >= 0) settings.ogg.quality = n;
        }
        
        private void gUnavail_CheckedChanged(object sender, EventArgs e)
        {
            settings.showUnavail = gUnavail.Checked;
        }

        private void gAutoconn_CheckedChanged(object sender, EventArgs e)
        {
            settings.autoconn = gAutoconn.Checked;
        }

        private void gAutohide_CheckedChanged(object sender, EventArgs e)
        {
            settings.autohide = gAutohide.Checked;
        }

        private void gHost_MouseEnter(object sender, EventArgs e)
        {
            //tPop.Start();
            popTop = (Control)sender;
            tPop_Tick(sender, e);
        }

        private void gHost_MouseLeave(object sender, EventArgs e)
        {
            try
            {
                tt.Hide(button1);
            }
            catch { }
            tPop.Stop();
        }

        void tPop_Tick(object sender, EventArgs e)
        {
            try
            {
                string msg = tt.GetToolTip(popTop);
                //this.Text = popTop.Name + " = " + msg;
                tPop.Stop();
                //if (popTop == gHost) popTop = gMount;
                // fucking hell microsoft how did you fuck THIS up
                tt.SetToolTip(button1, msg);
                tt.Show(msg, button1);
            }
            catch { }
        }

        private void gTitle_TextChanged(object sender, EventArgs e)
        {
            settings.title = gTitle.Text;
        }

        private void gDescription_TextChanged(object sender, EventArgs e)
        {
            settings.description = gDescription.Text;
        }

        private void gGenre_TextChanged(object sender, EventArgs e)
        {
            settings.genre = gGenre.Text;
        }

        private void gURL_TextChanged(object sender, EventArgs e)
        {
            settings.url = gURL.Text;
        }

        private void gPublic_CheckedChanged(object sender, EventArgs e)
        {
            settings.pubstream = gPublic.Checked;
        }

        private void gMeta_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (gMeta.SelectedIndex == 0) { setDescText(welcomeText); return; }
            settings.meta.apply((LSSettings.LSMeta)gMeta.SelectedItem);
            loadMetaReader(false);
            if (settings.meta.reader == LSSettings.LSMeta.Reader.ProcessMemory && !disregardEvents)
            {
                // TODO: Clean this up
                //       (was thrown in in a hurry when I realized
                //        mempoke didn't work on 32bit apps from world of 64)

                /*if (IntPtr.Size == 4)
                {
                    MessageBox.Show("I see you're running the 32bit version of Loopstream!\n\n" +
                        "If iTunes still doesn't produce correct tags with this edition of Loopstream, then the problem is probably that your version of iTunes is different from the one that Loopstream supports.\n\n" +
                        "Yes, iTunes is /that/ picky. Sorry :(");
                    return;
                }*/

                /*if (DialogResult.Yes == MessageBox.Show(
                    "This media player's a tricky one.\n\n" +
                    "If the tags you send appear to be bullshit,\n" +
                    "I can make a copy of myself that might\n" +
                    "work better for this.\n\n" +
                    "Should I clone myself to Loopstream32.exe?",
                    "Hot Cloning Action",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question))
                {
                    
                }*/
            }
        }

        private void ConfigSC_Resize(object sender, EventArgs e)
        {
            pTabs.Width = pFooter.Width;
        }

        private void gTagsAdvanced_Click(object sender, EventArgs e)
        {
            //gTagsAdvanced.Visible = !gTagsAdvanced.Visible;
            pTagAdvanced1.Visible = pTagsAdvanced2.Visible = !pTagAdvanced1.Visible;
        }

        private void gReader_SelectedIndexChanged(object sender, EventArgs e)
        {
            settings.meta.reader = (LSSettings.LSMeta.Reader)gReader.SelectedItem;
            gTarget.Visible = settings.meta.reader == LSSettings.LSMeta.Reader.WindowCaption;
            gMake32.Visible = settings.meta.reader == LSSettings.LSMeta.Reader.ProcessMemory;
            gEncoding.Visible = gEncodingL.Visible = (settings.meta.reader != LSSettings.LSMeta.Reader.WindowCaption);
            gTarget.Enabled = string.IsNullOrWhiteSpace(settings.meta.src) || settings.meta.src.Trim() != "*";
            gMake32.Enabled = IntPtr.Size != 4;
            if (!gMake32.Enabled)
            {
                gMake32.Text = "currently running in 32-bit mode";
            }
            if (!disregardEvents)
            {
                if (settings.meta.reader == LSSettings.LSMeta.Reader.Website ||
                settings.meta.reader == LSSettings.LSMeta.Reader.File)
                {
                    gEncoding.Text = "utf-8";
                }
                if (settings.meta.reader == LSSettings.LSMeta.Reader.ProcessMemory)
                {
                    gEncoding.Text = "utf-16";
                }
            }
        }

        private void gSource_TextChanged(object sender, EventArgs e)
        {
            settings.meta.src = gSource.Text.Trim();
            gTarget.Enabled = string.IsNullOrWhiteSpace(settings.meta.src) || settings.meta.src != "*";
        }

        string metaRaw = null;
        private void gPattern_TextChanged(object sender, EventArgs e)
        {
            if (sender == gPattern)
            {
                settings.meta.ptn = gPattern.Text;
            }
            bool mem = settings.meta.reader == LSSettings.LSMeta.Reader.ProcessMemory;
            if (gTest.Checked || mem)
            {
                metaRaw = LSTag.get(settings.meta, true).tag;
                if (metaRaw == null)
                {
                    this.Text = "(metafetch failure)";
                    return;
                }
                this.Text = mem ? metaRaw :
                    LSTag.get(settings.meta, metaRaw).tag.Replace("\r", "").Replace("\n", "");
            }
        }

        private void gFreq_TextChanged(object sender, EventArgs e)
        {
            int n = getValue(gFreq);
            if (n >= 0) settings.meta.freq = n;
        }

        private void gName_TextChanged(object sender, EventArgs e)
        {
            settings.meta.tit = gName.Text;
        }

        private void gEncoding_TextChanged(object sender, EventArgs e)
        {
            try
            {
                settings.meta.encoding = gEncoding.Text;
                gEncoding.BackColor = SystemColors.Window;
                gEncoding.ForeColor = SystemColors.WindowText;
            }
            catch
            {
                gEncoding.BackColor = Color.Firebrick;
                gEncoding.ForeColor = Color.Gold;
            }
        }

        private void gTest_CheckedChanged(object sender, EventArgs e)
        {
            doRegexTests = gTest.Checked;
        }

        private void gStore_Click(object sender, EventArgs e)
        {
            //settings.metas.Add(settings.meta);
            bool done = false;
            foreach (LSSettings.LSMeta meta in settings.metas)
            {
                if (meta.tit == settings.meta.tit)
                {
                    if (DialogResult.Yes == MessageBox.Show(
                        "About to OVERWRITE the existing profile:\n\n        «" + settings.meta.tit + "»\n\nContinue?",
                        "Confirm OVERWRITE", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                    {
                        meta.apply(settings.meta);
                        done = true;
                    }
                    if (!done) return;
                }
            }
            if (!done)
            {
                if (DialogResult.Yes == MessageBox.Show(
                    "About to create a NEW profile:\n\n        «" + settings.meta.tit + "»\n\nContinue?",
                    "Confirm NEW profile", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                {
                    settings.metas.Add(LSSettings.LSMeta.copy(settings.meta));
                    done = true;
                }
            }
            if (done)
            {
                loadMetaReader(true);
                gMeta.SelectedItem = settings.meta;
            }
        }

        private void gLatinize_CheckedChanged(object sender, EventArgs e)
        {
            settings.latin = gLatinize.Checked;
        }

        void loadTriggers()
        {
            gEvList.Items.Clear();
            settings.triggers.Sort();
            //settings.triggers.Sort((a, b) => a.eType - b.eType);
            foreach (LSSettings.LSTrigger ev in settings.triggers)
            {
                gEvList.Items.Add(ev);
            }
            gEvList.SelectedItem = 0;
        }

        void loadMetaReader(bool redoPresets)
        {
            if (redoPresets)
            {
                gMeta.Items.Clear();
                gMeta.Items.Add("(custom)");
                foreach (LSSettings.LSMeta meta in settings.metas)
                {
                    gMeta.Items.Add(meta);
                }
            }
            bool reset = disregardEvents; //fuck
            disregardEvents = true; //fuck
            gReader.SelectedItem = settings.meta.reader;
            disregardEvents = reset; //fuck

            if (!string.IsNullOrWhiteSpace(settings.meta.desc))
            {
                setDescText(settings.meta.desc);
            }
            else if (!string.IsNullOrWhiteSpace(settings.meta.tit))
            {
                setDescText("(no description is available for this profile)");
                //gDescText.Text = "<" + settings.meta.tit + ">" + gMeta.SelectedIndex;
            }
            else
            {
                setDescText(welcomeText);
            }

            gSource.Text = settings.meta.src;
            gPattern.Text = settings.meta.ptn;
            gFreq.Text = settings.meta.freq.ToString();
            gBounce.Text = settings.meta.bnc.ToString();
            gName.Text = settings.meta.tit;
            gEncoding.Text = settings.meta.enc.WebName;
            gURLDecode.Checked = settings.meta.urldecode;
            gLatinize.Checked = settings.latin;
            gTagSock.Checked = settings.tagsock;
            gTest.Checked = doRegexTests;
            //gGroup.Text = settings.meta.grp.ToString();
            gYield.Text = settings.meta.yield;

            if (redoPresets)
            {
                gMeta.SelectedIndex = 0;
                foreach (LSSettings.LSMeta m in settings.metas)
                {
                    if (m.eq(settings.meta))
                    {
                        gMeta.SelectedItem = m;
                        return;
                    }
                }
            }
        }

        /*private void gGroup_TextChanged(object sender, EventArgs e)
        {
            int n = getValue(gGroup);
            if (n >= 0) settings.meta.grp = n;
        }*/

        private void gTagMan_CheckedChanged(object sender, EventArgs e)
        {
            settings.tagAuto = gTagAuto.Checked;
        }

        private void gRate_TextChanged(object sender, EventArgs e)
        {
            int n = getValue(gRate);
            if (n >= 0) settings.samplerate = n;
        }

        private void gReverb_TextChanged(object sender, EventArgs e)
        {
            int n = getValue(gReverbP);
            if (n >= 0) settings.reverbP = n;
        }

        private void gReverbS_TextChanged(object sender, EventArgs e)
        {
            int n = getValue(gReverbS);
            if (n >= 0) settings.reverbS = n;
        }

        private void gVU_CheckedChanged(object sender, EventArgs e)
        {
            settings.vu = gVU.Checked;
        }














        int httpd_port;
        void oneshot()
        {
            System.Threading.Thread.Sleep(500);
            System.Diagnostics.Process.Start(string.Format("http://127.0.0.1:{0}/", httpd_port));
        }
        void wizard()
        {
            try
            {
                bool quit = false;
                Regex r = new Regex("^GET /(.*) HTTP/1..$");
                string head = File.ReadAllText(Program.tools + "web\\head", System.Text.Encoding.UTF8);
                string tail = File.ReadAllText(Program.tools + "web\\tail", System.Text.Encoding.UTF8);
                TcpListener tl = new TcpListener(IPAddress.Any, 0);
                tl.Start();
                httpd_port = ((IPEndPoint)tl.LocalEndpoint).Port;
                new System.Threading.Thread(new System.Threading.ThreadStart(oneshot)).Start();
                while (!quit)
                {
                    try
                    {
                        byte[] data = { }, hdr = { };
                        string ctype = "text/html; charset=utf-8";

                        TcpClient tc = tl.AcceptTcpClient();
                        NetworkStream s = tc.GetStream();
                        StreamReader sr = new StreamReader(s);
                        string action = sr.ReadLine();
                        while (sr.ReadLine() != "") ;
                        Match m = r.Match(action);
                        if (false)
                        {
                            head = File.ReadAllText(Program.tools + "web\\head", System.Text.Encoding.UTF8);
                            tail = File.ReadAllText(Program.tools + "web\\tail", System.Text.Encoding.UTF8);
                        }
                        if (m != null && m.Groups.Count > 1)
                        {
                            action = m.Groups[1].Value.Trim('\\', '/', ' ', '\t', '.');
                            while (action.Contains("..")) action = action.Replace("..", "");
                            while (action.Contains("//")) action = action.Replace("//", "");
                            while (action.Contains("\\")) action = action.Replace("\\", "");

                            if (action.Length < 3 ||
                                action.StartsWith("index"))
                            {
                                action = "page/index";
                            }
                            if (action.Contains('?'))
                            {
                                string[] asdf = action.Split('?');
                                action = asdf[0];
                                string[] mods = asdf[1].Split('&');
                                foreach (string mod in mods)
                                {
                                    var devRec = settings.devRec as LSDevice;
                                    var devMic = settings.devMic as LSDevice;
                                    if (mod.StartsWith("resampling") && (
                                        (devRec == null || devRec.wf == null || devRec.wf.SampleRate != settings.samplerate) ||
                                        (devMic != null && devMic.wf != null && devMic.wf.SampleRate != settings.samplerate)))
                                    {
                                        action = "page/" + mod;
                                    }
                                    if (mod == "reload")
                                    {
                                        this.Invoke((MethodInvoker)delegate
                                        {
                                            reDevice();
                                            initDevs();
                                        });
                                    }
                                    if (mod == "clone")
                                    {
                                        this.Invoke((MethodInvoker)delegate
                                        {
                                            settings.devRec = settings.devOut;
                                            initDevs();
                                        });
                                    }
                                    if (mod == "serv")
                                    {
                                        this.Invoke((MethodInvoker)delegate
                                        {
                                            setTab(1);
                                            gUser.Focus();
                                        });
                                    }
                                    if (mod == "scp")
                                    {
                                        System.Diagnostics.Process proc = new System.Diagnostics.Process();
                                        proc.StartInfo.Arguments = "shell32.dll,Control_RunDLL mmsys.cpl,,playback";
                                        proc.StartInfo.FileName = "rundll32.exe";
                                        proc.Start();
                                    }
                                    if (mod.Contains("="))
                                    {
                                        string trg = mod.Substring(0, mod.IndexOf('='));
                                        string id = mod.Substring(trg.Length + 1);
                                        id = id.Replace('-', '+').Replace('_', '/');
                                        id = Encoding.UTF8.GetString(Convert.FromBase64String(id));

                                        if (trg.Length == 1)
                                        {
                                            int ofs = Convert.ToInt32(trg);
                                            ComboBox[] boxen = { gMicS, gMusS, gOutS };
                                            this.Invoke((MethodInvoker)delegate
                                            {
                                                boxen[ofs].SelectedItem = null;
                                                boxen[ofs].SelectedItem = settings.getDevByID(id);
                                            });
                                            //data = Encoding.UTF8.GetBytes(head + "OK" + tail);
                                        }
                                        else
                                        {
                                            TextBox tb =
                                                trg == "host" ? gHost :
                                                trg == "user" ? gUser :
                                                trg == "pass" ? gPass :
                                                trg == "mount" ? gMount :
                                                null;

                                            if (tb != null)
                                            {
                                                this.Invoke((MethodInvoker)delegate
                                                {
                                                    tb.Text = id;
                                                    if (tb.BackColor != gName.BackColor)
                                                    {
                                                        data = Encoding.UTF8.GetBytes(head + "That " + trg + " is invalid!" + tail);
                                                    }
                                                });
                                            }
                                        }
                                    }
                                }
                            }
                            string[] ar = action.Split('/');
                            if (ar[0] == "page" && data.Length == 0)
                            {
                                action = Program.tools + "web/" + action;
                                action = action.Replace('/', '\\');
                                string ret = "<h1>404 Not Found</h1><h2>" + action + "</h2>";
                                if (File.Exists(action))
                                {
                                    ret = File.ReadAllText(action, Encoding.UTF8);
                                    if (ret.Contains("TPL_OUT_DEV"))
                                    {
                                        string nam = "(ERROR: INVALID DEVICE)";
                                        if (settings.devOut != null)
                                        {
                                            nam = settings.devOut.name;
                                            nam = nam.StartsWith("OK - ") ? nam.Substring(5) : nam;
                                        }
                                        ret = ret.Replace("TPL_OUT_DEV", nam);
                                    }
                                    if (ret.Contains("TPL_REC_DEV"))
                                    {
                                        string nam = "(ERROR: INVALID DEVICE)";
                                        if (settings.devRec != null)
                                        {
                                            nam = settings.devRec.name;
                                            nam = nam.StartsWith("OK - ") ? nam.Substring(5) : nam;
                                        }
                                        ret = ret.Replace("TPL_REC_DEV", nam);
                                    }
                                    if (ret.Contains("TPL_BADSMP"))
                                    {
                                        StringBuilder sb = new StringBuilder();
                                        LSDevice[] devs = { (LSDevice)settings.devMic, (LSDevice)settings.devRec };
                                        foreach (LSDevice dev in devs)
                                        {
                                            if (dev != null && dev.wf != null && dev.wf.SampleRate != settings.samplerate)
                                            {
                                                string nam = dev.ToString();
                                                nam = nam.StartsWith("OK - ") ? nam.Substring(5) : nam;

                                                sb.AppendFormat("<a href=\"#\"><strong>{0}</strong> {1}</a>\n",
                                                    dev.isRec ? "Recording tab:" : "Playback tab:",
                                                    nam);
                                            }
                                        }
                                        ret = ret.Replace("TPL_BADSMP", sb.ToString().Trim('\r', '\n'));
                                    }
                                    if (ret.Contains("TPL_DEV_"))
                                    {
                                        StringBuilder sb0 = new StringBuilder();
                                        StringBuilder sb1 = new StringBuilder();
                                        StringBuilder sb2 = new StringBuilder();
                                        foreach (LSAudioSrc idev in settings.devs)
                                        {
                                            if (!(idev is LSDevice))
                                                continue;

                                            var dev = (LSDevice)idev;

                                            if (dev.wf != null)
                                            {
                                                string nam = dev.ToString();
                                                nam = nam.StartsWith("OK - ") ? nam.Substring(5) : nam;

                                                string id = dev.id;
                                                id = Convert.ToBase64String(Encoding.UTF8.GetBytes(id));
                                                id = id.Replace('+', '-').Replace('/', '_');

                                                if (dev.isPlay)
                                                {
                                                    if (settings.devOut != null && settings.devOut != dev)
                                                    {
                                                        sb1.AppendFormat("<a href=\"?1={0}#anchor\">{1}{2}</a>\n",
                                                            id, settings.devRec == dev ? "(♫) " : "", nam);
                                                    }
                                                    sb2.AppendFormat("<a href=\"?2={0}#anchor\">{1}{2}</a>\n",
                                                        id, settings.devOut == dev ? "(♫) " : "", nam);
                                                }
                                                else
                                                {
                                                    sb0.AppendFormat("<a href=\"?0={0}#anchor\">{1}{2}</a>\n",
                                                        id, settings.devMic == dev ? "(♫) " : "", nam);
                                                }
                                            }
                                        }
                                        ret = ret
                                            .Replace("TPL_DEV_0", sb0.ToString().Trim('\r', '\n'))
                                            .Replace("TPL_DEV_1", sb1.ToString().Trim('\r', '\n'))
                                            .Replace("TPL_DEV_2", sb2.ToString().Trim('\r', '\n'));
                                    }
                                }
                                bool validateRec = ret.Contains("TPL_REQUIRE_1");
                                bool validateOut = ret.Contains("TPL_REQUIRE_2");
                                if (validateRec || validateOut)
                                {
                                    if ((validateRec && settings.devRec == null) ||
                                        (validateOut && settings.devOut == null))
                                    {
                                        ret = "You did not select a device!";
                                    }
                                    ret = ret.Replace("TPL_REQUIRE_1", "");
                                    ret = ret.Replace("TPL_REQUIRE_2", "");
                                }
                                data = Encoding.UTF8.GetBytes(head + ret + tail);
                            }
                            else if (ar[0] == "shutdown")
                            {
                                //quit = true;
                                data = Encoding.UTF8.GetBytes(head + "bye <img id=\"bye\" src=\"/png/win95.png\" />" + tail);
                                //data = Encoding.UTF8.GetBytes(head + "bye <script>setTimeout(function(){window.close();},500);</script>" + tail);
                            }
                            else
                            {
                                ctype =
                                    ar[0] == "font" ? "font/woff" :
                                    ar[0] == "jpg" ? "image/jpeg" :
                                    ar[0] == "png" ? "image/png" :
                                    "application/octet-stream";

                                action = Program.tools + "web/" + action;
                                action = action.Replace('/', '\\');
                                string ret = "<h1>404 Not Found</h1><h2>" + action + "</h2>";
                                if (File.Exists(action))
                                {
                                    data = File.ReadAllBytes(action);
                                }
                                quit = action.Contains("win95");
                            }
                        }

                        data = data.Length > 0 ? data : Encoding.UTF8.GetBytes(
                            head + "<h1>Loopstream HTTP Server Error" + tail);

                        hdr = hdr.Length > 0 ? hdr : Encoding.UTF8.GetBytes(
                            "HTTP/1.1 200 OK\r\n" +
                            "Connection: Close\r\n" +
                            "Cache-Control: no-cache, must-revalidate\r\n" +
                            "Expires: Wed, 09 Sep 2009 09:09:09 GMT\r\n" +
                            "Content-Type: " + ctype + "\r\n" +
                            "Content-Length: " + data.Length + "\r\n" +
                            "\r\n");
                        s.Write(hdr, 0, hdr.Length);
                        s.Write(data, 0, data.Length);
                        s.Flush();
                        s.Dispose();
                        tc.Close();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("Transient HTTPd error:\n\n" + e.Message + "\n\n" + e.StackTrace,
                            "HTTPd glitch", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                if (quit) readServerSharekey = true;
            }
            catch (Exception e)
            {
                MessageBox.Show("Critical HTTPd error:\n\n" + e.Message + "\n\n" + e.StackTrace,
                    "HTTPd crash", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void gCPoor_CheckedChanged(object sender, EventArgs e)
        {
            //settings.warn_poor_DEPRECATED = gCPoor_DEPRECATED.Checked;
        }

        private void gCDrop_CheckedChanged(object sender, EventArgs e)
        {
            //settings.warn_drop_DEPRECATED = gCDrop_DEPRECATED.Checked;
        }

        private void gLPoor_TextChanged(object sender, EventArgs e)
        {
            //int n = getValue(gLPoor_DEPRECATED);
            //if (n >= 0) settings.lim_poor_DEPRECATED = n / 100.0;
        }

        private void gLDrop_TextChanged(object sender, EventArgs e)
        {
            //int n = getValue(gLDrop_DEPRECATED);
            //if (n >= 0) settings.lim_drop_DEPRECATED = n / 100.0;
        }

        private void gTarget_Click(object sender, EventArgs e)
        {
            UI_Winpeck wp = new UI_Winpeck();
            wp.ShowDialog();
            gSource.Text = wp.starget + "*" + wp.itarget.ToString("x");
            gSource_TextChanged(sender, e);
            wp.Dispose();
        }

        private void gURLDecode_CheckedChanged(object sender, EventArgs e)
        {
            settings.meta.urldecode = gURLDecode.Checked;
        }

        private void gUnhide_CheckedChanged(object sender, EventArgs e)
        {
            gPass.PasswordChar = gPass.PasswordChar == '\0' ? 'o' : '\0';
        }

        private void gServerLoad_Click(object sender, EventArgs e)
        {
            while (kills.Count > 0)
            {
                c_Click(kills[0], null);
            }
            ((LSSettings.LSServerPreset)gServerSel.SelectedItem).LoadFromProfile(settings);
            visualizeServerSettings();
        }

        private void gServerSave_Click(object sender, EventArgs e)
        {
            string presetName = gServerSel.Text;
            foreach (LSSettings.LSServerPreset preset in settings.serverPresets)
            {
                if (preset.presetName == presetName)
                {
                    if (DialogResult.Yes == MessageBox.Show(
                        "About to OVERWRITE the existing profile:\n\n        «" + preset.presetName + "»\n\nContinue?",
                        "Confirm OVERWRITE", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                    {
                        preset.SaveToProfile(settings);
                        return;
                    }
                }
            }
            if (DialogResult.Yes == MessageBox.Show(
                "About to create a NEW profile:\n\n        «" + presetName + "»\n\nContinue?",
                "Confirm NEW profile", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
            {
                LSSettings.LSServerPreset preset = new LSSettings.LSServerPreset();
                preset.SaveToProfile(settings);
                preset.presetName = presetName;
                settings.serverPresets.Add(preset);
                visualizeServerSettings();
            }
        }

        private void gServerDel_Click(object sender, EventArgs e)
        {
            LSSettings.LSServerPreset preset = (LSSettings.LSServerPreset)gServerSel.SelectedItem;
            if (preset == null) return;
            if (DialogResult.Yes == MessageBox.Show(
                        "About to DELETE the profile:\n\n        «" + preset.presetName + "»\n\nContinue?",
                        "Confirm DELETE", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
            {
                settings.serverPresets.Remove(preset);
                visualizeServerSettings();
            }
        }

        private void gDelete_Click(object sender, EventArgs e)
        {
            if (gMeta.SelectedIndex <= 0) return;
            LSSettings.LSMeta meta = (LSSettings.LSMeta)gMeta.SelectedItem;
            if (meta == null) return;

            if (DialogResult.Yes == MessageBox.Show(
                "Do you really want to DELETE this profile?\n\n        «" + meta.tit + "»",
                "Confirm DELETION", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
            {
                settings.metas.Remove(meta);
                loadMetaReader(true);
                gMeta.SelectedItem = settings.meta;
            }
        }

        private void gMake32_Click(object sender, EventArgs e)
        {
            string fo = Application.ExecutablePath;
            using (System.IO.FileStream i = new System.IO.FileStream(fo, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                fo = fo.Substring(0, fo.LastIndexOf('.')) + "32.exe";
                using (System.IO.FileStream o = new System.IO.FileStream(fo, System.IO.FileMode.Create))
                {
                    bool first = true;
                    byte[] buf = new byte[8192];
                    while (true)
                    {
                        int n = i.Read(buf, 0, buf.Length);
                        if (first)
                        {
                            first = false;
                            buf[0x218] = 3; //1=any
                        }
                        if (n <= 0) break;
                        o.Write(buf, 0, n);
                    }
                }
            }
            System.Diagnostics.Process rs = new System.Diagnostics.Process();
            rs.StartInfo.FileName = "Loopstream32.exe";
            rs.StartInfo.Arguments = "sign";
            rs.Start();
            Application.DoEvents();
            rs.Refresh();
            while (!rs.HasExited)
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(10);
            }
            for (int a = 0; a < 10; a++)
            {
                try
                {
                    System.IO.File.Delete("Loopstream32.exe");
                    System.IO.File.Move("Loopstream32.exe.exe", "Loopstream32.exe");
                    break;
                }
                catch { }
                System.Threading.Thread.Sleep(100);
            }

            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = "Loopstream32.exe";
            proc.Start();
            proc.Refresh();
            Program.kill();
        }

        private void gRunTest_Click(object sender, EventArgs e)
        {
            LSTag.LSTD td = LSTag.get(settings.meta, false);
            if (td.ok)
            {
                MessageBox.Show("it works!  Well done bro.\nThe tags are displayed below.\n\n«" + td.tag + "»");
            }
            else
            {
                MessageBox.Show("Error:   " + td.tag + "\n\n===[ Description ]==========================\n\n" + td.error);
            }
        }

        private void gResetTags_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (DialogResult.Yes == MessageBox.Show(
                "About to perform a FACTORY RESET:\n\nDeleteing all your profiles and restoring the default ones.\n\nContinue?",
                "Confirm factory reset", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
            {
                settings.resetMetas();
                loadMetaReader(true);
                gMeta.SelectedIndex = 0;
                gTagsAdvanced_Click(sender, e);
            }
        }

        private void gDescText_TextChanged(object sender, EventArgs e)
        {
            if (sender == null || sender != gDescText || disregardEvents) return;

            try
            {
                settings.meta.desc = gDescText.Text.Replace("\r", "");
            }
            catch { }
            // idk if this is really necessary but too tired to think or test
            try
            {
                foreach (LSSettings.LSMeta meta in settings.metas)
                {
                    if (meta.eq(settings.meta))
                    {
                        meta.desc = gDescText.Text.Replace("\r", "");
                    }
                }
            }
            catch { }
        }

        void setDescText(string str)
        {
            bool a = disregardEvents;
            disregardEvents = true;
            gDescText.Text = str
                .Replace("\r", "")
                .Replace("\n", "\r\n");
            disregardEvents = a;
        }

        private void label29_Click(object sender, EventArgs e)
        {
            string ret = Z.gze(gDescText.Text.Replace("\r", ""));
            //string ret = gDescText.Text.Replace("\r", "").Replace("\n", "\\n").Replace("\"", "\\\"");
            setDescText(ret);
            Clipboard.Clear();
            Clipboard.SetText(ret);
        }

        private string serialize(LSSettings.LSMeta lsm)
        {
            var cfg = new System.Xml.XmlWriterSettings();
            cfg.NamespaceHandling = System.Xml.NamespaceHandling.OmitDuplicates;
            cfg.NewLineHandling = System.Xml.NewLineHandling.None;
            cfg.NewLineOnAttributes = false;
            cfg.OmitXmlDeclaration = true;
            cfg.Indent = false;

            var sb = new StringBuilder();
            var x = new System.Xml.Serialization.XmlSerializer(lsm.GetType());
            var w = System.Xml.XmlWriter.Create(sb, cfg);
            x.Serialize(w, lsm);
            w.Flush();
            w.Close();
            return sb.ToString();
        }

        private void gTagexport_Click(object sender, EventArgs e)
        {
            LSSettings.LSMeta lsm = LSSettings.LSMeta.copy(settings.meta);
            bool shredText = DialogResult.No == MessageBox.Show(
                "Do you want to KEEP the description text when sharing this profile?\n\n" +
                "Selecting NO will make the share-key shorter,\n" +
                "but the description may be useful for the recipient.",
                "Share entire profile?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (shredText) lsm.desc = "";

            string key = serialize(lsm);
            key = key.Substring(key.IndexOf('>') + 1);
            key = key.Substring(0, key.LastIndexOf('<'));
            //string gz = Z.gze(key);
            //string xz = Z.lze(key);
            key = Z.gze(key);
            key = "##gz#" + key + "##";
            Clipboard.Clear();
            Clipboard.SetText(key);
            if (DialogResult.Yes == MessageBox.Show(
                "The share-key has been made, and put on your clipboard.\n\n" +
                "Length: " + key.Length + " characters.\n\n" +
                //"Length: " + gz.Length + " characters.\n\n" +
                //"Length: " + xz.Length + " characters.\n\n" +
                "You should share this with Pastebin or bpaste.\n" +
                "Open bpaste now?",
                "OK !", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
            {
                System.Diagnostics.Process.Start("http://bpaste.net/");
            }
        }

        private void gTagimport_Click(object sender, EventArgs e)
        {
            string key = Clipboard.GetText();
            if (!key.Contains("#gz#"))
            {
                if (key.ToLower().Contains("bpaste.net"))
                {
                    // http://bpaste.net/show/PNaO8zoBtZdPRv8NLoNs/
                    // http://bpaste.net/raw/PNaO8zoBtZdPRv8NLoNs/
                    var r = Regex.Match(key, "(.*[^a-zA-Z0-9]|^)bpaste.net/[^/]*/([^/]*)(/|$)", RegexOptions.IgnoreCase).Groups;
                    if (r.Count > 2)
                    {
                        key = new System.Net.WebClient().DownloadString("http://bpaste.net/raw/" + r[2].Value + "/");
                    }
                }
                else if (key.ToLower().Contains("pastebin.com"))
                {
                    // http://pastebin.com/gbwsufzV
                    // http://pastebin.com/raw.php?i=gbwsufzV
                    var r = Regex.Match(key, ".*(.*[^a-zA-Z0-9]|^)pastebin.com.*[/=]([^/=]*)$", RegexOptions.IgnoreCase).Groups;
                    if (r.Count > 2)
                    {
                        key = new System.Net.WebClient().DownloadString("http://pastebin.com/raw.php?i=" + r[2].Value + "/");
                    }
                }
            }
            try
            {
                int i = key.IndexOf("gz#");
                int j = key.IndexOf("#", i + 4);
                if (i < 0 || j < 0) throw new Exception();
                i += 3;
                key = key.Substring(i, j - i);
            }
            catch
            {
                MessageBox.Show(
                    "Padding error\n\n" +
                    "Sorry, but the share-key is invalid.");
                return;
            }
            try
            {
                key = Z.gzd(key);
            }
            catch
            {
                MessageBox.Show(
                    "Decoder error\n\n" +
                    "Sorry, but the share-key is invalid.");
                return;
            }
            LSSettings.LSMeta fds = new LSSettings.LSMeta();
            try
            {
                string chrome = serialize(fds);
                string chrome1 = chrome.Substring(0, chrome.IndexOf('>') + 1);
                string chrome2 = chrome.Substring(chrome.LastIndexOf('<'));
                key = chrome1 + key + chrome2;

                var sr = new System.IO.StringReader(key);
                var ds = new System.Xml.Serialization.XmlSerializer(typeof(LSSettings.LSMeta));
                fds = (LSSettings.LSMeta)ds.Deserialize(sr);
            }
            catch
            {
                MessageBox.Show(
                    "Serializer error\n\n" +
                    "Sorry, but the share-key is invalid:\n\n" +
                    key);
                return;
            }
            UI_input inp = new UI_input(fds, settings.metas);
            inp.ShowDialog();
            if (inp.wasAdded)
            {
                loadMetaReader(true);
                gMeta.SelectedItem = settings.metas[settings.metas.Count - 1];
            }
        }

        private void gYield_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (sender == gYield)
                {
                    settings.meta.yield = gYield.Text;
                }
                //this.Text = settings.meta.yi.assert();
                indicateOK(gYield);
            }
            catch (Exception ex)
            {
                indicateError(gYield);
                return;
            }
            gPattern_TextChanged(sender, e);
        }

        private void gEvList_SelectedIndexChanged(object sender, EventArgs e)
        {
            var ev = (LSSettings.LSTrigger)gEvList.SelectedItem;
            if (ev == null)
            {
                gEvUploadC.Checked = false;
                gEvMouseC.Checked = false;
                gEvAudioC.Checked = false;
                gEvUploadP.Text = "";
                gEvMouseS.Text = "";
                gEvAudioP.Text = "";
                gEvAudioS.Text = "";
                return;
            }
            gEvUploadC.Checked = ev.bUpload;
            gEvMouseC.Checked = ev.bMouse;
            gEvAudioC.Checked = ev.bAudio;

            gEvType.SelectedItem = ev.eType;
            gEvMouseS.Text = ev.sMouse.ToString();
            gEvAudioS.Text = ev.sAudio.ToString();
            gEvAudioP.Text = Math.Round(ev.pAudio * 100).ToString();
            gEvUploadP.Text = Math.Round(ev.pUpload * 100).ToString();
        }

        private void gEvUploadC_CheckedChanged(object sender, EventArgs e)
        {
            gEvUploadP.Enabled = gEvUploadC.Checked;
        }

        private void gEvMouseC_CheckedChanged(object sender, EventArgs e)
        {
            gEvMouseS.Enabled = gEvMouseC.Checked;
        }

        private void gEvAudioC_CheckedChanged(object sender, EventArgs e)
        {
            gEvAudioP.Enabled = gEvAudioC.Checked;
            gEvAudioS.Enabled = gEvAudioC.Checked;
        }

        LSSettings.LSTrigger makeTrigger()
        {
            return new LSSettings.LSTrigger(

                (LSSettings.LSTrigger.EventType)gEvType.SelectedItem,

                gEvUploadC.Checked,
                Convert.ToDouble(gEvUploadP.Text) / 100,

                gEvAudioC.Checked,
                Convert.ToDouble(gEvAudioP.Text) / 100,
                Convert.ToInt32(gEvAudioS.Text),

                gEvMouseC.Checked,
                Convert.ToInt32(gEvMouseS.Text)
            );
        }

        private void gEvSave_Click(object sender, EventArgs e)
        {
            try
            {
                LSSettings.LSTrigger ev = makeTrigger();
                settings.triggers.Add(ev);
                loadTriggers();
                gEvList.SelectedItem = ev;
            }
            catch
            {
                MessageBox.Show("Bad input.   Trigger not saved.");
            }
        }

        private void gEvOverwrite_Click(object sender, EventArgs e)
        {
            try
            {
                int i = settings.triggers.IndexOf((LSSettings.LSTrigger)gEvList.SelectedItem);
                LSSettings.LSTrigger ev = makeTrigger();
                settings.triggers[i] = ev;
                loadTriggers();
                gEvList.SelectedItem = ev;
            }
            catch
            {
                MessageBox.Show("Bad input.   Trigger not updated.");
            }
        }

        private void gEvDelete_Click(object sender, EventArgs e)
        {
            int i = gEvList.SelectedIndex;
            settings.triggers.RemoveAt(settings.triggers.IndexOf((LSSettings.LSTrigger)gEvList.SelectedItem));
            loadTriggers();


            if (i < gEvList.Items.Count)
            {
                gEvList.SelectedIndex = i;
            }
            else if (gEvList.Items.Count > 0)
            {
                gEvList.SelectedIndex = 0;
            }
        }

        private void gBounce_TextChanged(object sender, EventArgs e)
        {
            int n = getValue(gBounce);
            if (n >= 0) settings.meta.bnc = n;
        }

        private void gServerShare_Click(object sender, EventArgs e)
        {
            string key = ""
                + (gHost.Left > 0 ? "1" : "0")
                + (gUser.Left > 0 ? "1" : "0")
                + (gPass.Left > 0 ? "1" : "0")
                + (gMount.Left > 0 ? "1" : "0")
                + " " + gServerSel.Text
                + "\n" + settings.host
                + "\n" + settings.port
                + "\n" + (gUser.Left > 0 ? "" : settings.user)
                + "\n" + (gPass.Left > 0 ? "" : settings.pass)
                + "\n" + (gMount.Left > 0 ? "" : settings.mount);

            key = Z.gze(key);
            key = "##cs#" + key + "##";
            Clipboard.Clear();
            Clipboard.SetText(key);
            MessageBox.Show(
                "The share-key has been copied to your clipboard.\n" +
                "Press CTRL-V somewhere to paste it.\n\n" + key);
        }

        List<Label> kills = new List<Label>();
        private void toggleReadOnly(object sender, MouseEventArgs e)
        {
            if (e != null && e.Button != System.Windows.Forms.MouseButtons.Right) return;
            TextBox t = (TextBox)sender;
            //t.ContextMenu = dummyctx;
            //t.ReadOnly = !t.ReadOnly;
            
            var c = new Label();
            c.Tag = t;
            c.Text = t.Text;
            c.BorderStyle = BorderStyle.Fixed3D;
            //c.ReadOnly = false;
            //t.ContextMenu = dummyctx;
            t.Parent.Controls.Add(c);
            c.Bounds = t.Bounds;
            c.Height -= 2;
            c.Padding = new System.Windows.Forms.Padding(c.Padding.Left, c.Padding.Top + 2, c.Padding.Right, c.Padding.Bottom); // fuck
            //t.Visible = false;
            t.ShortcutsEnabled = false;
            t.TabStop = false;
            t.Left = -573;
            kills.Add(c);
            c.Click += new EventHandler(c_Click);
            gServerSel.Focus();
        }

        void c_Click(object sender, EventArgs e)
        {
            var asdf = (Label)sender;
            var fdsa = (TextBox)asdf.Tag;
            // fdsa.Visible = true;
            fdsa.ShortcutsEnabled = true;
            fdsa.Left = asdf.Left;
            fdsa.TabStop = true;
            kills.Remove(asdf);
            asdf.Dispose();
        }

        private void toggleReadOnlyPost(object sender, MouseEventArgs e)
        {
            //((TextBox)sender).ContextMenu = gTitle.ContextMenu;
        }

        private void gTagSock_CheckedChanged(object sender, EventArgs e)
        {
            settings.tagsock = gTagSock.Checked;
        }

        private void gKillMic_CheckedChanged(object sender, EventArgs e)
        {
            settings.killmic = gKillMic.Checked;
        }

        private void gRescan_Click(object sender, EventArgs e)
        {
            reDevice();
            initDevs();
        }

        private void gOneB_Click(object sender, EventArgs e)
        {
            chanvis(gMusS, ref settings.chRec);
        }

        private void gTwoB_Click(object sender, EventArgs e)
        {
            chanvis(gMicS, ref settings.chMic);
        }

        private void gOutB_Click(object sender, EventArgs e)
        {
            chanvis(gOutS, ref settings.chOut);
        }

        void chanvis(ComboBox cb, ref int[] chans)
        {
            LSAudioSrc src = (LSAudioSrc)cb.SelectedItem;
            if (src is LSWavetail)
            {
                configWavetailer(cb);
                return;
            }

            var tmp_chans = chans;
            var lsdev = (LSDevice)src;
            Array.Copy(chans, tmp_chans, chans.Length);
            
            var w = new UI_Chanvis(lsdev, tmp_chans);
            w.ShowDialog();

            if (w.chans.Length < 1 || w.chans.Length > 2)
                return;
            
            chans = w.chans;
            freshenChanselButtons();
        }

        void configWavetailer(ComboBox cb)
        {
            new UI_WavetailCfg((LSWavetail)cb.SelectedItem).ShowDialog();
            cb.Items[cb.SelectedIndex] = cb.Items[cb.SelectedIndex];
        }

        private void gOpusEnable_CheckedChanged(object sender, EventArgs e)
        {
            settings.opus.enabled = gOpusEnable.Checked;
        }

        private void gRecOpus_CheckedChanged(object sender, EventArgs e)
        {
            settings.recOpus = gRecOpus.Checked;
        }

        private void gOpusQualityV_TextChanged(object sender, EventArgs e)
        {
            int n = getValue(gOpusQualityV);
            if (n >= 0) settings.opus.quality = n;
        }

        private void gOpusMono_CheckedChanged(object sender, EventArgs e)
        {
            if (gOpusMono.Checked) settings.opus.channels = LSSettings.LSChannels.mono;
        }

        private void gOpusStereo_CheckedChanged(object sender, EventArgs e)
        {
            if (gOpusStereo.Checked) settings.opus.channels = LSSettings.LSChannels.stereo;
        }

        private void ConfigSC_FormClosing(object sender, FormClosingEventArgs e)
        {
            System.Diagnostics.Debug.Print("close configsc");
        }
    }
}
