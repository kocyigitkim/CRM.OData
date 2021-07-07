using CRM.OData.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRM.OData
{
    public class EntityReference
    {
        public string LogicalName { get; set; }
        public Guid Id { get; set; }
        public EntityReference() { }
        public EntityReference(string logicalName, Guid id)
        {
            this.LogicalName = logicalName;
            this.Id = id;
        }
    }
    public class Money
    {
        public decimal Value { get; set; }
        public Money() { }
        public Money(decimal value)
        {
            this.Value = value;
        }
    }
    public class Entity
    {
        public Dictionary<string, object> Attributes { get; internal set; } = new Dictionary<string, object>();
        public string LogicalName { get; set; }
        public CRMId Id { get; internal set; }

        public bool HasAttribute(string name)
        {
            return Attributes.ContainsKey(name);
        }
        public bool TryGetAttributeValue<T>(string name, out T value)
        {
            value = default(T);
            if (HasAttribute(name))
            {
                var v = GetAttribute(name);
                if (v != null && v.GetType() == typeof(string))
                {
                    value = (T)(dynamic)v;
                    return true;
                }
                value = TypeConverter.Convert<T>(v);
                return true;
            }
            return false;
        }
        public T GetAttributeValue<T>(string name)
        {
            if (TryGetAttributeValue<T>(name, out var value))
            {
                return value;
            }
            return default(T);
        }
        public object GetAttribute(string name)
        {
            return Attributes[name];
        }
        public void SetAttribute(string name, object value)
        {
            Attributes[name] = value;
        }
        public object this[string name]
        {
            get { return GetAttribute(name); }
            set { SetAttribute(name, value); }
        }
        public Entity() { }
        public Entity(string Name)
        {
            this.LogicalName = Name;
        }

        internal void SetAttributes<T>(KeyValuePair<string, T>[] fields)
        {
            foreach (var field in fields)
            {
                this.SetAttribute(field.Key, TypeConverter.ConvertToString(field.Value));
            }
        }
    }
    public class EntityCollection
    {
        internal List<Entity> entities { get; private set; } = new List<Entity>();
        public int RecordCount => entities.Count;
        public int TotalRecordCount { get; internal set; }
        public int Page { get; internal set; }
        public bool MoreRecords { get; internal set; }
        public bool TotalRecordCountLimitExceeded { get; internal set; }

        public void Add(Entity entity)
        {
            this.entities.Add(entity);
        }
        public void Remove(Entity entity)
        {
            this.entities.Remove(entity);
        }
        public void Clear()
        {
            this.entities.Clear();
        }
        public Entity Find(Guid id)
        {
            return entities.Where(p => p.Id.Id == id).FirstOrDefault();
        }
    }
    public class OptionSetValue
    {
        public int Value { get; set; }
        public OptionSetValue() { Value = 0; }
        public OptionSetValue(int value)
        {
            this.Value = value;
        }
    }

    public enum EntityState
    {
        Unknown = -1,
        Active = 0,
        Inactive = 1,
        Disabled = 2
    }
}
