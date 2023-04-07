#if UNITY_EDITOR
using ParrelSync;
#endif

/// <summary>
/// Generates unique profile names per Parrelsync clone, but could be expanded to include unique build profile names. (Using launch parameters for example)
/// </summary>
public static class LocalProfileTool
{
    static string s_LocalProfileSuffix;

    public static string LocalProfileSuffix => s_LocalProfileSuffix ??= GetCloneNameEnd();

    static string GetCloneNameEnd()
    {
#if UNITY_EDITOR

        //The code below makes it possible for the clone instance to log in as a different user profile in Authentication service.
        //This allows us to test services integration locally by utilising Parrelsync.
        if (ClonesManager.IsClone())
        {
            var cloneName = ClonesManager.GetCurrentProject().name;
            var lastUnderscoreIndex = cloneName.LastIndexOf("_"); // Get the last occurrence of "_" in the string
            var numberStr =
                cloneName.Substring(lastUnderscoreIndex +
                    1); // Extract the substring that follows the last occurrence of "_"

            return numberStr;
        }
#endif
        return "";
    }
}