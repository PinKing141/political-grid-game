using System;
using System.IO;
using System.Text.Json;
using Godot;

namespace PolisGrid.Core
{
    public static class GameSaver
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static bool TrySave(string userPath, SaveGameData data, out string error)
        {
            error = string.Empty;
            if (data == null)
            {
                error = "Save data is null.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(userPath))
            {
                error = "Save path is empty.";
                return false;
            }

            try
            {
                string absolutePath = ProjectSettings.GlobalizePath(userPath);
                string directory = Path.GetDirectoryName(absolutePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonSerializer.Serialize(data, JsonOptions);
                File.WriteAllText(absolutePath, json);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool TryLoad(string userPath, out SaveGameData data, out string error)
        {
            data = null;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(userPath))
            {
                error = "Save path is empty.";
                return false;
            }

            try
            {
                string absolutePath = ProjectSettings.GlobalizePath(userPath);
                if (!File.Exists(absolutePath))
                {
                    error = "Save file does not exist.";
                    return false;
                }

                string json = File.ReadAllText(absolutePath);
                data = JsonSerializer.Deserialize<SaveGameData>(json, JsonOptions);
                if (data == null)
                {
                    error = "Save file is empty or invalid.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
