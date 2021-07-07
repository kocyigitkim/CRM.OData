using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRM.OData.Metadata
{
    public class MetadataResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public IEnumerable<EntityMetadata> Metadata { get; set; }
    }
    public class EntityMetadata
    {
        public string Name { get; internal set; }
        public string PrimaryKey { get; internal set; }
        public FieldMetadata[] Fields { get; internal set; }
    }
    public class FieldMetadata
    {
        public string Name { get; internal set; }
        public string SchemaName { get; internal set; }
        public string Type { get; internal set; }
        public string LookupEntityName { get; internal set; }
        public string LookupEntityPrimaryKey { get; internal set; }
    }
}
