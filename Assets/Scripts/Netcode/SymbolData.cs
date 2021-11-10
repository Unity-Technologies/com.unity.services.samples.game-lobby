using System.Collections.Generic;
using UnityEngine;

namespace LobbyRelaySample.ngo
{
    public class SymbolData : ScriptableObject
    {
        [SerializeField] public List<Sprite> m_availableSymbols;
        public int SymbolCount => m_availableSymbols.Count;

        public Sprite GetSymbolForIndex(int index)
        {
            if (index < 0 || index >= m_availableSymbols.Count)
                index = 0;
            return m_availableSymbols[index];
        }
    }
}
