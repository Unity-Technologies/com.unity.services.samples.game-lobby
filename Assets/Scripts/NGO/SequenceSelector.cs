using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace LobbyRelaySample.ngo
{
    /// <summary>
    /// Handles selecting the randomized sequence of symbols to spawn, choosing a subset to be the ordered target sequence that each player needs to select.
    /// This also handles selecting randomized positions for the symbols, and it sets up the target sequence animation for the instruction sequence.
    /// </summary>
    public class SequenceSelector : NetworkBehaviour, IReceiveMessages
    {
        [SerializeField] private SymbolData m_symbolData = default;
        [SerializeField] private Image[] m_targetSequenceOutput = default;
        public const int k_symbolCount = 200;
        private bool m_hasReceivedTargetSequence = false;
        private ulong m_localId;
        private bool m_canAnimateTargets = false;

        private List<int> m_fullSequence = new List<int>(); // This is owned by the host, and each index is assigned as a NetworkVariable to each SymbolObject.
        private NetworkList<int> m_targetSequence; // This is owned by the host but needs to be available to all clients, so it's a NetworkedList here.
        private Dictionary<ulong, int> m_targetSequenceIndexPerPlayer = new Dictionary<ulong, int>(); // Each player's current target. Also owned by the host, indexed by client ID.

        public void Awake()
        {
            m_targetSequence = new NetworkList<int>();
            Locator.Get.Messenger.Subscribe(this);
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            Locator.Get.Messenger.Unsubscribe(this);
        }

        public override void OnNetworkSpawn()
        {
            if (IsHost)
                ChooseSymbols();
            m_localId = NetworkManager.Singleton.LocalClientId;
            AddClient_ServerRpc(m_localId);
        }

        private void ChooseSymbols()
        {
            // Choose some subset of the list of symbols to be present in this game, along with a target sequence.
            int numSymbolTypes = 8;
            List<int> symbolsForThisGame = SelectSymbols(m_symbolData.m_availableSymbols.Count, numSymbolTypes);
            m_targetSequence.Add(symbolsForThisGame[0]);
            m_targetSequence.Add(symbolsForThisGame[1]);
            m_targetSequence.Add(symbolsForThisGame[2]);

            // Then, ensure that the target sequence is present in order throughout most of the full set of symbols to spawn.
            int numTargetSequences = (int)(k_symbolCount * 2 / 3f) / 3; // About 2/3 of the symbols will be definitely part of the target sequence.
            for (; numTargetSequences >= 0; numTargetSequences--)
            {
                m_fullSequence.Add(m_targetSequence[2]); // We want a List instead of a Queue or Stack for faster insertion, but we will remove indices backwards so as to not resize other entries.
                m_fullSequence.Add(m_targetSequence[1]);
                m_fullSequence.Add(m_targetSequence[0]);
            }
            // Then, fill in with a good mix of the remaining symbols.
            for (int n = 3; n < numSymbolTypes - 1; n++)
                AddHalfRemaining(n, 2);
            AddHalfRemaining(numSymbolTypes - 1, 1); // 1 as the divider ensures all remaining symbols get an index.

            void AddHalfRemaining(int symbolIndex, int divider)
            {
                int remaining = k_symbolCount - m_fullSequence.Count;
                for (int n = 0; n < remaining / divider; n++)
                {
                    int randomIndex = UnityEngine.Random.Range(0, m_fullSequence.Count);
                    m_fullSequence.Insert(randomIndex, symbolsForThisGame[symbolIndex]);
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void AddClient_ServerRpc(ulong id)
        {
            m_targetSequenceIndexPerPlayer.Add(id, 0);
        }

        // Very simple random selection. Duplicates are allowed.
        private static List<int> SelectSymbols(int numOptions, int targetCount)
        {
            List<int> list = new List<int>();
            for (int n = 0; n < targetCount; n++)
                list.Add(UnityEngine.Random.Range(0, numOptions));
            return list;
        }

        public void Update()
        {
            // A client can't guarantee timing with the host's selection of the target sequence, so retrieve it once it's available.
            if (!m_hasReceivedTargetSequence && m_targetSequence.Count > 0)
            {
                for (int n = 0; n < m_targetSequence.Count; n++)
                    m_targetSequenceOutput[n].sprite = m_symbolData.GetSymbolForIndex(m_targetSequence[n]);
                m_hasReceivedTargetSequence = true;
                ScaleTargetUi(m_localId, 0);
            }
        }

        /// <summary>
        /// If the index is correct, this will advance the current sequence index.
        /// </summary>
        /// <returns>True if the correct symbol index was chosen, false otherwise.</returns>
        public bool ConfirmSymbolCorrect(ulong id, int symbolIndex)
        {
            int index = m_targetSequenceIndexPerPlayer[id];
            if (symbolIndex != m_targetSequence[index])
                return false;
            if (++index >= m_targetSequence.Count)
                index = 0;
            m_targetSequenceIndexPerPlayer[id] = index;

            ScaleTargetUi_ClientRpc(id, index);
            return true;
        }

        [ClientRpc]
        private void ScaleTargetUi_ClientRpc(ulong id, int sequenceIndex)
        {
            ScaleTargetUi(id, sequenceIndex);
        }
        private void ScaleTargetUi(ulong id, int sequenceIndex)
        {
            if (NetworkManager.Singleton.LocalClientId == id)
                for (int i = 0; i < m_targetSequenceOutput.Length; i++)
                    m_targetSequenceOutput[i].transform.localScale = Vector3.one * (sequenceIndex == i || !m_canAnimateTargets ? 1 : 0.7f);
        }

        public int GetNextSymbol(int symbolObjectIndex)
        {
            return m_fullSequence[symbolObjectIndex];
        }

        public void OnReceiveMessage(MessageType type, object msg)
        {
            if (type == MessageType.InstructionsShown)
            {
                m_canAnimateTargets = true;
                ScaleTargetUi(m_localId, 0);
            }
        }

        /// <summary>
        /// Used for the binary space partition (BSP) algorithm, which makes alternating "cuts" to subdivide rectangles while maintaining a buffer of space between them.
        /// This ensures all symbols will be randomly (though not uniformly) distributed without overlapping each other.
        /// </summary>
        private struct RectCut
        {
            public Rect rect;
            // The spawn region will be much taller than it is wide, so we'll do more horizontal cuts (instead of just alternating between horizontal and vertical).
            public int cutIndex;
            public bool isVertCut { get { return cutIndex % 3 == 2; } }

            public RectCut(Rect rect, int cutIndex) { this.rect = rect; this.cutIndex = cutIndex; }
            public RectCut(float xMin, float xMax, float yMin, float yMax, int cutIndex)
            {
                this.rect = new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
                this.cutIndex = cutIndex;
            }
        }

        /// <summary>
        /// Selects a randomized series of spawn positions within the provided xy-bounds, or just a simple grid of positions if selection fails.
        /// </summary>
        /// <param name="bounds">Rectangle of space to subdivide.</param>
        /// <param name="extent">The minimum space between points, to ensure that spawned symbol objects won't overlap.</param>
        /// <param name="count">How many positions to choose.</param>
        /// <returns>Position list in arbitrary order.</returns>
        public List<Vector2> GenerateRandomSpawnPoints(Rect bounds, float extent, int count = k_symbolCount)
        {
            int numTries = 3;
            List<Vector2> points = new List<Vector2>();
            while (numTries > 0)
            {
                Queue<RectCut> rects = new Queue<RectCut>();
                points.Clear();
                rects.Enqueue(new RectCut(bounds, -1)); // Start with an extra horizontal cut since the space is so tall.

                // For each rect, subdivide it with an alternating cut, and then enqueue the two smaller rects for recursion until enough points are chosen or the rects are all too small.
                while (rects.Count + points.Count < count && rects.Count > 0)
                {
                    RectCut currRect = rects.Dequeue();
                    bool isLargeEnough = (currRect.isVertCut && currRect.rect.width > extent * 2) || (!currRect.isVertCut && currRect.rect.height > extent * 2);
                    if (!isLargeEnough)
                    {   points.Add(currRect.rect.center);
                        continue;
                    }

                    float xMin = currRect.rect.xMin, xMax = currRect.rect.xMax, yMin = currRect.rect.yMin, yMax = currRect.rect.yMax;
                    if (currRect.isVertCut)
                    {   float cutPosX = Random.Range(xMin + extent, xMax - extent);
                        rects.Enqueue( new RectCut(xMin, cutPosX, yMin, yMax, currRect.cutIndex + 1) );
                        rects.Enqueue( new RectCut(cutPosX, xMax, yMin, yMax, currRect.cutIndex + 1) );
                    } 
                    else
                    {   float cutPosY = Random.Range(yMin + extent, yMax - extent);
                        rects.Enqueue( new RectCut(xMin, xMax, yMin, cutPosY, currRect.cutIndex + 1) );
                        rects.Enqueue( new RectCut(xMin, xMax, cutPosY, yMax, currRect.cutIndex + 1) );
                    }
                }

                while (rects.Count > 0)
                    points.Add(rects.Dequeue().rect.center);

                if (points.Count >= count)
                    return points;
                numTries--;
            }

            Debug.LogError("Failed to generate symbol spawn points. Defaulting to a simple grid of points.");
            points.Clear();
            int numPerLine = Mathf.CeilToInt(bounds.width / (extent * 1.5f));
            for (int n = 0; n < count; n++)
                points.Add(new Vector2(Mathf.Lerp(bounds.xMin, bounds.xMax, (n % numPerLine) / (numPerLine - 1f)), n / numPerLine * extent * 1.5f));
            return points;
        }
    }
}
