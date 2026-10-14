using System.Runtime.InteropServices;

namespace VoxTail.TypeB.Ui;

internal static class NativeMethods
{
    public const string WindowClassName = "VoxTailTypeBEditorWindow";
    public const uint CsHRedraw = 0x0002;
    public const uint CsVRedraw = 0x0001;
    public const uint WsChild = 0x40000000;
    public const uint WsVisible = 0x10000000;
    public const uint WsClipChildren = 0x02000000;
    public const int GwlpUserData = -21;
    public const uint WmDestroy = 0x0002;
    public const uint WmPaint = 0x000F;
    public const uint WmEraseBackground = 0x0014;
    public const uint WmCancelMode = 0x001F;
    public const uint WmLeftButtonDown = 0x0201;
    public const uint WmLeftButtonUp = 0x0202;
    public const uint WmMouseMove = 0x0200;
    public const uint WmCaptureChanged = 0x0215;
    public const uint WmTimer = 0x0113;
    public const uint SwpNoSize = 0x0001;
    public const uint SwpNoZOrder = 0x0004;
    public const uint SwpNoActivate = 0x0010;
    public const int ErrorClassAlreadyExists = 1410;
    public const int IdcArrow = 32512;
    public const int PsSolid = 0;
    public const int PsDot = 2;
    public const int FwNormal = 400;
    public const int FwBold = 700;
    public const uint DefaultCharset = 1;
    public const uint OutDefaultPrecision = 0;
    public const uint ClipDefaultPrecision = 0;
    public const uint ClearTypeQuality = 5;
    public const uint DefaultPitch = 0;
    public const uint FfDontCare = 0;
    public const uint Transparent = 1;
    public const uint DtLeft = 0;
    public const uint DtRight = 2;
    public const uint DtVCenter = 4;
    public const uint DtSingleLine = 0x20;
    public const uint Srccopy = 0x00CC0020;

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate nint WindowProcedure(nint window, uint message, nuint wParam, nint lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WindowClassEx
    {
        public uint Size;
        public uint Style;
        public nint WindowProcedure;
        public int ClassExtra;
        public int WindowExtra;
        public nint Instance;
        public nint Icon;
        public nint Cursor;
        public nint Background;
        public nint MenuName;
        [MarshalAs(UnmanagedType.LPWStr)] public string? ClassName;
        public nint SmallIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    public struct PaintReservedBytes
    {
        private byte _firstByte;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PaintStruct
    {
        public nint DeviceContext;
        public int Erase;
        public Rect PaintRectangle;
        public int Restore;
        public int IncrementalUpdate;
        public PaintReservedBytes Reserved;
    }

    [DllImport("user32.dll", EntryPoint = "RegisterClassExW", SetLastError = true)]
    public static extern ushort RegisterClassEx(ref WindowClassEx windowClass);

    [DllImport("user32.dll", EntryPoint = "CreateWindowExW", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern nint CreateWindowEx(
        uint extendedStyle,
        string className,
        string windowName,
        uint style,
        int x,
        int y,
        int width,
        int height,
        nint parent,
        nint menu,
        nint instance,
        nint parameter);

    [DllImport("kernel32.dll", EntryPoint = "GetModuleHandleW", CharSet = CharSet.Unicode)]
    public static extern nint GetModuleHandle(string? moduleName);

    [DllImport("user32.dll", EntryPoint = "LoadCursorW", SetLastError = true)]
    public static extern nint LoadCursor(nint instance, nint cursorName);

    [DllImport("user32.dll", EntryPoint = "DefWindowProcW")]
    public static extern nint DefWindowProc(nint window, uint message, nuint wParam, nint lParam);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    public static extern nint SetWindowLongPtr(nint window, int index, nint value);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    public static extern nint GetWindowLongPtr(nint window, int index);

    [DllImport("user32.dll", EntryPoint = "SetTimer", SetLastError = true)]
    public static extern nuint SetTimer(nint window, nuint id, uint interval, nint callback);

    [DllImport("user32.dll", EntryPoint = "KillTimer", SetLastError = true)]
    public static extern bool KillTimer(nint window, nuint id);

    [DllImport("user32.dll", EntryPoint = "InvalidateRect", SetLastError = true)]
    public static extern bool InvalidateRect(nint window, nint rectangle, bool erase);

    [DllImport("user32.dll", EntryPoint = "BeginPaint", SetLastError = true)]
    public static extern nint BeginPaint(nint window, ref PaintStruct paint);

    [DllImport("user32.dll", EntryPoint = "EndPaint", SetLastError = true)]
    public static extern bool EndPaint(nint window, ref PaintStruct paint);

    [DllImport("user32.dll", EntryPoint = "GetClientRect", SetLastError = true)]
    public static extern bool GetClientRect(nint window, out Rect rect);

    [DllImport("user32.dll", EntryPoint = "SetWindowPos", SetLastError = true)]
    public static extern bool SetWindowPos(nint window, nint insertAfter, int x, int y, int width, int height, uint flags);

    [DllImport("user32.dll", EntryPoint = "DestroyWindow", SetLastError = true)]
    public static extern bool DestroyWindow(nint window);

    [DllImport("user32.dll", EntryPoint = "SetCapture", SetLastError = true)]
    public static extern nint SetCapture(nint window);

    [DllImport("user32.dll", EntryPoint = "GetCapture", SetLastError = true)]
    public static extern nint GetCapture();

    [DllImport("user32.dll", EntryPoint = "ReleaseCapture", SetLastError = true)]
    public static extern bool ReleaseCapture();

    [DllImport("gdi32.dll", EntryPoint = "CreateCompatibleDC", SetLastError = true)]
    public static extern nint CreateCompatibleDC(nint dc);

    [DllImport("gdi32.dll", EntryPoint = "CreateCompatibleBitmap", SetLastError = true)]
    public static extern nint CreateCompatibleBitmap(nint dc, int width, int height);

    [DllImport("gdi32.dll", EntryPoint = "SelectObject", SetLastError = true)]
    public static extern nint SelectObject(nint dc, nint obj);

    [DllImport("gdi32.dll", EntryPoint = "DeleteObject", SetLastError = true)]
    public static extern bool DeleteObject(nint obj);

    [DllImport("gdi32.dll", EntryPoint = "DeleteDC", SetLastError = true)]
    public static extern bool DeleteDC(nint dc);

    [DllImport("gdi32.dll", EntryPoint = "BitBlt", SetLastError = true)]
    public static extern bool BitBlt(nint destination, int x, int y, int width, int height, nint source, int sourceX, int sourceY, uint operation);

    [DllImport("gdi32.dll", EntryPoint = "CreateSolidBrush", SetLastError = true)]
    public static extern nint CreateSolidBrush(int color);

    [DllImport("gdi32.dll", EntryPoint = "CreatePen", SetLastError = true)]
    public static extern nint CreatePen(int style, int width, int color);

    [DllImport("gdi32.dll", EntryPoint = "RoundRect", SetLastError = true)]
    public static extern bool RoundRect(nint dc, int left, int top, int right, int bottom, int ellipseWidth, int ellipseHeight);

    [DllImport("gdi32.dll", EntryPoint = "MoveToEx", SetLastError = true)]
    public static extern bool MoveToEx(nint dc, int x, int y, nint previousPoint);

    [DllImport("gdi32.dll", EntryPoint = "LineTo", SetLastError = true)]
    public static extern bool LineTo(nint dc, int x, int y);

    [DllImport("gdi32.dll", EntryPoint = "SetBkMode", SetLastError = true)]
    public static extern int SetBkMode(nint dc, uint mode);

    [DllImport("gdi32.dll", EntryPoint = "SetTextColor", SetLastError = true)]
    public static extern int SetTextColor(nint dc, int color);

    [DllImport("gdi32.dll", EntryPoint = "SetDCBrushColor", SetLastError = true)]
    public static extern int SetDCBrushColor(nint dc, int color);

    [DllImport("gdi32.dll", EntryPoint = "FillRect", SetLastError = true)]
    public static extern int FillRect(nint dc, ref Rect rect, nint brush);

    [DllImport("gdi32.dll", EntryPoint = "CreateFontW", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern nint CreateFont(
        int height,
        int width,
        int escapement,
        int orientation,
        int weight,
        uint italic,
        uint underline,
        uint strikeOut,
        uint characterSet,
        uint outputPrecision,
        uint clipPrecision,
        uint quality,
        uint pitchAndFamily,
        string faceName);

    [DllImport("user32.dll", EntryPoint = "DrawTextW", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int DrawText(nint dc, string text, int length, ref Rect rect, uint format);
}
