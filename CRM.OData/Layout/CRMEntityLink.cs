using System.Reflection;

namespace CRM.OData.Layout
{
    internal class CRMEntityLink
    {
        internal CRMFieldAttribute Field { get; set; }
        internal MemberInfo FieldDefinition { get; set; }
        internal string SourceEntityName { get; set; }
        internal string SourceFieldName { get; set; }
        internal string ChildEntityName { get; set; }
        internal string ChildFieldName { get; set; }
        internal bool FetchRuntime { get; set; }
    }
}