# RedisMQ

一个基于Redis Stream的轻量级高性能消息队列，基本特性：

1. 单机消息发送吞吐量大于10W/S（446字节）
2. 支持多次重试，超过指定次数报警
3. 支持死信队列
4. 消息保证至少消费一次
5. 无重复消费
6. 支持延迟队列 (待实现)

# 环境及依赖

- .NET 6
- StackExchange.Redis

# 安装

| 包名 | Nuget地址                                                 |
| :------------------------: | :----------------------------------------------------------: |
| RedisMQ| [![Nuget](https://img.shields.io/nuget/dt/RedisMQ)](https://www.nuget.org/packages/RedisMQ)|
| RedisMQ.Serialization.MessagePack | [![Nuget](https://img.shields.io/nuget/dt/RedisMQ.Serialization.MessagePack)](https://www.nuget.org/packages/RedisMQ.Serialization.MessagePack) |

# 开始使用

## 注册服务

```c#
builder.Services.AddRedisMQ(opt =>
{
    opt.Configuration = ConfigurationOptions.Parse("localhost:55000,password=redispw");
    // 如果要使用MessagePack而不是Json序列化，需要安装对应的库并使用下面这行代码
    //mqOptions.UseMessagePack();
});
```

## 发送消息

```c#
public class  TransDto
{
    public string Name { get; set; } = "hello world";
    public int Age { get; set; } = 18;
}

IRedisPublisher redisPublisher= ...; // 依赖注入

await _redisPublisher.PublishAsync("test",new TransDto()); // topic ，消息内容
```

## 接收消息

```c#
[NonAction]
[RedisSubscribe("test")]
public void Test(TransDto msg,[FromRedis] RedisHeader headers)
{
    _logger.LogInformation($"received from {msg.Name} - {msg.Age}");
}   

```

详细可参考示例工程：[https://github.com/li-zheng-hao/LZH.RedisMQ/tree/main/samples](https://github.com/li-zheng-hao/LZH.RedisMQ/tree/main/samples)

## 重试

注册服务时使用`FailedRetryCount`字段配置重试次数，值为0时关闭重试功能，`FailedThresholdCallback`配置自定义告警：

```c#
builder.Services.AddRedisMQ(mqOptions =>
{
    mqOptions.Configuration = ConfigurationOptions.Parse("localhost:6379");
    mqOptions.FailedRetryCount = 3;
    mqOptions.FailedThresholdCallback += message =>
    {
        var topic = message.GetName();
        var group = message.GetGroup();
        var msgId = message.GetId();
        var payload = message.Body;
        // 短信 日志 邮件通知。。。
    };
   
});
```

当重试达到上限次数时自动移送至全局的死信队列(所有消息)

# 性能测试

```
// * Summary *

BenchmarkDotNet=v0.13.4, OS=Windows 11 (10.0.22621.1105)
AMD Ryzen 5 5600G with Radeon Graphics, 1 CPU, 12 logical and 6 physical cores
.NET SDK=6.0.301
  [Host]     : .NET 6.0.6 (6.0.622.26707), X64 RyuJIT AVX2
  DefaultJob : .NET 6.0.6 (6.0.622.26707), X64 RyuJIT AVX2


|                   Method |      Mean |    Error |   StdDev |      Gen0 |      Gen1 |     Gen2 | Allocated |
|------------------------- |----------:|---------:|---------:|----------:|----------:|---------:|----------:|
|        Publish_10000_Json | 102.06 ms | 2.003 ms | 3.763 ms | 4166.6667 | 1500.0000 | 333.3333 |  31.61 MB |
| Publish_10000_MessagePack | 101.02 ms | 2.014 ms | 5.126 ms | 4666.6667 | 1666.6667 | 333.3333 |     35 MB |
|      StreamAdd_10000_Json |  60.95 ms | 1.201 ms | 2.854 ms | 2888.8889 | 1222.2222 | 444.4444 |  19.91 MB |
```

Redis配置：

- Docker Desktop For Windows
- 版本：7.0.4
- 配置文件：默认

> 以上测试过程中，Redis与测试程序运行在同一机器上

测试代码：[https://github.com/li-zheng-hao/LZH.RedisMQ/tree/main/RedisMQ.Benchmarks](https://github.com/li-zheng-hao/LZH.RedisMQ/tree/main/RedisMQ.Benchmarks)
