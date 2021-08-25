using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MuteUI : MonoBehaviour
{
    [SerializeField]
    CanvasGroup m_canvasGroup;

    public void SetSpeakerAlpha(float alpha)
    {
        m_canvasGroup.alpha = alpha;
    }
}
