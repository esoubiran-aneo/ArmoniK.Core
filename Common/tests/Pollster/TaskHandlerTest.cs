// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2022. All rights reserved.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
//   J. Fonseca        <jfonseca@aneo.fr>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY, without even the implied warranty of
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

using ArmoniK.Api.gRPC.V1.Agent;
using ArmoniK.Core.Common.Pollster;
using ArmoniK.Core.Common.Storage;
using ArmoniK.Core.Common.Stream.Worker;
using ArmoniK.Core.Common.Tests.Helpers;

using Google.Protobuf.WellKnownTypes;

using Moq;

using NUnit.Framework;

using Result = ArmoniK.Api.gRPC.V1.Agent.Result;
using TaskOptions = ArmoniK.Api.gRPC.V1.TaskOptions;
using TaskRequest = ArmoniK.Core.Common.gRPC.Services.TaskRequest;
using TaskStatus = ArmoniK.Api.gRPC.V1.TaskStatus;

namespace ArmoniK.Core.Common.Tests.Pollster;

[TestFixture]
public class TaskHandlerTest
{
  [SetUp]
  public void SetUp()
  {
  }

  [TearDown]
  public virtual void TearDown()
  {
  }

  [Test]
  public void InitializeTaskHandler()
  {
    var mockStreamHandler       = new Mock<IWorkerStreamHandler>();
    var mockQueueMessageHandler = new Mock<IQueueMessageHandler>();
    var mockAgentHandler        = new Mock<IAgentHandler>();
    using var testServiceProvider = new TestTaskHandlerProvider(mockStreamHandler.Object,
                                                                mockAgentHandler.Object,
                                                                mockQueueMessageHandler.Object);
  }

  [Test]
  // Mocks are not initialized so it is expected that the acquisition should not work
  public async Task AcquireTaskShouldFail()
  {
    var mockStreamHandler       = new Mock<IWorkerStreamHandler>();
    var mockQueueMessageHandler = new Mock<IQueueMessageHandler>();
    var mockAgentHandler        = new Mock<IAgentHandler>();
    using var testServiceProvider = new TestTaskHandlerProvider(mockStreamHandler.Object,
                                                                mockAgentHandler.Object,
                                                                mockQueueMessageHandler.Object);

    var acquired = await testServiceProvider.TaskHandler.AcquireTask()
                                            .ConfigureAwait(false);

    Assert.IsFalse(acquired);
  }

  [Test]
  public async Task AcquireTaskShouldSucceed()
  {
    var sqmh = new SimpleQueueMessageHandler
               {
                 CancellationToken = CancellationToken.None,
                 Status            = QueueMessageStatus.Waiting,
                 MessageId = Guid.NewGuid()
                                 .ToString(),
               };

    var mockStreamHandler = new Mock<IWorkerStreamHandler>();
    var mockAgentHandler  = new Mock<IAgentHandler>();
    using var testServiceProvider = new TestTaskHandlerProvider(mockStreamHandler.Object,
                                                                mockAgentHandler.Object,
                                                                sqmh);

    var taskRequests = new List<TaskRequest>();
    taskRequests.Add(new TaskRequest(new List<string>
                                     {
                                       "ExpectedOutput",
                                     },
                                     new List<string>(),
                                     new List<ReadOnlyMemory<byte>>
                                     {
                                       ReadOnlyMemory<byte>.Empty,
                                     }.ToAsyncEnumerable()));

    await testServiceProvider.PartitionTable.CreatePartitionsAsync(new[]
                                                                   {
                                                                     new PartitionData("part1",
                                                                                       new List<string>(),
                                                                                       10,
                                                                                       10,
                                                                                       20,
                                                                                       1,
                                                                                       new PodConfiguration(new Dictionary<string, string>())),
                                                                     new PartitionData("part2",
                                                                                       new List<string>(),
                                                                                       10,
                                                                                       10,
                                                                                       20,
                                                                                       1,
                                                                                       new PodConfiguration(new Dictionary<string, string>())),
                                                                   })
                             .ConfigureAwait(false);

    var sessionId = (await testServiceProvider.Submitter.CreateSession(new[]
                                                                       {
                                                                         "part1",
                                                                         "part2",
                                                                       },
                                                                       new TaskOptions
                                                                       {
                                                                         MaxDuration = Duration.FromTimeSpan(TimeSpan.FromMinutes(2)),
                                                                         MaxRetries  = 2,
                                                                         Priority    = 1,
                                                                         PartitionId = "part1",
                                                                       },
                                                                       CancellationToken.None)
                                              .ConfigureAwait(false)).SessionId;

    var (requests, priority, whichPartitionId) = await testServiceProvider.Submitter.CreateTasks(sessionId,
                                                                                                 sessionId,
                                                                                                 new TaskOptions
                                                                                                 {
                                                                                                   MaxDuration = Duration.FromTimeSpan(TimeSpan.FromMinutes(2)),
                                                                                                   MaxRetries  = 2,
                                                                                                   Priority    = 1,
                                                                                                   PartitionId = "part1",
                                                                                                 },
                                                                                                 taskRequests.ToAsyncEnumerable(),
                                                                                                 CancellationToken.None)
                                                                          .ConfigureAwait(false);

    await testServiceProvider.Submitter.FinalizeTaskCreation(requests,
                                                             priority,
                                                             whichPartitionId,
                                                             sessionId,
                                                             sessionId,
                                                             CancellationToken.None)
                             .ConfigureAwait(false);

    var taskId = requests.First()
                         .Id;

    sqmh.TaskId = taskId;

    var acquired = await testServiceProvider.TaskHandler.AcquireTask()
                                            .ConfigureAwait(false);

    Assert.IsTrue(acquired);
    Assert.AreEqual(taskId,
                    testServiceProvider.TaskHandler.GetAcquiredTask());
  }

