﻿// This file is part of ArmoniK project.
// 
// Copyright (c) ANEO. All rights reserved.
//   W. Kirschenmann <wkirschenmann@aneo.fr>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using ArmoniK.Core.Exceptions;
using ArmoniK.Core.gRPC;
using ArmoniK.Core.gRPC.V1;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

namespace ArmoniK.Core.Storage
{
  [PublicAPI]
  public class QueueStorageWrapper : IQueueStorage
  {
    private readonly ILockedQueueStorage                                             lockedQueueStorage_;
    private readonly ILeaseProvider                                                  leaseProvider_;
    private readonly ConcurrentDictionary<string, LockedQueueMessageDeadlineHandler> deadlineHandlers_ = new ();
    private readonly ConcurrentDictionary<string, LeaseHandler>                      leaseHandlers_    = new ();
    private readonly ILogger<QueueStorageWrapper>                                    logger_;

    /// <inheritdoc />
    public int MaxPriority => lockedQueueStorage_.MaxPriority;

    /// <inheritdoc />
    public async IAsyncEnumerable<QueueMessage> PullAsync(int nbMessages,
                                                          [EnumeratorCancellation] 
                                                          CancellationToken cancellationToken = default)
    {
      using var logFunction = logger_.LogFunction();
      await foreach (var qm in lockedQueueStorage_.PullAsync(nbMessages, cancellationToken)
                                                  .WithCancellation(cancellationToken))
      {
        using var logScope = logger_.BeginPropertyScope(("messageId", qm.MessageId), ("taskId", qm.TaskId.ToPrintableId()));

        var cancellationTokens = new List<CancellationToken>();

        var deadlineHandler = lockedQueueStorage_.GetDeadlineHandler(qm.MessageId, logger_, cancellationToken);
        if (!deadlineHandlers_.TryAdd(qm.MessageId, deadlineHandler))
        {
          throw new ArmoniKException($"A deadline handler already exists for message {qm.MessageId}");
        }

        deadlineHandler.MessageLockLost.Register(() => deadlineHandlers_.TryRemove(qm.MessageId, out _));

        cancellationTokens.Add(deadlineHandler.MessageLockLost);

        if (!lockedQueueStorage_.AreMessagesUnique)
        {
            LeaseHandler lease;
          try
          {
            lease = await leaseProvider_.GetLeaseHandlerAsync(qm.TaskId, logger_, cancellationToken);
            lease.LeaseExpired.ThrowIfCancellationRequested();
          }
          catch (Exception e)
          {
            logger_.LogWarning(e, "Could not acquire lease. Message is considered as a duplicate and will be deleted");
            var deleteTask = lockedQueueStorage_.DeleteAsync(qm.MessageId, cancellationToken);
            await deadlineHandler.DisposeAsync();
            await deleteTask;
            continue;
          }

          if (!leaseHandlers_.TryAdd(qm.MessageId, lease))
          {
            await deadlineHandler.DisposeAsync();
            throw new ArmoniKException($"A lease handler already exists for message {qm.MessageId}");
          }

          deadlineHandler.MessageLockLost.Register(() =>
                                                   {
                                                     if(leaseHandlers_.TryRemove(qm.MessageId, out var handler))
                                                       handler.DisposeAsync().AsTask().Wait(cancellationToken);
                                                   });

          lease.LeaseExpired.Register(() =>
                                      {
                                        if (deadlineHandlers_.TryRemove(qm.MessageId, out var handler))
                                          handler.DisposeAsync().AsTask().Wait(cancellationToken);
                                      });

          cancellationTokens.Add(lease.LeaseExpired);
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokens.ToArray());

        yield return qm with { CancellationToken = cts.Token };
      }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
      leaseHandlers_.TryRemove(id, out var leaseHandler);
      deadlineHandlers_.TryRemove(id, out var deadlineHandler);
      await lockedQueueStorage_.DeleteAsync(id, cancellationToken);

      if (deadlineHandler is not null)
        await deadlineHandler.DisposeAsync();
      if (leaseHandler is not null)
        await leaseHandler.DisposeAsync();
    }

    /// <inheritdoc />
    public Task EnqueueMessagesAsync(IEnumerable<TaskId> messages,
                                           int                 priority          = 1,
                                           CancellationToken   cancellationToken = default)
      => lockedQueueStorage_.EnqueueMessagesAsync(messages, priority, cancellationToken);

    /// <inheritdoc />
    public async Task RequeueMessageAsync(string id, CancellationToken cancellationToken = default)
    {
      leaseHandlers_.TryRemove(id, out var leaseHandler);
      deadlineHandlers_.TryRemove(id, out var deadlineHandler);
      await lockedQueueStorage_.RequeueMessageAsync(id, cancellationToken);

      if (deadlineHandler is not null)
        await deadlineHandler.DisposeAsync();
      if (leaseHandler is not null)
        await leaseHandler.DisposeAsync();
    }
  }

  [PublicAPI]
  public interface IQueueStorage
  { int MaxPriority { get; }

    IAsyncEnumerable<QueueMessage> PullAsync(int nbMessages, CancellationToken cancellationToken = default);

    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    Task EnqueueMessagesAsync(IEnumerable<TaskId> messages,
                              int                 priority          = 1,
                              CancellationToken   cancellationToken = default);

    Task RequeueMessageAsync(string message, CancellationToken cancellationToken = default);
  }
}
