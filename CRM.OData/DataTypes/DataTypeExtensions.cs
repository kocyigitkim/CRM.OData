using Dynamics365.OData.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamics365.OData.DataTypes
{
    public static class DataTypeExtensions
    {
        public static bool Exists<T>(this Null<T> _this)
        {
            return _this != null && _this.HasValue;
        }
    }
}
