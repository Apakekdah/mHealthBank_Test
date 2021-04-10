using mHealthBank.Attributes;
using mHealthBank.Shared;
using System;
using System.IO;

namespace mHealthBank.Entities
{
    [TableName("tr_Customer")]
    public class Customer : IEntityModification
    {
        [FieldKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [FieldIndex("idx_cust")]
        public string CustomerName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string KtpImage { get; set; }
        [FieldIgnore]
        public Stream KtpImageStream { get; set; }

        public string CreateBy { get; set; }
        public DateTime CreateDt { get; set; }
        public string UpdateBy { get; set; }
        public DateTime UpdateDt { get; set; }
    }
}