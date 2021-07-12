using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.Services.Core.Registration")]

// Required for access to Networking API
[assembly: InternalsVisibleTo("Unity.Services.Core.Editor")]
[assembly: InternalsVisibleTo("Unity.Services.Core.Networking")]
[assembly: InternalsVisibleTo("Unity.Services.Core.Internal")]
[assembly: InternalsVisibleTo("Unity.Services.Core.Configuration")]

// Test assemblies
#if UNITY_INCLUDE_TESTS
[assembly: InternalsVisibleTo("Unity.Services.Core.Tests")]

// Required for access to Networking API
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("Unity.Services.Core.EditorTests")]
#endif
