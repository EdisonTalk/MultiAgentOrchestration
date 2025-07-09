namespace CommonShared;

public sealed class OpenAiConfiguration
{
    public string ModelId { get; set; }
    public string EndPoint { get; set; }
    public string ApiKey { get; set; }

    public OpenAiConfiguration(string modelId, string endPoint, string apiKey)
    {
        ModelId = modelId;
        EndPoint = endPoint;
        ApiKey = apiKey;
    }
}