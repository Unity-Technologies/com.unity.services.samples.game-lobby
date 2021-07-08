using System.Runtime.CompilerServices;
using UnityEngine.Scripting;

[assembly: AlwaysLinkAssembly]

#if UNITY_INCLUDE_TESTS
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("Unity.Services.Core.Tests")]
#endif
