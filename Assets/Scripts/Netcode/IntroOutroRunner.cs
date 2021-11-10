using UnityEngine;

namespace LobbyRelaySample.ngo
{
    /// <summary>
    /// Handles any visual tasks for running the NGO minigame's intro and outro.
    /// </summary>
    public class IntroOutroRunner : MonoBehaviour
    {
        [SerializeField] private Animator m_animator;

        public void DoIntro()
        {
            m_animator.SetTrigger("DoIntro");
        }

        /// <summary>
        /// Called via an AnimationEvent.
        /// </summary>
        public void OnIntroComplete()
        {
            Locator.Get.Messenger.OnReceiveMessage(MessageType.InstructionsShown, null);
        }
    }
}
