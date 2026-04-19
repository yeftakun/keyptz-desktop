using System.Diagnostics;
using System.Globalization;
using Newtonsoft.Json;

namespace Key2Xbox.Rewrite;

public sealed class ConfigValidationResult
{
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();

    public bool HasErrors => Errors.Count > 0;
    public bool HasWarnings => Warnings.Count > 0;
    public bool HasIssues => HasErrors || HasWarnings;
}

public sealed class ConfigLoadResult
{
    public ConfigLoadResult(AppConfig config, ConfigValidationResult validation)
    {
        Config = config;
        Validation = validation;
    }

    public AppConfig Config { get; }
    public ConfigValidationResult Validation { get; }
}

public static class ConfigValidator
{
    public static ConfigValidationResult Validate(AppConfig cfg)
    {
        var result = new ConfigValidationResult();

        ValidateSingleKey(cfg.ModifierKey, "modifier_key", result);
        ValidateSingleKey(cfg.BoostKey, "boost_key", result);

        foreach (var pair in cfg.Buttons.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            ValidateSingleKey(pair.Value, $"buttons.{pair.Key}", result);
        }

        foreach (var pair in cfg.Triggers.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            ValidateKeyList(pair.Value?.Keys, $"triggers.{pair.Key}.keys", result);
            ValidatePercentageValue(pair.Value?.Value, $"triggers.{pair.Key}.value", result);
        }

        foreach (var pair in cfg.Joysticks.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            ValidateKeyList(pair.Value?.Keys, $"joysticks.{pair.Key}.keys", result);
            ValidatePercentageValue(pair.Value?.Value, $"joysticks.{pair.Key}.value", result);
        }

        if (cfg.BoostMultiplier <= 0)
        {
            result.Warnings.Add("boost_multiplier <= 0. Runtime akan fallback ke 1.0.");
        }

        AddDuplicateWarnings(cfg, result);
        return result;
    }

    private static void ValidateSingleKey(string? keyText, string fieldName, ConfigValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(keyText))
        {
            return;
        }

        foreach (var token in KeyboardInput.SplitKeys(keyText))
        {
            if (KeyboardInput.ParseConfiguredKeys(token).Count == 0)
            {
                result.Errors.Add($"'{fieldName}' berisi key tidak dikenal: '{token}'.");
            }
        }
    }

    private static void ValidateKeyList(IEnumerable<string>? keys, string fieldName, ConfigValidationResult result)
    {
        if (keys is null)
        {
            result.Warnings.Add($"'{fieldName}' kosong/null. Default key list akan dipakai.");
            return;
        }

        foreach (var keyText in keys)
        {
            ValidateSingleKey(keyText, fieldName, result);
        }
    }

    private static void ValidatePercentageValue(string? valueText, string fieldName, ConfigValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(valueText))
        {
            result.Warnings.Add($"'{fieldName}' kosong. Runtime akan pakai nilai maksimal axis/trigger.");
            return;
        }

        var normalized = valueText.Replace("%", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();

        if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) &&
            !double.TryParse(normalized, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
        {
            result.Errors.Add($"'{fieldName}' bukan angka valid: '{valueText}'.");
            return;
        }

        if (value < 0 || value > 100)
        {
            result.Warnings.Add($"'{fieldName}' di luar rentang 0-100 dan akan di-clamp saat runtime: '{valueText}'.");
        }
    }

    private static void AddDuplicateWarnings(AppConfig cfg, ConfigValidationResult result)
    {
        var usage = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        static void AddUsage(Dictionary<string, HashSet<string>> map, string token, string source)
        {
            if (!map.TryGetValue(token, out var sources))
            {
                sources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                map[token] = sources;
            }

            sources.Add(source);
        }

        void AddSingle(string? text, string source)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            foreach (var token in KeyboardInput.SplitKeys(text))
            {
                AddUsage(usage, token.Trim().ToLowerInvariant(), source);
            }
        }

        void AddList(IEnumerable<string>? list, string source)
        {
            if (list is null)
            {
                return;
            }

            foreach (var entry in list)
            {
                AddSingle(entry, source);
            }
        }

        AddSingle(cfg.ModifierKey, "modifier_key");
        AddSingle(cfg.BoostKey, "boost_key");

        foreach (var pair in cfg.Buttons)
        {
            AddSingle(pair.Value, $"buttons.{pair.Key}");
        }

        foreach (var pair in cfg.Triggers)
        {
            AddList(pair.Value?.Keys, $"triggers.{pair.Key}.keys");
        }

        foreach (var pair in cfg.Joysticks)
        {
            AddList(pair.Value?.Keys, $"joysticks.{pair.Key}.keys");
        }

        foreach (var pair in usage.Where(p => p.Value.Count > 1).OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            var sources = string.Join(", ", pair.Value.OrderBy(v => v, StringComparer.OrdinalIgnoreCase));
            result.Warnings.Add($"Key '{pair.Key}' dipakai di beberapa mapping: {sources}.");
        }
    }
}

