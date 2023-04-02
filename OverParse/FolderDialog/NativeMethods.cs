using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace FolderDialog
{
    static class NativeMethods
    {
        [DllImport("kernel32", SetLastError = true),
        ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FreeLibrary(IntPtr hModule);


        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        internal static extern IntPtr GetActiveWindow();


        [DllImport("kernel32.dll"), ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        internal extern static void ReleaseActCtx(IntPtr hActCtx);


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
        internal struct COMDLG_FILTERSPEC
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string pszName;
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string pszSpec;
        }


        internal enum SIGDN : uint
        {
            NORMALDISPLAY = 0x00000000,           // SHGDN_NORMAL
            PARENTRELATIVEPARSING = 0x80018001,   // SHGDN_INFOLDER | SHGDN_FORPARSING
            DESKTOPABSOLUTEPARSING = 0x80028000,  // SHGDN_FORPARSING
            PARENTRELATIVEEDITING = 0x80031001,   // SHGDN_INFOLDER | SHGDN_FOREDITING
            DESKTOPABSOLUTEEDITING = 0x8004c000,  // SHGDN_FORPARSING | SHGDN_FORADDRESSBAR
            FILESYSPATH = 0x80058000,             // SHGDN_FORPARSING
            URL = 0x80068000,                     // SHGDN_FORPARSING
            PARENTRELATIVEFORADDRESSBAR = 0x8007c001,     // SHGDN_INFOLDER | SHGDN_FORPARSING | SHGDN_FORADDRESSBAR
            PARENTRELATIVE = 0x80080001           // SHGDN_INFOLDER
        }

        [Flags]
        internal enum FOS : uint
        {
            OVERWRITEPROMPT = 0x00000002,
            STRICTFILETYPES = 0x00000004,
            NOCHANGEDIR = 0x00000008,
            PICKFOLDERS = 0x00000020,
            FORCEFILESYSTEM = 0x00000040, // Ensure that items returned are filesystem items.
            ALLNONSTORAGEITEMS = 0x00000080, // Allow choosing items that have no storage.
            NOVALIDATE = 0x00000100,
            ALLOWMULTISELECT = 0x00000200,
            PATHMUSTEXIST = 0x00000800,
            FILEMUSTEXIST = 0x00001000,
            CREATEPROMPT = 0x00002000,
            SHAREAWARE = 0x00004000,
            NOREADONLYRETURN = 0x00008000,
            NOTESTFILECREATE = 0x00010000,
            HIDEMRUPLACES = 0x00020000,
            HIDEPINNEDPLACES = 0x00040000,
            NODEREFERENCELINKS = 0x00100000,
            DONTADDTORECENT = 0x02000000,
            FORCESHOWHIDDEN = 0x10000000,
            DEFAULTNOMINIMODE = 0x20000000
        }


        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern int SHCreateItemFromParsingName([MarshalAs(UnmanagedType.LPWStr)] string pszPath, IntPtr pbc, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppv);

        public static IShellItem CreateItemFromParsingName(string path)
        {
            Guid guid = new Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe"); // IID_IShellItem
            int hr = SHCreateItemFromParsingName(path, IntPtr.Zero, ref guid, out object item);
            if (hr != 0)
            {
                throw new System.ComponentModel.Win32Exception(hr);
            }

            return (IShellItem)item;
        }


    }
}
