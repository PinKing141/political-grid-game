using Godot;
using System.Collections.Generic;
using PolisGrid.Core;

public partial class HeatmapView : Node2D
{
	[Signal]
	public delegate void TileSelectedEventHandler(int x, int y);

	public enum HeatmapMode
	{
		Stability,
		Turnout,
		PartyStronghold,
		Wealth,
		PopulationDensity,
		DominantIdeology
	}

	[Export] public NodePath SimulationManagerPath;
	[Export] public int CellSize = 20;
	[Export] public bool PreventTileShrink = true;
	[Export] public bool LockTileSizeExact = true;
	[Export] public HeatmapMode Mode = HeatmapMode.Stability;
	[Export] public bool AutoFitToViewport = true;
	[Export] public int MinCellSize = 8;
	[Export] public int MaxCellSize = 64;
	[Export] public NodePath HudRootPath;
	[Export] public int HudPadding = 8;
	[Export] public int MinVisibleMapWidth = 260;

	private SimulationManager _simulationManager;
	private readonly List<DraggablePanel> _observedPanels = new List<DraggablePanel>();
	private bool _hasSelection;
	private Vector2I _selectedTile;
	private int _effectiveCellSize;
	private Vector2 _drawOrigin;

	public override void _Ready()
	{
		_simulationManager = ResolveSimulationManager();
		if (_simulationManager != null)
		{
			_simulationManager.TurnCompleted += OnTurnCompleted;
		}

		GetViewport().SizeChanged += OnViewportSizeChanged;
		ConnectHudPanelSignals();
		RecalculateLayout();

		QueueRedraw();
	}

	public override void _ExitTree()
	{
		if (_simulationManager != null)
		{
			_simulationManager.TurnCompleted -= OnTurnCompleted;
		}

		if (GetViewport() != null)
		{
			GetViewport().SizeChanged -= OnViewportSizeChanged;
		}

		DisconnectHudPanelSignals();
	}

	public void SetMode(int mode)
	{
		if (mode < 0 || mode > 5)
		{
			return;
		}

		Mode = (HeatmapMode)mode;
		QueueRedraw();
	}

	public void Refresh()
	{
		QueueRedraw();
	}

	public bool IsTileSizeLocked()
	{
		return LockTileSizeExact;
	}

	public void SetTileSizeLock(bool locked)
	{
		if (LockTileSizeExact == locked)
		{
			return;
		}

		LockTileSizeExact = locked;
		RecalculateLayout();
		QueueRedraw();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mouseButton)
		{
			return;
		}

		if (!mouseButton.Pressed || mouseButton.ButtonIndex != MouseButton.Left)
		{
			return;
		}

		if (_simulationManager == null || _simulationManager.Grid.Length == 0)
		{
			return;
		}

		Vector2 localPos = mouseButton.Position - _drawOrigin;
		int x = Mathf.FloorToInt(localPos.X / _effectiveCellSize);
		int y = Mathf.FloorToInt(localPos.Y / _effectiveCellSize);

		if (x < 0 || y < 0 || x >= _simulationManager.GridSize.X || y >= _simulationManager.GridSize.Y)
		{
			return;
		}

