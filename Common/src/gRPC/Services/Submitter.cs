﻿// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2022. All rights reserved.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.Core.Common.Exceptions;
using ArmoniK.Core.Common.Storage;
using ArmoniK.Core.Common.Utils;

using Google.Protobuf;

using Grpc.Core;

using Microsoft.Extensions.Logging;

using KeyNotFoundException = ArmoniK.Core.Common.Exceptions.KeyNotFoundException;
using TaskStatus = ArmoniK.Api.gRPC.V1.TaskStatus;

namespace ArmoniK.Core.Common.gRPC.Services;

public class Submitter : ISubmitter
{
  private readonly IQueueStorage                    lockedQueueStorage_;
  private readonly ILogger<Submitter> logger_;
  private readonly ITableStorage                    tableStorage_;
  private readonly IObjectStorageFactory            objectStorageFactory_;

  public Submitter(ITableStorage         tableStorage,
                   IQueueStorage         lockedQueueStorage,
                   IObjectStorageFactory objectStorageFactory,
                   ILogger<Submitter>    logger)
  {
    tableStorage_         = tableStorage;
    objectStorageFactory_ = objectStorageFactory;
    logger_               = logger;
    lockedQueueStorage_   = lockedQueueStorage;
  }

  private IObjectStorage ResultStorage(string  session) => objectStorageFactory_.CreateResultStorage(session);
  private IObjectStorage PayloadStorage(string session) => objectStorageFactory_.CreateResultStorage(session);


  /// <inheritdoc />
  public  Task<ConfigurationReply> GetServiceConfiguration(Empty request, CancellationToken cancellationToken)
    => Task.FromResult(new ConfigurationReply()
                       {
                         DataChunkMaxSize = PayloadConfiguration.MaxChunkSize,
                       });

  public  async Task<Empty> CancelSession(string sessionId, CancellationToken cancellationToken)
  {
    using var _ = logger_.LogFunction();

    if (logger_.IsEnabled(LogLevel.Trace))
    {
      cancellationToken.Register(() => logger_.LogTrace("CancellationToken from ServerCallContext has been triggered"));
    }

    try
    {
      await tableStorage_.CancelSessionAsync(sessionId,
                                             cancellationToken);
    }
    catch (KeyNotFoundException e)
    {
      throw new RpcException(new(StatusCode.FailedPrecondition,
                                 e.Message));
    }
    catch (Exception e)
    {
      throw new RpcException(new(StatusCode.Unknown,
                                 e.Message));
    }

    return new();
  }

  /// <inheritdoc />
  async Task ISubmitter.CancelDispatchSessionAsync(string dispatchId, CancellationToken cancellationToken)
  {
    await CancelDispatchSessionAsync(dispatchId,
                                cancellationToken);
  }

  /// <inheritdoc />
  async Task ISubmitter.CancelTasks(TaskFilter       request,    CancellationToken cancellationToken)
  {
    await CancelTasks(request,
                      cancellationToken);
  }

  /// <inheritdoc />
  async Task ISubmitter.CancelSession(string sessionId, CancellationToken cancellationToken)
  {
    await CancelSession(sessionId,
                        cancellationToken);
  }

  /// <inheritdoc />
  public async Task<Empty> CancelDispatchSessionAsync(string dispatchId, CancellationToken cancellationToken)
  {
    using var _ = logger_.LogFunction(dispatchId);
    await tableStorage_.CancelDispatchAsync(dispatchId,
                                            cancellationToken);
    return new();
  }

  public  async Task<Empty> CancelTasks(TaskFilter request, CancellationToken cancellationToken)
  {
    using var _ = logger_.LogFunction();

    if (logger_.IsEnabled(LogLevel.Trace))
    {
      cancellationToken.Register(() => logger_.LogTrace("CancellationToken from ServerCallContext has been triggered"));
    }

    try
    {
      await tableStorage_.CancelTasks(request,
                                     cancellationToken);
    }
    catch (KeyNotFoundException e)
    {
      throw new RpcException(new(StatusCode.FailedPrecondition,
                                 e.Message));
    }
    catch (Exception e)
    {
      throw new RpcException(new(StatusCode.Unknown,
                                 e.Message));
    }

    return new();
  }

