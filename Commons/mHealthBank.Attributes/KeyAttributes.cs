using System;

namespace mHealthBank.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TableName : Attribute
    {
        public TableName(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class FieldKey : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class FieldForeignIgnore : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class FieldIndex : Attribute
    {
        public FieldIndex(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
        public bool Ascending { get; set; } = true;
        public bool Unique { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class FieldIgnore : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class FieldName : Attribute
    {
        public FieldName(string name = null)
        {
            Name = name;
        }

        public string Name { get; private set; }
        public int Order { get; set; } = -1;
    }
}