  [Test]
  public async Task AcquireNotReadyTaskShouldFail()
  {
    var sqmh = new SimpleQueueMessageHandler
               {
                 CancellationToken = CancellationToken.None,
                 Status            = QueueMessageStatus.Waiting,
                 MessageId = Guid.NewGuid()
                                 .ToString(),
               };

    var mockStreamHandler = new Mock<IWorkerStreamHandler>();
    var mockAgentHandler  = new Mock<IAgentHandler>();
    using var testServiceProvider = new TestTaskHandlerProvider(mockStreamHandler.Object,
                                                                mockAgentHandler.Object,
                                                                sqmh);

    var taskRequests = new List<TaskRequest>();
    taskRequests.Add(new TaskRequest(new List<string>
                                     {
                                       "ExpectedOutput",
                                     },
                                     new List<string>
                                     {
                                       "DataDep",
                                     },
                                     new List<ReadOnlyMemory<byte>>
                                     {
                                       ReadOnlyMemory<byte>.Empty,
                                     }.ToAsyncEnumerable()));

    await testServiceProvider.PartitionTable.CreatePartitionsAsync(new[]
                                                                   {
                                                                     new PartitionData("part1",
                                                                                       new List<string>(),
                                                                                       10,
                                                                                       10,
                                                                                       20,
                                                                                       1,
                                                                                       new PodConfiguration(new Dictionary<string, string>())),
                                                                     new PartitionData("part2",
                                                                                       new List<string>(),
                                                                                       10,
                                                                                       10,
                                                                                       20,
                                                                                       1,
                                                                                       new PodConfiguration(new Dictionary<string, string>())),
                                                                   })
                             .ConfigureAwait(false);

    var sessionId = (await testServiceProvider.Submitter.CreateSession(new[]
                                                                       {
                                                                         "part1",
                                                                         "part2",
                                                                       },
                                                                       new TaskOptions
                                                                       {
                                                                         MaxDuration = Duration.FromTimeSpan(TimeSpan.FromMinutes(2)),
                                                                         MaxRetries  = 2,
                                                                         Priority    = 1,
                                                                         PartitionId = "part1",
                                                                       },
                                                                       CancellationToken.None)
                                              .ConfigureAwait(false)).SessionId;

    var (requests, priority, partitionId) = await testServiceProvider.Submitter.CreateTasks(sessionId,
                                                                                            sessionId,
                                                                                            new TaskOptions
                                                                                            {
                                                                                              MaxDuration = Duration.FromTimeSpan(TimeSpan.FromMinutes(2)),
                                                                                              MaxRetries  = 2,
                                                                                              Priority    = 1,
                                                                                              PartitionId = "part1",
                                                                                            },
                                                                                            taskRequests.ToAsyncEnumerable(),
                                                                                            CancellationToken.None)
                                                                     .ConfigureAwait(false);

    await testServiceProvider.Submitter.FinalizeTaskCreation(requests,
                                                             priority,
                                                             partitionId,
                                                             sessionId,
                                                             sessionId,
                                                             CancellationToken.None)
                             .ConfigureAwait(false);

    var taskId = requests.First()
                         .Id;

    sqmh.TaskId = taskId;

    var acquired = await testServiceProvider.TaskHandler.AcquireTask()
                                            .ConfigureAwait(false);

    Assert.IsFalse(acquired);
  }


