using System.Runtime.InteropServices;
using NPlug;

namespace VoxTail.TypeB.Ui;

public sealed partial class EditorView : IAudioPluginView
{
    private const int ViewWidth = 760;
    private const int ViewHeight = 800;
    private const int TimerId = 1;
    private const float MeterFloorDb = -60f;
    private const int MeterHistoryLength = 120;
    private static readonly UiControl[] Controls =
    [
        new("Input Gain", EditorParameter.InputGain, "dB", -12f, 12f, 0, 0),
        new("Intensity", EditorParameter.Intensity, "%", 0f, 100f, 1, 0),
        new("Drive", EditorParameter.Drive, "%", 0f, 100f, 2, 0),
        new("Tone", EditorParameter.Tone, "%", 0f, 100f, 0, 1),
        new("Wet Mix", EditorParameter.Mix, "%", 0f, 100f, 1, 1),
        new("Output Gain", EditorParameter.OutputGain, "dB", -12f, 12f, 2, 1),
        new("Boost Amount", EditorParameter.DynamicBoost, "%", 0f, 100f, 0, 2),
        new("Voice Threshold", EditorParameter.VoiceThreshold, "dB", -72f, -24f, 1, 2),
        new("Target Level", EditorParameter.TargetLevel, "dBFS", -36f, -12f, 2, 2),
        new("Maximum Boost", EditorParameter.MaximumBoost, "dB", 0f, 18f, 3, 2),
        new("Tail Amount", EditorParameter.TailAmount, "%", 0f, 100f, 0, 3),
        new("Tail Drop", EditorParameter.TailDrop, "%", 0f, 100f, 1, 3),
        new("Tail Time", EditorParameter.TailTime, "ms", 50f, 1500f, 2, 3),
        new("Tail Character", EditorParameter.TailCharacter, "", 0f, 2f, 3, 3, true)
    ];
    private static readonly object WindowClassLock = new();
    private static readonly NativeMethods.WindowProcedure WindowProcedure = WindowProc;
    private static readonly nint WindowProcedurePointer = Marshal.GetFunctionPointerForDelegate(WindowProcedure);
    private static bool _windowClassRegistered;

    private readonly IEditorBridge _bridge;
    private readonly float[] _inputHistory = new float[MeterHistoryLength];
    private readonly float[] _outputHistory = new float[MeterHistoryLength];
    private GCHandle _selfHandle;
    private nint _window;
    private int _width = ViewWidth;
    private int _height = ViewHeight;
    private int _draggedControl = -1;
    private int _historyPosition;
    private float _inputRms;
    private float _outputRms;
    private float _boostDb;
    private bool _voiceActive;

    public EditorView(IEditorBridge bridge)
    {
        _bridge = bridge;
        Array.Fill(_inputHistory, MeterFloorDb);
        Array.Fill(_outputHistory, MeterFloorDb);
    }

    public ViewRectangle Size => new(0, 0, _width, _height);

    public bool IsPlatformTypeSupported(AudioPluginViewPlatform platform) =>
        OperatingSystem.IsWindows() && platform == AudioPluginViewPlatform.Hwnd;

