namespace VoxTail.TypeB.Ui;

public sealed partial class EditorView
{
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
        DrawGroupCard(dc, 24, 312, width - 48, 168);
        DrawText(dc, "DYNAMIC BOOST", 28, 492, UiColors.Ink, 11, true);
        DrawText(dc, "LEVEL CONTROL", 650, 492, UiColors.Secondary, 9, true, width: 81, alignRight: true);
        DrawGroupCard(dc, 24, 510, width - 48, 96);
        DrawText(dc, "TAIL", 28, 620, UiColors.Ink, 11, true);
        DrawText(dc, "TEXTURE", 673, 620, UiColors.Secondary, 9, true, width: 58, alignRight: true);
        DrawGroupCard(dc, 24, 638, width - 48, 96);
        DrawText(dc, "VOXTAIL TYPE-B", 28, 765, UiColors.Secondary, 9, true);
        DrawText(dc, "A LITTLE PINK. A LOT OF CHARACTER.", width - 285, 765, UiColors.Secondary, 9, false, width: 255, alignRight: true);

        foreach (var control in Controls)
        {
            DrawControl(dc, control, width);
        }
    }

    private void DrawGraph(nint dc, int width)
    {
        const int left = 70;
        const int top = 119;
        const int plotHeight = 112;
        var plotWidth = width - 112;
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

    private static void DrawReferenceLine(nint dc, int left, int top, int plotWidth, int plotHeight, float decibels, int color, string label)
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

    private static void DrawGroupCard(nint dc, int x, int y, int width, int height)
    {
        DrawRoundedRectangle(dc, x, y + 2, width, height, 14, UiColors.Shadow, UiColors.Shadow);
        DrawRoundedRectangle(dc, x, y, width, height, 14, UiColors.White, UiColors.Border);
    }

    private static ControlBounds GetControlBounds(UiControl control) =>
        control.Section switch
        {
            0 => new ControlBounds(38 + (control.Column * 234), 331 + (control.Row * 75), 216, 64),
            1 => new ControlBounds(38 + (control.Column * 176), 530, 164, 64),
            _ => new ControlBounds(38 + (control.Column * 176), 658, 164, 64)
        };

    private static string FormatValue(UiControl control, float normalized)
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

    private static void FillRectangle(nint dc, int x, int y, int width, int height, int color)
    {
        var brush = NativeMethods.CreateSolidBrush(color);
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
            -size, 0, 0, 0, bold ? NativeMethods.FwBold : NativeMethods.FwNormal,
            0, 0, 0, NativeMethods.DefaultCharset, NativeMethods.OutDefaultPrecision,
            NativeMethods.ClipDefaultPrecision, NativeMethods.ClearTypeQuality,
            NativeMethods.DefaultPitch | NativeMethods.FfDontCare, "Segoe UI");
        var oldFont = NativeMethods.SelectObject(dc, font);
        NativeMethods.SetTextColor(dc, color);
        var rect = new NativeMethods.Rect { Left = x, Top = y, Right = x + width, Bottom = y + size + 8 };
        NativeMethods.DrawText(dc, text, text.Length, ref rect,
            NativeMethods.DtSingleLine | NativeMethods.DtVCenter | (alignRight ? NativeMethods.DtRight : NativeMethods.DtLeft));
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
}
