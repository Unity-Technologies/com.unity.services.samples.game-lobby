using System.Runtime.CompilerServices;
using UnityEngine.Scripting;

[assembly: AlwaysLinkAssembly]

[assembly: InternalsVisibleTo("Unity.Services.Core.Registration")]

// Test assemblies
#if UNITY_INCLUDE_TESTS
[assembly: InternalsVisibleTo("Unity.Services.Core.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
#endif