    public void Attached(nint parent, AudioPluginViewPlatform type)
    {
        if (_window != 0)
        {
            throw new InvalidOperationException("The VoxTail editor view is already attached.");
        }

        if (!IsPlatformTypeSupported(type))
        {
            throw new PlatformNotSupportedException($"The VoxTail editor does not support the {type} view platform.");
        }

        EnsureWindowClass();
        _window = NativeMethods.CreateWindowEx(
            0,
            NativeMethods.WindowClassName,
            "VoxTail Type-B",
            NativeMethods.WsChild | NativeMethods.WsVisible | NativeMethods.WsClipChildren,
            0,
            0,
            _width,
            _height,
            parent,
            0,
            NativeMethods.GetModuleHandle(null),
            0);
        if (_window == 0)
        {
            throw new InvalidOperationException($"Could not create the VoxTail editor window. Win32 error: {Marshal.GetLastWin32Error()}.");
        }

        _selfHandle = GCHandle.Alloc(this);
        try
        {
            Marshal.SetLastPInvokeError(0);
            var previousUserData = NativeMethods.SetWindowLongPtr(
                _window,
                NativeMethods.GwlpUserData,
                GCHandle.ToIntPtr(_selfHandle));
            var userDataError = Marshal.GetLastPInvokeError();
            if (previousUserData == 0 && userDataError != 0)
            {
                throw new InvalidOperationException($"Could not initialize the VoxTail editor window. Win32 error: {userDataError}.");
            }

            if (NativeMethods.SetTimer(_window, TimerId, 33, 0) == 0)
            {
                throw new InvalidOperationException($"Could not start the VoxTail editor meter timer. Win32 error: {Marshal.GetLastWin32Error()}.");
            }

            NativeMethods.InvalidateRect(_window, 0, false);
        }
        catch
        {
            NativeMethods.SetWindowLongPtr(_window, NativeMethods.GwlpUserData, 0);
            NativeMethods.DestroyWindow(_window);
            _window = 0;
            _selfHandle.Free();
            throw;
        }
    }

