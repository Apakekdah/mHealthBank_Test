using AutoMapper;
using mHealthBank.Interfaces;
using System;

namespace mHealthBank.Mapper
{
    public class MappingObject : IMappingObject
    {
        private readonly IMapper map;

        public MappingObject(IMapper mapper)
        {
            map = mapper;
        }

        public TDestination Get<TDestination>(object source) where TDestination : class, new()
        {
            return Get<TDestination>(source, null);
        }

        public TDestination Get<TDestination>(object source, Action<object, TDestination> afterMap = null) where TDestination : class, new()
        {
            TDestination dest;
            try
            {
                if (afterMap != null)
                {
                    dest = map.Map<TDestination>(source, opt => opt.AfterMap(afterMap));
                }
                else
                    dest = map.Map<TDestination>(source);
            }
            catch
            {
                throw;
            }

            return dest;
        }
    }
}