namespace Unity.Services.Lobbies.Http
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