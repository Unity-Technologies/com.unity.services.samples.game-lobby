using System;
using UnityEngine;

namespace LobbyRelaySample.ngo
{
    /// <summary>
    /// Handles any visual tasks for running the NGO minigame's intro and outro.
    /// </summary>
    public class IntroOutroRunner : MonoBehaviour
    {
        [SerializeField]
        InGameRunner m_inGameRunner;
        [SerializeField] Animator m_animator;
        Action m_onIntroComplete, m_onOutroComplete;


        public void DoIntro(Action onIntroComplete)
        {
            m_onIntroComplete = onIntroComplete;
            m_animator.SetTrigger("DoIntro");
        }

        public void DoOutro(Action onOutroComplete)
        {
            m_onOutroComplete = onOutroComplete;
            m_animator.SetTrigger("DoOutro");
        }

        /// <summary>
        /// Called via an AnimationEvent.
        /// </summary>
        public void OnIntroComplete()
        {
            m_onIntroComplete?.Invoke();
        }
        /// <summary>
        /// Called via an AnimationEvent.
        /// </summary>
        public void OnOutroComplete()
        {
            m_onOutroComplete?.Invoke();
        }
    }
}
