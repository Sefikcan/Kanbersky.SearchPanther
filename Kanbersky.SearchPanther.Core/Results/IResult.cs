namespace Kanbersky.SearchPanther.Core.Results
{
    public interface IResult
    {
        int StatusCode { get; }

        bool Success { get; }

        string Message { get; }
    }
}
