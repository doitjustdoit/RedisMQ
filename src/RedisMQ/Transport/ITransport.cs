// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using RedisMQ.Messages;

namespace RedisMQ.Transport
{
    public interface ITransport
    {
        Task<OperateResult> SendAsync(TransportMessage message);
    }
}
