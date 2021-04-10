using Microsoft.Extensions.Configuration;
using SAGE.Core.Commons;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace mHealthBank
{
    public static class Extensions
    {
        public static ICollection<TSource> ToCollection<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
                return new HashSet<TSource>();
            return new HashSet<TSource>(source);
        }

        public static T GetAs<T>(this IConfiguration configuration, string key, T defaultValue = default)
        {
            var theT = typeof(T);
            if (!theT.IsEnum && !theT.IsPrimitive)
                throw new NotSupportedException($"Type of '{theT.Name}' is not support");
            var value = configuration[key];
            if (value == null)
                return defaultValue;
            return (T)Convert.ChangeType(value, theT);
        }

        public static bool IsClosesAsType(this Type type, Type asType)
        {
            if (type.IsGenericTypeDefinition)
            {
                if (type.IsInterface)
                {
                    return asType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == type);
                }

                if (type.IsGenericType && type.GetGenericTypeDefinition() == asType)
                    return true;

                Type baseType = type.BaseType;
                if (baseType == null) return false;

                return IsClosesAsType(baseType, asType);
            }
            return type.IsAssignableFrom(asType);
        }

        private static IDictionary<string, ICollection<string>> dicContentType = new Dictionary<string, ICollection<string>>();

        public static string GetContentType(this string name)
        {
            if (name.IsNullOrEmpty())
                return null;

            const string DEFAULT_MIME = "application/octet-stream";

            if (dicContentType.Count == 0)
            {
                dicContentType.Add("image/jpeg",
                    new HashSet<string>(new[] {
                        ".jpeg", ".jpg"
                        }));
                dicContentType.Add("image/png", new HashSet<string>(new[] { ".png" }));
                dicContentType.Add("image/bmp", new HashSet<string>(new[] { ".bmp" }));
            }

            var ext = Path.GetExtension("name").ToLower();

            return dicContentType.FirstOrDefault(kvp => kvp.Value.Contains(ext)).Key ?? DEFAULT_MIME;
        }
    }

    public static class DateTimeHelper
    {
        public static DateTime MinimumDate => new DateTime(1900, 1, 1);

        public static DateTime GetMinimumDate(this DateTime date)
        {
            return MinimumDate;
        }
    }
}
