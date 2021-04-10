//#define USING_LAMBDA

using mHealthBank.MongoEF.Helpers;
using mHealthBank.MongoEF.Interfaces;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using SAGE.Core.Commons;
using SAGE.IoC;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mHealthBank.MongoEF.Context
{
    internal class MongoContext : Disposable, IMongoContext
    {
        private readonly ConcurrentBag<string> tableMaps;

        private readonly ConcurrentQueue<Func<Task>> commands;
        private readonly IDisposableIoC life;
        private IClientSessionHandle transaction;
        private bool? supportSession;
        private readonly bool isShardHashed;

        private readonly bool enableTransaction;

        public MongoContext(IDisposableIoC life)
        {
            this.life = life;

            RegisterSerializer();

            IConfiguration configuration = life.GetInstance<IConfiguration>();
            ConfigureMongo(configuration);

            commands = new ConcurrentQueue<Func<Task>>();

            tableMaps = life.GetInstance<ConcurrentBag<string>>(Bootstrap.TABLE_MAPS);

            enableTransaction = configuration.GetAs<bool>("MongoSettings:enableTransaction");
            isShardHashed = configuration.GetAs<bool>("MongoSettings:enableShardingHashed");

            life.GetInstance<MongoTabelRunning>();
        }

        protected IMongoDatabase Database { get; set; }

        protected IMongoClient Client { get; set; }

        public void AddCommand(Func<Task> func)
        {
            commands.Enqueue(func);
        }

        public async Task<int> SaveChanges()
        {
            int count = 0;
            try
            {
                if (commands.Count > 0)
                {
                    while (!commands.IsEmpty)
                    {
                        if (!commands.TryDequeue(out Func<Task> fn))
                            throw new Exception("Failed to dequeue comand");
                        await fn().ConfigureAwait(false);
                        count++;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                commands.Clear();
            }

            return count;
        }

        public async Task ClearChanges()
        {
            commands.Clear();

            await Task.FromResult(0).ConfigureAwait(false);
        }

        public IMongoTable<T> GetCollection<T>()
            where T : class
        {
            var tblName = Helper.GetTableNameAttribute<T>();

            IMongoTable<T> tbl;

            lock (tableMaps)
            {
                //var dbset = Database.GetCollection<T>(tblName);
                var dbset = CreateCollection<T>(tblName);

                tbl = life.GetInstance<IMongoTable<T>>(
                    new KeyValueParameter("session", transaction),
                    new KeyValueParameter("collection", dbset)
                );

                if (!tableMaps.Contains(tblName))
                {
                    RebuildIndex<T>(dbset);
                    tableMaps.Add(tblName);
                }
            }

            return tbl;
        }

        public async Task<IClientSessionHandle> CreateTransaction()
        {
            if (!enableTransaction) return null;
            if (transaction != null)
                return transaction;
            transaction = await Client.StartSessionAsync().ConfigureAwait(false);
            return transaction;
        }

        public IClientSessionHandle GetActiveTransaction()
        {
            return transaction;
        }

        public void DestroyTransaction()
        {
            try
            {
                transaction?.Dispose();
            }
            finally
            {
                transaction = null;
            }
        }

        public bool SupportReplicate()
        {
            if (!enableTransaction) return false;
            if (supportSession.HasValue)
                return supportSession.Value;

            try
            {
                var db = Client.GetDatabase("admin");

                var command = new BsonDocumentCommand<BsonDocument>(
                    new BsonDocument() { { "replSetGetStatus", 1 } });

                var result = db.RunCommand(command);

                return (supportSession = true).Value;
            }
            catch (Exception)
            {
                return false;
            }
        }

        protected override void DisposeCore()
        {
            commands?.Clear();

            transaction?.Dispose();

            Client = null;
            Database = null;
        }

        private void RebuildIndex<T>(IMongoCollection<T> dbSet)
        {
            ICollection<IndexKeysDefinition<T>> col;
            ICollection<CreateIndexModel<T>> colIndexModel = new HashSet<CreateIndexModel<T>>();

            var colIndexes = GetIndexFromCollection(dbSet.CollectionNamespace.CollectionName);

            var pks = Helper.GetFieldKeyAttributeNew<T>();
            var dicIndex = Helper.GetFieldIndexAttribute<T>();

            string idxName;

            bool containString = false;
            var typeString = typeof(string);
            var strength = Optional.Create<CollationStrength?>(CollationStrength.Primary);
            var caseLevel = Optional.Create<bool?>(true);
            CreateIndexOptions cio;

            // Checking PK
            if (pks.Count > 0)
            {
                cio = new CreateIndexOptions()
                {
                    Name = $"{dbSet.CollectionNamespace.CollectionName}_pk",
                    // Deprecated : https://docs.mongodb.com/manual/reference/method/db.collection.createIndex/
                    //Background = true,
                    Unique = (isShardHashed ? false : true)
                };

                if (!colIndexes.Contains(cio.Name))
                {
                    if (pks.Count == 1)
                    {
                        var fkd = pks.Single();

                        if (fkd.Type.Equals(typeString))
                        {
                            cio.Collation = new Collation("en_US",
                                strength: strength,
                                caseLevel: caseLevel);
                        }

                        idxName = fkd.MappingPropertyName ?? fkd.PropertyName;
                        colIndexModel.Add(new CreateIndexModel<T>(Builders<T>.IndexKeys.Ascending(idxName), cio));
                    }
                    else if (pks.Count > 1)
                    {
                        col = new HashSet<IndexKeysDefinition<T>>();
                        pks.Each(s =>
                        {
                            idxName = s.MappingPropertyName ?? s.PropertyName;
                            col.Add(Builders<T>.IndexKeys.Ascending(idxName));
                            if (!containString)
                                containString = s.Type.Equals(typeString);
                        });

                        if (containString)
                        {
                            cio.Collation = new Collation("en_US",
                                strength: strength,
                                caseLevel: caseLevel);
                        }

                        colIndexModel.Add(new CreateIndexModel<T>(Builders<T>.IndexKeys.Combine(col.ToArray()), cio));
                        col.Clear();
                    }
                }
            }

            // Check Index
            if (dicIndex?.Count > 0)
            {
                dicIndex.Each(kvp =>
                {
                    containString = false;

                    if (colIndexes.Contains(kvp.Key)) return;

                    col = new HashSet<IndexKeysDefinition<T>>();

                    IndexKeysDefinition<T> ikd;
                    bool isUnique = false;

                    kvp.Value.Details.Each(d =>
                    {
                        idxName = d.MappingPropertyName ?? d.PropertyName;
                        if (d.Ascending)
                        {
                            ikd = Builders<T>.IndexKeys.Ascending(idxName);
                        }
                        else
                        {
                            ikd = Builders<T>.IndexKeys.Descending(idxName);
                        }

                        if (!containString)
                            containString = d.Type.Equals(typeString);

                        isUnique = d.Unique;
                        col.Add(ikd);
                    });

                    if (isShardHashed) isUnique = false;

                    cio = new CreateIndexOptions()
                    {
                        Name = kvp.Key,
                        // Deprecated : https://docs.mongodb.com/manual/reference/method/db.collection.createIndex/
                        //Background = true,
                        Unique = isUnique
                    };

                    if (containString)
                    {
                        cio.Collation = new Collation("en_US",
                            strength: strength,
                            caseLevel: caseLevel);
                    }

                    colIndexModel.Add(new CreateIndexModel<T>(Builders<T>.IndexKeys.Combine(col.ToArray()), cio));

                    col.Clear();
                });
            }

            if (colIndexModel.Count > 0)
                dbSet.Indexes.CreateMany(colIndexModel);

            colIndexModel.Clear();
            colIndexes.Clear();
        }

        private IMongoCollection<T> CreateCollection<T>(string collectionName)
        {
            bool exists;
            var filter = new BsonDocument("name", collectionName);
            using (var cursor = Database.ListCollectionNames(new ListCollectionNamesOptions() { Filter = filter }))
            {
                exists = cursor.Any();
            }
            if (!exists)
            {
                Database.CreateCollection(collectionName, new CreateCollectionOptions
                {
                    NoPadding = true
                });
            }

            return Database.GetCollection<T>(collectionName);
        }

        private ICollection<string> GetIndexFromCollection(string collectionName)
        {
            try
            {
                var cmd = new BsonDocumentCommand<BsonDocument>(
                    new BsonDocument() { { "listIndexes", collectionName } });

                var result = Database.RunCommand(cmd);

                return result["cursor"]["firstBatch"]
                    .AsBsonArray
                    .Select(p => p.AsBsonDocument.GetValue("name").AsString).ToCollection();
            }
            catch { }

            return new HashSet<string>();
        }

        private void ConfigureMongo(IConfiguration configuration)
        {
            if (Client != null)
                return;

            // Configure mongo (You can inject the config, just to simplify)
            Client = new MongoClient(configuration["MongoSettings:connection"]);

            Database = Client.GetDatabase(configuration["MongoSettings:databaseName"]);
        }

        private void RegisterSerializer()
        {
            BsonSerializer.RegisterSerializationProvider(new CustomSerialization());
        }
    }

    class CustomSerialization : IBsonSerializationProvider
    {
        public IBsonSerializer GetSerializer(Type type)
        {
            if (type.Equals(typeof(DateTime)))
            {
                return DateTimeSerializer.LocalInstance;
            }
            else if (type.Equals(typeof(DateTime?)))
            {
                return new NullableSerializer<DateTime>(DateTimeSerializer.LocalInstance);
            }

            return null;
        }
    }
}