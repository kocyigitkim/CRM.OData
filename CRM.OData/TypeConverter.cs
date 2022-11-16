using Dynamics365.OData.DataTypes;
using Newtonsoft.Json.Linq;
using System;

namespace Dynamics365.OData
{
    internal class TypeConverter
    {
        internal static T Convert<T>(object v)
        {
            try
            {
                if (v == null) return default(T);

                var t = typeof(T);
                if (t == typeof(object)) t = v.GetType();

                if (t == typeof(Guid))
                {
                    return (T)(dynamic)Guid.Parse(v.ToString());
                }
                else if (t == typeof(OptionSetValue))
                {
                    return (T)(dynamic)new OptionSetValue(int.Parse(v.ToString()));
                }
                else if(t == typeof(CRMMoney))
                {
                    
                }
                return (T)System.Convert.ChangeType(v, t);
            }
            catch
            {
                return default(T);
            }
        }

        internal static string ConvertToString<T>(T value)
        {
            if (value == null) return null;
            if(value is decimal ||value is double)
            {
                return value.ToString().Replace(",", ".");
            }
            else if(value is CRMReference)
            {
                return ConvertToString(((EntityReference)(CRMReference)(dynamic)value));
            }
            else if(value is EntityReference)
            {
                var eref = (EntityReference)(dynamic)value;
                return "/" + eref.LogicalName.GetPlural() + $"({eref.Id.ToString().Replace("{", "").Replace("}", "").ToLower()})";
            }
            else if(value is CRMMoney)
            {
                return ConvertToString(((CRMMoney)(dynamic)value).Value);
            }
            else if (value is CRMId)
            {
                return ConvertToString(((CRMId)(dynamic)value).Id);
            }
            else if(value is Guid)
            {
                return value.ToString().Replace("{", "").Replace("}", "").ToLower();
            }
            return value.ToString();
        }

        internal static JToken ConvertToJson(object value)
        {
            if (value == null) return JValue.CreateNull();
            else if (value is OptionSetValue) return ((OptionSetValue)value).Value;
            else if (value is Money) return ((Money)value).Value;
            else if (value is EntityReference)
            {
                return ConvertToJson(((EntityReference)value).Id);
            }
            else if(value is DateTime)
            {
                return ConvertToJson(((DateTime)value).ToUniversalTime().ToString("s") + "Z");
            }
            return JValue.FromObject(value);
        }
    }
}