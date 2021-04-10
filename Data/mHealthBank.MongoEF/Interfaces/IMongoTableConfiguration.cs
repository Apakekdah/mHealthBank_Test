namespace mHealthBank.MongoEF.Interfaces
{
    interface IMongoTableConfigurable<T>
        where T : class, new()
    {
        void Apply();
    }
}