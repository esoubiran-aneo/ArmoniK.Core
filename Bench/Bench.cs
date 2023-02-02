using ArmoniK.Core.Adapters.LocalStorage;
using ArmoniK.Core.Adapters.Redis;
using ArmoniK.Core.Adapters.S3;
using ArmoniK.Core.Common.Storage;
using ArmoniK.Core.Common.Utils;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Toolchains.DotNetCli;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Serilog;

namespace Bench;

[MemoryDiagnoser()]
public class Bench
{
    //[Benchmark]
    //public byte[] Md5() => md5.ComputeHash(data);

    private IAsyncEnumerable<byte[]> asyncByteArray;
    private IAsyncEnumerable<ReadOnlyMemory<byte>> asyncByteReadOnlyMemory;

    [Params(
            //1000 * 1000,
            1000 * 100,
            1000 * 1000,
            1000 * 10000
    )]
    public int chunkSize;

    private Microsoft.Extensions.Logging.ILogger iLogger_;
    private IObjectStorage? storageObjectS3_;
    private IObjectStorage? storageObjectRedis_;

    [Params(
    1000 * 1000 * 10,
    1000 * 1000 * 5,
    1000 * 500,
    1000 * 100,
    1000 * 10,
    1000 * 1,
    1
    )]
    public int totalSize;

    [GlobalSetup]
    public async void Setup()
  {
    //if you are using minio in docker locally you can use this config and uncomment //.AddInMemoryCollection(minimalConfig)
    //Dictionary<string, string> minimalConfig = new()
    //{
    //    {
    //        "Components:ObjectStorage", "ArmoniK.Adapters.S3.ObjectStorage"
    //    },
    //    {
    //        "S3:BucketName", "miniobucket"
    //    },
    //    {
    //        "S3:EndpointUrl", "http://127.0.0.1:9000"
    //    },
    //    {
    //        "S3:Login", "minioadmin"
    //    },
    //    {
    //        "S3:Password", "minioadmin"
    //    },
    //    {
    //        "S3:MustForcePathStyle", "true"
    //    }
    //};

    storageObjectS3_ = GetStorageObject("S3");
    storageObjectRedis_ = GetStorageObject("Redis");
  }

  private IObjectStorage GetStorageObject(string adapterName)
  {
    var configuration = new ConfigurationManager();
    configuration
  //.AddInMemoryCollection(minimalConfig)
  .AddEnvironmentVariables()
  .AddInMemoryCollection(new Dictionary<string, string>() { { "Components:ObjectStorage", $"ArmoniK.Adapters.{adapterName}.ObjectStorage" } });
  ;

    var services = new ServiceCollection();
    services.AddLogging();
    var logger = new LoggerInit(configuration);
    services.AddRedis(configuration,
                    logger.GetLogger())
                .AddLocalStorage(configuration,
                    logger.GetLogger())
                .AddS3(configuration,
                    logger.GetLogger());

    var provider = services.BuildServiceProvider(new ServiceProviderOptions
    {
      ValidateOnBuild = true,
    });


    var objectFactory = provider.GetRequiredService<IObjectStorageFactory>();
    objectFactory.Init(CancellationToken.None).Wait();

    return objectFactory.CreateObjectStorage($"bench_{adapterName}");
  }

  [Benchmark]
    public void WRITE_S3()
    {
      storageObjectS3_!.AddOrUpdateAsync("bench_S3", asyncByteReadOnlyMemory).Wait();
    }

  [Benchmark]
  public void WRITE_Redis()
  {
    storageObjectRedis_!.AddOrUpdateAsync("bench_Redis", asyncByteReadOnlyMemory).Wait();
  }

  [Benchmark]
  public async Task READ_S3()
  {
    int count = 0;
    await foreach (var asyncByteArray in storageObjectS3_!.GetValuesAsync("bench_S3"))
    {
      count++;
    }
  }

  [Benchmark]
  public async Task READ_Redis()
  {
    int count = 0;
    await foreach (var asyncByteArray in storageObjectRedis_!.GetValuesAsync("bench_Redis"))
    {
      count++;
    }
  }


  [IterationSetup]
    public void IterationSetup()
    {
        //asyncByteArray = ConvertStringToIAsyncEnumerable(totalSize, chunkSize);
        asyncByteReadOnlyMemory = GetReadOnlyMemoryOfBytes(totalSize, chunkSize);
        //storageObjectS3_!.AddOrUpdateAsync("bench_S3_Read_Perf", asyncByteReadOnlyMemory).Wait();
        //storageObjectRedis_!.AddOrUpdateAsync("bench_Redis_Read_Perf", asyncByteReadOnlyMemory).Wait();

  }

  public static IAsyncEnumerable<byte[]> ConvertStringToIAsyncEnumerable(int totalSize, int chunkSize)
    {
        var chunkString = Enumerable.Repeat(Convert.ToByte('a'),
            totalSize).Chunk(chunkSize);

      //tolist because we want memory alloc during the IterationSetup
      return chunkString.ToList().ToAsyncEnumerable();
    }

    public static IAsyncEnumerable<ReadOnlyMemory<byte>> GetReadOnlyMemoryOfBytes(int totalSize, int chunkSize)
    {
        var chunkString = Enumerable.Repeat(Convert.ToByte('a'),
                totalSize)
            .Chunk(chunkSize).Select(elem => new ReadOnlyMemory<byte>(elem));

    //tolist because we want memory alloc during the IterationSetup
      return chunkString.ToList().ToAsyncEnumerable();
    }
}
