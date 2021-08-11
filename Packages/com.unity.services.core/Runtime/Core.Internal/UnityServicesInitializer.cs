using UnityEngine;

namespace Unity.Services.Core.Internal
{
    static class UnityServicesInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void CreateStaticInstance()
        {
            UnityServices.Instance = new UnityServicesInternal(CoreRegistry.Instance);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnableServicesInitialization()
        {
            var instance = (UnityServicesInternal)UnityServices.Instance;
            instance.EnableInitialization();
        }
    }
}
