using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamics365.OData
{
    internal static class ReflectionExtensions
    {
        internal static T GetCustomAttribute<T>(this Type t)
        {
            return (T)(t.GetCustomAttributes(true).Where(p => p.GetType() == typeof(T))).FirstOrDefault();
        }
    }
}
