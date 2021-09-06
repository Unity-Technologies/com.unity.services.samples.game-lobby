using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LobbyRelaySample
{
    [SerializeField]
    public class LobbyUserAudio
    {
        public string ID { get; private set; }
        public bool HasVoice { get; set; }
        public bool Muted { get; set; }
        public float UserVolume { get; set; }

        public LobbyUserAudio(string userID)
        {
            ID = userID;
            HasVoice = false;
            Muted = false;
            UserVolume = 1;
        }
    }
}
