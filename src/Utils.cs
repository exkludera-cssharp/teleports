using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.Json;

public partial class Plugin
{
    public bool HasPermission(CCSPlayerController player)
    {
        return string.IsNullOrEmpty(Config.Permission) || AdminManager.PlayerHasPermissions(player, Config.Permission);
    }

    public void PrintToChat(CCSPlayerController player, string message)
    {
        player.PrintToChat($"{Config.Prefix} {ChatColors.Grey}{message}");
    }

    public void PlaySound(CCSPlayerController player, string sound)
    {
        player.ExecuteClientCommand($"play {sound}");
    }

    public void PrintToChatAll(string message)
    {
        Server.PrintToChatAll($"{Config.Prefix} {ChatColors.Grey}{message}");
    }

    public bool IsValidJson(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Logger.LogInformation($"JSON Check: file does not exist ({filePath})");
            return false;
        }

        string fileContent = File.ReadAllText(filePath);

        if (string.IsNullOrWhiteSpace(fileContent))
        {
            Logger.LogError($"JSON Check: file is empty ({filePath})");
            return false;
        }

        try
        {
            JsonDocument.Parse(fileContent);
            return true;
        }
        catch (JsonException)
        {
            Logger.LogError($"JSON Check: invalid content ({filePath})");
            return false;
        }
    }

    public CBaseProp? GetAimTarget(CCSPlayerController player)
    {
        var GameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault()?.GameRules;

        if (GameRules is null)
            return null;

        VirtualFunctionWithReturn<IntPtr, IntPtr, IntPtr> findPickerEntity = new(GameRules.Handle, 27);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) findPickerEntity = new(GameRules.Handle, 28);

        var target = new CBaseProp(findPickerEntity.Invoke(GameRules.Handle, player.Handle));

        if (target != null && target.IsValid && target.Entity != null && target.DesignerName.Contains("prop_physics_override")) return target;

        return null;
    }

    public int GetPlacedCount()
    {
        int teleportCount = 0;

        foreach (var entity in Utilities.GetAllEntities().Where(b => b.DesignerName == "prop_physics_override"))
        {
            if (entity == null || !entity.IsValid || entity.Entity == null)
                continue;

            if (!String.IsNullOrEmpty(entity.Entity.Name) && entity.Entity.Name.StartsWith("teleport"))
                teleportCount++;
        }

        return teleportCount;
    }

    public string GetMapName()
    {
        return Server.MapName.ToString();
    }

    public Color ParseColor(string colorValue)
    {
        var colorParts = colorValue.Split(' ');
        if (colorParts.Length == 4 &&
            int.TryParse(colorParts[0], out var r) &&
            int.TryParse(colorParts[1], out var g) &&
            int.TryParse(colorParts[2], out var b) &&
            int.TryParse(colorParts[3], out var a))
        {
            return Color.FromArgb(a, r, g, b);
        }
        return Color.FromArgb(255, 255, 255, 255);
    }
}