using UnityEngine;
using System.Threading.Tasks;

using Unity.Services.Relay.Apis.Allocations;

using Unity.Services.Relay.Http;
using Unity.Services.Relay.Scheduler;
using TaskScheduler = Unity.Services.Relay.Scheduler.TaskScheduler;
using Unity.Services.Core.Internal;
using Unity.Services.Authentication.Internal;

namespace Unity.Services.Relay
{
    internal class RelayServiceProvider : IInitializablePackage
    {
        private static GameObject _gameObjectFactory;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            // Pass an instance of this class to Core
            var generatedPackageRegistry =
            CoreRegistry.Instance.RegisterPackage(new RelayServiceProvider());
                // And specify what components it requires, or provides.
            generatedPackageRegistry.DependsOn<IAccessToken>();
;
        }

        public Task Initialize(CoreRegistry registry)
        {
            _gameObjectFactory = GameObjectFactory.CreateCoreSdkGameObject();
            var httpClient = new HttpClient();
            
            var accessTokenAllocationsApi = registry.GetServiceComponent<IAccessToken>();

            if (accessTokenAllocationsApi != null)
            {
                RelayService.AllocationsApiClient = new AllocationsApiClient(httpClient, accessTokenAllocationsApi);
            }
            
            return Task.CompletedTask;
        }
    }
}
