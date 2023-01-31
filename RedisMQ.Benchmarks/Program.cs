using System.Diagnostics;
using BenchmarkDotNet.Running;
using StackExchange.Redis;

#if DEBUG
var conn=ConnectionMultiplexer.Connect("localhost:6379");
TestBenchmarks testBenchmark = new();
// testBenchmark.Setup();
// Stopwatch sw = new();
// for (int i = 0; i < 5; i++)
// {
//     sw.Restart();
//     testBenchmark.Publish_WithoutSerialization_1000();
//     Console.WriteLine($"Publish Without Serialization 1000 messages in {sw.ElapsedMilliseconds} ms");
//     sw.Restart();
//     testBenchmark.StreamAdd_1000();
//     Console.WriteLine($"StreamAdd 1000 messages in {sw.ElapsedMilliseconds} ms");
//     sw.Restart();
//     testBenchmark.StreamAdd_1000_Json();
//     Console.WriteLine($"StreamAdd_Json 1000 messages in {sw.ElapsedMilliseconds} ms");
//     sw.Restart();
//     testBenchmark.StreamAdd_1000_MsgPack();
//     Console.WriteLine($"StreamAdd_MsgPack 1000 messages in {sw.ElapsedMilliseconds} ms");
//     sw.Restart();
//     testBenchmark.Publish_1000();
//     Console.WriteLine($"Publish 1000 messages in {sw.ElapsedMilliseconds} ms");
//     sw.Restart();
//     testBenchmark.Publish_1000_MessagePack();
//     Console.WriteLine($"Publish With MessagePack 1000 messages in {sw.ElapsedMilliseconds} ms");
// }
#else
var summary = BenchmarkRunner.Run<TestBenchmarks>();
#endif

