using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace EncryptFile.Models
{
    internal class IconHelper
    {
        private const uint SHGFI_ICON = 0x100;
        private const uint SHGFI_LARGEICON = 0x0; // 'Large icon 32x32
        private const uint SHGFI_SMALLICON = 0x1; // 'Small icon 16x16
        private const uint SHIL_EXTRALARGE = 0x2; // 'Extralarge icon 48x48
        private const uint SHIL_JUMBO = 0x4; // 'Jumbo icon: 256x256

        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public IntPtr iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            int left, top, right, bottom;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            int x, y;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGELISTDRAWPARAMS
        {
            int cbSize;
            IntPtr himl;
            int i;
            IntPtr hdcDst;
            int x, y, xc, xy;
            int xBitmap, yBitmap;
            int rgbBk, rgbFg;
            int fStyle;
            int dwRop;
            int fState;
            int Frame;
            int crEFFect;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGEINFO
        {
            IntPtr hbmImage;
            IntPtr hbmMask;
            int Unused1, Unused2;
            RECT rcImage;
        }
        #region Private ImageList COM Interop (XP)
        [ComImportAttribute()]
        [GuidAttribute("46EB5926-582E-4017-9FDF-E8998DAA0950")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        //helpstring("Image List"),
        interface IImageList
        {
            [PreserveSig]
            int Add(
                IntPtr hbmImage,
                IntPtr hbmMask,
                ref int pi);

            [PreserveSig]
            int ReplaceIcon(
                int i,
                IntPtr hicon,
                ref int pi);

            [PreserveSig]
            int SetOverlayImage(
                int iImage,
                int iOverlay);

            [PreserveSig]
            int Replace(
                int i,
                IntPtr hbmImage,
                IntPtr hbmMask);

            [PreserveSig]
            int AddMasked(
                IntPtr hbmImage,
                int crMask,
                ref int pi);

            [PreserveSig]
            int Draw(
                ref IMAGELISTDRAWPARAMS pimldp);

            [PreserveSig]
            int Remove(
            int i);

            [PreserveSig]
            int GetIcon(
                int i,
                int flags,
                ref IntPtr picon);

            [PreserveSig]
            int GetImageInfo(
                int i,
                ref IMAGEINFO pImageInfo);

            [PreserveSig]
            int Copy(
                int iDst,
                IImageList punkSrc,
                int iSrc,
                int uFlags);

            [PreserveSig]
            int Merge(
                int i1,
                IImageList punk2,
                int i2,
                int dx,
                int dy,
                ref Guid riid,
                ref IntPtr ppv);

            [PreserveSig]
            int Clone(
                ref Guid riid,
                ref IntPtr ppv);

            [PreserveSig]
            int GetImageRect(
                int i,
                ref RECT prc);

            [PreserveSig]
            int GetIconSize(
                ref int cx,
                ref int cy);

            [PreserveSig]
            int SetIconSize(
                int cx,
                int cy);

            [PreserveSig]
            int GetImageCount(
            ref int pi);

            [PreserveSig]
            int SetImageCount(
                int uNewCount);

            [PreserveSig]
            int SetBkColor(
                int clrBk,
                ref int pclr);

            [PreserveSig]
            int GetBkColor(
                ref int pclr);

            [PreserveSig]
            int BeginDrag(
                int iTrack,
                int dxHotspot,
                int dyHotspot);

            [PreserveSig]
            int EndDrag();

            [PreserveSig]
            int DragEnter(
                IntPtr hwndLock,
                int x,
                int y);

            [PreserveSig]
            int DragLeave(
                IntPtr hwndLock);

            [PreserveSig]
            int DragMove(
                int x,
                int y);

            [PreserveSig]
            int SetDragCursorImage(
                ref IImageList punk,
                int iDrag,
                int dxHotspot,
                int dyHotspot);

            [PreserveSig]
            int DragShowNolock(
                int fShow);

            [PreserveSig]
            int GetDragImage(
                ref POINT ppt,
                ref POINT pptHotspot,
                ref Guid riid,
                ref IntPtr ppv);

            [PreserveSig]
            int GetItemFlags(
                int i,
                ref int dwFlags);

            [PreserveSig]
            int GetOverlayImage(
                int iOverlay,
                ref int piIndex);
        }
        #endregion

        [DllImport("shell32.dll", EntryPoint = "#727")]
        private static extern int SHGetImageList(int iImageList, ref Guid riid, out IImageList ppv);

        [DllImport("shell32.dll")]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        [DllImport("user32.dll")]
        private static extern int DestroyIcon(IntPtr o);

        private static BitmapSource bitmap_source_of_icon(Icon icon)
        {
            var source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(icon.Handle,
                    System.Windows.Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        private static BitmapSource icon_of_small_large(string fileName, bool small = true)
        {
            uint SHGFI_USEFILEATTRIBUTES = 0x10;
            var shinfo = new SHFILEINFO();

            uint flags;
            if (small)
                flags = SHGFI_ICON | SHGFI_SMALLICON;
            else
                flags = SHGFI_ICON | SHGFI_LARGEICON;
            flags |= SHGFI_USEFILEATTRIBUTES;

            var res = SHGetFileInfo(fileName, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);

            Icon myIcon = System.Drawing.Icon.FromHandle(shinfo.hIcon);
            BitmapSource source = bitmap_source_of_icon(myIcon);
            myIcon.Dispose();
            source.Freeze();
            DestroyIcon(shinfo.hIcon);

            return source;
        }

        public static BitmapSource icon_of_extralarge_jumbo(string fileName, bool small = true)
        {
            var shinfo = new SHFILEINFO();

            uint SHGFI_SYSICONINDEX = 0x4000;
            uint SHGFI_USEFILEATTRIBUTES = 0x10;
            uint FILE_ATTRIBUTE_NORMAL = 0x80;

            uint flags = SHGFI_SYSICONINDEX | SHGFI_USEFILEATTRIBUTES;

            var res = SHGetFileInfo(fileName, FILE_ATTRIBUTE_NORMAL, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);

            var iconIndex = shinfo.iIcon;

            // Get the System IImageList object from the Shell;
            Guid iidImageList = new Guid("46EB5926-582E-4017-9FDF-E8998DAA0950");

            IImageList iml;
            int size = (int)(small ? SHIL_EXTRALARGE : SHIL_JUMBO);
            var hres = SHGetImageList(size, ref iidImageList, out iml);

            IntPtr hIcon = IntPtr.Zero;
            int ILD_TRANSPARENT = 1;
            hres = iml.GetIcon((int)iconIndex, ILD_TRANSPARENT, ref hIcon);
            Marshal.FinalReleaseComObject(iml);

            Icon myIcon = System.Drawing.Icon.FromHandle(hIcon);
            BitmapSource source = bitmap_source_of_icon(myIcon);
            myIcon.Dispose();
            source.Freeze();
            DestroyIcon(hIcon);

            return source;
        }
    }
}
