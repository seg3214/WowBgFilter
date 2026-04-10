using Magic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Glue = WowBGFilter.MainForm.Glue;

using PacketIDSize = System.UInt64;
public static partial class Wow
{
    [DllImport("user32.dll")]
    public static extern int PostMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x101;
    private const int VK_F10 = 0x79;
    private const int VK_F11 = 0x7A;
    private const int VK_F9 = 0x78;

    public static List<Packet> packets = new List<Packet>();

    public static void ProcessPackets()
    {
        while (PacketEditor.DataQueueService.TryDequeueData(out byte[] receivedBytes))
        {
            bool r = Wow.CreatePacket(ref receivedBytes);
            if (!r)
                continue;
        }
    }
    public static bool CreatePacket(ref byte[] data)
    {
        PacketIDSize t1 = 0;

        bool r = Array_To_ULONG_BE(ref data, Packet.Data.PACKET_ID_position, Packet.Data.PACKET_ID_length, ref t1);
        Debug.Assert(r == true, "Array_To_ULONG_BE failed");

        Packet.Data.PacketID_patterns t = (Packet.Data.PacketID_patterns)t1;

        switch (t)
        {
            case Packet.Data.PacketID_patterns.PACKET_ID_BGINVITE:
                Packet p = new Wow.Packet_BG_invite(ref data, t);
                packets.Add(p);
                break;
            case Packet.Data.PacketID_patterns.PACKET_ID_BGQUEUED:
                break;
            default:
                return false;
        }
        return true;
    }

    public abstract class Packet
    {
        public static class Data
        {
            public enum PacketID_patterns : PacketIDSize
            {
                //Big-Endian values
                PACKET_ID_BGINVITE = 0x5055,
                PACKET_ID_BGQUEUED = 0x222,//dummy
            }
            public const int PACKET_ID_position = 14;   //Big-Endian 0..n
            public const int PACKET_ID_length = 2;

            public enum BGID_patterns
            {
                ALTERAC = 0x1E,
                ISLE = 0x74,
                WARSONG = 0xE9,
                EYE = 0x36,
                STRAND = 0x5F,
                ARATHI = 0x11
            }
            public const int DATA_BGID_position = 25;   //Big-Endian 0..n
            public const int DATA_BGID_length = 1;

            public static readonly ReadOnlyDictionary<BGID_patterns, (string BG_Name, bool dummy)> BG =
                new ReadOnlyDictionary<BGID_patterns, (string, bool)>(
                    new Dictionary<BGID_patterns, (string, bool)>
                {
                        { BGID_patterns.ALTERAC, ( "Alterac Valley",false) },
                        { BGID_patterns.ISLE, ( "Isle of Conquest",false) },
                        { BGID_patterns.WARSONG, ( "Warsong Gulch",false) },
                        { BGID_patterns.EYE, ( "Eye Of The Storm",false) },
                        { BGID_patterns.STRAND, ( "Strand of the Ancients",false) },
                        { BGID_patterns.ARATHI, ( "Arathi",false) },
                });


            /// <summary>
            ///  state of the games world
            /// </summary>
            /// <value>1-loaded; 0-not loaded</value>
            public const uint IsInGame = 0x00BD0792;
            /// <summary>
            /// bg\arena status. 
            /// </summary>
            ///<value>
            ///0-none;1-registered;2- invitation;3-in battleground
            ///</value>
            public const uint BGStatus = 0x00BEA4D0;
        }
        public class DATA_Unit
        {
            public int position; //Big-Endian 0..n
            public int length;
            public byte[] data;

            public DATA_Unit(ref byte[] data, int position, int length)
            {
                this.position = position;
                this.length = length;
                this.data = new byte[length];
                Array.Copy(data, this.position, this.data, 0, length);
            }
        }

        private readonly Data.PacketID_patterns PACKET_ID;

        protected Packet(Data.PacketID_patterns t)
        {
            PACKET_ID = t;
        }
        public Data.PacketID_patterns GetPacketID
        {
            get
            {
                return PACKET_ID;
            }
        }
        public bool IsID_Equals(Data.PacketID_patterns ID)
        {
            if (PACKET_ID == ID)
                return true;
            return false;
        }
        public bool IsID_Defined()
        {
            return Enum.IsDefined(typeof(Data.PacketID_patterns), PACKET_ID);
        }
    }
    public class Packet_BG_invite : Packet
    {
        private readonly Data.BGID_patterns BG_ID;

