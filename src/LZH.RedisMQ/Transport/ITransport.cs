// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using DotNetCore.CAP.Transport;
using LZH.RedisMQ.Messages;

namespace LZH.RedisMQ.Transport
{
    public interface ITransport
    {
        BrokerAddress BrokerAddress { get; }

        Task<OperateResult> SendAsync(TransportMessage message);
    }
}
