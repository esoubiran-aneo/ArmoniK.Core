// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2023. All rights reserved.
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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using ArmoniK.Core.Base.DataStructures;
using ArmoniK.Core.Common.Exceptions;
using ArmoniK.Core.Common.Storage;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using NUnit.Framework;

namespace ArmoniK.Core.Common.Tests.TestBase;

[TestFixture]
public class PartitionTableTestBase
{
  [SetUp]
  public async Task SetUp()
  {
    GetPartitionTableInstance();

    if (!RunTests || CheckForSkipSetup())
    {
      return;
    }

    await PartitionTable!.Init(CancellationToken.None)
                         .ConfigureAwait(false);

    await PartitionTable!.CreatePartitionsAsync(new[]
                                                {
                                                  new PartitionData("PartitionId0",
                                                                    new List<string>(),
                                                                    1,
                                                                    12,
                                                                    50,
                                                                    2,
                                                                    new PodConfiguration(new Dictionary<string, string>())),
                                                  new PartitionData("PartitionId1",
                                                                    new List<string>(),
                                                                    1,
                                                                    10,
                                                                    50,
                                                                    1,
                                                                    new PodConfiguration(new Dictionary<string, string>())),
                                                  new PartitionData("PartitionId2",
                                                                    new List<string>
                                                                    {
                                                                      "ParentPartitionId",
                                                                    },
                                                                    1,
                                                                    13,
                                                                    50,
                                                                    1,
                                                                    new PodConfiguration(new Dictionary<string, string>())),
                                                })
                         .ConfigureAwait(false);
  }

  [TearDown]
  public virtual void TearDown()
  {
    PartitionTable = null;
    RunTests       = false;
  }

  private static bool CheckForSkipSetup()
  {
    var category = TestContext.CurrentContext.Test.Properties.Get("Category") as string;
    return category is "SkipSetUp";
  }

  /* Interface to test */
  protected IPartitionTable? PartitionTable;

  /* Boolean to control that tests are executed in
   * an instance of this class */
  protected bool RunTests;

  /* Function be override so it returns the suitable instance
   * of PartitionTable to the corresponding interface implementation */
  public virtual void GetPartitionTableInstance()
  {
  }

  [Test]
  [Category("SkipSetUp")]
  public async Task InitShouldSucceed()
  {
    if (RunTests)
    {
      Assert.AreNotEqual(HealthStatus.Healthy,
                         (await PartitionTable!.Check(HealthCheckTag.Liveness)
                                               .ConfigureAwait(false)).Status);
      Assert.AreNotEqual(HealthStatus.Healthy,
                         (await PartitionTable.Check(HealthCheckTag.Readiness)
                                              .ConfigureAwait(false)).Status);
      Assert.AreNotEqual(HealthStatus.Healthy,
                         (await PartitionTable.Check(HealthCheckTag.Startup)
                                              .ConfigureAwait(false)).Status);

      await PartitionTable.Init(CancellationToken.None)
                          .ConfigureAwait(false);

      Assert.AreEqual(HealthStatus.Healthy,
                      (await PartitionTable.Check(HealthCheckTag.Liveness)
                                           .ConfigureAwait(false)).Status);
      Assert.AreEqual(HealthStatus.Healthy,
                      (await PartitionTable.Check(HealthCheckTag.Readiness)
                                           .ConfigureAwait(false)).Status);
      Assert.AreEqual(HealthStatus.Healthy,
                      (await PartitionTable.Check(HealthCheckTag.Startup)
                                           .ConfigureAwait(false)).Status);
    }
  }

  [Test]
  [Category("DoSetup")]
  public async Task ReadPartitionAsyncShouldSucceed()
  {
    if (RunTests)
    {
      var result = await PartitionTable!.ReadPartitionAsync("PartitionId0",
                                                            CancellationToken.None)
                                        .ConfigureAwait(false);

      Assert.AreEqual("PartitionId0",
                      result.PartitionId);
    }
  }

  [Test]
  public Task ReadTaskAsyncShouldThrowException()
  {
    if (RunTests)
    {
      Assert.ThrowsAsync<PartitionNotFoundException>(() => PartitionTable!.ReadPartitionAsync("PartitionIdNotFound",
                                                                                              CancellationToken.None));
    }

    return Task.CompletedTask;
  }

  [Test]
  public Task DeleteTaskAsyncShouldThrowException()
  {
    if (RunTests)
    {
      Assert.ThrowsAsync<PartitionNotFoundException>(() => PartitionTable!.DeletePartitionAsync("PartitionIdNotFound",
                                                                                                CancellationToken.None));
    }

    return Task.CompletedTask;
  }

  [Test]
  public async Task DeleteAlreadyDeletedTaskAsyncShouldThrowException()
  {
    if (RunTests)
    {
      await PartitionTable!.DeletePartitionAsync("PartitionId0",
                                                 CancellationToken.None)
                           .ConfigureAwait(false);

      Assert.ThrowsAsync<PartitionNotFoundException>(() => PartitionTable!.DeletePartitionAsync("PartitionId0",
                                                                                                CancellationToken.None));
    }
  }

  [Test]
  public async Task ArePartitionExistingAsyncShouldSucceed()
  {
    if (RunTests)
    {
      var result = await PartitionTable!.ArePartitionsExistingAsync(new[]
                                                                    {
                                                                      "PartitionId0",
                                                                      "PartitionId1",
                                                                    },
                                                                    CancellationToken.None)
                                        .ConfigureAwait(false);

      Assert.IsTrue(result);
    }
  }

  [Test]
  public async Task ArePartitionExistingAsyncShouldReturnFalse()
  {
    if (RunTests)
    {
      var result = await PartitionTable!.ArePartitionsExistingAsync(new[]
                                                                    {
                                                                      "PartitionId0",
                                                                      "PartitionIdNotExisting",
                                                                    },
                                                                    CancellationToken.None)
                                        .ConfigureAwait(false);

      Assert.IsFalse(result);
    }
  }

  [Test]
  public async Task ListPartitionsEmptyResultShouldSucceed()
  {
    if (RunTests)
    {
      var listTasks = await PartitionTable!.ListPartitionsAsync(data => data.PartitionId == "NotExisting",
                                                                data => data.ParentPartitionIds,
                                                                false,
                                                                0,
                                                                20,
                                                                CancellationToken.None)
                                           .ConfigureAwait(false);

      Assert.AreEqual(0,
                      listTasks.totalCount);
    }
  }

  [Test]
  public async Task ListPartitionsContainsShouldSucceed()
  {
    if (RunTests)
    {
      var listTasks = await PartitionTable!.ListPartitionsAsync(data => data.ParentPartitionIds.Contains("ParentPartitionId"),
                                                                data => data.PartitionId,
                                                                false,
                                                                0,
                                                                20,
                                                                CancellationToken.None)
                                           .ConfigureAwait(false);

      Assert.AreEqual(1,
                      listTasks.totalCount);
    }
  }
}
