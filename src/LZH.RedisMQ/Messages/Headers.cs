// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace LZH.RedisMQ.Messages;

public static class Headers
{
    public const string MessageId = "message-id";
    public const string Group = "msg-group";
    public const string SentTime = "sent-time";
    public const string Exception="failed-exception"; 
    public const string CallbackName = "cap-callback-name";
}