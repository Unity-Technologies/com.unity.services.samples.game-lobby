#if ENABLE_EDITOR_GAME_SERVICES
using System;
using UnityEditor;

namespace Unity.Services.Authentication.Editor
{
    static class AuthenticationTopMenu
    {
        const int k_ConfigureMenuPriority = 100;
        const int k_ToolsMenuPriority = k_ConfigureMenuPriority + 11;
        const string k_ServiceMenuRoot = "Services/Authentication/";

        [MenuItem(k_ServiceMenuRoot + "Configure", priority = k_ConfigureMenuPriority)]
        static void ShowProjectSettings()
        {
            SettingsService.OpenProjectSettings("Project/Services/Authentication");
        }
    }
}
#endif
