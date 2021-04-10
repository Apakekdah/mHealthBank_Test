using mHealthBank.MongoEF.Helpers;
using mHealthBank.MongoEF.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using SAGE.Core.Commons;
using SAGE.Core.Interface;
using SAGE.IoC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mHealthBank.MongoEF
{
    class RepositoryBase<T> : IRepositoryAsync<T>
        where T : class
    {
        readonly string primarys;
        readonly ICollection<string> collectionPrimary;
        readonly ICollection<string> collectionUnique;

        protected readonly IMongoCollection<T> dbset;
        protected IClientSessionHandle session;

        protected IDbFactoryES DatabaseFactory { get; set; }

        protected IMongoContext DataContext
        { get { return DatabaseFactory.Get(); } }

        public virtual object DataObject
        { get { return dbset; } }

        public RepositoryBase(IDisposableIoC life, IDbFactoryES factory)
        {
            DatabaseFactory = factory;

            var tbl = DataContext.GetCollection<T>();

            dbset = tbl.Collection;
            session = tbl.Session;

            var colsKeys = Helper.GetFieldKeyAttributeNew<T>();
            collectionPrimary = new HashSet<string>(colsKeys.Select(f => f.PropertyName).ToArray());
            if (collectionPrimary.Count < 1)
                collectionPrimary.Add("_id");
            primarys = collectionPrimary?.FirstOrDefault() ?? "_id";

            collectionUnique = Helper.GetUniqueKey<T>();
        }

        public virtual Task Add(T entity)
        {
            RefreshTransaction();

            if (collectionUnique.Count > 0)
            {
                IsExistsUnique(entity);
            }

            return Task.Run(() => DataContext.AddCommand(() =>
            {
                if (session == null)
                    return dbset.InsertOneAsync(entity);
                else
                    return dbset.InsertOneAsync(session, entity);
            }));
        }

        public virtual Task Update(T entity)
        {
            RefreshTransaction();

            if (collectionUnique.Count > 0)
            {
                IsExistsUnique(entity, Builders<T>.Filter.Empty);
            }

            ICollection<WriteModel<T>> coll = new HashSet<WriteModel<T>>();
            ICollection<FilterDefinition<T>> colFilter = new HashSet<FilterDefinition<T>>();
            FieldMapName fieldMap;
            FilterDefinition<T> clause = FilterDefinition<T>.Empty;

            var mapTo = Helper.GetFieldMapNameAttribute<T>();

            collectionPrimary.Each(mi =>
            {
                var pv = entity.GetPropertyOrFieldValue(mi);
                fieldMap = mapTo.FirstOrDefault(c => c.OriginalName == mi);
                if (fieldMap != null)
                    mi = fieldMap.NewName;
                colFilter.Add(Builders<T>.Filter.Eq(mi, pv));
            });

            if (colFilter.Count > 0)
            {
                if (colFilter.Count == 1)
                {
                    clause = colFilter.Single();
                }
                else
                {
                    clause = Builders<T>.Filter.And(colFilter);
                }
                coll.Add(new DeleteOneModel<T>(clause));
            }

            colFilter.Clear();

            return Task.Run(() => DataContext.AddCommand(() =>
            {
                if (session != null)
                    return dbset.FindOneAndReplaceAsync(session, clause, entity);
                else
                    return dbset.FindOneAndReplaceAsync(clause, entity);
            }));
        }

        public virtual Task Delete(T entity)
        {
            RefreshTransaction();

            ICollection<WriteModel<T>> coll = new HashSet<WriteModel<T>>();
            ICollection<FilterDefinition<T>> colFilter = new HashSet<FilterDefinition<T>>();
            FieldMapName fieldMap;
            FilterDefinition<T> clause = FilterDefinition<T>.Empty;

            var mapTo = Helper.GetFieldMapNameAttribute<T>();

            collectionPrimary.Each(mi =>
            {
                var pv = entity.GetPropertyOrFieldValue(mi);
                fieldMap = mapTo.FirstOrDefault(c => c.OriginalName == mi);
                if (fieldMap != null)
                    mi = fieldMap.NewName;
                colFilter.Add(Builders<T>.Filter.Eq(mi, pv));
            });

            if (colFilter.Count > 0)
            {
                if (colFilter.Count == 1)
                {
                    clause = colFilter.Single();
                }
                else
                {
                    clause = Builders<T>.Filter.And(colFilter);
                }
                coll.Add(new DeleteOneModel<T>(clause));
            }

            colFilter.Clear();

            return Task.Run(() => DataContext.AddCommand(() =>
            {
                if (session == null)
                    return dbset.DeleteOneAsync(clause);
                else
                    return dbset.DeleteOneAsync(session, clause);
            }));
        }

        public virtual Task Delete(Expression<Func<T, bool>> where)
        {
            RefreshTransaction();

            return Task.Run(() => DataContext.AddCommand(() =>
            {
                if (session == null)
                    return dbset.DeleteManyAsync(where);
                else
                    return dbset.DeleteManyAsync(session, where);
            }));
        }

        public virtual Task Delete(long id)
        {
            RefreshTransaction();

            return Task.Run(() => DataContext.AddCommand(() =>
            {
                if (session == null)
                    return dbset.DeleteOneAsync(Builders<T>.Filter.Eq(primarys, id));
                else
                    return dbset.DeleteOneAsync(session, Builders<T>.Filter.Eq(primarys, id));
            }));
        }

        public virtual Task Delete(string id)
        {
            RefreshTransaction();

            return Task.Run(() => DataContext.AddCommand(() =>
            {
                if (session == null)
                    return dbset.DeleteOneAsync(Builders<T>.Filter.Eq(primarys, id));
                else
                    return dbset.DeleteOneAsync(session, Builders<T>.Filter.Eq(primarys, id));
            }));
        }

        public virtual async Task<bool> Exists(long id)
        {
            RefreshTransaction();

            using var cursor = await dbset.FindAsync(Builders<T>.Filter.Eq(primarys, id), new FindOptions<T>
            {
                BatchSize = 1,
                ReturnKey = true
            }).ConfigureAwait(false);
            return await cursor.AnyAsync().ConfigureAwait(false);
        }

        public virtual async Task<bool> Exists(string id)
        {
            RefreshTransaction();

            using var cursor = await dbset.FindAsync(Builders<T>.Filter.Eq(primarys, id), new FindOptions<T>
            {
                BatchSize = 1,
                ReturnKey = true
            }).ConfigureAwait(false);
            return await cursor.AnyAsync().ConfigureAwait(false);
        }

        public virtual async Task<bool> Exists(Expression<Func<T, bool>> where)
        {
            RefreshTransaction();

            using var cursor = await dbset.FindAsync<T>(where, new FindOptions<T>
            {
                BatchSize = 1,
                ReturnKey = true
            }).ConfigureAwait(false);
            return await cursor.AnyAsync().ConfigureAwait(false);
        }

        public virtual async Task<T> GetById(long id)
        {
            RefreshTransaction();

            using var cursor = await dbset.FindAsync(Builders<T>.Filter.Eq(primarys, id), new FindOptions<T>()
            {
                BatchSize = 1
            }).ConfigureAwait(false);
            return await cursor.FirstOrDefaultAsync().ConfigureAwait(false);
        }

        public virtual async Task<T> GetById(string id)
        {
            RefreshTransaction();

            using var cursor = await dbset.FindAsync(Builders<T>.Filter.Eq(primarys, id), new FindOptions<T>()
            {
                BatchSize = 1
            }).ConfigureAwait(false);
            return await cursor.FirstOrDefaultAsync().ConfigureAwait(false);
        }

        public virtual async Task<T> Get(Expression<Func<T, bool>> where)
        {
            RefreshTransaction();

            using var cursor = await dbset.FindAsync(where, new FindOptions<T>()
            {
                BatchSize = 1
            }).ConfigureAwait(false);
            return await cursor.FirstOrDefaultAsync().ConfigureAwait(false);
        }

        public virtual async Task<IEnumerable<T>> GetAll()
        {
            RefreshTransaction();

            using var cursor = await dbset.FindAsync(Builders<T>.Filter.Empty).ConfigureAwait(false);
            return await cursor.ToListAsync().ConfigureAwait(false);
        }

        public virtual async Task<IEnumerable<T>> GetMany(Expression<Func<T, bool>> where)
        {
            RefreshTransaction();

            using var cursor = await dbset.FindAsync(where).ConfigureAwait(false);
            return await cursor.ToListAsync().ConfigureAwait(false);
        }

        public virtual IQueryable<T> GetQuery()
        {
            return dbset.AsQueryable();
        }

        public virtual IQueryable<T> GetQueryReadOnly()
        {
            return dbset.AsQueryable();
        }

        public virtual IEnumerable<T> WhereRelation(Expression<Func<T, bool>> clause, Action<IIncludeRelation<T>> include)
        {
            throw new NotImplementedException();
        }

        public virtual IQueryable<T> Where(Expression<Func<T, bool>> clause)
        {
            //throw new NotImplementedException();
            return dbset.AsQueryable().Where(clause).AsQueryable();
        }

        public virtual Task Merge(params T[] entityData)
        {
            RefreshTransaction();

            return CreateReplaceValue(true, entityData);
        }

        public virtual Task<IEnumerable<TResult>> ExecCommandList<TResult>(string query, params object[] paramValues) where TResult : class
        {
            throw new NotImplementedException();
        }

        public virtual Task<TResult> ExecCommand<TResult>(string query, params object[] paramValues) where TResult : class
        {
            throw new NotImplementedException();
        }

        public virtual Task<IEnumerable<TResult>> RawSqlList<TResult>(string query, params DbParameterSet[] @params) where TResult : class
        {
            throw new NotImplementedException();
        }

        public virtual Task<TResult> RawSql<TResult>(string query, params DbParameterSet[] @params) where TResult : class
        {
            throw new NotImplementedException();
        }

        public virtual Task AddMany(IEnumerable<T> entities)
        {
            if (!entities.Any())
                return Task.FromResult(0);

            RefreshTransaction();

            ICollection<WriteModel<T>> coll = new HashSet<WriteModel<T>>();

            entities.Each(entity =>
            {
                coll.Add(new InsertOneModel<T>(entity));
            });

            return Task.Run(() => DataContext.AddCommand(() =>
            {
                Task<BulkWriteResult<T>> task;

                if (session == null)
                    task = dbset.BulkWriteAsync(coll.ToArray());
                else
                    task = dbset.BulkWriteAsync(session, coll.ToArray());

                coll.Clear();

                return task;
            }));
        }

        public virtual Task<int> DeleteMany(IEnumerable<T> entities)
        {
            if (!entities.Any())
                return Task.FromResult(0);

            RefreshTransaction();

            ICollection<WriteModel<T>> coll = new HashSet<WriteModel<T>>();
            ICollection<FilterDefinition<T>> colFilter = new HashSet<FilterDefinition<T>>();
            FieldMapName fieldMap;

            var mapTo = Helper.GetFieldMapNameAttribute<T>();

            entities.Each(entity =>
            {
                FilterDefinition<T> clause;

                collectionPrimary.Each(mi =>
                {
                    var pv = entity.GetPropertyOrFieldValue(mi);
                    fieldMap = mapTo.FirstOrDefault(c => c.OriginalName == mi);
                    if (fieldMap != null)
                        mi = fieldMap.NewName;
                    colFilter.Add(Builders<T>.Filter.Eq(mi, pv));
                });

                if (colFilter.Count > 0)
                {
                    if (colFilter.Count == 1)
                    {
                        clause = colFilter.Single();
                    }
                    else
                    {
                        clause = Builders<T>.Filter.And(colFilter);
                    }
                    coll.Add(new DeleteOneModel<T>(clause));
                }

                colFilter.Clear();
            });

            if (!coll.Any())
                return Task.FromResult(0);

            return Task.Run(() =>
            {
                DataContext.AddCommand(() =>
                {
                    Task<BulkWriteResult<T>> task;

                    if (session == null)
                        task = dbset.BulkWriteAsync(coll.ToArray());
                    else
                        task = dbset.BulkWriteAsync(session, coll.ToArray());

                    coll.Clear();

                    return task;
                });
                return Task.FromResult(coll.Count);
            });
        }

        public virtual Task<int> UpdateMany(IEnumerable<T> entities)
        {
            if (!entities.Any())
                return Task.FromResult(0);

            RefreshTransaction();

            return CreateReplaceValue(false, entities);
        }

        private Task<int> CreateReplaceValue(bool upSert, IEnumerable<T> entities)
        {
            ICollection<WriteModel<T>> coll = new HashSet<WriteModel<T>>();
            ICollection<FilterDefinition<T>> colFilter = new HashSet<FilterDefinition<T>>();
            FieldMapName fieldMap;

            var mapTo = Helper.GetFieldMapNameAttribute<T>();

            entities.Each(entity =>
            {
                FilterDefinition<T> clause;

                collectionPrimary.Each(mi =>
                {
                    var pv = entity.GetPropertyOrFieldValue(mi);
                    fieldMap = mapTo.FirstOrDefault(c => c.OriginalName == mi);
                    if (fieldMap != null)
                        mi = fieldMap.NewName;
                    colFilter.Add(Builders<T>.Filter.Eq(mi, pv));
                });

                if (colFilter.Count > 0)
                {
                    if (colFilter.Count == 1)
                    {
                        clause = colFilter.Single();
                    }
                    else
                    {
                        clause = Builders<T>.Filter.And(colFilter);
                    }
                    coll.Add(new ReplaceOneModel<T>(clause, entity) { IsUpsert = upSert });
                }

                colFilter.Clear();
            });

            if (!coll.Any())
                return Task.FromResult(0);

            return Task.Run(() =>
            {
                DataContext.AddCommand(() =>
                {
                    Task<BulkWriteResult<T>> task;

                    if (session == null)
                        task = dbset.BulkWriteAsync(coll.ToArray());
                    else
                        task = dbset.BulkWriteAsync(session, coll.ToArray());

                    coll.Clear();

                    return task;
                });

                return Task.FromResult(entities.Count());
            });
        }

        private void RefreshTransaction()
        {
            if (session != null) return;
            session = DataContext.GetActiveTransaction();
        }

        private bool IsExistsUnique(T entity, FilterDefinition<T> filter = null, bool throwOnExists = true)
        {
            bool isExists = false;

            ICollection<FilterDefinition<T>> cols = new HashSet<FilterDefinition<T>>();
            IDictionary<string, string> dic = new Dictionary<string, string>();

            try
            {
                if ((filter != null) || (filter != Builders<T>.Filter.Empty))
                    cols.Add(filter);

                collectionUnique.Each(field =>
                {
                    var val = entity.GetPropertyOrFieldValue(field);
                    if (val == null)
                    {
                        val = BsonNull.Value;
                        dic[field] = "<null>";
                    }
                    else
                    {
                        dic[field] = val.ToString();
                    }
                    cols.Add(Builders<T>.Filter.Eq(field, val));
                });

                var cursor = dbset.Find(Builders<T>.Filter.And(cols));
                if (cursor.Any())
                {
                    isExists = true;
                }
            }
            finally
            {
                cols.Clear();
            }

            if (isExists)
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("Unique data found for data ");
                dic.Each(kvp =>
                {
                    sb.AppendLine($"{kvp.Key} with value {kvp.Value}");
                });
                sb.Append("please check again your input");

                var tmp = sb.ToString();
                sb.Clear();

                throw new Exception(tmp);
            }

            return isExists;
        }
    }
}