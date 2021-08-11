#if UNITY_IOS
using System.Runtime.InteropServices;

namespace Unity.Services.Core.Device
{
    class NSUserDefaults
    {
        public static string GetString(string key) => UserDefaultsGetString(key);

        public static void SetString(string key, string value) => UserDefaultsSetString(key, value);

        [DllImport("__Internal", EntryPoint = "UOCPUserDefaultsGetString")]
        static extern string UserDefaultsGetString(string key);

        [DllImport("__Internal", EntryPoint = "UOCPUserDefaultsSetString")]
        static extern void UserDefaultsSetString(string key, string value);
    }
}
#endif
