using System;

namespace mHealthBank.Shared
{
    public interface IEntityModification
    {
        string CreateBy { get; set; }
        DateTime CreateDt { get; set; }
        string UpdateBy { get; set; }
        DateTime UpdateDt { get; set; }
    }
}