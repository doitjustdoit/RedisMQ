﻿// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace RedisMQ.Internal
{
    /// <summary>
    /// Represents bootstrapping logic. For example, adding initial state to the storage or querying certain entities.
    /// </summary>
    public interface IBootstrapper
    {
        Task BootstrapAsync();
    }
}