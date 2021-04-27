using UnityEngine;

    using Unity.Services.Rooms.Apis.Rooms;

using Unity.Services.Rooms.Http;
using Unity.Services.Rooms.Scheduler;

namespace Unity.Services.Rooms
{
    internal class RoomsServiceProvider
    {
        private static GameObject _gameObjectFactory;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        static void OnLoad()
        {
            _gameObjectFactory = GameObjectFactory.CreateCoreSdkGameObject();
            var scheduler = _gameObjectFactory.GetComponent<TaskScheduler>();
            var httpClient = new HttpClient(scheduler);
            RoomsService.RoomsApiClient = new RoomsApiClient(httpClient, scheduler);
            
        }
    }
}