    public void Removed()
    {
        _bridge.EndEditorParameterEdit();
        _draggedControl = -1;
        if (_window != 0)
        {
            NativeMethods.KillTimer(_window, TimerId);
            NativeMethods.SetWindowLongPtr(_window, NativeMethods.GwlpUserData, 0);
            NativeMethods.DestroyWindow(_window);
            _window = 0;
        }

        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }
    }

    public void OnWheel(float distance)
    {
        if (_window != 0)
        {
            NativeMethods.InvalidateRect(_window, 0, false);
        }
    }

    public void OnKeyDown(ushort key, short keyCode, short modifiers)
    {
    }

    public void OnKeyUp(ushort key, short keyCode, short modifiers)
    {
    }

    public void OnSize(ViewRectangle newSize)
    {
        _width = Math.Max(ViewWidth, newSize.Right - newSize.Left);
        _height = Math.Max(ViewHeight, newSize.Bottom - newSize.Top);
        if (_window != 0)
        {
            NativeMethods.SetWindowPos(_window, 0, 0, 0, _width, _height, NativeMethods.SwpNoZOrder | NativeMethods.SwpNoActivate);
            NativeMethods.InvalidateRect(_window, 0, false);
        }
    }

    public void OnFocus(bool state)
    {
    }

    public void SetFrame(IAudioPluginFrame frame)
    {
    }

    public bool CanResize() => false;

    public bool CheckSizeConstraint(ref ViewRectangle rect)
    {
        rect = new ViewRectangle(rect.Left, rect.Top, rect.Left + ViewWidth, rect.Top + ViewHeight);
        return true;
    }

    public void SetContentScaleFactor(float factor)
    {
    }

    public bool TryFindParameter(int xPos, int yPos, out AudioParameterId parameterId)
    {
        foreach (var control in Controls)
        {
            var bounds = GetControlBounds(control);
            if (xPos >= bounds.X && xPos <= bounds.X + bounds.Width &&
                yPos >= bounds.Y && yPos <= bounds.Y + bounds.Height)
            {
                parameterId = _bridge.GetHostParameterId(control.ParameterId);
                return true;
            }
        }

        parameterId = default;
        return false;
    }

    public void OnMeterFrame(EditorMeterFrame frame)
    {
        _inputRms = frame.InputRms;
        _outputRms = frame.OutputRms;
        _boostDb = frame.BoostDb;
        _voiceActive = frame.VoiceActive;
        _inputHistory[_historyPosition] = ToDecibels(frame.InputRms);
        _outputHistory[_historyPosition] = ToDecibels(frame.OutputRms);
        _historyPosition = (_historyPosition + 1) % MeterHistoryLength;
        if (_window != 0)
        {
            NativeMethods.InvalidateRect(_window, 0, false);
        }
    }

    private static void EnsureWindowClass()
    {
        lock (WindowClassLock)
        {
            if (_windowClassRegistered)
            {
                return;
            }

            var windowClass = new NativeMethods.WindowClassEx
            {
                Size = (uint)Marshal.SizeOf<NativeMethods.WindowClassEx>(),
                Style = NativeMethods.CsHRedraw | NativeMethods.CsVRedraw,
                WindowProcedure = WindowProcedurePointer,
                Instance = NativeMethods.GetModuleHandle(null),
                Cursor = NativeMethods.LoadCursor(0, NativeMethods.IdcArrow),
                ClassName = NativeMethods.WindowClassName
            };
            if (NativeMethods.RegisterClassEx(ref windowClass) == 0)
            {
                var error = Marshal.GetLastWin32Error();
                if (error != NativeMethods.ErrorClassAlreadyExists)
                {
                    throw new InvalidOperationException($"Could not register the VoxTail editor window class. Win32 error: {error}.");
                }
            }

            _windowClassRegistered = true;
        }
    }

    private static nint WindowProc(nint window, uint message, nuint wParam, nint lParam)
    {
        try
        {
            var userData = NativeMethods.GetWindowLongPtr(window, NativeMethods.GwlpUserData);
            if (userData == 0)
            {
                return NativeMethods.DefWindowProc(window, message, wParam, lParam);
            }

            var view = (EditorView?)GCHandle.FromIntPtr(userData).Target;
            return view?.HandleMessage(window, message, wParam, lParam) ??
                NativeMethods.DefWindowProc(window, message, wParam, lParam);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError($"VoxTail editor window callback failed: {exception}");
            return NativeMethods.DefWindowProc(window, message, wParam, lParam);
        }
    }

    private nint HandleMessage(nint window, uint message, nuint wParam, nint lParam)
    {
        switch (message)
        {
            case NativeMethods.WmEraseBackground:
                return 1;
            case NativeMethods.WmPaint:
                Paint(window);
                return 0;
            case NativeMethods.WmTimer when wParam == TimerId:
                _bridge.RequestMeterFrame();
                NativeMethods.InvalidateRect(window, 0, false);
                return 0;
            case NativeMethods.WmLeftButtonDown:
                BeginDrag(window, GetSignedLowWord(lParam), GetSignedHighWord(lParam));
                return 0;
            case NativeMethods.WmMouseMove when _draggedControl >= 0:
                UpdateDraggedControl(GetSignedLowWord(lParam));
                return 0;
            case NativeMethods.WmLeftButtonUp:
            case NativeMethods.WmCaptureChanged:
            case NativeMethods.WmCancelMode:
                EndDrag(window);
                return 0;
            case NativeMethods.WmDestroy:
                NativeMethods.KillTimer(window, TimerId);
                return 0;
            default:
                return NativeMethods.DefWindowProc(window, message, wParam, lParam);
        }
    }

    private void BeginDrag(nint window, int x, int y)
    {
        for (var index = 0; index < Controls.Length; index++)
        {
            var bounds = GetControlBounds(Controls[index]);
            var sliderTop = bounds.Y + bounds.Height - 22;
            if (x < bounds.X || x > bounds.X + bounds.Width || y < sliderTop || y > bounds.Y + bounds.Height)
            {
                continue;
            }

            _draggedControl = index;
            _bridge.BeginEditorParameterEdit(Controls[index].ParameterId);
            NativeMethods.SetCapture(window);
            UpdateDraggedControl(x);
            return;
        }
    }

    private void UpdateDraggedControl(int x)
    {
        if (_draggedControl < 0)
        {
            return;
        }

        var control = Controls[_draggedControl];
        var bounds = GetControlBounds(control);
        var normalized = Math.Clamp((x - bounds.X - 12f) / Math.Max(bounds.Width - 24f, 1f), 0f, 1f);
        if (control.IsChoice)
        {
            normalized = MathF.Round(normalized * 2f) / 2f;
        }

        _bridge.SetEditorParameterValue(control.ParameterId, normalized);
        NativeMethods.InvalidateRect(_window, 0, false);
    }

    private void EndDrag(nint window)
    {
        if (_draggedControl < 0)
        {
            return;
        }

        _draggedControl = -1;
        _bridge.EndEditorParameterEdit();
        if (NativeMethods.GetCapture() == window)
        {
            NativeMethods.ReleaseCapture();
        }

        NativeMethods.InvalidateRect(window, 0, false);
    }

    private void Paint(nint window)
    {
        var paint = new NativeMethods.PaintStruct();
        var target = NativeMethods.BeginPaint(window, ref paint);
        if (target == 0)
        {
            return;
        }

        nint buffer = 0;
        nint bitmap = 0;
        nint oldBitmap = 0;
        try
        {
            NativeMethods.GetClientRect(window, out var clientRect);
            var width = Math.Max(1, clientRect.Right - clientRect.Left);
            var height = Math.Max(1, clientRect.Bottom - clientRect.Top);
            buffer = NativeMethods.CreateCompatibleDC(target);
            bitmap = NativeMethods.CreateCompatibleBitmap(target, width, height);
            if (buffer == 0 || bitmap == 0)
            {
                System.Diagnostics.Trace.TraceError("Could not allocate the VoxTail editor's double-buffered surface; drawing directly.");
                NativeMethods.SetBkMode(target, NativeMethods.Transparent);
                DrawEditorOrReportError(target, width, height);
                return;
            }

            oldBitmap = NativeMethods.SelectObject(buffer, bitmap);
            if (oldBitmap == 0 || oldBitmap == (nint)(-1))
            {
                System.Diagnostics.Trace.TraceError("Could not select the VoxTail editor's double-buffered bitmap; drawing directly.");
                NativeMethods.SetBkMode(target, NativeMethods.Transparent);
                DrawEditorOrReportError(target, width, height);
                return;
            }

            NativeMethods.SetBkMode(buffer, NativeMethods.Transparent);
            DrawEditorOrReportError(buffer, width, height);
            if (!NativeMethods.BitBlt(target, 0, 0, width, height, buffer, 0, 0, NativeMethods.Srccopy))
            {
                System.Diagnostics.Trace.TraceError($"Could not copy the VoxTail editor's double-buffered surface. Win32 error: {Marshal.GetLastWin32Error()}.");
                NativeMethods.SetBkMode(target, NativeMethods.Transparent);
                DrawEditorOrReportError(target, width, height);
            }
        }
        finally
        {
            if (oldBitmap != 0)
            {
                NativeMethods.SelectObject(buffer, oldBitmap);
            }

            if (bitmap != 0)
            {
                NativeMethods.DeleteObject(bitmap);
            }

            if (buffer != 0)
            {
                NativeMethods.DeleteDC(buffer);
            }

            NativeMethods.EndPaint(window, ref paint);
        }
    }

    private void DrawEditorOrReportError(nint dc, int width, int height)
    {
        try
        {
            DrawEditor(dc, width, height);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError($"VoxTail editor rendering failed: {exception}");
            FillRectangle(dc, 0, 0, width, height, UiColors.Canvas);
            DrawText(dc, "EDITOR RENDER ERROR", 28, 28, UiColors.DeepRose, 16, true);
            DrawText(dc, "See the host or debugger log for details.", 28, 58, UiColors.Secondary, 11, false);
        }
    }

    private static int GetSignedLowWord(nint value) => (short)(value.ToInt64() & 0xffff);

    private static int GetSignedHighWord(nint value) => (short)((value.ToInt64() >> 16) & 0xffff);

    private static float ToDecibels(float rms) =>
        MathF.Max(MeterFloorDb, 20f * MathF.Log10(MathF.Max(float.IsFinite(rms) ? rms : 0f, 1e-6f)));
}
