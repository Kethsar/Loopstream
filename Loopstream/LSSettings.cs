﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Loopstream
{
    public class LSInfo
    {
        public DateTime begin;
        public string filename;
        public string timestamp()
        {
            TimeSpan ts = DateTime.UtcNow.Subtract(begin);
            int h = (int)ts.TotalHours;
            int m = ((int)ts.TotalMinutes) - 60 * h;
            int s = ((int)ts.TotalSeconds) - 60 * ((60 * h) + m);
            return string.Format("{0:D2}:{1:D2}:{2:D2}", h, m, s);
        }
    }

    public class LSSettings
    {
        [XmlIgnore]
        public static LSSettings singleton;

        public class LSTrigger : IComparable
        {
            public enum EventType
            {
                WARN_CONN_POOR,
                WARN_CONN_DROP,
                WARN_NO_AUDIO,
                DISCONNECT
            };
            public EventType eType;

            public double pUpload;
            public double pAudio;
            public int sAudio;
            public int sMouse;

            public long lastAudio;
            public long lastMouse;
            public double lastNet;

            public bool bUpload;
            public bool bAudio;
            public bool bMouse;

            public LSTrigger()
            {
                pUpload = sMouse = 0;
                bUpload = bMouse = false;
                lastAudio = lastMouse = -1;
                eType = EventType.WARN_NO_AUDIO;
                bAudio = true;
                pAudio = 0.2;
                sAudio = 9001;
            }
            public LSTrigger(EventType ev, bool bup, double pup, bool baudio, double paudio, int saudio, bool bmouse, int smouse)
            {
                eType = ev;
                bUpload = bup;
                pUpload = pup;
                bAudio = baudio;
                pAudio = paudio;
                sAudio = saudio;
                bMouse = bmouse;
                sMouse = smouse;
                lastAudio = lastMouse = -1;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(
                    eType == EventType.WARN_CONN_POOR ? "WARNING - Connection Poor       " :
                    eType == EventType.WARN_CONN_DROP ? "WARNING - Connection DROP    " :
                    eType == EventType.WARN_NO_AUDIO ? "WARNING - No Audio Signal        " :
                    "DISCONNECT (shut down stream)"
                );
                string bw = " when ";
                
                if (bUpload)
                {
                    sb.Append(bw);
                    sb.Append("(( broadcast rate drops below ");
                    sb.Append(Math.Round(pUpload * 100));
                    sb.Append("% ))");
                    bw = "  AND  ";
                }
                if (bAudio)
                {
                    sb.Append(bw);
                    sb.Append("(( audio signal below ");
                    sb.Append(Math.Round(pAudio * 100));
                    sb.Append("% for ");
                    sb.Append(sAudio);
                    sb.Append(" s.))");
                    bw = "  AND  ";
                }
                if (bMouse)
                {
                    sb.Append(bw);
                    sb.Append("(( no mouse activity in ");
                    sb.Append(sMouse);
                    sb.Append(" s.))");
                    bw = "  AND  ";
                }
                return sb.ToString();
            }

            public int CompareTo(object o)
            {
                var ev = (LSSettings.LSTrigger)o;

                if (eType != ev.eType)
                {
                    return eType - ev.eType;
                }
                return highest() - ev.highest();
            }

            public int lowest()
            {
                int ret = int.MaxValue;
                // if (bUpload) ret = Math.Min(ret, (int)(pUpload * 100));
                if (bAudio) ret = Math.Min(ret, sAudio * 1000);
                if (bMouse) ret = Math.Min(ret, sMouse * 1000);
                return ret;
            }

            public int highest()
            {
                int ret = int.MinValue;
                // if (bUpload) ret = Math.Min(ret, (int)(pUpload * 100));
                if (bAudio) ret = Math.Max(ret, sAudio * 1000);
                if (bMouse) ret = Math.Max(ret, sMouse * 1000);
                return ret;
            }

            public struct Until
            {
                public bool isAudio;
                public long required;
                public long msec;
            }
            public Until until(long now)
            {
                Until ret = new Until();
                ret.isAudio = false;
                ret.msec = 0;
                
                if (eType == EventType.WARN_CONN_POOR ||
                    eType == EventType.WARN_CONN_DROP)
                {
                    ret.msec = lastNet <= pUpload ? 0 : int.MaxValue;
                    return ret;
                }
                if (bAudio)
                {
                    long rem = sAudio * 1000 - (now - lastAudio);
                    if (lastAudio < 0) rem = int.MaxValue;
                    if (rem > ret.msec)
                    {
                        ret.isAudio = true;
                        ret.required = sAudio * 1000;
                        ret.msec = rem;
                    }
                }
                if (bMouse)
                {
                    long rem = sMouse * 1000 - (now - lastMouse);
                    if (lastMouse < 0) rem = int.MaxValue;
                    if (rem > ret.msec)
                    {
                        ret.isAudio = false;
                        ret.required = sMouse * 1000;
                        ret.msec = rem;
                    }
                }
                return ret;
            }
        }

        public class LSServerPreset
        {
            public int port;
            public LSRelay relay;
            public string host, user, pass, mount;
            public string title, description, genre, url;
            public bool pubstream;
            public string presetName;

            public LSServerPreset()
            {
                presetName = "<no name>";
                host = user = pass = mount = "";
                relay = LSRelay.ice;
                port = 0;
            }
            public void SaveToProfile(LSSettings settings)
            {
                host = settings.host;
                port = settings.port;
                user = settings.user;
                pass = settings.pass;
                mount = settings.mount;
                relay = settings.relay;

                title = settings.title;
                description = settings.description;
                genre = settings.genre;
                url = settings.url;
                pubstream = settings.pubstream;
            }
            public void LoadFromProfile(LSSettings settings)
            {
                settings.host = host;
                settings.port = port;
                settings.user = user;
                settings.pass = pass;
                settings.mount = mount;
                settings.relay = relay;

                settings.title = title;
                settings.description = description;
                settings.genre = genre;
                settings.url = url;
                settings.pubstream = pubstream;
            }
            public bool Matches(LSSettings settings)
            {
                return (
                    settings.host == host &&
                    settings.port == port &&
                    settings.user == user &&
                    settings.pass == pass &&
                    settings.mount == mount &&
                    settings.relay == relay &&

                    settings.title == title &&
                    settings.description == description &&
                    settings.genre == genre &&
                    settings.url == url &&
                    settings.pubstream == pubstream
                );
            }
            public override string ToString()
            {
                return presetName;
            }
        }

        public class LSPreset
        {
            public double vRec, vMic, vSpd, vOut;
            public bool bRec, bMic, bOut;
            public double xRec, xMic;
            public double yRec, yMic;

            public LSPreset()
            {
            }
            public LSPreset(double vrec, double vmic, double vspd, double vout, bool brec, bool bmic, bool bout, double xrec, double xmic, double yrec, double ymic)
            {
                vRec = vrec;
                vMic = vmic;
                vSpd = vspd;
                vOut = vout;
                bRec = brec;
                bMic = bmic;
                bOut = bout;
                xRec = xrec;
                xMic = xmic;
                yRec = yrec;
                yMic = ymic;
            }
            public void apply(LSPreset preset)
            {
                vRec = preset.vRec;
                vMic = preset.vMic;
                vOut = preset.vOut;
                vSpd = preset.vSpd;
                bRec = preset.bRec;
                bMic = preset.bMic;
                bOut = preset.bOut;
                xRec = preset.xRec;
                xMic = preset.xMic;
                yRec = preset.yRec;
                yMic = preset.yMic;
            }
        }

        public class LSMeta
        {
            public class Yield
            {
                public class YieldMember
                {
                    public int bref;
                    public string text;
                    public YieldMember(int i)
                    {
                        bref = i;
                        text = null;
                    }
                    public YieldMember(string s)
                    {
                        text = s;
                        bref = -1;
                    }
                }
                string _base;
                public int max;
                List<YieldMember> mset;
                public Yield(string asdf)
                {
                    max = -1;
                    _base = asdf;
                    mset = new List<YieldMember>();
                    string strText = "";
                    string strBref = "";
                    foreach (char c in asdf)
                    {
                        strText += c;
                        if (c == '{')
                        {
                            strBref += c;
                        }
                        else if (strBref.Length != 0)
                        {
                            if (c >= '0' && c <= '9')
                            {
                                strBref += c;
                            }
                            else if (c == '}')
                            {
                                strBref += '}';
                                if (strBref.Length == 2)
                                {
                                    strBref = "";
                                }
                                else
                                {
                                    strText = strText.Substring(0, strText.Length - strBref.Length);
                                    if (!string.IsNullOrEmpty(strText))
                                    {
                                        mset.Add(new YieldMember(strText));
                                        strText = "";
                                    }
                                    // TODO: can throw, see 1401474325860.zm
                                    int v = Convert.ToInt32(strBref.Trim('{', '}'));
                                    mset.Add(new YieldMember(v));
                                    max = Math.Max(max, v);
                                    strBref = "";
                                }
                            }
                            else
                            {
                                strBref = "";
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(strText))
                    {
                        mset.Add(new YieldMember(strText));
                        strText = "";
                    }
                }
                public override string ToString()
                {
                    return _base;
                }
                public string format(System.Text.RegularExpressions.GroupCollection r)
                {
                    //r[m.grp].Value.Trim(' ', '\t', '\r', '\n');
                    if (r.Count <= max) return null;
                    StringBuilder ret = new StringBuilder();
                    foreach (YieldMember ym in mset)
                    {
                        if (ym.bref < 0)
                        {
                            ret.Append(ym.text);
                        }
                        else
                        {
                            ret.Append(r[ym.bref].Value.Trim(' ', '\t', '\r', '\n'));
                        }
                    }
                    return ret.ToString();
                }
                public string assert()
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (YieldMember ym in mset)
                    {
                        sb.Append(ym.bref > -1 ? "[" + ym.bref + "]" : "(" + ym.text + ")");
                    }
                    return sb.ToString();
                }
            }
            public enum Reader { WindowCaption, File, Website, ProcessMemory };
            public string src, ptn, tit, desc;
            [XmlIgnore]
            public Encoding enc;
            public string encoding { get { return enc.WebName; } set { enc = Encoding.GetEncoding(value); } }
            [XmlIgnore]
            public Yield yi;
            public string yield { get { return yi.ToString(); } set { yi = new Yield(value); } }
            public Reader reader;
            public int freq, grp, bnc;
            public bool urldecode;

            public LSMeta()
            {
                reader = Reader.WindowCaption;
                src = ptn = tit = desc = "";
                encoding = "utf-8";
                urldecode = false;
                yield = "{1}";
                freq = 500;
                grp = 1;
                bnc = 0;
            }
            public LSMeta(
                Reader r,
                string profileTitle,
                string dataSource,
                int pollingFrequency,
                string parserPattern,
                string parserYield = "{1}",
                string textEncoding = "utf-8",
                bool urlDecode = false,
                int keepGroup = 1,
                int debounce = 0,
                string description = "")
            {
                reader = r;
                tit = profileTitle;
                src = dataSource;
                ptn = parserPattern;
                yield = parserYield;
                freq = pollingFrequency;
                encoding = textEncoding;
                grp = keepGroup;
                urldecode = urlDecode;
                desc = description;
                bnc = debounce;
            }
            public override string ToString()
            {
                return tit;
            }
            public void apply(LSMeta meta)
            {
                LSMeta.apply(meta, this);
            }
            public static LSMeta copy(LSMeta meta)
            {
                LSMeta ret = new LSMeta();
                apply(meta, ret);
                return ret;
            }
            public static void apply(LSMeta src, LSMeta target)
            {
                target.src = src.src;
                target.ptn = src.ptn;
                target.tit = src.tit;
                target.yield = src.yield;
                target.encoding = src.encoding;
                target.reader = src.reader;
                target.freq = src.freq;
                target.grp = src.grp;
                target.urldecode = src.urldecode;
                target.desc = src.desc;
                target.bnc = src.bnc;
            }
            public bool eq(LSMeta meta)
            {
                return
                    src == meta.src &&
                    ptn == meta.ptn &&
                    tit == meta.tit &&
                    yield == meta.yield &&
                    encoding == meta.encoding &&
                    reader == meta.reader &&
                    freq == meta.freq &&
                    grp == meta.grp &&
                    urldecode == meta.urldecode &&
                    bnc == meta.bnc;
                // description left out intentionally
            }
        }
        public List<LSMeta> metas;
        public LSMeta meta;
        public bool latin;
        public bool tagAuto;

        public enum LSCompression { cbr, q };
        public enum LSChannels { mono, stereo }
        public class LSParams
        {
            public bool enabled;
            public LSCompression compression;
            public long bitrate;
            public long quality;
            public LSChannels channels;
            public string ext;

            [XmlIgnore]
            public double FIXME_kbps;

            [XmlIgnore]
            public LSInfo i;

            public LSParams()
            {
                i = new LSInfo();
            }
        }

        LSAudioSrc _devRec, _devMic, _devOut;
        public string s_devRec, s_devMic, s_devOut;
        public bool micLeft, micRight; //deprecated
        public int[] chRec, chMic, chOut;
        public LSWavetail wavetailer;

        [XmlIgnore]
        public LSAudioSrc[] devs;
        [XmlIgnore]
        public LSAudioSrc devRec { get { return _devRec; } set { _devRec = value; s_devRec = value == null ? "" : value.id; } }
        [XmlIgnore]
        public LSAudioSrc devMic { get { return _devMic; } set { _devMic = value; s_devMic = value == null ? "" : value.id; } }
        [XmlIgnore]
        public LSAudioSrc devOut { get { return _devOut; } set { _devOut = value; s_devOut = value == null ? "" : value.id; } }

        public LSPreset mixer;
        public LSPreset[] presets;
        public LSParams mp3, ogg, opus;
        public int samplerate;
        public int reverbP, reverbS;

        public List<LSServerPreset> serverPresets;
        public enum LSRelay { ice, shout, siren }
        public int port;
        public LSRelay relay;
        public string host, user, pass, mount;
        public string title, description, genre, url;
        public bool pubstream;
        public bool tagsock;

        public bool splash;
        public bool vu;
        public bool testDevs;
        public bool killmic;
        public bool recPCM;
        public bool recMp3;
        public bool recOgg;
        public bool recOpus;
        public bool showUnavail;
        public bool autoconn;
        public bool autohide;

        public string tboxFont;
        public double tboxSize;
        public bool tboxBold;
        public bool tboxItalic;
        public string tboxColorFront;
        public string tboxColorBack;
        public int tboxAlign;
        public string tboxAntialias;
        public string tboxRendermode;
        public int tboxWidth;
        public int tboxHeight;

        public bool warn_poor_DEPRECATED, warn_drop_DEPRECATED; // TODO: Delete all references
        public double lim_poor_DEPRECATED, lim_drop_DEPRECATED; // TODO: Delete all references

        public bool warn_poor, warn_drop; //backwards compatibility
        public double lim_poor, lim_drop; //backwards compatibility

        public List<LSTrigger> triggers;

        public LSSettings()
        {
            s_devRec = s_devMic = s_devOut = "";
            chMic = new int[] { 0 };
            chRec = new int[] { 0, 1 };
            chOut = new int[] { 0, 1 };

            wavetailer = new LSWavetail();

            mp3 = new LSParams();
            mp3.enabled = true;
            mp3.compression = LSCompression.cbr;
            mp3.bitrate = 192;
            mp3.quality = 2;
            mp3.channels = LSChannels.stereo;
            mp3.ext = "mp3";

            ogg = new LSParams();
            ogg.enabled = false;
            ogg.compression = LSCompression.q;
            ogg.bitrate = 192;
            ogg.quality = 5;
            ogg.channels = LSChannels.stereo;
            ogg.ext = "ogg";

            opus = new LSParams();
            opus.enabled = false;
            opus.compression = LSCompression.q;
            opus.bitrate = 128;
            opus.quality = 128;
            opus.channels = LSChannels.stereo;
            opus.ext = "opus";

            samplerate = 44100;
            reverbP = 0;
            reverbS = 90;

            serverPresets = new List<LSServerPreset>();
            host = "become.stream.r-a-d.io";
            port = 1337;
            user = "source";
            pass = "hackme";
            mount = "main";
            relay = LSRelay.ice;
            title = "Loopstream";
            description = "Cave Explorer Committee";
            genre = "Post-Avant Jazzcore";
            url = "https://github.com/9001/loopstream";
            pubstream = false;

            tboxFont = "Comic Sans MS";
            tboxSize = 24;
            tboxBold = false;
            tboxItalic = false;
            tboxColorFront = "fff";
            tboxColorBack = "000";
            tboxAlign = 5;
            tboxAntialias = "Clear";
            tboxRendermode = "Bitmap";
            tboxWidth = -1;
            tboxHeight = -1;

            latin = false;
            tagAuto = true;
            meta = new LSMeta();
            metas = new List<LSMeta>();
            triggers = new List<LSTrigger>();

            resetPresets();
            //presets[0] = new LSPreset(1, 1, 0.32, 1, true, true, true); //DEBUG
            //presets[0] = new LSPreset(0.5, 0.875, 0.32, 0.75, true, true, true); //DEBUG
            mixer = new LSPreset();
            mixer.apply(presets[0]);
            splash = true;
            vu = true;
            testDevs = true;
            killmic = false;
            recMp3 = true;
            recOgg = true;
            recOpus = true;
            recPCM = false;
            showUnavail = false;
            autoconn = false;
            autohide = false;

            // Backwards compatibility
            warn_poor = warn_drop = true;
            lim_poor = 0.92;
            lim_drop = 0.78;

            Logger.app.a("Base LSSettings ready, doing init");
            init();
        }

        public void init()
        {
            List<LSAudioSrc> ldev = new List<LSAudioSrc>();
            try
            {
                NAudio.CoreAudioApi.MMDeviceEnumerator mde = new NAudio.CoreAudioApi.MMDeviceEnumerator();
                Logger.app.a("Created MM enumerator");
                try
                {
                    foreach (
                        NAudio.CoreAudioApi.MMDevice device
                         in mde.EnumerateAudioEndPoints(
                            NAudio.CoreAudioApi.DataFlow.All,
                            NAudio.CoreAudioApi.DeviceState.All))
                    {
                        try
                        {
                            LSDevice add = new LSDevice();
                            add.mm = device;
                            add.isRec = device.DataFlow == NAudio.CoreAudioApi.DataFlow.Capture;
                            add.isPlay = device.DataFlow == NAudio.CoreAudioApi.DataFlow.Render;
                            if (device.DataFlow == NAudio.CoreAudioApi.DataFlow.All)
                            {
                                add.isRec = add.isPlay = true;
                            }
                            Logger.app.a("Df " + add.isPlay + " " + add.isRec);

                            add.id = device.ID;
                            Logger.app.a("ID " + add.id);

                            add.name = device.ToString();
                            Logger.app.a("Na " + add.name);

                            ldev.Add(add);
                        }
                        catch { Logger.app.a("Failed !"); }
                    }
                }
                catch { Logger.app.a("Failed !!"); }
            }
            catch { Logger.app.a("Failed !!!"); }

            ldev.Add(wavetailer);
            devs = ldev.ToArray();
            if (string.IsNullOrEmpty(s_devRec)) s_devRec = "";
            if (string.IsNullOrEmpty(s_devMic)) s_devMic = "";
            if (string.IsNullOrEmpty(s_devOut)) s_devOut = "";
            if (!string.IsNullOrEmpty(s_devRec)) devRec = getDevByID(s_devRec); // ?? devs.First(x => x.isPlay);
            if (!string.IsNullOrEmpty(s_devMic)) devMic = getDevByID(s_devMic);
            if (!string.IsNullOrEmpty(s_devOut)) devOut = getDevByID(s_devOut);
        }
        public void initFinally()
        {
            if (metas.Count == 0)
                resetMetas();

            if (triggers.Count == 0)
                resetTriggers();
            
            wavetailer.setFormat();
            LSSettings.singleton = this;

            bool scrap_it = true;
            foreach (var m in metas) {
                if (m.tit == "Foobar 2000  (window title)" && m.ptn == @" *(.*[^ ]) *( - foobar2000$|\[foobar2000 v([0-9\.]*)\]$)")
                    m.ptn = @" *(.*[^ ]) *( - foobar2000$|\[foobar2000( v[0-9\.]*)?\]$)";

                if (m.tit.Contains("Rekordbox"))
                    scrap_it = false;
            }
            if (scrap_it)
                resetMetas();
        }

        public void resetMetas()
        {
            metas.Clear();
            metas.AddRange(new LSMeta[] {
                new LSMeta(
                    LSMeta.Reader.WindowCaption,
                    "Foobar 2000  (window title)",
                    "foobar2000",
                    200,
                    @" *(.*[^ ]) *( - foobar2000$|\[foobar2000( v[0-9\.]*)?\]$)",
                    "{1}",
                    "utf-8",
                    false,
                    1,
                    5,
                    "H4sIAAAAAAAEAHWRMU8DMQyF9/sVbzmJSlBVjGyoLVKHAoIyVR3Mna+JmksqJ9fj/j1OQAgGtsj2e++zszM24iwcOWG0zkGYWkxhEDyE8E5yu1gstOPbMCLZ5PgaGI1tDIY4kHMTogljRDOIsE+4l2RjAvkWuzw+r6pNlw1Bwqqx/gjS6ZhCj2S4z4ZBYL+GGkP+yN8Ev2PRBekpJdWrwCb0NMEHpQ5y0pCd0R1KhpriZb182m7Xj6v16o9LzDZ4TYpH0r5tcIXOivLOUJCXwQ29j6URuQlams2rZ8ek5j2dGHHQiEwq3IcLgy8sUzJ5LetLdlfuBhV39qj27Fol1o7X2nkqOWeKSbUf1CQ33VXVvqZytxo3OOzrwlof/qnnd/fzOZ+am6aNwgEAAA=="
                ),
                new LSMeta(
                    LSMeta.Reader.ProcessMemory,
                    "Foobar 2000  (foo_audioscrobbler)",
                    "foobar2000",
                    200,
                    @"foo_audioscrobbler.dll+2b508*0, foo_audioscrobbler.dll+2b51c*0",
                    "{1}",
                    "utf-8",
                    false,
                    1,
                    5,
                    "H4sIAAAAAAAEAHWST2sbMRDF7/oU71owwnUCBUMLPtRQSOLQ7q2Uol3N2qLaGUeazcb99NXKDtiH3vTn6c383qg5hIxjokyKKcSIRM5D3T6jTzJADwT0Ir/d6IPkLknbRkrWF2knw1GYWBE4B0/YirQurZbLpTU/AndULNnLBA0aCaUSi2LM5BcozmU/SfqTQa/ECD1OMmIIHIbw99oMKtDkTtZsMqLwHi5XbXHCg8tqt49XzUxBD1evF1XaOa7yWrUT7sMeWOODMe8GrutknFkynnZNyeFlDKl22o5aPZjIz70UWnWFf87mGMd9YDj2IHbtTKlrsw1lgc9fgOdEPSUqWeTzQSMSL8vNTaTns59vv/D1bHR7bYz59vi8+95snpo1sON4uqRXef83I7xSykEYH+29/bSYG7XWmst4+prSjHy3aoMiS6+TS7S44U0j40HkmLV8jqHgX9SDeLKmEOZcsyg5aanUUpxHLuiKXOnq6d3K0hvVsIo+vU/Eqf0HYDNJeIcCAAA="
                ),
                new LSMeta(
                    LSMeta.Reader.WindowCaption,
                    "Spotify",
                    "spotify",
                    200,
                    @"^Spotify - *(.*[^ ]) *– *(.*[^ ]) *$",
                    "{1} - {2}",
                    "utf-8",
                    false,
                    1,
                    5,
                    "H4sIAAAAAAAEAGWQTWrDMBCF9z7FKy39MakgWRWyyqYXSHalgYk1tkRlyUjjuCYEeofesCepXBIaWm1Gw0hvvvfWXRBbj+gTJxDq3rnBajEwY2fYw/okTBqhztPITe8onmc7loFzXUWxSUBeY2PFsSo2xiZUwdcnWRdkUoikbUDiuOeYZqgik1jfQKhJcPaNcUttt7x+WsznSwBKqRTgWO4SavsOMSRXRYHT2a5P8I8o71X5ssXrA8qvj8/L9qbINIyORDhmPwna7q1mna1JgAwTUEeRhNGSVGYCamLou0wYPIOpMqhDvLS5cru+nRXnjxq7McPxv/gU8Gw9OTfOMGR9jg3/PJzWSmg432POftr5N171a/QwP2aPh8XxGwsP5XauAQAA"
                ),
                new LSMeta(
                    LSMeta.Reader.WindowCaption,
                    "VLC",
                    "vlc",
                    200,
                    @" *(.*[^ ]) * - VLC media player",
                    "{1}",
                    "utf-8",
                    false,
                    1,
                    5,
                    "H4sIAAAAAAAEAGWRwWrDMAyG73mKn55ayPIOJZcdOtih7O7WciJw7CCrzfr2k2MGg/lkydKvT7/PKetMgsLLGgmr5MCRejxJXjXJ0Qk04+NzfHsfe2ysM3Ii3HiC5xBIKN1pGIbuOhOUvhWHr8uIhTw7rNG9SA7WFiNuBKElP8kjSF5gg0HJI4f9qqw2ucN+CpsqWMEFhghOhT3tdUfgBEvswepUSdLQdecCZ7qcJoOjRIG1t7KmXYk2Tj5vbQx8piZcAfal/lP3nbUmXHJeiwq5pa1RuyzyDdpNBcdsBlYhyyzVrfpSSMzEk5FdZ1vi7lI14FEoPCK2KrwJq+H+ml4QTKd9x1+OMvwAo4V1mqUBAAA="
                ),
                new LSMeta(
                    LSMeta.Reader.WindowCaption,
                    "Winamp",
                    "winamp",
                    200,
                    @"([0-9]*\. )? *(.*[^ ]) * - Winamp$",
                    "{1}",
                    "utf-8",
                    false,
                    2,
                    5,
                    "H4sIAAAAAAAEAF2QzWrDMBCE736KweckUHoo5FboA5RSKD2u7VW8VD9GWtf123cth5ZEFyFm95sZfbKCYtKRM6acnHjGMko/IjMNBSYg8CCEydNqQ4vEIS1QUc8HoJttH44koxNFSJnRpzB56Ul5ODXNCzuaveJDIoXJxOjkMmdSSRFiDgll3IjmtHl4KQqOmlfEOXRmSVrFopQVydXH1d/LF5+bBvt5eHw64TnrRjjifRuxezdumuZ9nMuhbr+SKue4wyzDX3Px/lxpxyPeOKRvvoYocMn7tPCAbrXG7akFXE6h8jq+SIwSLzfx7kAUV5SJei4277aPojiAnCWpO+1t8haT9b1jVDb/KNq9VfufgQ124/4Lus7FPNsBAAA="
                ),
                new LSMeta(
                    LSMeta.Reader.WindowCaption,
                    "Aimp",
                    "aimp",
                    200,
                    @"(.*)",
                    "{1}",
                    "utf-8",
                    false,
                    2,
                    5,
                    "H4sIAAAAAAAEAMsoKSkottLXLy8v10vMzC3QKyrVBwBTfJugFAAAAA=="
                ),
                new LSMeta(
                    LSMeta.Reader.File,
                    "---------------------------------------------------",
                    "tag.txt",
                    1000,
                    ""
                ),
                new LSMeta(
                    LSMeta.Reader.ProcessMemory,
                    "FamiTracker v0.4.2   [32bit]",
                    "ft042",
                    1000,
                    "ft042.exe+156b98*8c*680, ft042.exe+156b98*8c*480",
                    "{1}",
                    "utf-16",
                    false,
                    1,
                    0,
                    "H4sIAAAAAAAEAJ1Sy26DMBC8+ytWOaYE0SqN6JFDI0VqpSrhBwxehCVjW7ZpyN93jUNaRWoP5Qaz89hhKw1cfHLdogCc+GAVgtHQmzMEA6NHCD3ChzMtev+Og3EXcMgFupzVEeEhoNPQSVQCpI8UAdwDh4YTmwvhiJlBZ5QyZ8KaC2HWSE08CI5LFZ0SnOc5Y5CeLhTbpxwnfHh83jUv5bps17uyyH6BtmXB2F46H2KKq21Uro714VRntJ1VXOqYAKMV6ZywNVrczdeH+u01n1NsNrCXNBArWCZMN78ORoxU1eoWZQUgNUEkZlNbi0RlLV5FeqpYYCsHrkCPQ0MNFFPaIVrP7GS0kI/U9c/vPnV77S+LqvQHk0MxlW2S+Y47Z8Jl/n+q1Pq9bDwB6JwZ/lBHOgpNi94sSMxhGOlayK1yQfrAvgADvUSJgAIAAA=="
                ),
                new LSMeta(
                    LSMeta.Reader.Website,
                    "other icecast mount",
                    "http://wessie.info:1130/",
                    2000,
                    "<tr>\\n<td><h3>Mount Point /[^<]*</h3></td>.*?<td>Current Song:</td>\\n<td class=\"streamdata\">(.*?)</td>",
                    "{1}",
                    "utf-8",
                    true,
                    1,
                    0,
                    "H4sIAAAAAAAEAE2QsW7DMAxEd38FtyRA0A/o1rFApyIdMtIWbQuWRYOiYPjve0pSoIQmkXx3x2/hQD4LfZjH4sQ50C16EhpNV2IKMnJNTnGQgTFQnL0W2niSt677KTLWRKMamSQ+Yp6AUAANkya8Am5ap5m+VLfnF/buUuiTlqw7+uy08oKfrFQkF7l2ffVH41SIU9GHw164+kE6/kO9d3etNHCmoDTouiVxSQfMOFuQQGWO8D7+wfYmdmBl5+zwQa3O6MZCeAgbc3wQEALpDWMUM3hlUzgjV5ph+ma8OEJjp5dDcTQXs9gnubygrzoj5iqcr9RkT+3WMS80ia2c4UdrCkAQY7XBd4M8fAwC4c10Ml7L9em7HaHlufwCFqKqebUBAAA="
                ),
                new LSMeta(
                    LSMeta.Reader.Website,
                    "R/a/dio website",
                    "https://r-a-d.io/",
                    2000,
                    "\\n\\s*<span id=\"np\">\\n\\s*([^\\n]*[^\\n]*)\\n",
                    "{1}",
                    "utf-8",
                    true,
                    1,
                    0,
                    "H4sIAAAAAAAEACXLMQ6AIAwF0N1T/M0J2R29gInGA2Ao0CjWQB28vRrXl7yJnIe6WBGKZCTVs/bWFuOM71hs0yyVwrWDA265Wo+dN4IKKK/kPyv/5+PFcZghBVUyaeIjPpCDra5fAAAA"
                ),
                new LSMeta(
                    LSMeta.Reader.Website,
                    "Rekordbox plugin",
                    "http://127.0.0.1:41379",
                    1000,
                    "(.*)",
                    "{1}",
                    "utf-8",
                    true,
                    1,
                    0,
                    "H4sIAAAAAAAEACtOTVXIKCkpKLbS10/PLMkoTdJLzs/V904tyShOLNIPSs3OL0pJyq8ISk1MSS0CAAWU0J0uAAAA"
                ),
                new LSMeta(
                    LSMeta.Reader.Website,
                    "Traktor plugin  (does VirtualDJ too)",
                    "http://127.0.0.1:42069/statuls.xsl",
                    1000,
                    "\\n<h42>( - )?([^\\n<]*)\\n*([^\\n<]*)\\n*([^<]*)</h42>\\n",
                    "{2}{3}{4}",
                    "utf-8",
                    true,
                    1,
                    0,
                    "H4sIAAAAAAAEACtOTVXIKCkpKLbS189PLtPLTdXPyc8vKC4pSk3M1S8pSswuyS/SBwC6STfxJgAAAA=="
                ),
                new LSMeta(
                    LSMeta.Reader.File,
                    "---------------------------------------------------",
                    "tag.txt",
                    1000,
                    ""
                ),
                new LSMeta(
                    LSMeta.Reader.WindowCaption,
                    "SoundCloud, YouTube, Spotify    ( Firefox )",
                    "*",
                    1000,
                    @"^▶  *(.*?[^ ]) *(- YouTube|- Spotify)? - (Mozilla Firefox|Nightly|Google Chrome)$",
                    "{1}",
                    "utf-8",
                    false,
                    1,
                    0,
                    "H4sIAAAAAAAEADWQQU7EMAxF9znFPwAqe5YgsYPNDEKzdBpPY00aV4nTUk5PyoB39n+2nvxcdKtc6pPDUW/6LSkRXqXwVb/wiHeZoqXduU/2VYz/yZO2HF6StnDvL9rOzfNfuKjJtS85d45SUaO2FLBpuUEyeOWyY5Qytrka5ZEfAModEOukIdIqeYIpjMrEhplyo5T2AfjIXcI4DO5k3RWa0w7yiQ+8MAVYZNBosvYR+X66Km7My28wcxDCkmjncrgQKi9UyBj+/otukYNuww9HedJjGwEAAA=="
                ),
                new LSMeta(
                    LSMeta.Reader.WindowCaption,
                    "SoundCloud, YouTube, Spotify    ( Chrome )",
                    "*",
                    1000,
                    @"^ *(.*?[^ ]) *(- YouTube|- Spotify)? - (Google Chrome)$",
                    "{1}",
                    "utf-8",
                    false,
                    1,
                    0,
                    "H4sIAAAAAAAEADWQsW7FMAhFd38FH1Ale4cOfUOnTn1S1ZHEJEF1jGVwokj9+JKXlsmAL5zLa5VdqepzgDPeROZEcFuqrBTCJw3KRv/dD2k53pK0eOVf0u5toL9mEePpCCFcathRodJI2dIBrUQ0ik9eKQlHzjPYQmAVIxtLxgQ/L6DHOkiCnW0BBC2E31SBR8lduC+ssHpBgQ14LaLKg8OawMTJ/KM0g4JNKUIPalKKv1TyrIA5wsaRRGFyOhjoRKiEsQvvPhS01ccoyU674EYPvrHVehlw6uOUHL7EPfd63mI8b9Hvi1vbfL8bxtH41OLQ/QJGhKpcWwEAAA=="
                ),
                new LSMeta(
                    LSMeta.Reader.WindowCaption,
                    "Google Music                               ( Chrome + Firefox )",
                    "*",
                    1000,
                    //@"^ *(.*?[^ ])  *- Google Play Music - (Mozilla Firefox|Nightly|Google Chrome)$",
                    @"^ *(.*?[^ ])( - (.*?[^ ]))?  *- Google Play Music - (Mozilla Firefox|Nightly|Google Chrome)$",
                    "{3} - {1}",
                    "utf-8",
                    false,
                    1,
                    0,
                    "H4sIAAAAAAAEADWOsW7DMAxEd3/FbV0Cd++WFGinFBkCdJZlxiasiAZJw3W+vpKbHjjd3SN5UlmN1N4aVJ3lwSkFfLDSTX7wii8eRk/bX/wpMiTC+6hyp6b5ps7Y6R9+pufFODZVT2Mimg3LDB8JrqFnZ8mQGzriPKDnOBm6DfPiXo0re8G68oESjupsDs47vnLuZYXXRtscYxTtC5K2A/b8EtxJc63HMehA9UwpKcV9tY9sKBNwJ7O2bYFLomCEXvKLI4lMxRN9/ALIywW3GgEAAA=="
                ),
                new LSMeta(
                    LSMeta.Reader.WindowCaption,
                    "MediaMonkey",
                    "MediaMonkey",
                    500,
                    "^([0-9]*\\. )?(.*) - MediaMonkey$",
                    "{2}"
                ),
                new LSMeta(
                    LSMeta.Reader.WindowCaption,
                    "MusicBee",
                    "MusicBee",
                    500,
                    "^(.*) - MusicBee$",
                    "{1}"
                ),

            });
            metaDec();
        }
        void metaEnc()
        {
            foreach (LSMeta meta in metas)
            {
                metaEnc(meta);
            }
            metaEnc(this.meta);
        }
        void metaEnc(LSMeta meta)
        {
            try
            {
                meta.desc = Z.gze(meta.desc.Replace("\r", ""));
            }
            catch
            {
                meta.desc = "(encoding failed)";
            }
        }
        void metaDec()
        {
            foreach (LSMeta meta in metas)
            {
                metaDec(meta);
            }
            metaDec(this.meta);
        }
        void metaDec(LSMeta meta)
        {
            try
            {
                meta.desc = Z.gzd(meta.desc).Replace("\r", "").Replace("\n", "\r\n");
            }
            catch
            {
                //meta.desc = "(encoding failed)";
            }
        }
        public void resetTriggers()
        {
            triggers.Clear();
            triggers.AddRange(new LSTrigger[] {
                new LSTrigger(LSTrigger.EventType.WARN_CONN_POOR, true,  0.9,  false, 0.05,  0, false, 0),
                new LSTrigger(LSTrigger.EventType.WARN_CONN_DROP, true,  0.75, false, 0.05,  0, false, 0),
                new LSTrigger(LSTrigger.EventType.WARN_NO_AUDIO,  false, 0,    true,  0.05, 10, false, 0),
                new LSTrigger(LSTrigger.EventType.DISCONNECT,     false, 0,    true,  0.05, 60, true,  60),
                new LSTrigger(LSTrigger.EventType.DISCONNECT,     false, 0,    false, 0.05,  0, true,  1800)
            });
        }
        public void resetPresets()
        {
            presets = new LSPreset[] {
                new LSPreset(1.00, 0, 0.6, 1, true, true, false, 1, 1, -1, -1),
                new LSPreset(0.15, 1, 0.6, 1, true, true, false, 1, 4, -1, -1),
                new LSPreset(1.00, 0, 0.6, 1, true, true, true, 1, 1, -1, -1),
                new LSPreset(0.15, 1, 0.6, 1, true, true, true, 1, 4, -1, -1),
            };
        }
        public void runTests(Progress splesh, bool forceTest)
        {
            Program.DBGLOG = "";
            if (testDevs || forceTest)
            {
                StringBuilder sw = new StringBuilder();
                for (int a = 0; a < devs.Length; a++)
                {
                    if (!(devs[a] is LSDevice))
                        continue;

                    var dev = (LSDevice)devs[a];

                    if (splesh != null)
                    {
                        splesh.prog(a + 1, devs.Length);
                    }
                    //dev.test();
                    try
                    {
                        dev.test();
                        if (dev.mm != null)
                        {
                            sw.AppendLine(dev.mm.ID);
                            sw.AppendLine(dev.mm.DeviceFriendlyName);
                            sw.AppendLine(dev.mm.FriendlyName);
                            try
                            {
                                sw.AppendLine(LSDevice.stringer(dev.wf));
                            }
                            catch
                            {
                                sw.AppendLine("*** bad wf ***");
                            }
                        }
                    }
                    catch
                    {
                        sw.AppendLine("*!* bad dev *!*");
                    }
                    sw.AppendLine();
                    sw.AppendLine("---");
                    sw.AppendLine();
                }
                Program.DBGLOG += sw.ToString();
            }
        }

        public LSAudioSrc getDevByID(string id)
        {
            foreach (LSAudioSrc dev in devs)
            {
                if (dev.id == id)
                {
                    return dev;
                }
            }
            return null;
        }

        static int version()
        {
            string[] str = System.Windows.Forms.Application.ProductVersion.Split('.');
            return
                (Convert.ToInt32(str[0]) << 0x18) +
                (Convert.ToInt32(str[1]) << 0x10) +
                (Convert.ToInt32(str[2]) << 0x8) +
                (Convert.ToInt32(str[3]));
        }
        public void save()
        {
            // could and should probably use GeneralInfo.ser, but:
            //  * Keeping this for now ince It Just Works
            //  * metaEnc/Dec() might complicate things

            string fn = Program.beta > 0 ? "Loopstream-beta.ini" : "Loopstream.ini";
            metaEnc();
            var x = new XmlSerializer(this.GetType());
            using (var s = new System.IO.FileStream(fn, System.IO.FileMode.Create))
            {
                byte[] ver = System.Text.Encoding.UTF8.GetBytes(version().ToString("x") + "\n");
                s.Write(ver, 0, ver.Length);
                x.Serialize(s, this);
            }
            metaDec();
        }
        public static LSSettings load()
        {
            Logger.app.a("Loading LSSettings");
            string fn_rls = "Loopstream.ini";
            string fn_beta = "Loopstream-beta.ini";
            LSSettings ret;
            XmlSerializer x = new XmlSerializer(typeof(LSSettings));
            if (System.IO.File.Exists(fn_rls) ||
                (Program.beta > 0 && System.IO.File.Exists(fn_beta)))
            {
                Logger.app.a("Found existing .ini");

                if (Program.beta > 0 && !System.IO.File.Exists(fn_beta))
                {
                    System.Windows.Forms.MessageBox.Show("since this is a beta version i'll make a copy of\r\n" + fn_rls+ " with all your settings\r\n\r\ni will be using " + fn_beta);
                    System.IO.File.Copy(fn_rls, fn_beta);
                }

                try
                {
                    string str = System.IO.File.ReadAllText(Program.beta > 0 ? fn_beta : fn_rls, Encoding.UTF8);
                    string ver = str.Substring(0, str.IndexOf('\n'));
                    str = str.Substring(ver.Length + 1);
                    System.IO.MemoryStream s = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(str));

                    int myVer = version();
                    int iniVer = Convert.ToInt32(ver.Trim(), 16);
                    if (Program.beta == 0 && myVer != iniVer)
                    {
                        string fn_bak = "Loopstream-backup-" +
                            ((iniVer >> 0x18) & 0xff) + "." +
                            ((iniVer >> 0x10) & 0xff) + "." +
                            ((iniVer >> 0x8) & 0xff) + "." +
                            ((iniVer) & 0xff) + ".ini";

                        if (!System.IO.File.Exists(fn_bak))
                        {
                            System.IO.File.Move(fn_rls, fn_bak);
                            System.IO.File.Copy(fn_bak, fn_rls);
                        }
                    }

                    //if (myVer != iniVer)
                    if (iniVer < 0x01020600)
                    {
                        byte[] bver = BitConverter.GetBytes(iniVer);
                        Array.Reverse(bver);
                        System.Windows.Forms.MessageBox.Show(
                            "Your configuration file is from version " + BitConverter.ToString(bver) + ", which is rather old.\n\n" +
                            "Don't get your hopes up but I'll try to load it...",
                            "Incompatible config file",
                            System.Windows.Forms.MessageBoxButtons.OK,
                            System.Windows.Forms.MessageBoxIcon.Information);
                    }
                    //using (var s = System.IO.File.OpenRead("Loopstream.ini"))
                    {
                        ret = (LSSettings)x.Deserialize(s);
                        //
                        //  Upgrade from v1.2.8.0:
                        //     xRec xMic lim_poor lim_drop
                        //
                        //  Upgrade from v1.3.7.7:
                        //     yRec yMic
                        //
                        //  Upgrade from v1.4.0.4:
                        //     chMic
                        //
                        if (ret.mixer.xRec < 1) ret.mixer.xRec = 1;
                        if (ret.mixer.xMic < 1) ret.mixer.xMic = 1;
                        if (ret.mixer.yRec < 1) ret.mixer.yRec = -1;
                        if (ret.mixer.yMic < 1) ret.mixer.yMic = -1;
                        foreach (LSSettings.LSPreset pre in ret.presets)
                        {
                            if (pre.xRec < 1) pre.xRec = 1;
                            if (pre.xMic < 1) pre.xMic = 1;
                            if (pre.yRec < 1) pre.yRec = -1;
                            if (pre.yMic < 1) pre.yMic = -1;
                        }
                        if (ret.lim_drop_DEPRECATED <= 0 || ret.lim_poor_DEPRECATED <= 0)
                        {
                            ret.warn_poor_DEPRECATED = ret.warn_drop_DEPRECATED = true;
                            ret.lim_poor_DEPRECATED = 0.9;
                            ret.lim_drop_DEPRECATED = 0.6;
                        }
                        if (ret.reverbS <= 0) ret.reverbS = 90;
                        if (ret.reverbP <= 0) ret.reverbP = 0;
                        if (iniVer <= 0x01040004)
                        {
                            if (ret.micLeft && ret.micRight)
                                ret.chMic = new int[] { 0, 1 };
                            else if (ret.micRight)
                                ret.chMic = new int[] { 1 };
                            else
                                ret.chMic = new int[] { 0 };
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.app.a("Deserialize failed, fallback to new");
                    ret = new LSSettings();
                    System.Windows.Forms.MessageBox.Show(
                        "Failed to load settings:\n«Loopstream.ini» is probably from an old version of the program.\n\nDetailed information:\n" + e.Message + "\n" + e.StackTrace,
                        "Default settings",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Information);
                }
                try
                {
                    Logger.app.a("Calling init()");
                    ret.init();
                    ret.mp3.FIXME_kbps =
                    ret.ogg.FIXME_kbps =
                    ret.opus.FIXME_kbps = -1;
                    foreach (var v in ret.metas)
                    {
                        if (v.grp != 1)
                        {
                            v.yield = "{" + v.grp + "}";
                            v.grp = 1;
                        }
                    }
                    try
                    {
                        ret.metaDec();
                    }
                    catch { }
                    Logger.app.a("LSSettings deserialize successful");
                    ret.initFinally();
                    return ret;
                }
                catch (Exception e)
                {
                    Logger.app.a("LSSettings deserialize postprocessing failed");
                    System.Windows.Forms.MessageBox.Show(
                        "Failed to initialize settings object:\nPossibly from an outdated version of «Loopstream.ini», though more likely a developer fuckup. Go tell ed this:\n\n" +
                        e.Message + "\n\n" + e.Source + "\n\n" + e.InnerException + "\n\n" + e.StackTrace);
                }
            }
            Logger.app.a("Creating new LSSettings");
            ret = new LSSettings();
            
            Logger.app.a("LSSettings OK");
            ret.initFinally(); // it is 06:20 am, what are you looking at
            return ret;
        }
    }
}
