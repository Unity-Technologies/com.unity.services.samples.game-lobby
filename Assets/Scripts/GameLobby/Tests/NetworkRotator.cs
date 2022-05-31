using System;
using Unity.Netcode;
using UnityEngine;

public class NetworkRotator : NetworkBehaviour
{
    [SerializeField]
    float m_moveSpeed = 2;
    [SerializeField]
    float m_RotateSpeed = 10;
    bool m_CanRotate;

    public override void OnNetworkSpawn()
    {
        m_CanRotate = IsHost || IsServer;
    }

    void Update()
    {
        if (!m_CanRotate)
            return;
        transform.Translate(0, 0, m_moveSpeed * Time.deltaTime);
        transform.Rotate(0, m_RotateSpeed * Time.deltaTime, 0);
    }
}