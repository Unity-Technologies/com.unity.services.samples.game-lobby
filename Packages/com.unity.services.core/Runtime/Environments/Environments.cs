using System;
using UnityEngine;

namespace Unity.Services.Core.Environments
{
    /// <inheritdoc />
    class Environments : IEnvironments
    {
        public string Current { get; internal set; }
    }
}
