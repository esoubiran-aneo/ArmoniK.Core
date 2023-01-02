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

using ArmoniK.Api.gRPC.V1;

using JetBrains.Annotations;

namespace ArmoniK.Samples.Bench.Client.Options;

/// <summary>
///   Class containing options for BenchOptions
/// </summary>
[PublicAPI]
public class BenchOptions
{
  /// <summary>
  ///   Name of the section in dotnet options
  /// </summary>
  public const string SettingSection = nameof(BenchOptions);

  /// <summary>
  ///   Number of computing tasks (there are some supplementary aggregation tasks)
  /// </summary>
  public int NTasks { get; set; } = 100;

  /// <summary>
  ///   Duration of the task in milliseconds
  /// </summary>
  public int TaskDurationMs { get; set; } = 100;

  /// <summary>
  ///   Size of the payloads in kilobytes
  /// </summary>
  public int PayloadSize { get; set; } = 1;

  /// <summary>
  ///   Size of the results in kilobytes
  /// </summary>
  public int ResultSize { get; set; } = 1;

  /// <summary>
  ///   Raise RpcException when task id ends by this string, ignored if empty string
  /// </summary>
  public string TaskRpcException { get; set; } = string.Empty;

  /// <summary>
  ///   Finish task with Output of type <see cref="Output.TypeOneofCase.Error" /> when task id ends by this string, ignored
  ///   if empty string
  /// </summary>
  public string TaskError { get; set; } = string.Empty;

  /// <summary>
  ///   Partition in which to submit the tasks
  /// </summary>
  public string Partition { get; set; } = string.Empty;

  /// <summary>
  ///   Print the graph updates
  /// </summary>
  public bool ShowGraph { get; set; } = false;
}