		_selectedTile = new Vector2I(x, y);
		_hasSelection = true;
		EmitSignal(SignalName.TileSelected, x, y);
		QueueRedraw();
		GetViewport().SetInputAsHandled();
	}

	public override void _Draw()
	{
		if (_simulationManager == null || _simulationManager.Grid.Length == 0)
		{
			return;
		}

		int width = _simulationManager.GridSize.X;
		int height = _simulationManager.GridSize.Y;
		float drawCellSize = Mathf.Max(1f, _effectiveCellSize - 1f);

		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < height; y++)
			{
				DistrictTile tile = _simulationManager.Grid[x, y];
				Rect2 rect = new Rect2(
					_drawOrigin.X + x * _effectiveCellSize,
					_drawOrigin.Y + y * _effectiveCellSize,
					drawCellSize,
					drawCellSize);
				DrawRect(rect, GetTileColor(tile));
			}
		}

		if (_hasSelection)
		{
			Rect2 selectionRect = new Rect2(
				_drawOrigin.X + _selectedTile.X * _effectiveCellSize,
				_drawOrigin.Y + _selectedTile.Y * _effectiveCellSize,
				_effectiveCellSize,
				_effectiveCellSize);
			DrawRect(selectionRect, Colors.White, false, 2f);
		}
	}

	private void OnViewportSizeChanged()
	{
		RecalculateLayout();
		QueueRedraw();
	}

	private void OnHudPanelLayoutChanged()
	{
		RecalculateLayout();
		QueueRedraw();
	}

	private void ConnectHudPanelSignals()
	{
		DisconnectHudPanelSignals();

		Node hudRoot = ResolveHudRoot();
		if (hudRoot == null)
		{
			return;
		}

		CollectDraggablePanels(hudRoot, _observedPanels);
		foreach (DraggablePanel panel in _observedPanels)
		{
			panel.LayoutChanged += OnHudPanelLayoutChanged;
		}
	}

	private void DisconnectHudPanelSignals()
	{
		foreach (DraggablePanel panel in _observedPanels)
		{
			if (panel != null)
			{
				panel.LayoutChanged -= OnHudPanelLayoutChanged;
			}
		}

		_observedPanels.Clear();
	}

	private void RecalculateLayout()
	{
		int fallbackCell = Mathf.Max(1, CellSize);
		_effectiveCellSize = fallbackCell;
		_drawOrigin = Vector2.Zero;

		if (_simulationManager == null || _simulationManager.Grid.Length == 0)
		{
			return;
		}

		if (!AutoFitToViewport)
		{
			return;
		}

		Vector2 viewportSize = GetViewportRect().Size;
		int gridWidth = Mathf.Max(1, _simulationManager.GridSize.X);
		int gridHeight = Mathf.Max(1, _simulationManager.GridSize.Y);

		GetHudHorizontalMargins(viewportSize, out float leftReserved, out float rightReserved);
		float sidePadding = Mathf.Max(0, HudPadding);
		leftReserved = Mathf.Clamp(leftReserved, 0f, viewportSize.X * 0.45f);
		rightReserved = Mathf.Clamp(rightReserved, 0f, viewportSize.X * 0.45f);

		float minMapWidth = Mathf.Clamp(MinVisibleMapWidth, 64, Mathf.FloorToInt(viewportSize.X));
		float allowedReserved = Mathf.Max(0f, viewportSize.X - minMapWidth - sidePadding * 2f);
		float totalReserved = leftReserved + rightReserved;
		if (totalReserved > allowedReserved && totalReserved > 0f)
		{
			float scale = allowedReserved / totalReserved;
			leftReserved *= scale;
			rightReserved *= scale;
		}

		float availableWidth = Mathf.Max(64f, viewportSize.X - leftReserved - rightReserved - sidePadding * 2f);
		float availableHeight = Mathf.Max(64f, viewportSize.Y - sidePadding * 2f);

		int fitByWidth = Mathf.FloorToInt(availableWidth / gridWidth);
		int fitByHeight = Mathf.FloorToInt(availableHeight / gridHeight);
		int fitted = Mathf.Max(1, Mathf.Min(fitByWidth, fitByHeight));

		int baseCell = Mathf.Max(1, CellSize);
		if (LockTileSizeExact)
		{
			_effectiveCellSize = baseCell;

			Vector2 fixedMapPixelSize = new Vector2(gridWidth * _effectiveCellSize, gridHeight * _effectiveCellSize);
			_drawOrigin = new Vector2(
				leftReserved + sidePadding + Mathf.Max(0f, (availableWidth - fixedMapPixelSize.X) * 0.5f),
				sidePadding + Mathf.Max(0f, (availableHeight - fixedMapPixelSize.Y) * 0.5f));
			return;
		}

		if (PreventTileShrink)
		{
			fitted = Mathf.Max(fitted, baseCell);
		}

		int minCell = Mathf.Max(1, MinCellSize);
		int maxCell = Mathf.Max(minCell, MaxCellSize);
		if (PreventTileShrink)
		{
			minCell = Mathf.Max(minCell, baseCell);
			maxCell = Mathf.Max(maxCell, minCell);
		}

		_effectiveCellSize = Mathf.Clamp(fitted, minCell, maxCell);

		Vector2 mapPixelSize = new Vector2(gridWidth * _effectiveCellSize, gridHeight * _effectiveCellSize);
		_drawOrigin = new Vector2(
			leftReserved + sidePadding + Mathf.Max(0f, (availableWidth - mapPixelSize.X) * 0.5f),
			sidePadding + Mathf.Max(0f, (availableHeight - mapPixelSize.Y) * 0.5f));
	}

	private void GetHudHorizontalMargins(Vector2 viewportSize, out float leftReserved, out float rightReserved)
	{
		leftReserved = 0f;
		rightReserved = 0f;

		Node hudRoot = ResolveHudRoot();
		if (hudRoot == null)
		{
			return;
		}

		List<DraggablePanel> panels = new List<DraggablePanel>();
		CollectDraggablePanels(hudRoot, panels);

		float viewportCenterX = viewportSize.X * 0.5f;
		foreach (DraggablePanel panel in panels)
		{
			Rect2 rect = panel.GetGlobalRect();
			float rectLeft = Mathf.Clamp(rect.Position.X, 0f, viewportSize.X);
			float rectRight = Mathf.Clamp(rect.Position.X + rect.Size.X, 0f, viewportSize.X);
			if (rectRight <= rectLeft)
			{
				continue;
			}

			if (rectRight <= viewportCenterX)
			{
				leftReserved = Mathf.Max(leftReserved, rectRight);
			}
			else if (rectLeft >= viewportCenterX)
			{
				rightReserved = Mathf.Max(rightReserved, viewportSize.X - rectLeft);
			}
			else
			{
				float leftUse = rectRight;
				float rightUse = viewportSize.X - rectLeft;
				if (leftUse <= rightUse)
				{
					leftReserved = Mathf.Max(leftReserved, leftUse);
				}
				else
				{
					rightReserved = Mathf.Max(rightReserved, rightUse);
				}
			}
		}
	}

	private Node ResolveHudRoot()
	{
		if (HudRootPath != null && !HudRootPath.IsEmpty)
		{
			Node configuredHud = GetNodeOrNull<Node>(HudRootPath);
			if (configuredHud == null)
			{
				GD.PushWarning($"HeatmapView wiring: '{nameof(HudRootPath)}' path not found ({HudRootPath}).");
			}

			return configuredHud;
		}

		Node hud = GetNodeOrNull<Node>("../HUD");
		if (hud != null)
		{
			return hud;
		}

		return GetTree()?.Root?.FindChild("HUD", true, false);
	}

	private static void CollectDraggablePanels(Node node, List<DraggablePanel> output)
	{
		if (node is DraggablePanel panel)
		{
			output.Add(panel);
		}

		foreach (Node child in node.GetChildren())
		{
			CollectDraggablePanels(child, output);
		}
	}

	private Color GetTileColor(DistrictTile tile)
	{
		switch (Mode)
		{
			case HeatmapMode.Turnout:
				return new Color(0.15f, 0.15f + tile.Turnout * 0.7f, 0.95f - tile.Turnout * 0.55f);
			case HeatmapMode.PartyStronghold:
				Party leadingParty = FindLeadingParty(tile);
				return leadingParty?.Color ?? new Color(0.3f, 0.3f, 0.3f);
			case HeatmapMode.Wealth:
				return GetWealthColor(tile);
			case HeatmapMode.PopulationDensity:
				return GetPopulationDensityColor(tile);
			case HeatmapMode.DominantIdeology:
				return GetDominantIdeologyColor(tile);
			case HeatmapMode.Stability:
			default:
				float t = tile.Stability / 100f;
				return new Color(0.95f - t * 0.75f, 0.20f + t * 0.75f, 0.18f);
		}
	}

	private static Color GetWealthColor(DistrictTile tile)
	{
		float wealth = Mathf.Clamp((tile.AverageIdeology.Economic + 1f) * 0.5f, 0f, 1f);
		Color poorGrey = new Color(0.34f, 0.34f, 0.34f);
		Color richGold = new Color(0.94f, 0.78f, 0.28f);
		return poorGrey.Lerp(richGold, wealth);
	}

	private static Color GetPopulationDensityColor(DistrictTile tile)
	{
		return tile.Density switch
		{
			PopulationDensity.Rural => new Color(0.12f, 0.12f, 0.12f),
			PopulationDensity.Urban => new Color(0.58f, 0.58f, 0.58f),
			PopulationDensity.Metro => new Color(0.92f, 0.92f, 0.92f),
			_ => new Color(0.40f, 0.40f, 0.40f)
		};
	}

	private static Color GetDominantIdeologyColor(DistrictTile tile)
	{
		PoliticalCompass c = tile.AverageIdeology;

		Color economic = c.Economic >= 0f ? new Color(0.95f, 0.72f, 0.18f) : new Color(0.87f, 0.22f, 0.22f);
		Color societal = c.Societal >= 0f ? new Color(0.22f, 0.80f, 0.40f) : new Color(0.58f, 0.22f, 0.74f);
		Color authority = c.Authority >= 0f ? new Color(0.20f, 0.44f, 0.90f) : new Color(0.18f, 0.82f, 0.88f);
		Color diplomatic = c.Diplomatic >= 0f ? new Color(0.94f, 0.30f, 0.70f) : new Color(0.72f, 0.48f, 0.18f);

		float eWeight = Mathf.Abs(c.Economic) + 0.1f;
		float sWeight = Mathf.Abs(c.Societal) + 0.1f;
		float aWeight = Mathf.Abs(c.Authority) + 0.1f;
		float dWeight = Mathf.Abs(c.Diplomatic) + 0.1f;
		float total = eWeight + sWeight + aWeight + dWeight;

		return new Color(
			(economic.R * eWeight + societal.R * sWeight + authority.R * aWeight + diplomatic.R * dWeight) / total,
			(economic.G * eWeight + societal.G * sWeight + authority.G * aWeight + diplomatic.G * dWeight) / total,
			(economic.B * eWeight + societal.B * sWeight + authority.B * aWeight + diplomatic.B * dWeight) / total,
			1f);
	}

	private Party FindLeadingParty(DistrictTile tile)
	{
		if (_simulationManager == null || _simulationManager.Parties.Count == 0)
		{
			return null;
		}

		Party bestParty = null;
		float bestDistance = float.MaxValue;

		foreach (Party party in _simulationManager.Parties)
		{
			float distance = tile.AverageIdeology.DistanceTo(party.Platform);
			if (distance < bestDistance)
			{
				bestDistance = distance;
				bestParty = party;
			}
		}

		return bestParty;
	}

	private SimulationManager ResolveSimulationManager()
	{
		if (SimulationManagerPath != null && !SimulationManagerPath.IsEmpty)
		{
			return GetNodeOrNull<SimulationManager>(SimulationManagerPath);
		}

		var managers = GetTree().GetNodesInGroup("simulation_manager");
		if (managers.Count > 0)
		{
			return managers[0] as SimulationManager;
		}

		return GetNodeOrNull<SimulationManager>("../SimulationManager");
	}

	private void OnTurnCompleted()
	{
		RecalculateLayout();
		QueueRedraw();
	}
}
