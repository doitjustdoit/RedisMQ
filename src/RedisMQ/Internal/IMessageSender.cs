// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using RedisMQ.Messages;

namespace RedisMQ.Internal
{
    public interface IMessageSender
    {
        Task<OperateResult> SendAsync(Message message);
    }
}