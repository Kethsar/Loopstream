﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Loopstream
{
    public partial class Verter : UserControl
    {
        public Verter()
        {
            InitializeComponent();
            _enabled = true;
            _boost = 1;
            _boostLock = -1;
            defaultFG = gLabel.ForeColor;
            defaultBG = gLabel.BackColor;
            w8fuckOn = new Padding(0, 4, 0, 0);
            w8fuckOff = new Padding(1, 5, 1, 0);
            giSlider.Font = new Font(giSlider.Font.FontFamily, giSlider.Font.Size * 2);

            Skinner.add(this);
        }
        ~Verter()
        {
            Skinner.rem(this);
        }

        public event EventHandler valueChanged;
        public bool timeScale { get; set; }
        Color defaultFG;
        Color defaultBG;
        Padding w8fuckOn;
        Padding w8fuckOff;
        public enum EventType { set, slide, mute, boost, boostLock, solo, airhorn };
        public EventType eventType;
        bool _enabled;
        public bool enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                _enabled = value;
                gLabel.ForeColor = value ? defaultFG : Color.White;
                gLabel.BackColor = value ? defaultBG : Color.DarkRed;
                gLabel.FlatStyle = value ? FlatStyle.Standard : FlatStyle.Popup;
                
                //gButton.Padding.Left = value ? 0 : 1;
                //gButton.Padding.Right = value ? 0 : 1;
                gButton.Padding = value ? w8fuckOn : w8fuckOff;
                gLabel.Height = value ? 40 : 38;
            }
        }
        public string title
        {
            get
            {
                return gLabel.Text;
            }
            set
            {
                gLabel.Text = value;
            }
        }
        public bool canToggle
        {
            get
            {
                return gLabel.Enabled;
            }
            set
            {
                gLabel.Enabled = value;
            }
        }
        double _boost;
        public double boost
        {
            get
            {
                return _boost;
            }
            set
            {
                _boost = Math.Max(1, value);
                _boost = Math.Max(_boost, _boostLock);
                level = level;
            }
        }
        double _boostLock;
        public double boostLock
        {
            get
            {
                return _boostLock;
            }
            set
            {
                _boostLock = value > 0 && CanBoost ? Math.Max(1, value) : -1;
                level = level;
            }
        }
        int _level;
        public int level
        {
            get
            {
                return Math.Min(Math.Max(_level - 40, 0), 255);
            }
            set
            {
                //System.Windows.Forms.MessageBox.Show(value.ToString());
                bool inval = _level <= 40 || _level >= 255 + 40;
                _level = Math.Min(Math.Max(value, -40), 295) + 40;
                int top = gOSlider.Height - _level;
                giSlider.Bounds = new Rectangle(0, top - 2, gOSlider.Width, _level + 2);
                string txt = timeScale ?
                    (Math.Round(level / 200.0, 2) + " s") :
                    (Math.Round(level /  2.55, 0) + " %");

                if (_boost > 1 && CanBoost)
                {
                    txt = _boost.ToString("0.0") + " x\n" + txt;
                }
                giSlider.Text = txt;
                giSlider.ForeColor =
                    CanBoost &&
                    boostLock > 1.1 &&
                    boostLock + 0.05 > boost ?
                    Color.FromArgb(160, 0, 0) :
                    giSlider.ForeColor = Color.White;
                //gLabel.Text = level.ToString();

                inval = inval || _level <= 40 || _level >= 255 + 40;
                if (inval)
                {
                    if (_level > 100) graden1.Invalidate();
                    if (_level < 100) graden2.Invalidate();
                }
            }
        }
        public bool CanBoost { get; set; }
        public Color A_GRAD_1 { get { return giSlider.A_GRAD_1; } set { giSlider.A_GRAD_1 = value; } }
        public Color A_GRAD_2 { get { return giSlider.A_GRAD_2; } set { giSlider.A_GRAD_2 = value; } }

        public void makeInteractive()
        {
            foreach (var c in new Control[] {  graden1, graden2, giSlider, gOSlider })
            {
                c.MouseDown += slider_MouseDown;
                c.MouseMove += slider_MouseMove;
                c.MouseUp += slider_MouseUp;
            }
            gSolo.Enabled = false;
            gAirhorn.Enabled = false;
        }

        public void disableOutput()
        {
            gAirhorn.Enabled = false;
        }

        private void gLabel_Click(object sender, EventArgs e)
        {
            enabled = !enabled;
            eventType = EventType.mute;
            emit();
        }
        void emit()
        {
            if (valueChanged != null)
            {
                valueChanged(this, null);
            }
        }

        int slideDelta = -1;
        long clickTime = -1;
        int clickOffset = -1;
        Point clickPosition = Point.Empty;
        double oldBoost = -1;

        private void slider_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                clickTime = DateTime.UtcNow.Ticks / 10000;
                clickOffset = ((Control)sender).Top + 40;
                clickPosition = Cursor.Position;
                slideDelta = level - (gOSlider.Height - e.Y - clickOffset);
                oldBoost = -1;
                //gLabel.Text = slideDelta.ToString();
            }
            else if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                eventType = EventType.boostLock;
                boostLock = boost - boostLock < 0.05 ? -1 : boost;
                boost = boost;
                emit();
                Application.DoEvents();
                if (sender == giSlider)
                {
                    clickOffset = giSlider.Top + 40;
                }
            }
        }

        bool fsckoff = false;
        object fscker = new object();
        private void slider_MouseMove(object sender, MouseEventArgs e)
        {
            lock (fscker)
            {
                if (fsckoff) return;
                fsckoff = true;
            }
            if (e.Button != System.Windows.Forms.MouseButtons.Left)
            {
                fsckoff = false;
                return;
            }
            long now = DateTime.UtcNow.Ticks / 10000;
            Point mouse = Cursor.Position;
            int dx = Math.Abs(mouse.X - clickPosition.X);
            int dy = Math.Abs(mouse.Y - clickPosition.Y);
            int d = dx + dy;
            if (now - clickTime < 150 && d < 8)
            {
                fsckoff = false;
                return;
            }
            if (dx > 100 && oldBoost < 0 && CanBoost)
            {
                oldBoost = boost;
                clickPosition = mouse;
            }
            else if (oldBoost >= 1)
            {
                //double delta = (mouse.X - clickPosition.X) * 0.005;
                //boost = Math.Max(1, oldBoost + delta);
                double pow = (mouse.X - clickPosition.X) / 200.0;
                pow = pow < 0 ? pow - 1 : pow + 1;
                boost *= pow < 0 ? 1 / -pow : pow;
                eventType = EventType.boost;
                clickPosition = mouse;
                level = level;
            }
            else
            {
                level = slideDelta + gOSlider.Height - e.Y - clickOffset;
                eventType = EventType.set;
            }

            clickTime -= 1000;
            emit();
            Application.DoEvents();
            if (sender == giSlider)
            {
                clickOffset = giSlider.Top + 40;
            }
            fsckoff = false;
        }

        private void slider_MouseUp(object sender, MouseEventArgs e)
        {
            long now = DateTime.UtcNow.Ticks / 10000;
            if (now - clickTime < 150)
            {
                level = gOSlider.Height - e.Y - clickOffset;
                if (enabled) eventType = EventType.slide;
                else eventType = EventType.set;
                emit();
            }
        }

        private void gSolo_Click(object sender, EventArgs e)
        {
            eventType = EventType.solo;
            emit();
        }

        private void gAirhorn_Click(object sender, EventArgs e)
        {
            eventType = EventType.airhorn;
            emit();
        }
    }
}
