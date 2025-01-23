using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

public partial class Teleports
{
    public static void Delete(CCSPlayerController player)
    {
        var entity = instance.GetAimTarget(player);

        if (entity != null)
        {
            var pairToRemove = teleportPairs.FirstOrDefault(pair => pair.Entry.Entity == entity || pair.Exit.Entity == entity);

            if (pairToRemove == null)
            {
                instance.PrintToChat(player, "Could not find teleport pair");
                return;
            }

            var entryEntity = pairToRemove.Entry.Entity;
            if (entryEntity != null && entryEntity.IsValid)
            {
                entryEntity.Remove();

                var entryTrigger = Triggers.FirstOrDefault(kvp => kvp.Value == entryEntity).Key;
                if (entryTrigger != null)
                {
                    entryTrigger.Remove();
                    Triggers.Remove(entryTrigger);
                }
            }

            var exitEntity = pairToRemove.Exit.Entity;
            if (exitEntity != null && exitEntity.IsValid)
            {
                exitEntity.Remove();

                var exitTrigger = Triggers.FirstOrDefault(kvp => kvp.Value == exitEntity).Key;
                if (exitTrigger != null)
                {
                    exitTrigger.Remove();
                    Triggers.Remove(exitTrigger);
                }
            }

            teleportPairs.Remove(pairToRemove);

            instance.PrintToChat(player, $"Deleted teleport pair");
        }

        else instance.PrintToChat(player, "Could not find a teleport to delete");
    }

    public static string savedPath = "";
    public static void Save()
    {
        if (string.IsNullOrEmpty(savedPath))
        {
            instance.Logger.LogError("Save path is not defined.");
            return;
        }

        if (!File.Exists(savedPath))
        {
            using (FileStream fs = File.Create(savedPath))
            {
                instance.Logger.LogInformation($"File does not exist, creating one ({savedPath})");
                fs.Close();
            }
        }

        try
        {
            var pairedDataList = new List<TeleportPairDTO>();

            foreach (var pair in teleportPairs)
            {
                if (pair.Entry != null && pair.Exit != null)
                {
                    pairedDataList.Add(new TeleportPairDTO
                    {
                        Entry = new SavedData
                        {
                            Name = pair.Entry.Name,
                            Model = pair.Entry.Model,
                            Position = new VectorDTO(pair.Entry.Entity.AbsOrigin!),
                            Rotation = new QAngleDTO(pair.Entry.Entity.AbsRotation!)
                        },
                        Exit = new SavedData
                        {
                            Name = pair.Exit.Name,
                            Model = pair.Exit.Model,
                            Position = new VectorDTO(pair.Exit.Entity.AbsOrigin!),
                            Rotation = new QAngleDTO(pair.Exit.Entity.AbsRotation!)
                        }
                    });
                }
            }

            if (pairedDataList.Count == 0)
            {
                instance.PrintToChatAll($"{ChatColors.Red}No teleport pairs to save");
                return;
            }

            var jsonString = JsonSerializer.Serialize(pairedDataList, new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(savedPath, jsonString);

            instance.PrintToChatAll($"Saved {ChatColors.White}{pairedDataList.Count} {ChatColors.Grey}teleport pair{(pairedDataList.Count == 1 ? "" : "s")} on {ChatColors.White}{instance.GetMapName()}");
        }
        catch (Exception ex)
        {
            instance.Logger.LogError($"Failed to save teleports: {ex.Message}");
        }
    }

    public static void Clear()
    {
        foreach (var entities in Utilities.GetAllEntities().Where(r => r.DesignerName == "prop_physics_override" || r.DesignerName == "trigger_multiple"))
        {
            if (entities == null || !entities.IsValid || entities.Entity == null)
                continue;

            if (!String.IsNullOrEmpty(entities.Entity.Name) && entities.Entity.Name.StartsWith("teleport_"))
                entities.Remove();
        }

        instance.Timers.Clear();
        teleportPairs.Clear();
        Triggers.Clear();
        instance.playerCooldowns.Clear();
    }
}
