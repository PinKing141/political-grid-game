using Godot;
using System.Collections.Generic;

public partial class DraggablePanel : PanelContainer
{
    [Signal]
    public delegate void LayoutChangedEventHandler();

    [Export] public string LayoutId = string.Empty;
    [Export] public bool PersistPosition = true;
    [Export] public bool EnableSnap = true;
    [Export] public int SnapSize = 16;
    [Export] public bool AutoFitToScreen = true;
    [Export] public int MinPanelWidth = 220;
    [Export] public int MinPanelHeight = 140;
    [Export] public bool EnableResize = true;
    [Export] public int ResizeHandleSize = 14;

    private bool _dragging;
    private bool _resizing;
    private bool _hoverResizeHandle;
    private Vector2 _dragOffset;
    private Vector2 _resizeStartMouseGlobal;
    private Vector2 _resizeStartSize;
    private const string LayoutFilePath = "user://ui_layout.cfg";
    private const string LayoutSection = "panels";
    private const string LayoutSizeSection = "panel_sizes";
    private static readonly HashSet<ulong> ResolvedParentIds = new HashSet<ulong>();
    private static readonly HashSet<ulong> ResizeReflowPendingParentIds = new HashSet<ulong>();

    public override void _Ready()
    {
        GetViewport().SizeChanged += OnViewportSizeChanged;
        CallDeferred(nameof(LoadAndClampInitialPosition));
        CallDeferred(nameof(RequestSiblingOverlapResolve));
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (!EnableResize)
        {
            return;
        }

        DrawResizeGrip();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationMouseExit)
        {
            _hoverResizeHandle = false;
            MouseDefaultCursorShape = CursorShape.Arrow;
            QueueRedraw();
        }
    }

    public override void _ExitTree()
    {
        if (GetViewport() != null)
        {
            GetViewport().SizeChanged -= OnViewportSizeChanged;
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left)
        {
            if (mouseButton.Pressed)
            {
                if (EnableResize && IsInResizeHandle(mouseButton.Position))
                {
                    _resizing = true;
                    _hoverResizeHandle = true;
                    MouseDefaultCursorShape = CursorShape.Fdiagsize;
                    _dragging = false;
                    _resizeStartMouseGlobal = mouseButton.GlobalPosition;
                    _resizeStartSize = Size;
                    QueueRedraw();
                    AcceptEvent();
                    return;
                }

                _dragging = true;
                _resizing = false;
                _hoverResizeHandle = false;
                MouseDefaultCursorShape = CursorShape.Arrow;
                _dragOffset = mouseButton.GlobalPosition - GlobalPosition;
                QueueRedraw();
                AcceptEvent();
            }
            else
            {
                bool movedOrResized = _dragging || _resizing;
                _dragging = false;
                _resizing = false;
                _hoverResizeHandle = false;
                MouseDefaultCursorShape = CursorShape.Arrow;
                Position = ClampToParentBounds(ApplySnap(ClampToParentBounds(Position)));
                SavePosition();
                SaveSize();
                if (movedOrResized)
                {
                    EmitSignal(SignalName.LayoutChanged);
                }

                QueueRedraw();
                AcceptEvent();
            }

            return;
        }

        if (@event is not InputEventMouseMotion motion)
        {
            return;
        }

        bool hoverNow = EnableResize && IsInResizeHandle(motion.Position);
        if (hoverNow != _hoverResizeHandle && !_dragging && !_resizing)
        {
            _hoverResizeHandle = hoverNow;
            MouseDefaultCursorShape = hoverNow ? CursorShape.Fdiagsize : CursorShape.Arrow;
            QueueRedraw();
        }

        if (_resizing)
        {
            Vector2 delta = motion.GlobalPosition - _resizeStartMouseGlobal;
            Vector2 desiredSize = _resizeStartSize + delta;
            Size = ClampSizeToBounds(ApplySnapToSize(desiredSize));
            QueueRedraw();
            AcceptEvent();
            return;
        }

        if (!_dragging)
        {
            return;
        }

        Vector2 desiredGlobal = motion.GlobalPosition - _dragOffset;
        Vector2 localTarget = desiredGlobal;
        if (GetParent() is Control parentControl)
        {
            localTarget = desiredGlobal - parentControl.GlobalPosition;
        }

        Position = ClampToParentBounds(localTarget);
        QueueRedraw();
        AcceptEvent();
    }

    private void LoadAndClampInitialPosition()
    {
        if (PersistPosition)
        {
            TryLoadSavedPosition();
            TryLoadSavedSize();
        }

        if (AutoFitToScreen)
        {
            FitSizeToBounds(GetBoundsSize());
        }

        Position = ClampToParentBounds(ApplySnap(ClampToParentBounds(Position)));
    }

    private void RequestSiblingOverlapResolve()
    {
        CallDeferred(nameof(ResolveSiblingOverlapsOnce));
    }

    private void OnViewportSizeChanged()
    {
        if (GetParent() == null)
        {
            return;
        }

        ulong parentId = GetParent().GetInstanceId();
        if (!ResizeReflowPendingParentIds.Add(parentId))
        {
            return;
        }

        CallDeferred(nameof(ApplyResizeReflow));
    }

    private void ApplyResizeReflow()
    {
        if (GetParent() == null)
        {
            return;
        }

        ulong parentId = GetParent().GetInstanceId();
        try
        {
            List<DraggablePanel> panels = GetSiblingPanels();
            Dictionary<DraggablePanel, Vector2> originalPositions = new Dictionary<DraggablePanel, Vector2>();
            Dictionary<DraggablePanel, Vector2> originalSizes = new Dictionary<DraggablePanel, Vector2>();

            foreach (DraggablePanel panel in panels)
            {
                originalPositions[panel] = panel.Position;
                originalSizes[panel] = panel.Size;

                if (panel.AutoFitToScreen)
                {
                    panel.FitSizeToBounds(panel.GetBoundsSize());
                }

                Vector2 clamped = panel.ClampToParentBounds(panel.Position);
                if (panel.EnableSnap)
                {
                    clamped = panel.ClampToParentBounds(panel.ApplySnap(clamped));
                }

                panel.Position = clamped;
            }

            if (panels.Count >= 2)
            {
                ResolveClusterTranslations(panels);
                ResolveRemainingOverlapsIndividually(panels);
            }

            foreach (DraggablePanel panel in panels)
            {
                bool moved = originalPositions.TryGetValue(panel, out Vector2 start) && start != panel.Position;
                bool resized = originalSizes.TryGetValue(panel, out Vector2 startSize) && startSize != panel.Size;
                if (moved || resized)
                {
                    if (moved)
                    {
                        panel.SavePosition();
                    }

                    if (resized)
                    {
                        panel.SaveSize();
                    }

                    panel.EmitSignal(SignalName.LayoutChanged);
                }
            }
        }
        finally
        {
            ResizeReflowPendingParentIds.Remove(parentId);
        }
    }

    private void ResolveSiblingOverlapsOnce()
    {
        if (GetParent() == null)
        {
            return;
        }

        ulong parentId = GetParent().GetInstanceId();
        if (!ResolvedParentIds.Add(parentId))
        {
            return;
        }

        List<DraggablePanel> panels = GetSiblingPanels();
        if (panels.Count < 2)
        {
            return;
        }

        ResolveClusterTranslations(panels);
        ResolveRemainingOverlapsIndividually(panels);
    }

    private List<DraggablePanel> GetSiblingPanels()
    {
        List<DraggablePanel> panels = new List<DraggablePanel>();
        if (GetParent() == null)
        {
            return panels;
        }

        foreach (Node child in GetParent().GetChildren())
        {
            if (child is DraggablePanel panel)
            {
                panels.Add(panel);
            }
        }

        return panels;
    }

    private void ResolveClusterTranslations(List<DraggablePanel> panels)
    {
        HashSet<DraggablePanel> visited = new HashSet<DraggablePanel>();

        foreach (DraggablePanel panel in panels)
        {
            if (visited.Contains(panel))
            {
                continue;
            }

            List<DraggablePanel> cluster = BuildOverlapCluster(panel, panels);
            foreach (DraggablePanel member in cluster)
            {
                visited.Add(member);
            }

            if (cluster.Count < 2)
            {
                continue;
            }

            TryMoveClusterToNearestSpot(cluster, panels);
        }
    }

    private static List<DraggablePanel> BuildOverlapCluster(DraggablePanel seed, List<DraggablePanel> panels)
    {
        List<DraggablePanel> cluster = new List<DraggablePanel>();
        Queue<DraggablePanel> queue = new Queue<DraggablePanel>();
        HashSet<DraggablePanel> seen = new HashSet<DraggablePanel>();

        queue.Enqueue(seed);
        seen.Add(seed);

        while (queue.Count > 0)
        {
            DraggablePanel current = queue.Dequeue();
            cluster.Add(current);
            Rect2 currentRect = current.GetRect();

            foreach (DraggablePanel candidate in panels)
            {
                if (seen.Contains(candidate))
                {
                    continue;
                }

                if (!currentRect.Intersects(candidate.GetRect()))
                {
                    continue;
                }

                seen.Add(candidate);
                queue.Enqueue(candidate);
            }
        }

        return cluster;
    }

    private bool TryMoveClusterToNearestSpot(List<DraggablePanel> cluster, List<DraggablePanel> allPanels)
    {
        if (GetParent() == null)
        {
            return false;
        }

        Vector2 bounds = GetParent() is Control parentControl ? parentControl.Size : GetViewportRect().Size;
        Dictionary<DraggablePanel, Vector2> startPositions = new Dictionary<DraggablePanel, Vector2>();
        foreach (DraggablePanel panel in cluster)
        {
            startPositions[panel] = panel.Position;
        }

        Rect2 clusterBounds = GetClusterBounds(cluster, startPositions);
        int step = GetClusterStep(cluster);
        int maxRadius = Mathf.CeilToInt(Mathf.Max(bounds.X, bounds.Y) / step) + 2;

        HashSet<Vector2I> tested = new HashSet<Vector2I>();
        Vector2 bestDelta = Vector2.Zero;
        float bestDistanceSq = float.MaxValue;

        for (int radius = 1; radius <= maxRadius; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != radius)
                    {
                        continue;
                    }

                    Vector2I key = new Vector2I(dx, dy);
                    if (!tested.Add(key))
                    {
                        continue;
                    }

                    Vector2 rawDelta = new Vector2(dx * step, dy * step);
                    Vector2 candidateDelta = ClampClusterDelta(rawDelta, clusterBounds, bounds);
                    if (candidateDelta == Vector2.Zero)
                    {
                        continue;
                    }

                    if (IntersectsOutsideCluster(cluster, allPanels, startPositions, candidateDelta))
                    {
                        continue;
                    }

                    float distanceSq = candidateDelta.LengthSquared();
                    if (distanceSq < bestDistanceSq)
                    {
                        bestDistanceSq = distanceSq;
                        bestDelta = candidateDelta;
                    }
                }
            }

            if (bestDistanceSq < float.MaxValue)
            {
                foreach (DraggablePanel panel in cluster)
                {
                    panel.Position = startPositions[panel] + bestDelta;
                }

                return true;
            }
        }

        return false;
    }

    private void ResolveRemainingOverlapsIndividually(List<DraggablePanel> panels)
    {
        int step = Mathf.Max(1, EnableSnap ? SnapSize : 16);
        int maxPasses = panels.Count * 4;

        for (int pass = 0; pass < maxPasses; pass++)
        {
            bool changed = false;

            foreach (DraggablePanel panel in panels)
            {
                if (!OverlapsAnyAt(panel, panel.Position, panels))
                {
                    continue;
                }

                Vector2 nearest = FindNearestFreePosition(panel, panel.Position, panels, step);
                if (nearest != panel.Position)
                {
                    panel.Position = nearest;
                    changed = true;
                }
            }

            if (!changed)
            {
                return;
            }
        }
    }

    private Vector2 FindNearestFreePosition(DraggablePanel panel, Vector2 start, List<DraggablePanel> panels, int step)
    {
        Vector2 bounds = GetParent() is Control parentControl ? parentControl.Size : GetViewportRect().Size;
        int maxRadius = Mathf.CeilToInt(Mathf.Max(bounds.X, bounds.Y) / step) + 2;

        HashSet<Vector2I> tested = new HashSet<Vector2I>();
        for (int radius = 1; radius <= maxRadius; radius++)
        {
            Vector2 best = panel.Position;
            float bestDistanceSq = float.MaxValue;
            bool found = false;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != radius)
                    {
                        continue;
                    }

                    Vector2I key = new Vector2I(dx, dy);
                    if (!tested.Add(key))
                    {
                        continue;
                    }

                    Vector2 candidate = start + new Vector2(dx * step, dy * step);
                    candidate = panel.ClampToParentBounds(candidate);
                    if (panel.EnableSnap)
                    {
                        candidate = panel.ClampToParentBounds(panel.ApplySnap(candidate));
                    }

                    if (OverlapsAnyAt(panel, candidate, panels))
                    {
                        continue;
                    }

                    float distanceSq = start.DistanceSquaredTo(candidate);
                    if (distanceSq < bestDistanceSq)
                    {
                        bestDistanceSq = distanceSq;
                        best = candidate;
                        found = true;
                    }
                }
            }

            if (found)
            {
                return best;
            }
        }

        return panel.ClampToParentBounds(Vector2.Zero);
    }

    private static bool OverlapsAnyAt(DraggablePanel movingPanel, Vector2 candidatePosition, List<DraggablePanel> panels)
    {
        Rect2 candidateRect = new Rect2(candidatePosition, movingPanel.Size);
        foreach (DraggablePanel panel in panels)
        {
            if (panel == movingPanel)
            {
                continue;
            }

            if (candidateRect.Intersects(panel.GetRect()))
            {
                return true;
            }
        }

        return false;
    }

    private static Rect2 GetClusterBounds(List<DraggablePanel> cluster, Dictionary<DraggablePanel, Vector2> positions)
    {
        Vector2 topLeft = positions[cluster[0]];
        Vector2 bottomRight = topLeft + cluster[0].Size;

        for (int i = 1; i < cluster.Count; i++)
        {
            DraggablePanel panel = cluster[i];
            Vector2 pos = positions[panel];
            Vector2 end = pos + panel.Size;

            topLeft = new Vector2(Mathf.Min(topLeft.X, pos.X), Mathf.Min(topLeft.Y, pos.Y));
            bottomRight = new Vector2(Mathf.Max(bottomRight.X, end.X), Mathf.Max(bottomRight.Y, end.Y));
        }

        return new Rect2(topLeft, bottomRight - topLeft);
    }

    private static int GetClusterStep(List<DraggablePanel> cluster)
    {
        int step = 16;
        for (int i = 0; i < cluster.Count; i++)
        {
            if (cluster[i].EnableSnap)
            {
                step = Mathf.Min(step, Mathf.Max(1, cluster[i].SnapSize));
            }
        }

        return Mathf.Max(1, step);
    }

    private static Vector2 ClampClusterDelta(Vector2 delta, Rect2 clusterBounds, Vector2 bounds)
    {
        float minDx = -clusterBounds.Position.X;
        float maxDx = bounds.X - (clusterBounds.Position.X + clusterBounds.Size.X);
        float minDy = -clusterBounds.Position.Y;
        float maxDy = bounds.Y - (clusterBounds.Position.Y + clusterBounds.Size.Y);

        return new Vector2(
            Mathf.Clamp(delta.X, minDx, maxDx),
            Mathf.Clamp(delta.Y, minDy, maxDy));
    }

    private static bool IntersectsOutsideCluster(
        List<DraggablePanel> cluster,
        List<DraggablePanel> allPanels,
        Dictionary<DraggablePanel, Vector2> startPositions,
        Vector2 delta)
    {
        HashSet<DraggablePanel> clusterSet = new HashSet<DraggablePanel>(cluster);

        foreach (DraggablePanel moving in cluster)
        {
            Rect2 movedRect = new Rect2(startPositions[moving] + delta, moving.Size);
            foreach (DraggablePanel other in allPanels)
            {
                if (clusterSet.Contains(other))
                {
                    continue;
                }

                if (movedRect.Intersects(other.GetRect()))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private Vector2 ClampToParentBounds(Vector2 candidate)
    {
        Vector2 boundsSize = GetBoundsSize();

        Vector2 max = boundsSize - Size;
        return new Vector2(
            Mathf.Clamp(candidate.X, 0f, Mathf.Max(0f, max.X)),
            Mathf.Clamp(candidate.Y, 0f, Mathf.Max(0f, max.Y)));
    }

    private Vector2 ClampSizeToBounds(Vector2 candidate)
    {
        Vector2 boundsSize = GetBoundsSize();
        float maxWidth = Mathf.Max(64f, boundsSize.X - Position.X);
        float maxHeight = Mathf.Max(64f, boundsSize.Y - Position.Y);

        return new Vector2(
            Mathf.Clamp(candidate.X, Mathf.Max(64f, MinPanelWidth), maxWidth),
            Mathf.Clamp(candidate.Y, Mathf.Max(64f, MinPanelHeight), maxHeight));
    }

    private Vector2 GetBoundsSize()
    {
        if (GetParent() is Control parentControl)
        {
            return parentControl.Size;
        }

        return GetViewportRect().Size;
    }

    private void FitSizeToBounds(Vector2 boundsSize)
    {
        float maxWidth = Mathf.Max(64f, boundsSize.X - 8f);
        float maxHeight = Mathf.Max(64f, boundsSize.Y - 8f);
        Vector2 target = new Vector2(
            Mathf.Min(Size.X, maxWidth),
            Mathf.Min(Size.Y, maxHeight));

        if (target != Size)
        {
            Size = target;
        }
    }

    private Vector2 ApplySnap(Vector2 value)
    {
        if (!EnableSnap)
        {
            return value;
        }

        int grid = Mathf.Max(1, SnapSize);
        return new Vector2(
            Mathf.Round(value.X / grid) * grid,
            Mathf.Round(value.Y / grid) * grid);
    }

    private Vector2 ApplySnapToSize(Vector2 value)
    {
        if (!EnableSnap)
        {
            return value;
        }

        int grid = Mathf.Max(1, SnapSize);
        return new Vector2(
            Mathf.Max(Mathf.Max(64f, MinPanelWidth), Mathf.Round(value.X / grid) * grid),
            Mathf.Max(Mathf.Max(64f, MinPanelHeight), Mathf.Round(value.Y / grid) * grid));
    }

    private bool IsInResizeHandle(Vector2 localMousePosition)
    {
        float handle = Mathf.Max(8f, ResizeHandleSize);
        return localMousePosition.X >= Size.X - handle && localMousePosition.Y >= Size.Y - handle;
    }

    private void DrawResizeGrip()
    {
        float handle = Mathf.Max(8f, ResizeHandleSize);
        float inset = 4f;
        float spacing = Mathf.Max(3f, handle * 0.24f);
        float gripLength = Mathf.Max(5f, handle * 0.55f);

        Color baseColor = GetThemeColor("font_color", "Label");
        float alpha = _hoverResizeHandle || _resizing ? 0.95f : 0.55f;
        Color color = new Color(baseColor.R, baseColor.G, baseColor.B, alpha);

        for (int i = 0; i < 3; i++)
        {
            float offset = i * spacing;
            Vector2 from = new Vector2(Size.X - inset - offset - gripLength, Size.Y - inset);
            Vector2 to = new Vector2(Size.X - inset, Size.Y - inset - offset - gripLength);
            DrawLine(from, to, color, 1.6f, true);
        }
    }

    private void TryLoadSavedPosition()
    {
        if (string.IsNullOrWhiteSpace(LayoutId))
        {
            return;
        }

        ConfigFile config = new ConfigFile();
        Error result = config.Load(LayoutFilePath);
        if (result != Error.Ok)
        {
            return;
        }

        Variant stored = config.GetValue(LayoutSection, LayoutId, Position);
        if (stored.VariantType == Variant.Type.Vector2)
        {
            Position = (Vector2)stored;
        }
    }

    private void SavePosition()
    {
        if (!PersistPosition || string.IsNullOrWhiteSpace(LayoutId))
        {
            return;
        }

        ConfigFile config = new ConfigFile();
        config.Load(LayoutFilePath);
        config.SetValue(LayoutSection, LayoutId, Position);
        config.Save(LayoutFilePath);
    }

    private void TryLoadSavedSize()
    {
        if (string.IsNullOrWhiteSpace(LayoutId))
        {
            return;
        }

        ConfigFile config = new ConfigFile();
        Error result = config.Load(LayoutFilePath);
        if (result != Error.Ok)
        {
            return;
        }

        Variant stored = config.GetValue(LayoutSizeSection, LayoutId, Size);
        if (stored.VariantType == Variant.Type.Vector2)
        {
            Size = ClampSizeToBounds((Vector2)stored);
        }
    }

    private void SaveSize()
    {
        if (!PersistPosition || string.IsNullOrWhiteSpace(LayoutId))
        {
            return;
        }

        ConfigFile config = new ConfigFile();
        config.Load(LayoutFilePath);
        config.SetValue(LayoutSizeSection, LayoutId, Size);
        config.Save(LayoutFilePath);
    }
}
