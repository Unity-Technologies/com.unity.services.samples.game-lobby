using System.Threading.Tasks;
using Unity.Services.Authentication.Internal;
using Unity.Services.Authentication.Utilities;
using Unity.Services.Core.Internal;
using Unity.Services.Core.Environments.Internal;
using UnityEngine;

namespace Unity.Services.Authentication
{
    class AuthenticationPackageInitializer : IInitializablePackage
    {
#if AUTHENTICATION_TESTING_STAGING_UAS
        const string k_UasHost = "https://api.stg.identity.corp.unity3d.com";
#else
        const string k_UasHost = "https://api.prd.identity.corp.unity3d.com";
#endif

        public Task Initialize(CoreRegistry registry)
        {
            var dateTime = new DateTimeWrapper();
            var networkUtilities = new NetworkingUtilities(Scheduler.Instance);
            var networkClient = new AuthenticationNetworkClient(k_UasHost,
                Application.cloudProjectId,
                registry.GetServiceComponent<IEnvironments>(),
                new CodeChallengeGenerator(),
                networkUtilities);
            var authenticationService = new AuthenticationServiceInternal(networkClient,
                new JwtDecoder(dateTime),
                new PlayerPrefsCache("unity.services.authentication"),
                Scheduler.Instance,
                dateTime);

            AuthenticationService.Instance = authenticationService;
            registry.RegisterServiceComponent<IPlayerId>(new PlayerIdComponent(authenticationService));
            registry.RegisterServiceComponent<IAccessToken>(new AccessTokenComponent(authenticationService));

            return Task.CompletedTask;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            CoreRegistry.Instance.RegisterPackage(new AuthenticationPackageInitializer())
                .DependsOn<IEnvironments>()
                .ProvidesComponent<IPlayerId>()
                .ProvidesComponent<IAccessToken>();
        }
    }
}
