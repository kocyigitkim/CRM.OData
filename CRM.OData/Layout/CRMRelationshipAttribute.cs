using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.OData.Layout
{
    public class CRMRelationshipAttribute : Attribute
    {
        #region Construction
        public CRMRelationshipAttribute(string name, string rootEntity = "", string childEntity = "", string rootEntityFieldName = "", string childEntityFieldName = "")
        {
            this.Name = name;
            RootEntity = rootEntity;
            ChildEntity = childEntity;
            RootEntityFieldName = rootEntityFieldName;
            ChildEntityFieldName = childEntityFieldName;
        }
        #endregion
        #region Fields
        public string Name { get; }
        public string RootEntity { get; }
        public string ChildEntity { get; }
        public string RootEntityFieldName { get; }
        public string ChildEntityFieldName { get; }
        #endregion
    }
}
