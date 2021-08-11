using Unity.Services.Core.Internal;

namespace Unity.Services.Core.Editor
{
    interface IServiceFlagRequest
    {
        IAsyncOperation<IServiceFlags> FetchServiceFlags();
    }
}
