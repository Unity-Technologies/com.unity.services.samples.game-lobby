namespace LobbyRelaySample.ngo
{
    /// <summary>
    /// Something that will handle player input while in the game.
    /// </summary>
    public interface IInGameInputHandler : IProvidable<IInGameInputHandler>
    {
        void OnPlayerInput(ulong id, SymbolObject selectedSymbol);
    }

    public class InGameInputHandlerNoop : IInGameInputHandler
    {
        public void OnPlayerInput(ulong id, SymbolObject selectedSymbol) { }
        public void OnReProvided(IInGameInputHandler previousProvider) { }
    }
}
