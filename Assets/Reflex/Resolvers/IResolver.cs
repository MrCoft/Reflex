using System;
using Reflex.Core;
using Reflex.Enums;

namespace Reflex.Resolvers
{
    public interface IResolver : IDisposable
    {
        Lifetime Lifetime { get; }
        object Resolve(Container container);
        void SetOwner(Container container) { }
    }
}