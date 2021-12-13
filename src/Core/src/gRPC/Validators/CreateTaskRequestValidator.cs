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

using ArmoniK.Core.gRPC.V1;

using FluentValidation;

using JetBrains.Annotations;

namespace ArmoniK.Core.gRPC.Validators
{
  [UsedImplicitly]
  public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
  {
    public CreateTaskRequestValidator()
    {
      RuleFor(r => r.SessionId).NotNull().SetValidator(new SessionIdValidator());
      RuleFor(r => r.TaskOptions).SetValidator(new TaskOptionsValidator());
      RuleFor(request => request.TaskRequests).NotNull().NotEmpty();
      RuleForEach(request => request.TaskRequests).SetValidator(new TaskRequestValidator());
    }
  }
}