  /// <inheritdoc />
  public async Task<CreateTaskReply> CreateTasks(string                        sessionId,
                                                 string                        parentId,
                                                 string                        dispatchId,
                                                 TaskOptions                   options,
                                                 IAsyncEnumerable<TaskRequest> taskRequests,
                                                 CancellationToken             cancellationToken)
  {

    using var logFunction = logger_.LogFunction(dispatchId);
    using var sessionScope = logger_.BeginPropertyScope(("Session", sessionId),
                                                        ("TaskId", parentId),
                                                        ("Dispatch", dispatchId));

    if (logger_.IsEnabled(LogLevel.Trace))
    {
      cancellationToken.Register(() => logger_.LogTrace("CancellationToken from ServerCallContext has been triggered"));
    }

    options ??= await tableStorage_.GetDefaultTaskOptionAsync(sessionId,
                                                              cancellationToken);

    if (options.Priority >= lockedQueueStorage_.MaxPriority)
    {
      var exception = new RpcException(new(StatusCode.InvalidArgument,
                                           $"Max priority is {lockedQueueStorage_.MaxPriority}"));
      logger_.LogError(exception,
                       "Invalid Argument");
      throw exception;
    }




    var requests           = new List<ITableStorage.TaskRequest>();
    var payloadUploadTasks = new List<Task>();

    await foreach (var taskRequest in taskRequests.WithCancellation(cancellationToken))
    {
      if (await taskRequest.PayloadChunks.CountAsync(cancellationToken) == 1)
      {
        requests.Add(new(taskRequest.Id,
                         taskRequest.ExpectedOutputKeys,
                         taskRequest.DataDependencies,
                         await taskRequest.PayloadChunks.SingleAsync(cancellationToken)));
      }
      else
      {
        requests.Add(new(taskRequest.Id,
                         taskRequest.ExpectedOutputKeys,
                         taskRequest.DataDependencies,
                         null));
        payloadUploadTasks.Add(PayloadStorage(sessionId).AddOrUpdateAsync(taskRequest.Id,
                                                                          taskRequest.PayloadChunks,
                                                                          cancellationToken));
      }
    }

    await tableStorage_.InitializeTaskCreationAsync(sessionId,
                                                    parentId,
                                                    dispatchId,
                                                    options,
                                                    requests,
                                                    cancellationToken);


    var finalizationFilter = new TaskFilter
                             {
                               Task = new()
                                      {
                                        Ids =
                                        {
                                          requests.Select(taskRequest => taskRequest.Id),
                                        },
                                      },
                             };
    await using var finalizer = AsyncDisposable.Create(async () => await tableStorage_.FinalizeTaskCreation(finalizationFilter,
                                                                                                            cancellationToken));


    var enqueueTask = lockedQueueStorage_.EnqueueMessagesAsync(requests.Select(taskRequest => taskRequest.Id),
                                                               options.Priority,
                                                               cancellationToken);

    await Task.WhenAll(enqueueTask,
                       Task.WhenAll(payloadUploadTasks));



    return new()
           {
             Successfull = new(),
           };
  }

  /// <inheritdoc />
  public  async Task<Count> CountTasks(TaskFilter request, CancellationToken cancellationToken)

  {
    using var _ = logger_.LogFunction();

    if (logger_.IsEnabled(LogLevel.Trace))
    {
      cancellationToken.Register(() => logger_.LogTrace("CancellationToken from ServerCallContext has been triggered"));
    }

    var count = await tableStorage_.CountTasksAsync(request,
                                                    cancellationToken);
    return new()
           {
             Values =
             {
               count.Select(tuple => new StatusCount
                                     {
                                       Status = tuple.Status,
                                       Count  = tuple.Count,
                                     }),
             },
           };
  }

  /// <inheritdoc />
  public async Task<CreateSessionReply> CreateSession(string sessionId, TaskOptions defaultTaskOptions, CancellationToken cancellationToken)
  {
    try
    {
      await tableStorage_.CreateSessionAsync(sessionId,
                                             defaultTaskOptions,
                                             cancellationToken);
      return new()
             {
               Ok = new(),
             };
    }
    catch (Exception e)
    {
      Console.WriteLine(e);
      return new()
             {
               Error = e.ToString(),
             };
    }
  }

