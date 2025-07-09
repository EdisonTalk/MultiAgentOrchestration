namespace CommonShared;
public sealed class OpenAiHttpHandler : HttpClientHandler
{
    private readonly string _openAiBaseAddress;

    public OpenAiHttpHandler(string openAiBaseAddress)
    {
        _openAiBaseAddress = openAiBaseAddress;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        UriBuilder uriBuilder;
        var uri = new Uri(_openAiBaseAddress);
        if (request.RequestUri?.LocalPath == "v1/chat/completions"
            || request.RequestUri?.LocalPath == "/v1/chat/completions") // Chatting
        {
            uriBuilder = new UriBuilder(request.RequestUri)
            {
                Scheme = "https",
                Host = uri.Host,
                Path = "v1/chat/completions",
            };
            request.RequestUri = uriBuilder.Uri;
        }
        else if (request.RequestUri?.LocalPath == "/v1/embeddings"
            || request.RequestUri?.LocalPath == "/v1/embeddings") // Embedding
        {
            uriBuilder = new UriBuilder(request.RequestUri)
            {
                Scheme = "https",
                Host = uri.Host,
                Path = "/v1/embeddings",
            };
            request.RequestUri = uriBuilder.Uri;
        }

        var response = await base.SendAsync(request, cancellationToken);
        return response;
    }
}