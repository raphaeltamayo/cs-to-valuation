using System.Runtime.Versioning;
using CStoValuation.Core.Abstractions;
using CStoValuation.Core.Models;
using Microsoft.Win32;

namespace CStoValuation.Infrastructure.Steam;

[SupportedOSPlatform("windows")]
public sealed class SteamAccountLocator : ISteamAccountLocator
{
    private const string SteamRegistryKey = @"Software\Valve\Steam";

    public IReadOnlyList<LocalSteamAccount> GetLocalAccounts()
    {
        if (!OperatingSystem.IsWindows())
        {
            return [];
        }

        using var steamKey = Registry.CurrentUser.OpenSubKey(SteamRegistryKey);
        if (steamKey is null)
        {
            return [];
        }

        var accounts = ReadLoginUsers(steamKey).ToDictionary(account => account.SteamId64);
        var activeSteamId64 = ReadActiveSteamId64(steamKey);

        if (activeSteamId64 is not null)
        {
            accounts[activeSteamId64] = accounts.TryGetValue(activeSteamId64, out var existing)
                ? existing with { IsActive = true }
                : new LocalSteamAccount(activeSteamId64, PersonaName: null, IsActive: true, IsMostRecent: false, Timestamp: 0);
        }

        return accounts.Values
            .OrderByDescending(account => account.IsActive)
            .ThenByDescending(account => account.IsMostRecent)
            .ThenByDescending(account => account.Timestamp)
            .ToList();
    }

    private static IEnumerable<LocalSteamAccount> ReadLoginUsers(RegistryKey steamKey)
    {
        if (steamKey.GetValue("SteamPath") is not string steamPath || string.IsNullOrEmpty(steamPath))
        {
            return [];
        }

        var normalizedPath = steamPath.Replace('/', Path.DirectorySeparatorChar);
        var loginUsersPath = Path.Combine(normalizedPath, "config", "loginusers.vdf");
        if (!File.Exists(loginUsersPath))
        {
            return [];
        }

        try
        {
            return LoginUsersVdfParser.Parse(File.ReadAllText(loginUsersPath));
        }
        catch (IOException)
        {
            return [];
        }
    }

    private static string? ReadActiveSteamId64(RegistryKey steamKey)
    {
        using var activeProcessKey = steamKey.OpenSubKey("ActiveProcess");
        if (activeProcessKey?.GetValue("ActiveUser") is not int accountId || accountId == 0)
        {
            return null;
        }

        return SteamIdConversion.AccountIdToSteamId64((uint)accountId);
    }
}
