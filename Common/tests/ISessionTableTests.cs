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
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Threading;
using System.Threading.Tasks;

using ArmoniK.Core.Common.Exceptions;
using ArmoniK.Core.Common.Storage;

using NUnit.Framework;

using TaskOptions = ArmoniK.Api.gRPC.V1.TaskOptions;

namespace ArmoniK.Core.Common.Tests;

[TestFixture]
public class SessionTableTestBase
{
  [SetUp]
  public void SetUp()
  {
    GetSessionTableInstance();

    if (RunTests)
    {
      SessionTable.CreateSessionDataAsync(RootSessionId,
                                          "TaskId",
                                          new TaskOptions(),
                                          CancellationToken.None)
                  .Wait();
    }
  }

  [TearDown]
  public virtual void TearDown()
  {
    SessionTable = null;
    RunTests     = false;
  }

  protected ISessionTable SessionTable;

  protected bool RunTests;

  private const string RootSessionId = "SessionId";

  public virtual void GetSessionTableInstance()
  {
  }

  [Test]
  public async Task IsSessionCancelledAsyncShouldSucceed()
  {
    if (RunTests)
    {
      // Inconsistent signature: the contract asks for session Id
      var res = await SessionTable.IsSessionCancelledAsync("SessionId",
                                                           CancellationToken.None)
                                  .ConfigureAwait(false);
      Assert.IsFalse(res);
    }
  }

  [Test]
  public void IsSessionCancelledAsyncShouldFail()
  {
    if (RunTests)
    {
      Assert.ThrowsAsync<ArmoniKException>(async () =>
                                           {
                                             await SessionTable.IsSessionCancelledAsync("BadSessionId",
                                                                                        CancellationToken.None)
                                                               .ConfigureAwait(false);
                                           });
    }
  }

  [Test]
  public async Task GetDefaultTaskOptionAsyncShouldSucceed()
  {
    if (RunTests)
    {
      // Inconsistent signature: the contract asks for sessionId
      var res = await SessionTable.GetDefaultTaskOptionAsync("SessionId",
                                                             CancellationToken.None)
                                  .ConfigureAwait(false);
      Assert.NotNull(res);
    }
  }

  [Test]
  public async Task CancelSessionAsyncShouldSucceed()
  {
    if (RunTests)
    {
      // Inconsistent signature: the contract asks for sessionId
      await SessionTable.CancelSessionAsync("SessionId",
                                            CancellationToken.None)
                        .ConfigureAwait(false);
      var wasSessionCanceled = await SessionTable.IsSessionCancelledAsync(RootSessionId,
                                                                          CancellationToken.None)
                                                 .ConfigureAwait(false);
      Assert.IsTrue(wasSessionCanceled);
    }
  }

  [Test]
  public void CancelSessionAsyncShouldFail()
  {
    if (RunTests)
    {
      Assert.ThrowsAsync<ArmoniKException>(async () =>
                                           {
                                             await SessionTable.CancelSessionAsync("BadSessionId",
                                                                                   CancellationToken.None)
                                                               .ConfigureAwait(false);
                                           });
    }
  }


  [Test]
  public async Task DeleteSessionAsyncShouldSucceed()
  {
    if (RunTests)
    {
      var res = SessionTable.DeleteSessionAsync(RootSessionId,
                                                CancellationToken.None);
      await res.ConfigureAwait(false);

      Assert.IsTrue(res.IsCompletedSuccessfully);
    }
  }

  [Test]
  public void DeleteSessionAsyncShouldFail()
  {
    if (RunTests)
    {
      Assert.ThrowsAsync<ArmoniKException>(async () =>
                                           {
                                             await SessionTable.DeleteSessionAsync("BadSessionId",
                                                                                   CancellationToken.None)
                                                               .ConfigureAwait(false);
                                           });
    }
  }
}