public sealed class AppConfig
{
    public const string KeyBlockingAlways = "always";
    public const string KeyBlockingWhenModifierActive = "when_modifier_active";
    public const string KeyBlockingDisabled = "disabled";

    [JsonProperty("hold_control")]
    public bool HoldControl { get; set; }

    [JsonProperty("key_blocking")]
    public string KeyBlocking { get; set; } = KeyBlockingWhenModifierActive;

    [JsonProperty("modifier_key")]
    public string ModifierKey { get; set; } = "numpad0";

    [JsonProperty("boost_key")]
    public string BoostKey { get; set; } = "numpad1";

    [JsonProperty("boost_multiplier")]
    public double BoostMultiplier { get; set; } = 1.3;

    [JsonProperty("buttons")]
    public Dictionary<string, string> Buttons { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonProperty("triggers")]
    public Dictionary<string, AxisBinding> Triggers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonProperty("joysticks")]
    public Dictionary<string, AxisBinding> Joysticks { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static AppConfig CreateDefault()
    {
        return new AppConfig
        {
            HoldControl = false,
            KeyBlocking = KeyBlockingWhenModifierActive,
            ModifierKey = "numpad0",
            BoostKey = "numpad1",
            BoostMultiplier = 1.3,
            Buttons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["DPAD_UP"] = "",
                ["DPAD_DOWN"] = "",
                ["DPAD_LEFT"] = "",
                ["DPAD_RIGHT"] = "",
                ["START"] = "",
                ["BACK"] = "",
                ["LEFT_THUMB_CLICK"] = "",
                ["RIGHT_THUMB_CLICK"] = "",
                ["LEFT_BUMPER"] = "",
                ["RIGHT_BUMPER"] = "",
                ["GUIDE"] = "",
                ["A"] = "multiply",
                ["B"] = "",
                ["X"] = "",
                ["Y"] = ""
            },
            Triggers = new Dictionary<string, AxisBinding>(StringComparer.OrdinalIgnoreCase)
            {
                ["LEFT_TRIGGER"] = new AxisBinding { Keys = new List<string> { "" }, Value = "26" },
                ["RIGHT_TRIGGER"] = new AxisBinding { Keys = new List<string> { "" }, Value = "26" }
            },
            Joysticks = new Dictionary<string, AxisBinding>(StringComparer.OrdinalIgnoreCase)
            {
                ["LEFT_X_MIN"] = new AxisBinding { Keys = new List<string> { "numpad4" }, Value = "26" },
                ["LEFT_X_MAX"] = new AxisBinding { Keys = new List<string> { "numpad6" }, Value = "26" },
                ["LEFT_Y_MIN"] = new AxisBinding { Keys = new List<string> { "numpad2" }, Value = "26" },
                ["LEFT_Y_MAX"] = new AxisBinding { Keys = new List<string> { "numpad8" }, Value = "26" },
                ["RIGHT_X_MIN"] = new AxisBinding { Keys = new List<string> { "" }, Value = "26" },
                ["RIGHT_X_MAX"] = new AxisBinding { Keys = new List<string> { "" }, Value = "26" },
                ["RIGHT_Y_MIN"] = new AxisBinding { Keys = new List<string> { "-" }, Value = "50" },
                ["RIGHT_Y_MAX"] = new AxisBinding { Keys = new List<string> { "+" }, Value = "50" }
            }
        };
    }

    public static string NormalizeKeyBlocking(string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
        {
            return KeyBlockingWhenModifierActive;
        }

        return mode.Trim().ToLowerInvariant() switch
        {
            KeyBlockingAlways => KeyBlockingAlways,
            KeyBlockingWhenModifierActive => KeyBlockingWhenModifierActive,
            KeyBlockingDisabled => KeyBlockingDisabled,
            "disable" => KeyBlockingDisabled,
            _ => KeyBlockingWhenModifierActive
        };
    }
}

public sealed class AxisBinding
{
    [JsonProperty("keys")]
    public List<string> Keys { get; set; } = new() { "" };

    [JsonProperty("value")]
    public string Value { get; set; } = "26";
}

public sealed class AppPaths
{
    public AppPaths(string baseDirectory)
    {
        BaseDirectory = baseDirectory;
        ConfigPath = Path.Combine(BaseDirectory, "config.json");
        ProfileDirectory = Path.Combine(BaseDirectory, "profiles");
    }

    public string BaseDirectory { get; }
    public string ConfigPath { get; }
    public string ProfileDirectory { get; }
}