  /// <inheritdoc />
  public  async Task TryGetResult(ResultRequest request, IServerStreamWriter<ResultReply> responseStream, CancellationToken cancellationToken)
  {
    var storage = ResultStorage(request.Session);
    await foreach (var chunk in storage.TryGetValuesAsync(request.Key, cancellationToken))
    {
      await responseStream.WriteAsync(new()
                                      {
                                        Result = UnsafeByteOperations.UnsafeWrap(new(chunk)),
                                      });
    }
  }

  public  async Task<Count> WaitForCompletion(WaitRequest request, CancellationToken cancellationToken)
  {
    using var _ = logger_.LogFunction();

    if (logger_.IsEnabled(LogLevel.Trace))
    {
      cancellationToken.Register(() => logger_.LogTrace("CancellationToken from ServerCallContext has been triggered"));
    }


    Task<IEnumerable<(TaskStatus Status, int Count)>> CountUpdateFunc()
      => tableStorage_.CountTasksAsync(request.Filter,
                                       cancellationToken);

    var output          = new Count();
    var countUpdateFunc = CountUpdateFunc;
    while (true)
    {
      var counts       = await countUpdateFunc();
      var notCompleted = 0;
      var error        = false;
      var cancelled    = false;

      // ReSharper disable once PossibleMultipleEnumeration
      foreach (var (status, count) in counts)
      {
        switch (status)
        {
          case TaskStatus.Creating:
            notCompleted += count;
            break;
          case TaskStatus.Submitted:
            notCompleted += count;
            break;
          case TaskStatus.Dispatched:
            notCompleted += count;
            break;
          case TaskStatus.Completed:
            break;
          case TaskStatus.Failed:
            notCompleted += count;
            error        =  true;
            break;
          case TaskStatus.Timeout:
            notCompleted += count;
            break;
          case TaskStatus.Canceling:
            notCompleted += count;
            cancelled    =  true;
            break;
          case TaskStatus.Canceled:
            notCompleted += count;
            cancelled    =  true;
            break;
          case TaskStatus.Processing:
            notCompleted += count;
            break;
          case TaskStatus.Error:
            notCompleted += count;
            break;
          case TaskStatus.Unspecified:
            notCompleted += count;
            break;
          case TaskStatus.Processed:
            notCompleted += count;
            break;
          default:
            throw new ArmoniKException($"Unknown TaskStatus {status}");
        }
      }

      if (notCompleted == 0 || (request.StopOnFirstTaskError && error) || (request.StopOnFirstTaskCancellation && cancelled))
      {
        // ReSharper disable once PossibleMultipleEnumeration
        output.Values.AddRange(counts.Select(tuple => new StatusCount
                                                      {
                                                        Count  = tuple.Count,
                                                        Status = tuple.Status,
                                                      }));
        logger_.LogDebug("All sub tasks have completed. Returning count={count}",
                         output);
        break;
      }


      await Task.Delay(tableStorage_.PollingDelay,
                       cancellationToken);
    }

    return output;
  }

  /// <inheritdoc />
  public async Task UpdateTaskStatusAsync(string id, TaskStatus status, CancellationToken cancellationToken = default)
  {
    using var _ = logger_.LogFunction();
    await tableStorage_.UpdateTaskStatusAsync(id,status, cancellationToken);
  }

  /// <inheritdoc />
  public async Task FinalizeDispatch(string taskId, IDispatch dispatch, Output output, CancellationToken cancellationToken)
  {
    var oldDispatchId = dispatch.Id;
    var targetDispatchId = await tableStorage_.GetDispatchId(taskId,
                                                             cancellationToken);
    while (oldDispatchId != targetDispatchId)
    {
      await tableStorage_.ChangeTaskDispatch(oldDispatchId,
                                             targetDispatchId,
                                             cancellationToken);

      // to be done after awaiting previous call to ensure proper modification sequencing
      await tableStorage_.ChangeResultDispatch(oldDispatchId,
                                               targetDispatchId,
                                               cancellationToken);

      oldDispatchId = targetDispatchId;
      targetDispatchId = await tableStorage_.GetDispatchId(taskId,
                                                           cancellationToken);
    }
  }
}
