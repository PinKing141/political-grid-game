using Godot;
using System.Collections.Generic;

public partial class HistoryChart : Control
{
    [Signal]
    public delegate void HoverInfoChangedEventHandler(string text);

    private readonly List<float> _stability = new List<float>();
    private readonly List<float> _turnout = new List<float>();
    private readonly List<float> _treasury = new List<float>();
    private readonly List<float> _capital = new List<float>();
    private int _hoverIndex = -1;
    private int _latestTurn;

    public void SetSeries(
        IReadOnlyList<float> stability,
        IReadOnlyList<float> turnout,
        IReadOnlyList<float> treasury,
        IReadOnlyList<float> capital,
        int latestTurn)
    {
        CopySeries(stability, _stability);
        CopySeries(turnout, _turnout);
        CopySeries(treasury, _treasury);
        CopySeries(capital, _capital);
        _latestTurn = latestTurn;

        if (_hoverIndex >= _stability.Count)
        {
            _hoverIndex = -1;
        }

        EmitSignal(SignalName.HoverInfoChanged, BuildHoverText(_hoverIndex));
        QueueRedraw();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseMotion motion)
        {
            return;
        }

        int index = GetIndexFromLocalX(motion.Position.X);
        if (index == _hoverIndex)
        {
            return;
        }

