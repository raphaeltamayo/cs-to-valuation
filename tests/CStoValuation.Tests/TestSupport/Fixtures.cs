namespace CStoValuation.Tests.TestSupport;

/// <summary>
/// Reads recorded API responses that are copied next to the test assembly at build time.
/// Centralising the path keeps individual tests focused on behaviour, not plumbing.
/// </summary>
internal static class Fixtures
{
    public static string Read(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
        return File.ReadAllText(path);
    }
}
