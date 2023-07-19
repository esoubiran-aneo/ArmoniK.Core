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

using Armonik.Api.gRPC.V1;
using Armonik.Api.Grpc.V1.Partitions;

namespace ArmoniK.Core.Common.Tests.ListPartitionsRequestExt;

public static class ListPartitionsHelper
{
  public static ListPartitionsRequest CreateListPartitionsRequest(ListPartitionsRequest.Types.Sort sort,
                                                                  IEnumerable<FilterField>         filterFields)
    => new()
       {
         Filters = new Filters
                   {
                     Filters_ = new FiltersOr
                                {
                                  Filters =
                                  {
                                    new FiltersAnd
                                    {
                                      Filters =
                                      {
                                        filterFields,
                                      },
                                    },
                                  },
                                },
                   },
         Sort = sort,
       };

  public static FilterField CreateListPartitionsFilterString(PartitionRawEnumField field,
                                                             FilterStringOperator  op,
                                                             string                value)
    => new()
       {
         String = new FilterString
                  {
                    Field = new PartitionField
                            {
                              PartitionRawField = new PartitionRawField
                                                  {
                                                    Field = field,
                                                  },
                            },
                    Operator = op,
                    Value    = value,
                  },
       };

  public static FilterField CreateListPartitionsFilterNumber(PartitionRawEnumField field,
                                                             FilterNumberOperator  op,
                                                             long                  value)
    => new()
       {
         Number = new FilterNumber
                  {
                    Field = new PartitionField
                            {
                              PartitionRawField = new PartitionRawField
                                                  {
                                                    Field = field,
                                                  },
                            },
                    Operator = op,
                    Value    = value,
                  },
       };

  public static FilterField CreateListPartitionsFilterArray(PartitionRawEnumField field,
                                                            FilterArrayOperator   op,
                                                            string                value)
    => new()
       {
         Array = new FilterArray
                 {
                   Field = new PartitionField
                           {
                             PartitionRawField = new PartitionRawField
                                                 {
                                                   Field = field,
                                                 },
                           },
                   Operator = op,
                   Value    = value,
                 },
       };
}
