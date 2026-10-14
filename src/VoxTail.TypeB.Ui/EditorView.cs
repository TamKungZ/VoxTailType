using System.Runtime.InteropServices;
using NPlug;

namespace VoxTail.TypeB.Ui;

public sealed class EditorView : IAudioPluginView
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
        new("Clean / Wet Mix", EditorParameter.Mix, "%", 0f, 100f, 1, 1),
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
        NativeMethods.SetWindowLongPtr(_window, NativeMethods.GwlpUserData, GCHandle.ToIntPtr(_selfHandle));
        NativeMethods.SetTimer(_window, TimerId, 33, 0);
        NativeMethods.InvalidateRect(_window, 0, false);
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
        var paint = new NativeMethods.PaintStruct { Reserved = new byte[32] };
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
                DrawEditor(target, width, height);
                return;
            }

            oldBitmap = NativeMethods.SelectObject(buffer, bitmap);
            NativeMethods.SetBkMode(buffer, NativeMethods.Transparent);
            DrawEditor(buffer, width, height);
            NativeMethods.BitBlt(target, 0, 0, width, height, buffer, 0, 0, NativeMethods.Srccopy);
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

    private void DrawEditor(nint dc, int width, int height)
    {
        FillRectangle(dc, 0, 0, width, height, UiColors.Canvas);
        DrawText(dc, "VOXTAIL", 24, 17, UiColors.Ink, 23, true);
        DrawText(dc, "TYPE-B  /  VOCAL CHARACTER", 26, 47, UiColors.Secondary, 10, true);
        DrawRoundedRectangle(dc, 625, 24, 110, 30, 15, UiColors.BlushSurface, UiColors.BlushSurface);
        DrawText(dc, _voiceActive ? "ACTIVITY" : "STANDBY", 636, 33, UiColors.DeepRose, 10, true);
        DrawRoundedRectangle(dc, 24, 80, width - 48, 198, 16, UiColors.Shadow, UiColors.Shadow);
        DrawRoundedRectangle(dc, 24, 77, width - 48, 198, 16, UiColors.White, UiColors.Border);
        DrawText(dc, "LOUDNESS", 42, 91, UiColors.Ink, 11, true);
        DrawText(dc, "LIVE ENVELOPE  /  APPROX. RMS", 132, 92, UiColors.Secondary, 9, false);
        DrawText(dc, $"{_boostDb:0.0} dB BOOST", 630, 91, UiColors.DeepRose, 10, true, width: 106, alignRight: true);
        DrawGraph(dc, width);
        DrawText(dc, "SHAPE", 28, 294, UiColors.Ink, 11, true);
        DrawText(dc, "CORE", 677, 294, UiColors.Secondary, 9, true, width: 54, alignRight: true);
        DrawGroupCard(dc, 24, 312, width - 48, 168, null);
        DrawText(dc, "DYNAMIC BOOST", 28, 492, UiColors.Ink, 11, true);
        DrawText(dc, "LEVEL CONTROL", 650, 492, UiColors.Secondary, 9, true, width: 81, alignRight: true);
        DrawGroupCard(dc, 24, 510, width - 48, 96, null);
        DrawText(dc, "TAIL", 28, 620, UiColors.Ink, 11, true);
        DrawText(dc, "TEXTURE", 673, 620, UiColors.Secondary, 9, true, width: 58, alignRight: true);
        DrawGroupCard(dc, 24, 638, width - 48, 96, null);
        DrawText(dc, "VOXTAIL TYPE-B", 28, 765, UiColors.Secondary, 9, true);
        DrawText(dc, "A LITTLE PINK. A LOT OF CHARACTER.", width - 285, 765, UiColors.Secondary, 9, false, width: 255, alignRight: true);

        foreach (var control in Controls)
        {
            DrawControl(dc, control, width);
        }
    }

    private void DrawGraph(nint dc, int width)
    {
        var left = 70;
        var top = 119;
        var plotWidth = width - 112;
        var plotHeight = 112;
        FillRoundedRectangle(dc, left, top, plotWidth, plotHeight, 9, UiColors.Plot, UiColors.Plot);
        for (var grid = 0; grid < 5; grid++)
        {
            var y = top + (plotHeight * grid / 4);
            DrawLine(dc, left, y, left + plotWidth, y, UiColors.Grid, 1);
            DrawText(dc, grid == 4 ? "-60" : $"{-15 * grid:0}", 35, y - 6, UiColors.Secondary, 8, false);
        }

        DrawText(dc, "dBFS", 34, 113, UiColors.Secondary, 8, false);
        var thresholdDb = -72f + (float)_bridge.GetEditorParameterValue(EditorParameter.VoiceThreshold) * 48f;
        var targetDb = -36f + (float)_bridge.GetEditorParameterValue(EditorParameter.TargetLevel) * 24f;
        DrawReferenceLine(dc, left, top, plotWidth, plotHeight, targetDb, UiColors.Navy, "TARGET");
        DrawReferenceLine(dc, left, top, plotWidth, plotHeight, thresholdDb, UiColors.DeepRose, "THRESHOLD");
        DrawHistory(dc, _inputHistory, _historyPosition, left, top, plotWidth, plotHeight, UiColors.PinkShadow, 5);
        DrawHistory(dc, _inputHistory, _historyPosition, left, top, plotWidth, plotHeight, UiColors.Pink, 2);
        DrawHistory(dc, _outputHistory, _historyPosition, left, top, plotWidth, plotHeight, UiColors.Ink, 1);
        DrawText(dc, "IN", 74, 241, UiColors.Pink, 9, true);
        DrawText(dc, "OUT", 103, 241, UiColors.Ink, 9, true);
        DrawText(dc, $"{ToDecibels(_inputRms):0.0} dBFS", width - 202, 241, UiColors.Pink, 9, true);
        DrawText(dc, $"{ToDecibels(_outputRms):0.0} dBFS", width - 112, 241, UiColors.Ink, 9, true);
        DrawText(dc, "~4 SEC", left + (plotWidth / 2) - 22, 241, UiColors.Secondary, 8, false, width: 46, alignRight: true);
    }

    private void DrawReferenceLine(nint dc, int left, int top, int plotWidth, int plotHeight, float decibels, int color, string label)
    {
        var y = top + (int)((-Math.Clamp(decibels, MeterFloorDb, 0f) / -MeterFloorDb) * plotHeight);
        DrawDottedLine(dc, left, y, left + plotWidth, y, color);
        DrawText(dc, label, left + plotWidth - 55, y - 12, color, 8, true, width: 48, alignRight: true);
    }

    private static void DrawHistory(nint dc, float[] history, int position, int left, int top, int plotWidth, int plotHeight, int color, int thickness)
    {
        var pen = NativeMethods.CreatePen(NativeMethods.PsSolid, thickness, color);
        var oldPen = NativeMethods.SelectObject(dc, pen);
        for (var point = 1; point < MeterHistoryLength; point++)
        {
            var previousIndex = (position + point - 1) % MeterHistoryLength;
            var index = (position + point) % MeterHistoryLength;
            var x1 = left + ((point - 1) * plotWidth / (MeterHistoryLength - 1));
            var x2 = left + (point * plotWidth / (MeterHistoryLength - 1));
            NativeMethods.MoveToEx(dc, x1, MapMeterY(history[previousIndex], top, plotHeight), 0);
            NativeMethods.LineTo(dc, x2, MapMeterY(history[index], top, plotHeight));
        }

        NativeMethods.SelectObject(dc, oldPen);
        NativeMethods.DeleteObject(pen);
    }

    private void DrawControl(nint dc, UiControl control, int width)
    {
        var bounds = GetControlBounds(control);
        var right = Math.Min(bounds.X + bounds.Width, width - 30);
        DrawRoundedRectangle(dc, bounds.X, bounds.Y + 2, right - bounds.X, bounds.Height, 9, UiColors.Shadow, UiColors.Shadow);
        DrawRoundedRectangle(dc, bounds.X, bounds.Y, right - bounds.X, bounds.Height, 9, UiColors.White, UiColors.Border);
        var normalized = (float)_bridge.GetEditorParameterValue(control.ParameterId);
        DrawText(dc, control.Title.ToUpperInvariant(), bounds.X + 11, bounds.Y + 9, UiColors.Secondary, 8, true);
        DrawText(dc, FormatValue(control, normalized), bounds.X + 11, bounds.Y + 23, UiColors.Ink, 12, true);
        var trackLeft = bounds.X + 12;
        var trackRight = right - 12;
        var trackY = bounds.Y + bounds.Height - 12;
        DrawRoundedRectangle(dc, trackLeft, trackY - 2, trackRight - trackLeft, 4, 2, UiColors.Track, UiColors.Track);
        var thumbX = trackLeft + (int)((trackRight - trackLeft) * normalized);
        DrawRoundedRectangle(dc, trackLeft, trackY - 2, Math.Max(0, thumbX - trackLeft), 4, 2, UiColors.Pink, UiColors.Pink);
        DrawRoundedRectangle(dc, thumbX - 5, trackY - 6, 10, 12, 5, UiColors.White, UiColors.Pink);
    }

    private static void DrawGroupCard(nint dc, int x, int y, int width, int height, string? title)
    {
        DrawRoundedRectangle(dc, x, y + 2, width, height, 14, UiColors.Shadow, UiColors.Shadow);
        DrawRoundedRectangle(dc, x, y, width, height, 14, UiColors.White, UiColors.Border);
        if (!string.IsNullOrEmpty(title))
        {
            DrawText(dc, title, x + 15, y + 9, UiColors.Secondary, 8, true);
        }
    }

    private static ControlBounds GetControlBounds(UiControl control)
    {
        return control.Section switch
        {
            0 => new ControlBounds(38 + (control.Column * 234), 331 + (control.Row * 75), 216, 64),
            1 => new ControlBounds(38 + (control.Column * 176), 530, 164, 64),
            _ => new ControlBounds(38 + (control.Column * 176), 658, 164, 64)
        };
    }

    private string FormatValue(UiControl control, float normalized)
    {
        if (control.IsChoice)
        {
            return Math.Clamp((int)MathF.Round(normalized * 2f), 0, 2) switch
            {
                0 => "Crackle",
                1 => "Breaks",
                _ => "Glitch"
            };
        }

        var value = control.Minimum + (normalized * (control.Maximum - control.Minimum));
        return control.Unit switch
        {
            "%" => $"{value:0}%",
            "ms" => $"{value:0} ms",
            "dBFS" => $"{value:0} dBFS",
            "dB" => $"{value:+0.0;-0.0;0.0} dB",
            _ => $"{value:0.0}"
        };
    }

    private static int MapMeterY(float decibels, int top, int height) =>
        top + (int)(Math.Clamp(-decibels / -MeterFloorDb, 0f, 1f) * height);

    private static float ToDecibels(float rms) =>
        MathF.Max(MeterFloorDb, 20f * MathF.Log10(MathF.Max(float.IsFinite(rms) ? rms : 0f, 1e-6f)));

    private static int GetSignedLowWord(nint value) => (short)(value.ToInt64() & 0xffff);

    private static int GetSignedHighWord(nint value) => (short)((value.ToInt64() >> 16) & 0xffff);

    private static void FillRectangle(nint dc, int x, int y, int width, int height, int color)
    {
        var brush = NativeMethods.CreateSolidBrush(color);
        NativeMethods.SetDCBrushColor(dc, color);
        var rect = new NativeMethods.Rect { Left = x, Top = y, Right = x + width, Bottom = y + height };
        NativeMethods.FillRect(dc, ref rect, brush);
        NativeMethods.DeleteObject(brush);
    }

    private static void DrawRoundedRectangle(nint dc, int x, int y, int width, int height, int radius, int fill, int outline)
    {
        var brush = NativeMethods.CreateSolidBrush(fill);
        var pen = NativeMethods.CreatePen(NativeMethods.PsSolid, 1, outline);
        var oldBrush = NativeMethods.SelectObject(dc, brush);
        var oldPen = NativeMethods.SelectObject(dc, pen);
        NativeMethods.RoundRect(dc, x, y, x + width, y + height, radius, radius);
        NativeMethods.SelectObject(dc, oldPen);
        NativeMethods.SelectObject(dc, oldBrush);
        NativeMethods.DeleteObject(pen);
        NativeMethods.DeleteObject(brush);
    }

    private static void FillRoundedRectangle(nint dc, int x, int y, int width, int height, int radius, int fill, int outline) =>
        DrawRoundedRectangle(dc, x, y, width, height, radius, fill, outline);

    private static void DrawLine(nint dc, int x1, int y1, int x2, int y2, int color, int thickness)
    {
        var pen = NativeMethods.CreatePen(NativeMethods.PsSolid, thickness, color);
        var oldPen = NativeMethods.SelectObject(dc, pen);
        NativeMethods.MoveToEx(dc, x1, y1, 0);
        NativeMethods.LineTo(dc, x2, y2);
        NativeMethods.SelectObject(dc, oldPen);
        NativeMethods.DeleteObject(pen);
    }

    private static void DrawDottedLine(nint dc, int x1, int y1, int x2, int y2, int color)
    {
        var pen = NativeMethods.CreatePen(NativeMethods.PsDot, 1, color);
        var oldPen = NativeMethods.SelectObject(dc, pen);
        NativeMethods.MoveToEx(dc, x1, y1, 0);
        NativeMethods.LineTo(dc, x2, y2);
        NativeMethods.SelectObject(dc, oldPen);
        NativeMethods.DeleteObject(pen);
    }

    private static void DrawText(nint dc, string text, int x, int y, int color, int size, bool bold, int width = 300, bool alignRight = false)
    {
        var font = NativeMethods.CreateFont(
            -size,
            0,
            0,
            0,
            bold ? NativeMethods.FwBold : NativeMethods.FwNormal,
            0,
            0,
            0,
            NativeMethods.DefaultCharset,
            NativeMethods.OutDefaultPrecision,
            NativeMethods.ClipDefaultPrecision,
            NativeMethods.ClearTypeQuality,
            NativeMethods.DefaultPitch | NativeMethods.FfDontCare,
            "Segoe UI");
        var oldFont = NativeMethods.SelectObject(dc, font);
        NativeMethods.SetTextColor(dc, color);
        var rect = new NativeMethods.Rect
        {
            Left = x,
            Top = y,
            Right = x + width,
            Bottom = y + size + 8
        };
        NativeMethods.DrawText(dc, text, text.Length, ref rect, NativeMethods.DtSingleLine | NativeMethods.DtVCenter | (alignRight ? NativeMethods.DtRight : NativeMethods.DtLeft));
        NativeMethods.SelectObject(dc, oldFont);
        NativeMethods.DeleteObject(font);
    }

    private readonly record struct UiControl(
        string Title,
        EditorParameter ParameterId,
        string Unit,
        float Minimum,
        float Maximum,
        int Column,
        int Row,
        bool IsChoice = false)
    {
        public int Section => Row switch
        {
            0 or 1 => 0,
            2 => 1,
            _ => 2
        };
    }

    private readonly record struct ControlBounds(int X, int Y, int Width, int Height);

    private static class UiColors
    {
        public static readonly int Canvas = ColorRef(255, 255, 255);
        public static readonly int White = ColorRef(255, 255, 255);
        public static readonly int Ink = ColorRef(27, 36, 56);
        public static readonly int Secondary = ColorRef(104, 116, 140);
        public static readonly int Pink = ColorRef(245, 163, 192);
        public static readonly int DeepRose = ColorRef(209, 105, 145);
        public static readonly int PinkShadow = ColorRef(255, 222, 234);
        public static readonly int BlushSurface = ColorRef(255, 242, 247);
        public static readonly int Navy = ColorRef(46, 59, 90);
        public static readonly int Plot = ColorRef(255, 250, 252);
        public static readonly int Grid = ColorRef(239, 234, 238);
        public static readonly int Track = ColorRef(240, 235, 239);
        public static readonly int Border = ColorRef(248, 239, 243);
        public static readonly int Shadow = ColorRef(252, 245, 248);

        private static int ColorRef(byte red, byte green, byte blue) =>
            red | (green << 8) | (blue << 16);
    }

    private static class NativeMethods
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

        [StructLayout(LayoutKind.Sequential)]
        public struct PaintStruct
        {
            public nint DeviceContext;
            public int Erase;
            public Rect PaintRectangle;
            public int Restore;
            public int IncrementalUpdate;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] Reserved;
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
}
