using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using static Wow.Packet.Data;

namespace WowBGFilter
{

    public partial class MainForm : Form
    {
        private readonly string savefile = "settings.xml";
        private readonly string wavfile_accept = "ee.wav";
        private readonly string wavfile_pending = "pending.wav";
        private readonly string exePath;
        private readonly System.Media.SoundPlayer play_accept;
        private readonly System.Media.SoundPlayer play_pending;

        public MainForm()
        {
            InitializeComponent();
            Glue.Init(this);
            exePath = AppDomain.CurrentDomain.BaseDirectory;
            string wavac = Path.Combine(exePath, @"Resources", wavfile_accept);
            string wavpen = Path.Combine(exePath, @"Resources", wavfile_pending);
            play_accept = new System.Media.SoundPlayer(wavac);
            play_pending = new System.Media.SoundPlayer(wavpen);
        }

        public void Button1_Click(object sender, EventArgs e)
        {
            if (Glue.Start())
            {

            }
            else Glue.Log("cant start");
        }
        private void Button2_Click(object sender, EventArgs e)
        {
            Glue.Stop();
        }
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Glue.Stop();
            SaveToAFile();
        }
        private void Timer1_Tick(object sender, EventArgs e)
        {
            Glue.Loop();
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            label1.Text = "Not Started";
            button2.Enabled = false;
            listBox1.Items.Clear();
            LoadFromAFile();
        }
        private void SaveToAFile()
        {
            string path = Path.Combine(exePath, savefile);

            var root =
                new XElement("Settings",
                    new XElement("CheckedItems",
                        checkedListBox1.CheckedIndices.Cast<int>().Select(i => new XElement("Index", i))
                    ),
                    new XElement("Checkboxes",
                        new XElement("autoAcceptBox", autoAcceptBox.Checked),
                        new XElement("autoJoinBox", autoJoinBox.Checked)
                    ),
                                        new XElement("Checkboxes",
                        new XElement("autoAcceptBox", autoAcceptBox.Checked),
                        new XElement("autoJoinBox", autoJoinBox.Checked)
                    )
                );
            root.Save(path);
        }
        private void LoadFromAFile()
        {
            string path = Path.Combine(exePath, savefile);
            if (File.Exists(path))
            {
                XDocument doc = XDocument.Load(savefile);
                var savedIndices = doc.Descendants("Index").Select(x => (int)x);

                foreach (int index in savedIndices)
                {
                    if (index < checkedListBox1.Items.Count)
                    {
                        checkedListBox1.SetItemChecked(index, true);
                    }
                }

                XElement cbFolder = doc.Element("Settings")?.Element("Checkboxes");
                if (cbFolder != null)
                {
                    autoAcceptBox.Checked = (bool?)cbFolder.Element("autoAcceptBox") ?? false;
                    autoJoinBox.Checked = (bool?)cbFolder.Element("autoJoinBox") ?? false;
                }

            }
        }
        public static class Glue
        {
            private static MainForm mf;

            private static bool waitingForUserAction = false;
            private static bool playing_pending = false;
            public static bool Started { get; private set; } = false;

            public static void Init(MainForm f)
            {
                mf = f;
                BindBGFilter();

            }
            public static void Log(string s)
            {
                mf.listBox1.Items.Add(s);
                mf.listBox1.TopIndex = mf.listBox1.Items.Count - 1;
            }
            public static void Stop()
            {
                Started = false;

                Wow.Unload();
                mf.button1.Enabled = true;
                mf.button2.Enabled = false;
                mf.label1.Text = "Unloaded";
                mf.label1.ForeColor = Color.Black;
            }
            public static bool Start()
            {
                if (Started)
                {
                    Log("ERROR already started");
                    return false;
                }
                Log("Starting...");

                if (!Wow.Inject())
                {
                    Log("ERROR failed to Wow.Inject");
                    return false;
                }
                mf.label1.Text = "Injected";
                mf.button1.Enabled = false;
                mf.button2.Enabled = true;
                Started = true;
                return true;
            }
            public static void BindBGFilter()
            {
                CheckedListBox filter = mf.checkedListBox1;

                var bindableSource = Wow.Packet.Data.BG.Select(kvp => new
                {
                    Id = kvp.Key,
                    Display = kvp.Value.BG_Name,
                }).ToList();

                filter.DataSource = bindableSource;

                filter.DisplayMember = "Display";
                filter.ValueMember = "Id";
            }
            public static bool IsBG_Checked(BGID_patterns bgid)
            {
                foreach (dynamic item in mf.checkedListBox1.CheckedItems)
                {
                    if (item.Id == bgid)
                    {
                        return true;
                    }
                }
                return false;
            }

            private static void PlayPending()
            {
                if (playing_pending)
                    return;

                mf.play_pending.Play();
                playing_pending = true;
            }
            private static void StopPlayingPending()
            {
                if (!playing_pending)
                    return;

                mf.play_pending.Stop();
                playing_pending = false;
            }
            private static void SetUpLabels()
            {

            }
            public static void Loop()
            {
                if (!Started)
                    return;

                Wow.ProcessPackets();

                bool ingame = Wow.IsInGame();
                if (!ingame)
                {
                    mf.label1.Text = "Loading the world";
                    mf.label1.ForeColor = Color.Black;
                    StopPlayingPending();
                    return;
                }
                bool inbg = Wow.IsInBG();
                if (inbg)
                {
                    mf.label1.Text = "Joined BG";
                    mf.label1.ForeColor = Color.LightGreen;
                    StopPlayingPending();
                    return;
                }
                bool queued = Wow.IsBGQueued();
                if (queued)
                {
                    mf.label1.Text = "Queued";
                    mf.label1.ForeColor = Color.Black;
                    return;
                }

                bool inviteIsPending = Wow.IsBGInvitePending();

                bool autoAccept = mf.autoAcceptBox.Checked;
                bool autoJoin = mf.autoJoinBox.Checked;
                bool acceptInvite = false;
                if (Wow.IsReadyToQueue())
                {
                    mf.label1.Text = "Ready";
                    mf.label1.ForeColor = Color.Black;
                    StopPlayingPending();
                    waitingForUserAction = false;
                    if (autoJoin)
                    {
                        Wow.JoinBGQueue();
                        Log(">Joining BG Queue");
                    }
                }

                var p = Wow.packets.LastOrDefault();
                if (p != null)
                    if (p.IsID_Equals(Wow.Packet.Data.PacketID_patterns.PACKET_ID_BGINVITE))
                    {
                        Wow.Packet_BG_invite pp = (Wow.Packet_BG_invite)p;
                        Log($"found {pp.GetBGName}");
                        mf.label1.Text = pp.GetBGName;
                        bool filtered = IsBG_Checked(pp.GetBGID);
                        if (filtered && autoAccept) acceptInvite = true;
                        if (filtered && !autoAccept) waitingForUserAction = true;
                        Wow.packets.Remove(p);

                    }

                if (inviteIsPending && waitingForUserAction)
                {
                    mf.label1.ForeColor = Color.YellowGreen;
                    PlayPending();
                }
                else
                if (inviteIsPending)
                {
                    if (!acceptInvite)
                    {
                        Wow.LeaveBGQueue();
                        StopPlayingPending();
                        mf.label1.ForeColor = Color.Red;
                        Log(">Leaving BG Queue");
                    }
                    else
                    {
                        Wow.BGAcceptInvite();
                        mf.play_accept.Play();
                        mf.label1.ForeColor = Color.LightGreen;
                        Log(">Accepting the Invite");
                    }
                }

            }

        }
    }

}
