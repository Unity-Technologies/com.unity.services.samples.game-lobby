using System;

namespace Unity.Services.Lobbies.Models
{
    public interface IOneOf
    {
        Type Type { get; }
        object Value { get; }
    }
}