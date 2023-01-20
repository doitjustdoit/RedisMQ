// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using LZH.RedisMQ.Internal;

namespace LZH.RedisMQ;

public class RedisSubscribeAttribute : TopicAttribute
{
    public RedisSubscribeAttribute(string name, bool isPartial = false)
        : base(name, isPartial)
    {
    }

    public override string ToString()
    {
        return Name;
    }
}

[AttributeUsage(AttributeTargets.Parameter)]
public class FromRedisAttribute : Attribute
{
}

public class RedisHeader : ReadOnlyDictionary<string, string?>
{
    public RedisHeader(IDictionary<string, string?> dictionary) : base(dictionary)
    {
    }
}