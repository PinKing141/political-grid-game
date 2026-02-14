using Godot;
using PolisGrid.Core;
using System.IO;

public partial class MainMenuController : Control
{
    [Export] public string GameScenePath = "res://Scenes/Core/Main.tscn";
    [Export] public string SavePath = SessionLaunchContext.DefaultSavePath;

    private Button _loadCampaignButton;
    private Control _settingsPanel;
    private Control _creditsPanel;

    public override void _Ready()
    {
        _loadCampaignButton = GetNodeOrNull<Button>("Center/MenuVBox/LoadCampaignButton");
        _settingsPanel = GetNodeOrNull<Control>("SettingsPanel");
        _creditsPanel = GetNodeOrNull<Control>("CreditsPanel");

        if (_settingsPanel != null)
        {
            _settingsPanel.Visible = false;
        }

        if (_creditsPanel != null)
        {
            _creditsPanel.Visible = false;
        }

        UpdateLoadAvailability();
    }

    public void OnNewCampaignPressed()
    {
        SessionLaunchContext.Clear();
        GetTree().ChangeSceneToFile(GameScenePath);
    }

    public void OnLoadCampaignPressed()
    {
        if (!SaveFileExists())
        {
            UpdateLoadAvailability();
            return;
        }

        SessionLaunchContext.RequestLoadCampaign(SavePath);
        GetTree().ChangeSceneToFile(GameScenePath);
    }

    public void OnSettingsPressed()
    {
        if (_settingsPanel != null)
        {
            _settingsPanel.Visible = true;
        }
    }

    public void OnSettingsClosePressed()
    {
        if (_settingsPanel != null)
        {
            _settingsPanel.Visible = false;
        }
    }

    public void OnCreditsPressed()
    {
        if (_creditsPanel != null)
        {
            _creditsPanel.Visible = true;
        }
    }

    public void OnCreditsClosePressed()
    {
        if (_creditsPanel != null)
        {
            _creditsPanel.Visible = false;
        }
    }

    public void OnExitGamePressed()
    {
        GetTree().Quit();
    }

    private void UpdateLoadAvailability()
    {
        if (_loadCampaignButton == null)
        {
            return;
        }

        bool canLoad = SaveFileExists();
        _loadCampaignButton.Disabled = !canLoad;
        _loadCampaignButton.TooltipText = canLoad ? "Load saved campaign." : "No saved campaign found.";
    }

    private bool SaveFileExists()
    {
        string absolute = ProjectSettings.GlobalizePath(string.IsNullOrWhiteSpace(SavePath) ? SessionLaunchContext.DefaultSavePath : SavePath);
        return File.Exists(absolute);
    }
}
