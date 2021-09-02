using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using LobbyRelaySample;
using Unity.Netcode;
using UnityEngine;

public class AsyncNetworkRequest : AsyncRequest
{
    private static AsyncNetworkRequest s_instance;

    public static AsyncNetworkRequest Instance
    {
        get
        {
            if (s_instance == null)
                s_instance = new AsyncNetworkRequest();
            return s_instance;
        }
    }

    /// <summary>
    /// The Relay service will wrap HTTP errors in RelayServiceExceptions. We can filter on RelayServiceException.Reason for custom behavior.
    /// </summary>
    protected override void ParseServiceException(Exception e)
    {
        throw e;
    }
}

public class RelayNetcodeHost : NetworkBehaviour
{
    async void Start()
    {
        var hostSocket = NetworkManager.Singleton.StartHost();
        while (!hostSocket.IsDone)
        {
            await Task.Yield();
        }
    }
    
    
}