  [Test]
  public async Task ExecuteTaskShouldSucceed()
  {
    var sqmh = new SimpleQueueMessageHandler
               {
                 CancellationToken = CancellationToken.None,
                 Status            = QueueMessageStatus.Waiting,
                 MessageId = Guid.NewGuid()
                                 .ToString(),
               };

    var sh = new SimpleWorkerStreamHandler();

    var agentHandler = new SimpleAgentHandler();
    using var testServiceProvider = new TestTaskHandlerProvider(sh,
                                                                agentHandler,
                                                                sqmh);

    var taskRequests = new List<TaskRequest>();
    taskRequests.Add(new TaskRequest(new List<string>
                                     {
                                       "ExpectedOutput",
                                     },
                                     new List<string>(),
                                     new List<ReadOnlyMemory<byte>>
                                     {
                                       ReadOnlyMemory<byte>.Empty,
                                     }.ToAsyncEnumerable()));

    await testServiceProvider.PartitionTable.CreatePartitionsAsync(new[]
                                                                   {
                                                                     new PartitionData("part1",
                                                                                       new List<string>(),
                                                                                       10,
                                                                                       10,
                                                                                       20,
                                                                                       1,
                                                                                       new PodConfiguration(new Dictionary<string, string>())),
                                                                     new PartitionData("part2",
                                                                                       new List<string>(),
                                                                                       10,
                                                                                       10,
                                                                                       20,
                                                                                       1,
                                                                                       new PodConfiguration(new Dictionary<string, string>())),
                                                                   })
                             .ConfigureAwait(false);

    var sessionId = (await testServiceProvider.Submitter.CreateSession(new[]
                                                                       {
                                                                         "part1",
                                                                         "part2",
                                                                       },
                                                                       new TaskOptions
                                                                       {
                                                                         MaxDuration = Duration.FromTimeSpan(TimeSpan.FromMinutes(2)),
                                                                         MaxRetries  = 2,
                                                                         Priority    = 1,
                                                                         PartitionId = "part1",
                                                                       },
                                                                       CancellationToken.None)
                                              .ConfigureAwait(false)).SessionId;

    var (requests, priority, partitionId) = await testServiceProvider.Submitter.CreateTasks(sessionId,
                                                                                            sessionId,
                                                                                            new TaskOptions
                                                                                            {
                                                                                              MaxDuration = Duration.FromTimeSpan(TimeSpan.FromMinutes(2)),
                                                                                              MaxRetries  = 2,
                                                                                              Priority    = 1,
                                                                                              PartitionId = "part1",
                                                                                            },
                                                                                            taskRequests.ToAsyncEnumerable(),
                                                                                            CancellationToken.None)
                                                                     .ConfigureAwait(false);

    await testServiceProvider.Submitter.FinalizeTaskCreation(requests,
                                                             priority,
                                                             partitionId,
                                                             sessionId,
                                                             sessionId,
                                                             CancellationToken.None)
                             .ConfigureAwait(false);

    var taskId = requests.First()
                         .Id;

    sqmh.TaskId = taskId;

    var acquired = await testServiceProvider.TaskHandler.AcquireTask()
                                            .ConfigureAwait(false);

    Assert.IsTrue(acquired);

    await testServiceProvider.TaskHandler.PreProcessing()
                             .ConfigureAwait(false);

    await testServiceProvider.TaskHandler.ExecuteTask()
                             .ConfigureAwait(false);

    await testServiceProvider.TaskHandler.PostProcessing()
                             .ConfigureAwait(false);

    Assert.AreEqual(TaskStatus.Completed,
                    (await testServiceProvider.TaskTable.GetTaskStatus(new[]
                                                                       {
                                                                         taskId,
                                                                       })
                                              .ConfigureAwait(false)).Single()
                                                                     .Status);
  }

