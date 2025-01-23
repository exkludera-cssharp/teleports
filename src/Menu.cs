using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using static Plugin;

public static class Menu
{
    public static void Command(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid)
            return;

        if (!Instance.HasPermission(player))
        {
            Instance.PrintToChat(player, $"{ChatColors.Red}You don't have permission to use this command");
            return;
        }

        Menu.Open(player);
    }

    public static void Open(CCSPlayerController player)
    {
        CenterHtmlMenu Menu = new("Teleports Menu", Instance);

        Menu.AddMenuOption("Create", (player, menuOption) =>
        {
            Teleports.Create(player);
        });

        Menu.AddMenuOption("Delete", (player, menuOption) =>
        {
            Teleports.Delete(player);
        });

        Menu.AddMenuOption("Clear", (player, menuOption) =>
        {
            CenterHtmlMenu ConfirmMenu = new("Confirm", Instance);

            ConfirmMenu.AddMenuOption("NO - keep teleports", (player, menuOption) =>
            {
                MenuManager.OpenCenterHtmlMenu(Instance, player, Menu);
            });

            ConfirmMenu.AddMenuOption("YES - remove teleports", (player, menuOption) =>
            {
                Teleports.Clear();
                MenuManager.OpenCenterHtmlMenu(Instance, player, Menu);
            });

            MenuManager.OpenCenterHtmlMenu(Instance, player, ConfirmMenu);
        });

        Menu.AddMenuOption("Save", (player, menuOption) =>
        {
            Teleports.Save();
        });

        MenuManager.OpenCenterHtmlMenu(Instance, player, Menu);
    }
}