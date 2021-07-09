using System;

namespace Unity.Services.Relay.Models
{
    public interface IOneOf
    {
        Type Type { get; }
        object Value { get; }
    }
}