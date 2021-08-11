using System;
using System.IO;
using UnityEngine;
using Unity.Services.Core.Internal;

namespace Unity.Services.Core.Configuration.Editor
{
    static class IoUtils
    {
        const string k_MetaExtension = ".meta";

        public static bool TryDeleteAssetFile(string path)
        {
            return TryDeleteFile(path) && TryDeleteFile(path + k_MetaExtension);
        }

        static bool TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    return true;
                }
            }
            catch (Exception e)
            {
                CoreLogger.LogException(e);
            }

            return false;
        }

        public static void TryDeleteAssetFolder(string path)
        {
            if (TryDeleteFolder(path))
            {
                TryDeleteFile(path + k_MetaExtension);
            }
        }

        static bool TryDeleteFolder(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                    return true;
                }
            }
            catch (Exception e)
            {
                CoreLogger.LogException(e);
            }

            return false;
        }
    }
}
