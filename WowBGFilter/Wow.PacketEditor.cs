using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

public static partial class Wow
{
    public static class PacketEditor
    {
        private class Glob
        {
            [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
            public struct PipeHeader
            {
                [MarshalAs(UnmanagedType.I1)]
                public byte command;
                public byte function;
                [MarshalAs(UnmanagedType.I4)]
                public int sockid;
                public int datasize;
                public int extra;
            }
            public static byte[] RawSerializeEx(object anything)
            {
                int rawsize = Marshal.SizeOf(anything);
                byte[] rawdatas = new byte[rawsize];
                GCHandle handle = GCHandle.Alloc(rawdatas, GCHandleType.Pinned);
                IntPtr buffer = handle.AddrOfPinnedObject();
                Marshal.StructureToPtr(anything, buffer, false);
                handle.Free();
                return rawdatas;
            }
            public static object RawDeserializeEx(byte[] rawdatas, Type anytype)
            {
                int rawsize = Marshal.SizeOf(anytype);
                if (rawsize > rawdatas.Length)
                    return null;
                GCHandle handle = GCHandle.Alloc(rawdatas, GCHandleType.Pinned);
                IntPtr buffer = handle.AddrOfPinnedObject();
                object retobj = Marshal.PtrToStructure(buffer, anytype);
                handle.Free();
                return retobj;
            }

            public const byte CMD_DEINIT = 9;
            public const byte CMD_INIT = 8;
            public const byte CMD_DNS_STRUCTDATA = 7;
            public const byte CMD_DNS_DATA = 6;
            public const byte CMD_NODATA = 5;
            public const byte CMD_NOFILTERSTRUCTDATA = 4;
            public const byte CMD_NOFILTERDATA = 3;
            public const byte CMD_STRUCTDATA = 2;
            public const byte CMD_DATA = 1;
            public const byte CMD_UNLOAD_DLL = 255;
            public const byte CMD_ENABLE_MONITOR = 254;
            public const byte CMD_DISABLE_MONITOR = 253;
            public const byte CMD_ENABLE_FILTER = 252;
            public const byte CMD_DISABLE_FILTER = 251;
            public const byte CMD_INJECT = 250;
            public const byte CMD_RECV = 249;
            public const byte CMD_FILTER = 248;
            public const byte CMD_FREEZE = 247;
            public const byte CMD_UNFREEZE = 246;
            public const byte CMD_QUERY = 245;

            public const byte INIT_DECRYPT = 1;

            public const byte FUNC_NULL = 0;
            public const byte FUNC_WSASEND = 1;
            public const byte FUNC_WSARECV = 2;
            public const byte FUNC_SEND = 3;
            public const byte FUNC_RECV = 4;
            public const byte FUNC_WSASENDTO = 5;
            public const byte FUNC_WSARECVFROM = 6;
            public const byte FUNC_SENDTO = 7;
            public const byte FUNC_RECVFROM = 8;
            public const byte FUNC_WSASENDDISCONNECT = 9;
            public const byte FUNC_WSARECVDISCONNECT = 10;
            public const byte FUNC_WSAACCEPT = 11;
            public const byte FUNC_ACCEPT = 12;
            public const byte FUNC_WSACONNECT = 13;
            public const byte FUNC_CONNECT = 14;
            public const byte FUNC_WSASOCKETW_IN = 15;
            public const byte FUNC_WSASOCKETW_OUT = 16;
            public const byte FUNC_BIND = 17;
            public const byte FUNC_CLOSESOCKET = 18;
            public const byte FUNC_LISTEN = 19;
            public const byte FUNC_SHUTDOWN = 20;
            public const byte CONN_WSASENDTO = 21;
            public const byte CONN_WSARECVFROM = 22;
            public const byte CONN_SENDTO = 23;
            public const byte CONN_RECVFROM = 24;
            public const byte DNS_GETHOSTBYNAME_OUT = 25;
            public const byte DNS_GETHOSTBYNAME_IN = 26;
            public const byte DNS_GETHOSTBYADDR_OUT = 27;
            public const byte DNS_GETHOSTBYADDR_IN = 28;
            public const byte DNS_WSAASYNCGETHOSTBYNAME_OUT = 29;
            public const byte DNS_WSAASYNCGETHOSTBYNAME_IN = 30;
            public const byte DNS_WSAASYNCGETHOSTBYADDR_OUT = 31;
            public const byte DNS_WSAASYNCGETHOSTBYADDR_IN = 32;
            public const byte DNS_GETHOSTNAME = 33;
            public const byte FUNC_WSACLEANUP = 34;
            public const byte FUNC_SOCKET_IN = 35;
            public const byte FUNC_SOCKET_OUT = 36;
            public const byte FUNC_GETSOCKNAME = 37;
            public const byte FUNC_GETPEERNAME = 38;

            public const byte ActionReplaceString = 0;
            public const byte ActionReplaceStringH = 1;
            public const byte ActionError = 2;
            public const byte ActionErrorH = 3;

        }
        private static NamedPipeServerStream pipeServer;
        private static NamedPipeClientStream pipeClient;
        private static readonly string strDLL = Directory.GetCurrentDirectory() + "\\Resources\\WSPE.dat";
        private static readonly Encoding ae = Encoding.GetEncoding(28591);
        private static Thread trdPipeRead;
        private static Glob.PipeHeader strPipeMsgOut;
        private static Glob.PipeHeader strPipeMsgIn;

