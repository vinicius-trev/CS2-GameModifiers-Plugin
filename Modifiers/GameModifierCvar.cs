
using System;
using System.Collections.Generic;
using System.Linq;

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

using GameModifiers.Types;

namespace GameModifiers.Modifiers;

class ModifierCvarConfig : ModifierConfig
{
    public string Name { get; private set; } = "";
    public string Description { get; private set; } = "";
    public bool SupportsRandomRounds { get; private set; } = false;
    public HashSet<string> IncompatibleModifiers { get; private set; } = new HashSet<string>();

    public override bool ParseConfigLine(string line, bool isClientCommand)
    {
        if (isClientCommand == false)
        {
            string[] lineParts = line.Split(new char[] {' '}, 2, StringSplitOptions.RemoveEmptyEntries);
            if (lineParts.Length == 2)
            {
                // Expected format "modifier_name SOME_NAME". Will be named "Unnamed Config Modifier" if not found.
                if (lineParts[0].Equals("modifier_name", StringComparison.OrdinalIgnoreCase))
                {
                    Name = lineParts[1].Trim();
                    return true;
                }

                // Expected format "modifier_description SOME_DESCRIPTION".
                if (lineParts[0].Equals("modifier_description", StringComparison.OrdinalIgnoreCase))
                {
                    Description = lineParts[1].Trim();
                    return true;
                }

                // Expected format "supports_random_rounds TRUE/FALSE".
                if (lineParts[0].Equals("supports_random_rounds", StringComparison.OrdinalIgnoreCase))
                {
                    if (bool.TryParse(lineParts[1].Trim(), out bool supports))
                    {
                        SupportsRandomRounds = supports;

                    }

                    return true;
                }

                // Expected format "incompatible_modifiers [modifiername1, modifiername2]".
                if (lineParts[0].Equals("incompatible_modifiers", StringComparison.OrdinalIgnoreCase))
                {
                    string incompatibleModifiers = lineParts[1].Trim().Trim('[', ']');
                    IncompatibleModifiers = incompatibleModifiers.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(modifier => modifier.Trim())
                        .ToHashSet();

                    return true;
                }
            }
        }

        return base.ParseConfigLine(line, isClientCommand);
    }
}

public class GameModifierCvar : GameModifierBase
{
    private ModifierCvarConfig? _config = null;
    public override bool IsRegistered { get; protected set; } = false;

    public override void Enabled()
    {
        base.Enabled();

        if (Core != null)
        {
            Core.RegisterListener<Listeners.OnClientConnected>(OnClientConnected);
            Core.RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        }

        if (_config != null)
        {
            _config.ApplyConfig();
        }
    }

    public override void Disabled()
    {
        base.Disabled();

        if (Core != null)
        {
            Core.RemoveListener<Listeners.OnClientConnected>(OnClientConnected);
            Core.RemoveListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        }

        if (_config != null)
        {
            _config.RemoveConfig();
        }
    }

    public bool ParseConfigFile(string filePath)
    {
        var tempConfig = new ModifierCvarConfig();
        if (tempConfig.ParseConfigFile(filePath) == false)
        {
            return false;
        }

        _config = tempConfig;

        Name = _config.Name;
        Description = _config.Description;
        SupportsRandomRounds = _config.SupportsRandomRounds;
        IncompatibleModifiers = _config.IncompatibleModifiers;

        if (Name == "Unnamed")
        {
            Console.WriteLine($"[GameModifierCvar::ParseConfigFile] Empty or non-existent modifier_name config file {filePath}.");
        }

        IsRegistered = true;
        return true;
    }

    private void OnClientConnected(int slot)
    {
        if (_config != null)
        {
            CCSPlayerController? player = Utilities.GetPlayerFromSlot(slot);
            if (player == null || player.IsValid is not true)
            {
                return;
            }

            _config.ApplyClientConfig(player);
        }
    }

    private void OnClientDisconnect(int slot)
    {
        if (_config != null)
        {
            CCSPlayerController? player = Utilities.GetPlayerFromSlot(slot);
            _config.RemoveClientConfig(player);
        }
    }
}
