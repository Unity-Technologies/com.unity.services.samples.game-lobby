using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Services.Authentication.Utilities
{
    class ContainerObject
    {
        static bool s_Created;
        static GameObject s_Container;

        public static GameObject Container
        {
            get
            {
                if (!s_Created)
                {
                    s_Container = new GameObject("UnityServicesContainer")
                    {
                        // NOTE: if users complain about this object cluttering up their heirarchy, we can also add HideInHierarchy.
                        hideFlags = HideFlags.NotEditable | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
                    };
                    Object.DontDestroyOnLoad(s_Container);

                    s_Created = true;
                }

                return s_Container;
            }
        }
    }
}
