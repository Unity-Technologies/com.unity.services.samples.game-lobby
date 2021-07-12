using Unity.Services.Authentication;
using UnityEngine;

namespace Unity.Services.Core
{
    class UnityServicesInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Initialize()
        {
            UnityServices.Instance = new UnityServicesInternal();
        }
    }
}