public sealed class ConfigStore
{
    private readonly AppPaths _paths;
    private readonly JsonSerializerSettings _jsonSettings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore
    };

    public ConfigStore(AppPaths paths)
    {
        _paths = paths;
        Directory.CreateDirectory(_paths.ProfileDirectory);
    }

    public AppConfig EnsureAndLoad()
    {
        return EnsureAndLoadValidated().Config;
    }

    public ConfigLoadResult EnsureAndLoadValidated()
    {
        if (!File.Exists(_paths.ConfigPath))
        {
            var defaultConfig = AppConfig.CreateDefault();
            Save(defaultConfig);
            return new ConfigLoadResult(defaultConfig, ConfigValidator.Validate(defaultConfig));
        }

        return LoadValidated();
    }

    public AppConfig Load()
    {
        return LoadValidated().Config;
    }

    public ConfigLoadResult LoadValidated()
    {
        return LoadFileValidated(_paths.ConfigPath, "config.json");
    }

    public void Save(AppConfig config)
    {
        Normalize(config);
        var text = JsonConvert.SerializeObject(config, _jsonSettings);
        File.WriteAllText(_paths.ConfigPath, text);
    }

    public AppConfig LoadProfile(string profileFile)
    {
        return LoadProfileValidated(profileFile).Config;
    }

    public ConfigLoadResult LoadProfileValidated(string profileFile)
    {
        return LoadFileValidated(profileFile, Path.GetFileName(profileFile));
    }

    public void SaveProfile(string profileFile, AppConfig config)
    {
        Normalize(config);
        var text = JsonConvert.SerializeObject(config, _jsonSettings);
        File.WriteAllText(profileFile, text);
    }

    private ConfigLoadResult LoadFileValidated(string filePath, string sourceName)
    {
        var validation = new ConfigValidationResult();
        AppConfig cfg;

        try
        {
            var text = File.ReadAllText(filePath);
            cfg = JsonConvert.DeserializeObject<AppConfig>(text) ?? AppConfig.CreateDefault();
        }
        catch (JsonException ex)
        {
            cfg = AppConfig.CreateDefault();
            validation.Errors.Add($"Format JSON '{sourceName}' tidak valid. Fallback ke default. Detail: {ex.Message}");
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            cfg = AppConfig.CreateDefault();
            validation.Errors.Add($"Gagal membaca '{sourceName}'. Fallback ke default. Detail: {ex.Message}");
        }

        Normalize(cfg);
        MergeValidation(validation, ConfigValidator.Validate(cfg));
        LogValidation(sourceName, validation);

        return new ConfigLoadResult(cfg, validation);
    }

    private static void MergeValidation(ConfigValidationResult target, ConfigValidationResult source)
    {
        target.Errors.AddRange(source.Errors);
        target.Warnings.AddRange(source.Warnings);
    }

    private static void LogValidation(string sourceName, ConfigValidationResult validation)
    {
        if (!validation.HasIssues)
        {
            return;
        }

        foreach (var error in validation.Errors)
        {
            Debug.WriteLine($"[ConfigValidation][{sourceName}][ERROR] {error}");
        }

        foreach (var warning in validation.Warnings)
        {
            Debug.WriteLine($"[ConfigValidation][{sourceName}][WARN] {warning}");
        }
    }

    private static void Normalize(AppConfig cfg)
    {
        var defaults = AppConfig.CreateDefault();

        cfg.Buttons ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        cfg.Triggers ??= new Dictionary<string, AxisBinding>(StringComparer.OrdinalIgnoreCase);
        cfg.Joysticks ??= new Dictionary<string, AxisBinding>(StringComparer.OrdinalIgnoreCase);

        cfg.KeyBlocking = AppConfig.NormalizeKeyBlocking(cfg.KeyBlocking);

        foreach (var key in defaults.Buttons.Keys)
        {
            if (!cfg.Buttons.ContainsKey(key))
            {
                cfg.Buttons[key] = defaults.Buttons[key];
            }
            else if (cfg.Buttons[key] is null)
            {
                cfg.Buttons[key] = string.Empty;
            }
        }

        foreach (var key in defaults.Triggers.Keys)
        {
            if (!cfg.Triggers.ContainsKey(key) || cfg.Triggers[key] is null)
            {
                cfg.Triggers[key] = defaults.Triggers[key];
            }
            else if (cfg.Triggers[key].Keys is null || cfg.Triggers[key].Keys.Count == 0)
            {
                cfg.Triggers[key].Keys = new List<string> { "" };
            }

            cfg.Triggers[key].Value ??= defaults.Triggers[key].Value;
        }

        foreach (var key in defaults.Joysticks.Keys)
        {
            if (!cfg.Joysticks.ContainsKey(key) || cfg.Joysticks[key] is null)
            {
                cfg.Joysticks[key] = defaults.Joysticks[key];
            }
            else if (cfg.Joysticks[key].Keys is null || cfg.Joysticks[key].Keys.Count == 0)
            {
                cfg.Joysticks[key].Keys = new List<string> { "" };
            }

            cfg.Joysticks[key].Value ??= defaults.Joysticks[key].Value;
        }
    }
}
