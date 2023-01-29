// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace RedisMQ.Messages;

public static class Headers
{
    public const string MessageId = "message-id";
    public const string MessageName = "msg-name";
    public const string Group = "msg-group";
    public const string SentTime = "sent-time";
    public const string Exception="failed-exception"; 
    public const string CallbackName = "callback-name";
    /// <summary>
    /// Message value .NET type
    /// </summary>
    public const string Type = "msg-type";
    public const string DelayTime = "delaytime";
    public const string Retries = "retries";
}