using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LobbyRelaySample.inGame
{
    public interface IInGameInputHandler : IProvidable<IInGameInputHandler>
    {
        void OnPlayerInput(SymbolObject selectedSymbol);
    }

    public class InGameInputHandlerNoop : IInGameInputHandler
    {
        public void OnPlayerInput(SymbolObject selectedSymbol) { }
        public void OnReProvided(IInGameInputHandler previousProvider) { }
    }
}
