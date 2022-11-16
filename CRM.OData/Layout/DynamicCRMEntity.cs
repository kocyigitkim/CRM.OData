using Dynamics365.OData.DataTypes;
using Dynamics365.OData.Layout;
using Dynamics365.OData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamics365.OData.Layout
{
    public class DynamicCRMEntity : CRMEntity
    {
        public object GetAttribute(string Name)
        {
            if (this.EntityRecord.Attributes.TryGetValue(Name, out var value)) return value;
            else
                return null;
        }
        public void SetAttribute(string Name, object Value)
        {
            this.EntityRecord.Attributes[Name] = Value;
        }
        public bool HasAttribute(string Name)
        {
            return this.EntityRecord.Attributes.ContainsKey(Name);
        }
        public void SetEntityName(string Name)
        {
            this.CRMEntityName = Name;
            if (this.EntityRecord != null)
            {
                this.EntityRecord.LogicalName = Name;
            }
        }
        public void SetEntityId(CRMId id)
        {
            this.CRMEntityId = id;
            if (this.EntityRecord != null)
            {
                this.EntityRecord.Id = id;
            }
        }
        public DynamicCRMEntity()
        {
            this.EntityRecord = new Entity();
        }
        public void SetEntityRecord(Entity entity)
        {
            this.EntityRecord = entity;
        }
    }
}
