
using System.Collections.Generic;
using System.IO;

namespace GameModifiers.Modifiers;

public abstract class GameModifierBase
{
    public virtual string Name { get; protected set; } = "Unnamed";
    public virtual string Description { get; protected set; } = "";
    public virtual bool SupportsRandomRounds { get; protected set; } = false;
    public virtual bool IsRegistered { get; protected set; } = true;
    public virtual bool IsActive { get; protected set; } = false;
    public virtual HashSet<string> IncompatibleModifiers { get; protected set; } = new HashSet<string>();
    public GameModifiersCore? Core { get; protected set; } = null;
    public ModifierConfig? Config { get; protected set; } = null;

    public virtual void Registered(GameModifiersCore? core)
    {
        if (core == null)
        {
            return;
        }

        Core = core;

        var pluginConfigPath = Path.Combine(GameModifiersUtils.GetPluginPath(core.ModulePath), "ModifierConfig");
        if (TryParseConfigPath(pluginConfigPath) == false)
        {
            var configPath = Path.Combine(GameModifiersUtils.GetConfigPath(core.ModulePath), "ModifierConfig");
            TryParseConfigPath(configPath);
        }
    }

    public virtual void Unregistered(GameModifiersCore? core)
    {
        Core = null;
    }

    public virtual void Enabled()
    {
        IsActive = true;

        if (Config != null)
        {
            Config.ApplyConfig();
        }
    }

    public virtual void Disabled()
    {
        IsActive = false;

        if (Config != null)
        {
            Config.RemoveConfig();
        }
    }

    public bool CheckIfIncompatible(GameModifierBase? modifier)
    {
        if (modifier == null)
        {
            return false;
        }

        return IncompatibleModifiers.Contains(modifier.Name);
    }

    public bool CheckIfIncompatibleByName(string modifierName)
    {
        return IncompatibleModifiers.Contains(modifierName);
    }

    private bool TryParseConfigPath(string path)
    {
        if (Directory.Exists(path) == false)
        {
            Directory.CreateDirectory(path);
            return false;
        }

        var configFiles = Directory.GetFiles(path, "*.cfg");
        foreach (var configFile in configFiles)
        {
            if (Path.GetFileNameWithoutExtension(configFile) == Name)
            {
                var tempConfig = new ModifierConfig();
                if (tempConfig.ParseConfigFile(configFile))
                {
                    Config = tempConfig;
                    return true;
                }

                break;
            }
        }

        return false;
    }
}
