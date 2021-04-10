using System;
using System.Collections.Generic;

namespace mHealthBank.MongoEF.Helpers
{
    class FieldIndexHeader
    {
        public FieldIndexHeader()
        {
            Details = new HashSet<FieldIndexDetail>();
        }

        public string IndexName { get; set; }

        public ICollection<FieldIndexDetail> Details { get; set; }
    }

    class FieldIndexDetail
    {
        public string PropertyName { get; set; }
        public bool Ascending { get; set; } = true;
        public Type Type { get; set; }
        public bool Unique { get; set; }
        public string MappingPropertyName { get; set; }
    }

    class FieldMapName
    {
        public string OriginalName { get; set; }
        public string NewName { get; set; }
        public int Order { get; set; } = -1;
    }

    class FieldKeyDetail
    {
        public string PropertyName { get; set; }
        public string MappingPropertyName { get; set; }
        public Type Type { get; internal set; }
    }
}