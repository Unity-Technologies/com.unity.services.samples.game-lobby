namespace LobbyRelaySample.inGame
{
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
