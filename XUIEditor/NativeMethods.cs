using System;
using System.Drawing;
using System.Runtime.InteropServices;
using static XUIEditor.NativeMethods;

namespace XUIEditor
{
    internal static class NativeMethods
    {
        // === SHGetFileInfo-based file/folder icons ===

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            ref SHFILEINFO psfi,
            uint cbFileInfo,
            uint uFlags
        );

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        // File-info flags
        public const uint SHGFI_ICON = 0x00000100;
        public const uint SHGFI_USEFILEATTRIBUTES = 0x00000010;
        public const uint SHGFI_SMALLICON = 0x00000001;
        public const uint SHGFI_LARGEICON = 0x00000000;
        public const uint SHGFI_OPENICON = 0x00000002;

        // File-attributes
        public const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
        public const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
        public const int MAX_PATH = 260;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        /// <summary>
        /// Retrieves the standard folder icon (closed or open) from the shell.
        /// </summary>
        public static Icon GetFolderIcon(bool smallSize = true)
        {
            uint flags = SHGFI_ICON | SHGFI_USEFILEATTRIBUTES | SHGFI_OPENICON |
                         (smallSize ? SHGFI_SMALLICON : SHGFI_LARGEICON);

            SHFILEINFO shfi = default;
            SHGetFileInfo(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                FILE_ATTRIBUTE_DIRECTORY,
                ref shfi,
                (uint)Marshal.SizeOf<SHFILEINFO>(),
                flags
            );

            var icon = (Icon)Icon.FromHandle(shfi.hIcon).Clone();
            DestroyIcon(shfi.hIcon);
            return icon;
        }

        /// <summary>
        /// Retrieves the icon for a given file or extension.
        /// </summary>
        public static Icon GetFileIcon(string pathOrExtension, bool smallSize = true)
        {
            uint flags = SHGFI_ICON | SHGFI_USEFILEATTRIBUTES |
                         (smallSize ? SHGFI_SMALLICON : SHGFI_LARGEICON);

            SHFILEINFO shfi = default;
            SHGetFileInfo(
                pathOrExtension,
                FILE_ATTRIBUTE_NORMAL,
                ref shfi,
                (uint)Marshal.SizeOf<SHFILEINFO>(),
                flags
            );

            var icon = (Icon)Icon.FromHandle(shfi.hIcon).Clone();
            DestroyIcon(shfi.hIcon);
            return icon;
        }

        // === New: SHGetStockIconInfo-based "stock" icons ===

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHGetStockIconInfo(
            SHSTOCKICONID siid,
            SHGSI flags,
            ref SHSTOCKICONINFO psii
        );

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHSTOCKICONINFO
        {
            public uint cbSize;
            public IntPtr hIcon;
            public int iSysImageIndex;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            public string szPath;
        }

        [Flags]
        public enum SHGSI : uint
        {
            ICON = 0x000000100,
            SMALLICON = 0x000000001,
            LARGEICON = 0x000000000,
            SYSICONINDEX = 0x000004000,
            LINKOVERLAY = 0x000008000,
        }

        public enum SHSTOCKICONID : uint
        {
            SIID_DOCNOASSOC = 0,
            SIID_DOCASSOC = 1,
            SIID_APPLICATION = 2,
            SIID_FOLDER = 3,
            SIID_FOLDEROPEN = 4,
            SIID_DRIVE525 = 5,
            SIID_DRIVE35 = 6,
            SIID_DRIVEREMOVE = 7,
            SIID_DRIVEFIXED = 8,
            SIID_DRIVENET = 9,
            SIID_DRIVENETDISABLED = 10,
            SIID_DRIVECD = 11,
            SIID_DRIVEDVD = 12,
            SIID_DRIVEUNKNOWN = 13,
            SIID_DRIVERAM = 14,
            SIID_AUTOLIST = 49,
            SIID_STACK = 55
        }

        /// <summary>
        /// Retrieves any of the shell's "stock" icons by ID.
        /// </summary>
        public static Icon GetStockIcon(SHSTOCKICONID id, bool smallSize = true)
        {
            var info = new SHSTOCKICONINFO { cbSize = (uint)Marshal.SizeOf<SHSTOCKICONINFO>() };
            SHGetStockIconInfo(
                id,
                SHGSI.ICON | (smallSize ? SHGSI.SMALLICON : SHGSI.LARGEICON),
                ref info
            );
            var icon = Icon.FromHandle(info.hIcon);
            var clone = (Icon)icon.Clone();
            DestroyIcon(info.hIcon);
            return clone;
        }
    }
}