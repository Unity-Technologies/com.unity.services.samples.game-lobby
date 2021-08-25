using System;
using Unity.Burst;

namespace Unity.Networking.Transport
{
    public struct TransportFunctionPointer<T> where T : Delegate
    {
        public TransportFunctionPointer(T executeDelegate)
        {
            Ptr = BurstCompiler.CompileFunctionPointer(executeDelegate);
        }

        internal readonly FunctionPointer<T> Ptr;
    }
}