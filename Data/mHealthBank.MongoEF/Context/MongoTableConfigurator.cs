using mHealthBank.MongoEF.Helpers;
using mHealthBank.MongoEF.Interfaces;
using MongoDB.Bson.Serialization;
using SAGE.Core.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace mHealthBank.MongoEF.Context
{
    class MongoTableConfigurator<T> : IMongoTableConfigurable<T>
        where T : class, new()
    {
        private readonly Type typeT;

        private string tableName;

        public MongoTableConfigurator()
        {
            typeT = typeof(T);

            tableName = Helper.GetTableNameAttribute<T>();
        }

        public void Apply()
        {
            var newTableName = TableName();
            if (newTableName.IsNullOrEmpty())
                throw new NullReferenceException("Table name required");

            tableName = newTableName;

            RegisterClass();
        }

        protected string TableName()
        {
            return tableName;
        }

        protected virtual bool Map(MemberInfo member, BsonMemberMap map)
        {
            return false;
        }

        private IDictionary<Type, BsonClassMap> GetBaseClass()
        {
            var type = typeof(T);
            var obj = typeof(object);
            BsonClassMap bcm = null;

            var col = new List<Type>();
            var colBcm = new List<BsonClassMap>();

            col.Add(type);

            while (!obj.Equals(type.BaseType) || (type.BaseType == null))
            {
                type = type.BaseType;
                col.Add(type);
            }
            var dic = new Dictionary<Type, BsonClassMap>();

            for (var n = col.Count - 1; n >= 0; n--)
            {
                type = col[n];
                bcm = new BsonClassMap(type, bcm);
                bcm.SetIgnoreExtraElements(true);
                bcm.SetIgnoreExtraElementsIsInherited(true);
                colBcm.Add(bcm);
            }

            for (var n = colBcm.Count - 1; n >= 0; n--)
            {
                bcm = colBcm[n];
                dic[bcm.ClassType] = bcm;
            }

            col.Clear();
            colBcm.Clear();

            return dic;
        }

        private void RegisterClass()
        {
            var typ = typeof(T);

            if (BsonClassMap.IsClassMapRegistered(typ))
                return;

            var basC = typ.BaseType;
            IDictionary<Type, ICollection<MemberInfo>> dicColMI = new Dictionary<Type, ICollection<MemberInfo>>();

            var dicBcm = GetBaseClass();

            var members = typ
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(f => (MemberInfo)f)
                .Union(typ.GetFields(BindingFlags.Public | BindingFlags.Instance)
                            .Select(f => (MemberInfo)f)).ToCollection();

            BsonClassMap bcm;
            FieldMapName fmn;
            BsonMemberMap bmm;

            var colMapField = Helper.GetFieldMapNameAttribute<T>();
            var colIgnore = Helper.GetFieldIgnoreAttribute<T>();
            var idx = 0;
            var isIdType = false;

            members.Each(mi =>
            {
                if (dicBcm.ContainsKey(mi.DeclaringType))
                {
                    bcm = dicBcm[mi.DeclaringType];

                    if (colIgnore.Contains(mi.Name))
                    {
                        if (mi.MemberType == MemberTypes.Field)
                            bcm.UnmapField(mi.Name);
                        else
                            bcm.UnmapProperty(mi.Name);
                        return;
                    }

                    if (Helper.FieldId.Contains(mi.Name, StringComparer.InvariantCultureIgnoreCase))
                    {
                        isIdType = true;
                        if (mi.MemberType == MemberTypes.Field)
                            bcm.UnmapField(mi.Name);
                        else
                            bcm.UnmapProperty(mi.Name);
                    }

                    if (mi.MemberType == MemberTypes.Field)
                        bmm = bcm.MapField(mi.Name);
                    else
                        bmm = bcm.MapProperty(mi.Name);
                    //bmm = bcm.MapMember(mi);

                    if (!Map(mi, bmm))
                    {
                        fmn = colMapField.FirstOrDefault(f => f.OriginalName == mi.Name);
                        if (fmn != null)
                        {
                            bmm.SetOrder(fmn.Order);
                            if (!fmn.NewName.IsNullOrEmpty() && !fmn.NewName.IsNullOrWhiteSpace())
                                bmm.SetElementName(fmn.NewName);
                        }

                        if (isIdType)
                        {
                            isIdType = false;
                            bmm.SetOrder(idx);
                        }
                    }

                    idx++;
                }
            });

            bcm = dicBcm.FirstOrDefault().Value;

            BsonClassMap.RegisterClassMap(bcm);

            members.Clear();
            dicBcm.Clear();
        }
    }
}