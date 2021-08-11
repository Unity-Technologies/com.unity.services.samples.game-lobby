using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.Services.Core.Registration")]

#if UNITY_INCLUDE_TESTS
[assembly: InternalsVisibleTo("Unity.Services.Core.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
#endif
