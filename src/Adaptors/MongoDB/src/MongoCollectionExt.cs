﻿// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2021. All rights reserved.
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
using System.Linq.Expressions;

using ArmoniK.Core.gRPC.V1;

using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace ArmoniK.Adapters.MongoDB
{
  public static class MongoCollectionExt
  {
    public static IMongoQueryable<TaskDataModel> FilterQuery(this IMongoQueryable<TaskDataModel> taskQueryable,
                                                              TaskFilter                          filter)
      => taskQueryable.Where(filter.ToFilterExpression());





    public static IMongoQueryable<TaskDataModel> FilterField<TField>(this IMongoQueryable<TaskDataModel>     taskQueryable,
                                                                     Expression<Func<TaskDataModel, TField>> expression,
                                                                     IEnumerable<TField>                     values,
                                                                     bool                                    include = true)
      => taskQueryable.Where(ExpressionsBuilders.FieldFilterExpression(expression,
                                                                       values,
                                                                       include));


    public static IQueryable<TaskDataModel> FilterQuery(this IQueryable<TaskDataModel> taskQueryable,
                                                        TaskFilter                     filter)
      => taskQueryable.Where(filter.ToFilterExpression());





    public static IQueryable<TaskDataModel> FilterField<TField>(this IQueryable<TaskDataModel>          taskQueryable,
                                                                Expression<Func<TaskDataModel, TField>> expression,
                                                                IEnumerable<TField>                     values,
                                                                bool                                    include = true)
      => taskQueryable.Where(ExpressionsBuilders.FieldFilterExpression(expression,
                                                                       values,
  }
}
