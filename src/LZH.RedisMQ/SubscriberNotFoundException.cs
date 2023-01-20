// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace LZH.RedisMQ;

public class SubscriberNotFoundException : Exception
{
    public SubscriberNotFoundException(string message) : base(message)
    {
    }
}