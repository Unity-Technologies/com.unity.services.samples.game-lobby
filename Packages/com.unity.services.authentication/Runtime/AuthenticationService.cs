using System;
using Unity.Services.Authentication.Utilities;
using UnityEngine;
using Logger = Unity.Services.Authentication.Utilities.Logger;

namespace Unity.Services.Authentication
{
    public class AuthenticationService
    {
        public static IAuthenticationService Instance { get; internal set; }
    }
}
