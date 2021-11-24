﻿// This file is part of ArmoniK project.
// 
// Copyright (c) ANEO. All rights reserved.
//   W. Kirschenmann <wkirschenmann@aneo.fr>

using System;
using System.Threading;
using System.Threading.Tasks;

using ArmoniK.Core.gRPC.V1;

namespace ArmoniK.Core.Storage
{
  public interface ILeaseProvider
  {
    TimeSpan AcquisitionPeriod { get; }

    TimeSpan AcquisitionDuration { get; }

    /// <summary>
    /// Try to acquire a lease to process the task. If processing will last after the expiration date, the Lease will have to be renewed.
    /// </summary>
    /// <param name="id">The Id of the task to process.</param>
    /// <param name="cancellationToken">Cancellation token for the request</param>
    /// <returns>The lease to be used for renewal or an empty leaseId and past expiration in case of failure</returns>
    Task<Lease> TryAcquireLease(TaskId id, CancellationToken cancellationToken = default);

    Task<Lease> TryRenewLease(TaskId id, string leaseId, CancellationToken cancellationToken = default);

    Task ReleaseLease(TaskId id, string leaseId, CancellationToken cancellationToken = default);
  }
}