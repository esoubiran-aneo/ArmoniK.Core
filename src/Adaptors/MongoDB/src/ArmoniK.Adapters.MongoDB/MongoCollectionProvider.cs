﻿using System.Threading;

using ArmoniK.Core.Injection;

using JetBrains.Annotations;

using Microsoft.Extensions.Options;

using MongoDB.Driver;


namespace ArmoniK.Adapters.MongoDB
{
    [PublicAPI]
    public class MongoCollectionProvider<TDataModel>
      : ProviderBase<IMongoCollection<TDataModel>>
      where TDataModel : IMongoDataModel<TDataModel>, new()
    {
        public MongoCollectionProvider(IOptions<Options.MongoDB> options,
                                       SessionProvider sessionProvider,
                                       IMongoDatabase mongoDatabase,
                                       CancellationToken cancellationToken = default) :
          base(async () =>
               {
                   var model = new TDataModel();
                   try
                   {
                       await mongoDatabase.CreateCollectionAsync(
                           model.CollectionName, new CreateCollectionOptions<TDataModel> { ExpireAfter = options.Value.DataRetention }, cancellationToken);
                   }
                   catch (MongoCommandException)
                   {
                   }

                   var output = mongoDatabase.GetCollection<TDataModel>(model.CollectionName);
                   var session = await sessionProvider.GetAsync();
                   try
                   {
                       await model.InitializeIndexesAsync(session, output);
                   }
                   catch (MongoCommandException)
                   {
                   }
                   return output;
               })
        {
        }
    }
}
