using System;

namespace mHealthBank.Interfaces
{
    public interface IMappingObject
    {
        TDestination Get<TDestination>(object source) where TDestination : class, new();
        TDestination Get<TDestination>(object source, Action<object, TDestination> afterMap = null) where TDestination : class, new();
    }
}