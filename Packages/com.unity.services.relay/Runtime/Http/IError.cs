namespace Unity.Services.Relay.Http
{
    public interface IError
    {
        string Type { get; }
        string Title { get; }
        int? Status { get; }
        int Code { get; }
        string Detail { get; }
    }
}