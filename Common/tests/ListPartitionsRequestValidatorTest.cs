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

using System;

using Armonik.Api.Grpc.V1.Partitions;

using ArmoniK.Core.Common.gRPC.Validators;

using NUnit.Framework;

namespace ArmoniK.Core.Common.Tests;

[TestFixture(TestOf = typeof(ListPartitionsRequestValidator))]
public class ListPartitionsRequestValidatorTest
{
  [SetUp]
  public void Setup()
    => validListPartitionsRequest_ = new ListPartitionsRequest
                                     {
                                       Filter = new ListPartitionsRequest.Types.Filter(),
                                       Sort = new ListPartitionsRequest.Types.Sort
                                              {
                                                Direction = ListPartitionsRequest.Types.OrderDirection.Asc,
                                                Field     = ListPartitionsRequest.Types.OrderByField.Id,
                                              },
                                       Page     = 0,
                                       PageSize = 1,
                                     };

  private readonly ListPartitionsRequestValidator validator_ = new();
  private          ListPartitionsRequest?         validListPartitionsRequest_;

  [Test]
  public void ListPartitionsRequestShouldBeValid()
    => Assert.IsTrue(validator_.Validate(validListPartitionsRequest_!)
                               .IsValid);

  [Test]
  public void ListPartitionsRequestDefaultFilterShouldFail()
  {
    validListPartitionsRequest_!.Filter = default;
    Assert.IsFalse(validator_.Validate(validListPartitionsRequest_)
                             .IsValid);
  }

  [Test]
  public void ListPartitionsRequestDefaultSortShouldFail()
  {
    validListPartitionsRequest_!.Sort = default;

    foreach (var error in validator_.Validate(validListPartitionsRequest_)
                                    .Errors)
    {
      Console.WriteLine(error);
    }

    Assert.IsFalse(validator_.Validate(validListPartitionsRequest_)
                             .IsValid);
  }

  [Test]
  public void ListPartitionsRequestMissingFieldShouldFail()
  {
    validListPartitionsRequest_!.Sort = new ListPartitionsRequest.Types.Sort
                                        {
                                          Direction = ListPartitionsRequest.Types.OrderDirection.Desc,
                                        };
    foreach (var error in validator_.Validate(validListPartitionsRequest_)
                                    .Errors)
    {
      Console.WriteLine(error);
    }

    Assert.IsFalse(validator_.Validate(validListPartitionsRequest_)
                             .IsValid);
  }

  [Test]
  public void ListPartitionsRequestMissingDirectionShouldFail()
  {
    validListPartitionsRequest_!.Sort = new ListPartitionsRequest.Types.Sort
                                        {
                                          Field = ListPartitionsRequest.Types.OrderByField.Id,
                                        };
    foreach (var error in validator_.Validate(validListPartitionsRequest_)
                                    .Errors)
    {
      Console.WriteLine(error);
    }

    Assert.IsFalse(validator_.Validate(validListPartitionsRequest_)
                             .IsValid);
  }

  [Test]
  public void ListPartitionsRequestNegativePageShouldFail()
  {
    validListPartitionsRequest_!.Page = -1;
    foreach (var error in validator_.Validate(validListPartitionsRequest_)
                                    .Errors)
    {
      Console.WriteLine(error);
    }

    Assert.IsFalse(validator_.Validate(validListPartitionsRequest_)
                             .IsValid);
  }

  [Test]
  public void ListPartitionsRequestNegativePageSizeShouldFail()
  {
    validListPartitionsRequest_!.PageSize = -1;
    Assert.IsFalse(validator_.Validate(validListPartitionsRequest_)
                             .IsValid);
  }

  [Test]
  public void ListPartitionsRequestZeroPageSizeShouldFail()
  {
    validListPartitionsRequest_!.PageSize = 0;
    Assert.IsFalse(validator_.Validate(validListPartitionsRequest_)
                             .IsValid);
  }
}