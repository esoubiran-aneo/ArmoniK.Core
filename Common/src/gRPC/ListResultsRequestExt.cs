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

using System;
using System.Linq.Expressions;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.gRPC.V1.Results;
using ArmoniK.Core.Common.Storage;

using LinqKit;

namespace ArmoniK.Core.Common.gRPC;

public static class ListResultsRequestExt
{
  public static Expression<Func<Result, object?>> ToResultField(this ListResultsRequest.Types.Sort sort)
  {
    switch (sort.Field.FieldCase)
    {
      case ResultField.FieldOneofCase.ResultRawField:
        return sort.Field.ResultRawField switch
               {
                 ResultRawField.SessionId   => result => result.SessionId,
                 ResultRawField.Name        => result => result.Name,
                 ResultRawField.OwnerTaskId => result => result.OwnerTaskId,
                 ResultRawField.Status      => result => result.Status,
                 ResultRawField.CreatedAt   => result => result.CreationDate,
                 ResultRawField.ResultId    => result => result.ResultId,
                 ResultRawField.CompletedAt => throw new ArgumentOutOfRangeException(),
                 ResultRawField.Unspecified => throw new ArgumentOutOfRangeException(),
                 _                          => throw new ArgumentOutOfRangeException(),
               };
      case ResultField.FieldOneofCase.None:
      default:
        throw new ArgumentOutOfRangeException();
    }
  }

  public static Expression<Func<Result, bool>> ToResultFilter(this ListResultsRequest.Types.Filter filter)
  {
    var predicate = PredicateBuilder.New<Result>();
    predicate = predicate.And(data => true);

    if (!string.IsNullOrEmpty(filter.SessionId))
    {
      predicate = predicate.And(data => data.SessionId == filter.SessionId);
    }

    if (!string.IsNullOrEmpty(filter.Name))
    {
      predicate = predicate.And(data => data.Name == filter.Name);
    }

    if (!string.IsNullOrEmpty(filter.OwnerTaskId))
    {
      predicate = predicate.And(data => data.OwnerTaskId == filter.OwnerTaskId);
    }

    if (filter.CreatedAfter is not null)
    {
      predicate = predicate.And(data => data.CreationDate > filter.CreatedAfter.ToDateTime());
    }

    if (filter.CreatedBefore is not null)
    {
      predicate = predicate.And(data => data.CreationDate < filter.CreatedBefore.ToDateTime());
    }

    if (filter.Status != ResultStatus.Unspecified)
    {
      predicate = predicate.And(data => data.Status == filter.Status);
    }

    return predicate;
  }
}
