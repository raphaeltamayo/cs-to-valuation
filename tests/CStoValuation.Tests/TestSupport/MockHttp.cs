using System.Net;
using Moq;
using Moq.Protected;

namespace CStoValuation.Tests.TestSupport;

/// <summary>
/// Helpers for faking <see cref="HttpClient"/> traffic. <see cref="HttpMessageHandler"/>'s
/// send method is <c>protected</c>, so Moq reaches it via <c>.Protected()</c>. The
/// <paramref name="responder"/> sees each outgoing request, which lets a test branch on the
/// URL (e.g. return XML for a vanity lookup) and is the seam for verifying call counts.
/// </summary>
internal static class MockHttp
{
    /// <summary>Builds a client whose responses are produced by <paramref name="responder"/>.</summary>
    public static HttpClient Client(string baseAddress, Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns((HttpRequestMessage request, CancellationToken _) => Task.FromResult(responder(request)));

        return new HttpClient(handler.Object) { BaseAddress = new Uri(baseAddress) };
    }

    /// <summary>A client that always returns the same body — the common single-endpoint case.</summary>
    public static HttpClient ClientReturning(
        string baseAddress, string body, HttpStatusCode status = HttpStatusCode.OK, string mediaType = "application/json") =>
        Client(baseAddress, _ => Response(body, status, mediaType));

    /// <summary>A client that fails every request with the given status (no body needed).</summary>
    public static HttpClient ClientWithStatus(string baseAddress, HttpStatusCode status) =>
        Client(baseAddress, _ => new HttpResponseMessage(status));

    public static HttpResponseMessage Response(
        string body, HttpStatusCode status = HttpStatusCode.OK, string mediaType = "application/json") =>
        new(status) { Content = new StringContent(body, System.Text.Encoding.UTF8, mediaType) };
}
