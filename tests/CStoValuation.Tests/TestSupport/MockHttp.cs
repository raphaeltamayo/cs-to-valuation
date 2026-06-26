using System.Net;
using Moq;
using Moq.Protected;

namespace CStoValuation.Tests.TestSupport;

internal static class MockHttp
{
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

    public static HttpClient ClientReturning(
        string baseAddress, string body, HttpStatusCode status = HttpStatusCode.OK, string mediaType = "application/json") =>
        Client(baseAddress, _ => Response(body, status, mediaType));

    public static HttpClient ClientWithStatus(string baseAddress, HttpStatusCode status) =>
        Client(baseAddress, _ => new HttpResponseMessage(status));

    public static HttpResponseMessage Response(
        string body, HttpStatusCode status = HttpStatusCode.OK, string mediaType = "application/json") =>
        new(status) { Content = new StringContent(body, System.Text.Encoding.UTF8, mediaType) };
}
