﻿// This file is part of the ArmoniK project
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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ArmoniK.Core.Common.Storage;

public interface IQueueStorageBase : IInitializable
{
  int MaxPriority { get; }

  IAsyncEnumerable<IQueueMessageHandler> PullAsync(int               nbMessages,
                                                   CancellationToken cancellationToken = default);

  /// <summary>
  ///   Submit new messages
  /// </summary>
  /// <param name="messages"></param>
  /// <param name="priority"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  Task EnqueueMessagesAsync(IEnumerable<string> messages,
                            int                 priority          = 1,
                            CancellationToken   cancellationToken = default);
}