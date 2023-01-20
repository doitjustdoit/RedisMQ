// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using DotNetCore.CAP.Processor;

namespace LZH.RedisMQ.Processor;

public interface IProcessor
{
    Task ProcessAsync(ProcessingContext context);
}