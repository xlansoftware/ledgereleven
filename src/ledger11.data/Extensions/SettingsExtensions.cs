using ledger11.data;
using ledger11.model.Data;
using Microsoft.EntityFrameworkCore;

namespace ledger11.data.Extensions;

public static class SettingsExtensions
{
    /// <summary>
    /// Gets the value of a setting by its key.
    /// </summary>
    /// <param name="settings">The settings collection.</param>
    /// <param name="key">The key of the setting to retrieve.</param>
    /// <returns>The value of the setting, or null if not found.</returns>
    public async static Task<string?> GetSettingValue(this LedgerDbContext ledger, string key, string? defaultValue = null)
    {
        var setting = await ledger.Settings.Where(s => s.Key == key).FirstOrDefaultAsync();
        if (setting != null)
        {
            return setting.Value;
        }

        // If the setting is not found, return the default value if provided
        return defaultValue;
    }

    /// <summary>
    /// Sets the value of a setting by its key. If the setting does not exist, it will be created.
    /// </summary>
    /// <param name="ledger"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public async static Task SetSettingValue(this LedgerDbContext ledger, string key, string value)
    {
        var setting = await ledger.Settings.Where(s => s.Key == key).FirstOrDefaultAsync();
        if (setting != null)
        {
            setting.Value = value;
        }
        else
        {
            await ledger.Settings.AddAsync(new Setting { Key = key, Value = value });
        }

        await ledger.SaveChangesAsync();
    }
}