  [Test]
  public async Task ExecuteTaskWithResultsShouldSucceed()
  {
    var sqmh = new SimpleQueueMessageHandler
               {
                 CancellationToken = CancellationToken.None,
                 Status            = QueueMessageStatus.Waiting,
                 MessageId = Guid.NewGuid()
                                 .ToString(),
               };

    var sh = new SimpleWorkerStreamHandler();

    var agentHandler = new SimpleAgentHandler();
    using var testServiceProvider = new TestTaskHandlerProvider(sh,
                                                                agentHandler,
                                                                sqmh);

    var taskRequests = new List<TaskRequest>();
    taskRequests.Add(new TaskRequest(new List<string>
                                     {
                                       "ExpectedOutput",
                                     },
                                     new List<string>(),
                                     new List<ReadOnlyMemory<byte>>
                                     {
                                       ReadOnlyMemory<byte>.Empty,
                                     }.ToAsyncEnumerable()));

    await testServiceProvider.PartitionTable.CreatePartitionsAsync(new[]
                                                                   {
                                                                     new PartitionData("part1",
                                                                                       new List<string>(),
                                                                                       10,
                                                                                       10,
                                                                                       20,
                                                                                       1,
                                                                                       new PodConfiguration(new Dictionary<string, string>())),
                                                                     new PartitionData("part2",
                                                                                       new List<string>(),
                                                                                       10,
                                                                                       10,
                                                                                       20,
                                                                                       1,
                                                                                       new PodConfiguration(new Dictionary<string, string>())),
                                                                   })
                             .ConfigureAwait(false);

    var sessionId = (await testServiceProvider.Submitter.CreateSession(new[]
                                                                       {
                                                                         "part1",
                                                                         "part2",
                                                                       },
                                                                       new TaskOptions
                                                                       {
                                                                         MaxDuration = Duration.FromTimeSpan(TimeSpan.FromMinutes(2)),
                                                                         MaxRetries  = 2,
                                                                         Priority    = 1,
                                                                         PartitionId = "part1",
                                                                       },
                                                                       CancellationToken.None)
                                              .ConfigureAwait(false)).SessionId;

    var (requests, priority, partitionId) = await testServiceProvider.Submitter.CreateTasks(sessionId,
                                                                                            sessionId,
                                                                                            new TaskOptions
                                                                                            {
                                                                                              MaxDuration = Duration.FromTimeSpan(TimeSpan.FromMinutes(2)),
                                                                                              MaxRetries  = 2,
                                                                                              Priority    = 1,
                                                                                              PartitionId = "part1",
                                                                                            },
                                                                                            taskRequests.ToAsyncEnumerable(),
                                                                                            CancellationToken.None)
                                                                     .ConfigureAwait(false);

    await testServiceProvider.Submitter.FinalizeTaskCreation(requests,
                                                             priority,
                                                             partitionId,
                                                             sessionId,
                                                             sessionId,
                                                             CancellationToken.None)
                             .ConfigureAwait(false);

    var taskId = requests.First()
                         .Id;

    sqmh.TaskId = taskId;

    var acquired = await testServiceProvider.TaskHandler.AcquireTask()
                                            .ConfigureAwait(false);

    Assert.IsTrue(acquired);

    await testServiceProvider.TaskHandler.PreProcessing()
                             .ConfigureAwait(false);

    await testServiceProvider.TaskHandler.ExecuteTask()
                             .ConfigureAwait(false);


    var taskStreamReader = new TestHelperAsyncStreamReader<CreateTaskRequest>(new[]
                                                                              {
                                                                                new CreateTaskRequest(),
                                                                              });
    if (agentHandler.Agent == null)
    {
      throw new NullReferenceException(nameof(agentHandler.Agent));
    }

    await agentHandler.Agent.CreateTask(taskStreamReader,
                                        CancellationToken.None)
                      .ConfigureAwait(false);


    var resultStreamReader = new TestHelperAsyncStreamReader<Result>(new[]
                                                                     {
                                                                       new Result(),
                                                                     });
    await agentHandler.Agent.SendResult(resultStreamReader,
                                        CancellationToken.None)
                      .ConfigureAwait(false);

    await testServiceProvider.TaskHandler.PostProcessing()
                             .ConfigureAwait(false);

    Assert.AreEqual(TaskStatus.Completed,
                    (await testServiceProvider.TaskTable.GetTaskStatus(new[]
                                                                       {
                                                                         taskId,
                                                                       })
                                              .ConfigureAwait(false)).Single()
                                                                     .Status);
  }
}