        public Packet_BG_invite(ref byte[] data, Data.PacketID_patterns t) : base(t)
        {
            DATA_Unit du = new DATA_Unit(ref data, Data.DATA_BGID_position, Data.DATA_BGID_length);
            this.BG_ID = (Data.BGID_patterns)du.data[0];
        }
        public Data.BGID_patterns GetBGID
        {
            get
            {
                return BG_ID;
            }
        }
        public string GetBGName
        {
            get
            {
                return Data.BG[BG_ID].BG_Name;
            }
        }
    }

    private static readonly BlackMagic wow = new BlackMagic();
    private const string pname = "Wow";
    public static int PID { get; private set; } = -1;
    public static bool Injected { get; private set; } = false;

    private static void Log(string s)
    {
        Glue.Log(s);
    }
    public static bool Inject()
    {
        int r = GetWowPID();
        if (r == 0 || r == -1)
        {
            Log("ERROR failed to get PID");
            goto FAIL;
        }
        PID = r;
        if (!PacketEditor.Attach())
        {
            Log("ERROR failed to PacketEditor.Attach");
            goto FAIL;
        }
        if (!wow.OpenProcessAndThread(PID))
        {
            Log("ERROR failed to init BlackMagic");
            goto FAIL;
        }

        Injected = true;
        Log("Injected");
        return true;

    FAIL:
        Injected = false;
        return false;
    }
    public static void Unload()
    {
        Injected = false;
        PID = -1;
        wow.Close();

        PacketEditor.Detach();
        Log("Unloaded");
    }
    //Reads Big-Endian value from array of bytes
    public static bool Array_To_ULONG_BE(ref byte[] data, int startIndex, int length, ref PacketIDSize buffer)
    {
        int MAX_buffer_Size = sizeof(ulong);
        if (length > MAX_buffer_Size || length < 1)
            return false;

        int l = data.Length;
        if (startIndex < 0 || startIndex >= l)
            return false;
        if ((l - startIndex) < length)
            return false;

        byte[] dest = new byte[MAX_buffer_Size];

        Array.Copy(data, startIndex, dest, 0, length);
        Array.Reverse(dest);
        buffer = BitConverter.ToUInt64(dest, 0);
        buffer = (buffer >> 64 - (length * 8));

        return true;
    }

    private static int GetWowPID()
    {
        var process = System.Diagnostics.Process.GetProcessesByName(pname).FirstOrDefault();
        if (process != null)
        {
            return process.Id;
        }
        return -1;
    }
    public static bool IsInGame()
    {
        byte i = wow.ReadByte(Wow.Packet.Data.IsInGame);
        if (i == 1) return true;
        else
            return false;
    }
    public static bool IsBGQueued()
    {
        byte i = wow.ReadByte(Wow.Packet.Data.IsInGame);
        uint f = wow.ReadUInt(Wow.Packet.Data.BGStatus);
        if (i == 1)
            if (f == 1) return true;
        return false;
    }
    public static bool IsBGInvitePending()
    {
        byte i = wow.ReadByte(Wow.Packet.Data.IsInGame);
        uint f = wow.ReadUInt(Wow.Packet.Data.BGStatus);
        if (i == 1)
            if (f == 2) return true;
        return false;
    }
    public static bool IsReadyToQueue()
    {
        byte i = wow.ReadByte(Wow.Packet.Data.IsInGame);
        uint f = wow.ReadUInt(Wow.Packet.Data.BGStatus);
        if (i == 1 & f == 0) return true; else return false;
    }
    public static bool IsInBG()
    {
        byte i = wow.ReadByte(Wow.Packet.Data.IsInGame);
        uint f = wow.ReadUInt(Wow.Packet.Data.BGStatus);
        if (i == 1 & f == 3) return true; else return false;
    }
    public static void LeaveBGQueue()
    {
        PostMessage(wow.WindowHandle, WM_KEYDOWN, (IntPtr)VK_F9, (IntPtr)0);
        PostMessage(wow.WindowHandle, WM_KEYUP, (IntPtr)VK_F9, (IntPtr)0);
    }

    public static void BGAcceptInvite()
    {
        PostMessage(wow.WindowHandle, WM_KEYDOWN, (IntPtr)VK_F10, (IntPtr)0);
        PostMessage(wow.WindowHandle, WM_KEYUP, (IntPtr)VK_F10, (IntPtr)0);
    }
    public static void JoinBGQueue()
    {
        PostMessage(wow.WindowHandle, WM_KEYDOWN, (IntPtr)VK_F11, (IntPtr)0);
        PostMessage(wow.WindowHandle, WM_KEYUP, (IntPtr)VK_F11, (IntPtr)0);
    }
}
