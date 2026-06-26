namespace CStoValuation.Tests.TestSupport;

internal static class Fixtures
{
    public static string Read(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
        return File.ReadAllText(path);
    }
}
