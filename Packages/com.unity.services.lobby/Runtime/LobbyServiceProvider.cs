using UnityEngine;
using System.Threading.Tasks;

using Unity.Services.Lobbies.Apis.Lobby;

using Unity.Services.Lobbies.Http;
using Unity.Services.Lobbies.Scheduler;
using TaskScheduler = Unity.Services.Lobbies.Scheduler.TaskScheduler;
using Unity.Services.Core.Internal;
using Unity.Services.Authentication.Internal;

namespace Unity.Services.Lobbies
{
    internal class LobbyServiceProvider : IInitializablePackage
    {
        private static GameObject _gameObjectFactory;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            // Pass an instance of this class to Core
            var generatedPackageRegistry =
            CoreRegistry.Instance.RegisterPackage(new LobbyServiceProvider());
                // And specify what components it requires, or provides.
            generatedPackageRegistry.DependsOn<IAccessToken>();
;
        }

        public Task Initialize(CoreRegistry registry)
        {
            _gameObjectFactory = GameObjectFactory.CreateCoreSdkGameObject();
            var httpClient = new HttpClient();
            
            var accessTokenLobbyApi = registry.GetServiceComponent<IAccessToken>();

            if (accessTokenLobbyApi != null)
            {
                LobbyService.LobbyApiClient = new LobbyApiClient(httpClient, accessTokenLobbyApi);
            }
            
            return Task.CompletedTask;
        }
    }
}