        private static readonly bool monitor = true;
        public static bool Attached { get; private set; } = false;

        [Flags]
        private enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VMOperation = 0x00000008,
            VMRead = 0x00000010,
            VMWrite = 0x00000020,
            DupHandle = 0x00000040,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            Synchronize = 0x00100000
        }
        [Flags()]
        private enum AllocationType : uint
        {
            COMMIT = 0x1000,
            RESERVE = 0x2000,
            RESET = 0x80000,
            LARGE_PAGES = 0x20000000,
            PHYSICAL = 0x400000,
            TOP_DOWN = 0x100000,
            WRITE_WATCH = 0x200000
        }
        [Flags()]
        private enum MemoryProtection : uint
        {
            EXECUTE = 0x10,
            EXECUTE_READ = 0x20,
            EXECUTE_READWRITE = 0x40,
            EXECUTE_WRITECOPY = 0x80,
            NOACCESS = 0x01,
            READONLY = 0x02,
            READWRITE = 0x04,
            WRITECOPY = 0x08,
            GUARD_Modifierflag = 0x100,
            NOCACHE_Modifierflag = 0x200,
            WRITECOMBINE_Modifierflag = 0x400
        }
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);
        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out int lpNumberOfBytesWritten);
        [DllImport("kernel32.dll")]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        public static class DataQueueService
        {
            public static ConcurrentQueue<byte[]> _dataQueue = new ConcurrentQueue<byte[]>();

            public static void EnqueueData(byte[] dataBuffer)
            {
                if (_dataQueue.Count > 10)
                {
                    Clear();
                }
                _dataQueue.Enqueue(dataBuffer);
            }


            public static bool TryDequeueData(out byte[] dataBuffer)
            {
                return _dataQueue.TryDequeue(out dataBuffer);
            }

            public static int Count => _dataQueue.Count;


            public static void Clear()
            {
                Interlocked.Exchange(ref _dataQueue, new ConcurrentQueue<byte[]>());
            }
        }

        private static void ProcessExited()
        {
            pipeServer.WaitForPipeDrain();
            pipeServer.Close();
            pipeClient.Close();
        }
        private static void DataReceived(byte[] data)
        {
            if (strPipeMsgIn.function == Glob.FUNC_RECV & data.Length == 41)
            {
                DataQueueService.EnqueueData(data);
                //System.Threading.Interlocked.Exchange(ref Form1.BGID, data[25]);
            }
        }
        public static void WritePipe()
        {
            pipeClient.Write(Glob.RawSerializeEx(strPipeMsgOut), 0, Marshal.SizeOf(strPipeMsgOut));
        }
        private static void PipeRead()
        {
            byte[] dbPipeMsgIn = new byte[14];
            byte[] dbPipeMsgInData;
        PipeLoop:
            while (pipeServer.Read(dbPipeMsgIn, 0, 14) != 0)
            {
                strPipeMsgIn = (Glob.PipeHeader)Glob.RawDeserializeEx(dbPipeMsgIn, typeof(Glob.PipeHeader));
                if (strPipeMsgIn.datasize != 0)
                {
                    dbPipeMsgInData = new byte[strPipeMsgIn.datasize];
                    pipeServer.Read(dbPipeMsgInData, 0, dbPipeMsgInData.Length);

                    switch (strPipeMsgIn.function)
                    {
                        case Glob.FUNC_SEND:
                        case Glob.FUNC_SENDTO:
                        case Glob.FUNC_WSASEND:
                        case Glob.FUNC_WSASENDTO:
                        case Glob.FUNC_WSASENDDISCONNECT:
                        case Glob.FUNC_RECV:
                        case Glob.FUNC_RECVFROM:
                        case Glob.FUNC_WSARECV:
                        case Glob.FUNC_WSARECVFROM:
                        case Glob.FUNC_WSARECVDISCONNECT:

                            DataReceived(dbPipeMsgInData);
                            break;
                        case Glob.CONN_RECVFROM:
                        case Glob.CONN_SENDTO:
                        case Glob.CONN_WSARECVFROM:
                        case Glob.CONN_WSASENDTO:
                        case Glob.DNS_GETHOSTBYADDR_IN:
                        case Glob.DNS_GETHOSTBYADDR_OUT:
                        case Glob.DNS_GETHOSTBYNAME_IN:
                        case Glob.DNS_GETHOSTBYNAME_OUT:
                        case Glob.DNS_GETHOSTNAME:
                        case Glob.DNS_WSAASYNCGETHOSTBYADDR_IN:
                        case Glob.DNS_WSAASYNCGETHOSTBYADDR_OUT:
                        case Glob.DNS_WSAASYNCGETHOSTBYNAME_IN:
                        case Glob.DNS_WSAASYNCGETHOSTBYNAME_OUT:
                        case Glob.FUNC_ACCEPT:
                        case Glob.FUNC_BIND:
                        case Glob.FUNC_CLOSESOCKET:
                        case Glob.FUNC_CONNECT:
                        case Glob.FUNC_GETPEERNAME:
                        case Glob.FUNC_GETSOCKNAME:
                        case Glob.FUNC_LISTEN:
                        case Glob.FUNC_SHUTDOWN:
                        case Glob.FUNC_SOCKET_IN:
                        case Glob.FUNC_SOCKET_OUT:
                        case Glob.FUNC_WSAACCEPT:
                        case Glob.FUNC_WSACLEANUP:
                        case Glob.FUNC_WSACONNECT:
                        case Glob.FUNC_WSASOCKETW_IN:
                        case Glob.FUNC_WSASOCKETW_OUT:
                            break;
                    }
                }
                else
                {
                    if (strPipeMsgIn.command == Glob.CMD_INIT)
                    {
                        if (strPipeMsgIn.function == Glob.INIT_DECRYPT)
                            if (strPipeMsgIn.extra == 0)
                            {
                                ProcessExited();
                                return;
                            }
                            else
                            {
                                strPipeMsgOut.datasize = 0;
                                if (monitor == true)
                                {
                                    strPipeMsgOut.command = Glob.CMD_ENABLE_MONITOR;
                                    strPipeMsgOut.datasize = 0;
                                    WritePipe();
                                }

                            }
                    }
                    else
                    {
                        switch (strPipeMsgIn.function)
                        {
                            case Glob.CONN_RECVFROM:
                            case Glob.CONN_SENDTO:
                            case Glob.CONN_WSARECVFROM:
                            case Glob.CONN_WSASENDTO:
                            case Glob.DNS_GETHOSTBYADDR_IN:
                            case Glob.DNS_GETHOSTBYADDR_OUT:
                            case Glob.DNS_GETHOSTBYNAME_IN:
                            case Glob.DNS_GETHOSTBYNAME_OUT:
                            case Glob.DNS_GETHOSTNAME:
                            case Glob.DNS_WSAASYNCGETHOSTBYADDR_IN:
                            case Glob.DNS_WSAASYNCGETHOSTBYADDR_OUT:
                            case Glob.DNS_WSAASYNCGETHOSTBYNAME_IN:
                            case Glob.DNS_WSAASYNCGETHOSTBYNAME_OUT:
                            case Glob.FUNC_ACCEPT:
                            case Glob.FUNC_BIND:
                            case Glob.FUNC_CLOSESOCKET:
                            case Glob.FUNC_CONNECT:
                            case Glob.FUNC_GETPEERNAME:
                            case Glob.FUNC_GETSOCKNAME:
                            case Glob.FUNC_LISTEN:
                            case Glob.FUNC_SHUTDOWN:
                            case Glob.FUNC_SOCKET_IN:
                            case Glob.FUNC_SOCKET_OUT:
                            case Glob.FUNC_WSAACCEPT:
                            case Glob.FUNC_WSACLEANUP:
                            case Glob.FUNC_WSACONNECT:
                            case Glob.FUNC_WSASOCKETW_IN:
                            case Glob.FUNC_WSASOCKETW_OUT:
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            if (pipeServer.IsConnected) goto PipeLoop;
            ProcessExited();
        }
        private static bool InvokeDLL()
        {
            pipeClient = new NamedPipeClientStream(".", "wspe.send." + Wow.PID.ToString("X8"), PipeDirection.Out, PipeOptions.Asynchronous);
            try
            {
                pipeServer = new NamedPipeServerStream("wspe.recv." + Wow.PID.ToString("X8"), PipeDirection.In, 1, PipeTransmissionMode.Message);
            }
            catch
            {
                MessageBox.Show("Cannot attach to process!\n\nA previous instance could still be loaded in the targets memory waiting to unload.\nTry flushing sockets by sending/receiving data to clear blocking sockets.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ProcessExited();
                return false;
            }
            // Inject WSPE.dat from current directory
            IntPtr hProc = OpenProcess(ProcessAccessFlags.All, false, Wow.PID);
            IntPtr ptrLoadLib = GetProcAddress(GetModuleHandle("KERNEL32.DLL"), "LoadLibraryA");

            if (hProc == IntPtr.Zero)
            {
                MessageBox.Show("Cannot open process.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            IntPtr ptrMem = VirtualAllocEx(hProc, (IntPtr)0, (uint)strDLL.Length, AllocationType.COMMIT, MemoryProtection.EXECUTE_READ);
            if (ptrMem == IntPtr.Zero)
            {
                MessageBox.Show("Cannot allocate process memory.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            byte[] dbDLL = ae.GetBytes(strDLL);
            int ipTmp = 0;

            if (!WriteProcessMemory(hProc, ptrMem, dbDLL, (uint)dbDLL.Length, out ipTmp))
            {
                MessageBox.Show("Cannot write to process memory.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            CreateRemoteThread(hProc, IntPtr.Zero, 0, ptrLoadLib, ptrMem, 0, IntPtr.Zero);

            pipeServer.WaitForConnection();
            pipeClient.Connect();

            string RegName = "PacketEditor.com";
            string RegKey = "7007C8466C99901EF555008BF90D0C0F11C2005CE042C84B7C1E2C0050DF305647026513";

            pipeClient.Write(BitConverter.GetBytes(RegName.Length), 0, 1);
            pipeClient.Write(ae.GetBytes(RegName), 0, RegName.Length);
            pipeClient.Write(ae.GetBytes(RegKey), 0, RegKey.Length);

            trdPipeRead = new Thread(new ThreadStart(PipeRead));
            trdPipeRead.IsBackground = true;
            trdPipeRead.Start();
            return true;
        }
        public static void Detach()
        {
            if (pipeClient != null)
            {
                if (pipeClient.IsConnected)
                {
                    strPipeMsgOut.command = Glob.CMD_UNLOAD_DLL;
                    try
                    {
                        WritePipe();
                    }
                    catch { }
                }
                pipeClient.Close();
            }
            if (pipeServer != null)
            {
                pipeServer.Close();
            }
            if (trdPipeRead != null)
            {
                if (trdPipeRead.IsAlive)
                {
                    trdPipeRead.Abort();
                }
            }

            Attached = false;
        }
        public static bool Attach()
        {
            if (Attached)
                return false;

            if (InvokeDLL())
            {
                Attached = true;
                return true;
            }
            return false;
        }
    }
}
