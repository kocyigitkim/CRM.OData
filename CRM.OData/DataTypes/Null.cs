using CRM.OData.Layout;
using CRM.OData;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRM.OData.DataTypes
{
    public class Null<T> : NullBase
    {
        #region Construction
        public Null() { }
        public Null(T value)
        {
            this.Value = value;
        }
        #endregion
        #region Fields
        public new T Value
        {
            get
            {
                if (base.Value == null) return default(T);
                else return (T)(base.Value);
            }
            set
            {
                base.Value = value;
            }
        }
        #endregion
        #region Type Conversations
        public static implicit operator T(Null<T> _this)
        {
            if (_this == null) return default(T);
            else return _this.Value;
        }
        public static implicit operator Null<T>(T value)
        {
            return new Null<T>() { Value = value };
        }
        public static implicit operator Null<T>(CRMEntity value)
        {
            if (value == null) return null;

            if (typeof(T) == typeof(CRMReference))
            {
                return new Null<T>() { Value = (dynamic)value.EntityReference };
            }
            if (typeof(T) == typeof(EntityReference))
            {
                return new Null<T>() { Value = (dynamic)(EntityReference)(dynamic)value.EntityReference };
            }
            return new Null<T>() { Value = default(T) };
        }
      /*  public static bool operator ==(Null<T> l, object r)
        {
            if (l == null && r != null) return false;
            return l.Value.Equals(r);
        }
        public static bool operator !=(Null<T> l, object r)
        {
            return (l == r) == false;
        }
        public static bool operator ==(Null<T> l, T r)
        {
            if (l == null) return false;
            return l.Value.Equals(r);
        }
        public static bool operator !=(Null<T> l, T r)
        {
            return l.Equals(r) == false;
        }*/
        public override bool Equals(object r)
        {
            if (r != null)
            {
                var rType = r.GetType();
                if (rType == typeof(NullBase) || rType.BaseType == typeof(NullBase))
                {
                    r = ((NullBase)r).Value;
                }
            }
            return Value.Equals(r);
        }

        public override int GetHashCode()
        {
            return HasValue ? Value.GetHashCode() : 0;
        }
        #endregion
    }
}