        _hoverIndex = index;
        EmitSignal(SignalName.HoverInfoChanged, BuildHoverText(_hoverIndex));
        QueueRedraw();
    }

    public override void _Notification(int what)
    {
        if (what != NotificationMouseExit)
        {
            return;
        }

        if (_hoverIndex == -1)
        {
            return;
        }

        _hoverIndex = -1;
        EmitSignal(SignalName.HoverInfoChanged, BuildHoverText(_hoverIndex));
        QueueRedraw();
    }

    public override void _Draw()
    {
        Rect2 area = GetRect();
        if (area.Size.X < 10 || area.Size.Y < 10)
        {
            return;
        }

        DrawRect(area, new Color(0.10f, 0.10f, 0.12f, 0.9f), true);
        DrawRect(area, new Color(0.28f, 0.28f, 0.32f), false, 1.0f);

        DrawHorizontalGuides(area, 4);

        DrawSeries(area, _stability, new Color(0.32f, 0.86f, 0.47f), 0f, 100f);
        DrawSeries(area, _turnout, new Color(0.35f, 0.72f, 0.94f), 0f, 1f);

        float treasuryMin = GetMin(_treasury);
        float treasuryMax = GetMax(_treasury);
        if (Mathf.IsEqualApprox(treasuryMin, treasuryMax))
        {
            treasuryMin -= 1f;
            treasuryMax += 1f;
        }

        DrawSeries(area, _treasury, new Color(0.94f, 0.78f, 0.28f), treasuryMin, treasuryMax);
        DrawSeries(area, _capital, new Color(0.78f, 0.45f, 0.93f), 0f, 100f);

        if (_hoverIndex >= 0 && _hoverIndex < _stability.Count)
        {
            float stepX = area.Size.X / Mathf.Max(1, _stability.Count - 1);
            float x = area.Position.X + stepX * _hoverIndex;
            DrawLine(new Vector2(x, area.Position.Y), new Vector2(x, area.Position.Y + area.Size.Y), new Color(1f, 1f, 1f, 0.45f), 1f);

            DrawHoverDot(area, _stability, _hoverIndex, new Color(0.32f, 0.86f, 0.47f), 0f, 100f);
            DrawHoverDot(area, _turnout, _hoverIndex, new Color(0.35f, 0.72f, 0.94f), 0f, 1f);
            DrawHoverDot(area, _treasury, _hoverIndex, new Color(0.94f, 0.78f, 0.28f), treasuryMin, treasuryMax);
            DrawHoverDot(area, _capital, _hoverIndex, new Color(0.78f, 0.45f, 0.93f), 0f, 100f);
        }
    }

    private int GetIndexFromLocalX(float x)
    {
        if (_stability.Count == 0)
        {
            return -1;
        }

        Rect2 area = GetRect();
        if (x < area.Position.X || x > area.Position.X + area.Size.X)
        {
            return -1;
        }

        if (_stability.Count == 1)
        {
            return 0;
        }

        float t = Mathf.Clamp((x - area.Position.X) / Mathf.Max(1f, area.Size.X), 0f, 1f);
        int index = Mathf.RoundToInt(t * (_stability.Count - 1));
        return Mathf.Clamp(index, 0, _stability.Count - 1);
    }

    private string BuildHoverText(int index)
    {
        if (index < 0 || index >= _stability.Count)
        {
            return "Hover chart for turn values";
        }

        int startTurn = Mathf.Max(1, _latestTurn - _stability.Count + 1);
        int turn = startTurn + index;

        return $"Turn {turn} | Stability {_stability[index]:0.0} | Turnout {_turnout[index]:P0} | Treasury {_treasury[index]:0} | Capital {_capital[index]:0.0}";
    }

    private static void CopySeries(IReadOnlyList<float> source, List<float> target)
    {
        target.Clear();
        if (source == null)
        {
            return;
        }

        for (int i = 0; i < source.Count; i++)
        {
            target.Add(source[i]);
        }
    }

    private void DrawHorizontalGuides(Rect2 area, int lines)
    {
        for (int i = 1; i < lines; i++)
        {
            float t = (float)i / lines;
            float y = area.Position.Y + area.Size.Y * t;
            Vector2 from = new Vector2(area.Position.X, y);
            Vector2 to = new Vector2(area.Position.X + area.Size.X, y);
            DrawLine(from, to, new Color(0.20f, 0.20f, 0.24f), 1f);
        }
    }

    private void DrawSeries(Rect2 area, List<float> series, Color color, float minValue, float maxValue)
    {
        if (series.Count < 2)
        {
            return;
        }

        float range = Mathf.Max(0.0001f, maxValue - minValue);
        float stepX = area.Size.X / Mathf.Max(1, series.Count - 1);

        Vector2 prev = ToPoint(area, 0, series[0], minValue, range, stepX);
        for (int i = 1; i < series.Count; i++)
        {
            Vector2 current = ToPoint(area, i, series[i], minValue, range, stepX);
            DrawLine(prev, current, color, 2f);
            prev = current;
        }
    }

    private void DrawHoverDot(Rect2 area, List<float> series, int index, Color color, float minValue, float maxValue)
    {
        if (series.Count == 0 || index < 0 || index >= series.Count)
        {
            return;
        }

        float range = Mathf.Max(0.0001f, maxValue - minValue);
        float stepX = area.Size.X / Mathf.Max(1, series.Count - 1);
        Vector2 point = ToPoint(area, index, series[index], minValue, range, stepX);

        DrawCircle(point, 3.5f, color);
        DrawCircle(point, 1.75f, Colors.White);
    }

    private static Vector2 ToPoint(Rect2 area, int index, float value, float minValue, float range, float stepX)
    {
        float x = area.Position.X + index * stepX;
        float yNormalized = Mathf.Clamp((value - minValue) / range, 0f, 1f);
        float y = area.Position.Y + area.Size.Y * (1f - yNormalized);
        return new Vector2(x, y);
    }

    private static float GetMin(List<float> values)
    {
        if (values.Count == 0)
        {
            return 0f;
        }

        float min = values[0];
        for (int i = 1; i < values.Count; i++)
        {
            min = Mathf.Min(min, values[i]);
        }

        return min;
    }

    private static float GetMax(List<float> values)
    {
        if (values.Count == 0)
        {
            return 0f;
        }

        float max = values[0];
        for (int i = 1; i < values.Count; i++)
        {
            max = Mathf.Max(max, values[i]);
        }

        return max;
    }
}
