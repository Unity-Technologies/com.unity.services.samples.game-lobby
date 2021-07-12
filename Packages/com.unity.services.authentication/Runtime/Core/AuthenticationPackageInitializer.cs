using System;
using System.Threading.Tasks;
using Unity.Services.Authentication.Utilities;
using Unity.Services.Core;
using UnityEngine;
using Logger = Unity.Services.Authentication.Utilities.Logger;

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
            var logger = new Logger("[Authentication]");

            var dateTime = new DateTimeWrapper();
            var networkUtilities = new NetworkingUtilities(Scheduler.Instance, logger);
            var networkClient = new AuthenticationNetworkClient(k_UasHost,
                Application.cloudProjectId,
                new CodeChallengeGenerator(),
                networkUtilities,
                logger);
            var authenticationService = new AuthenticationServiceInternal(networkClient,
                new JwtDecoder(dateTime, logger),
                new PlayerPrefsCache("unity.services.authentication"),
                Scheduler.Instance,
                dateTime,
                logger);

            AuthenticationService.Instance = authenticationService;
            registry.RegisterServiceComponent<IPlayerId>(new PlayerIdComponent(authenticationService));
            registry.RegisterServiceComponent<IAccessToken>(new AccessTokenComponent(authenticationService));

            return Task.CompletedTask;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            CoreRegistry.Instance.RegisterPackage(new AuthenticationPackageInitializer())
                .ProvidesComponent<IPlayerId>()
                .ProvidesComponent<IAccessToken>();
        }
    }
}
