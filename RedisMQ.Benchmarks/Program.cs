using System.Diagnostics;
using BenchmarkDotNet.Running;

Console.WriteLine("Hello World!");


#if DEBUG
TestBenchmarks testBenchmark = new();
testBenchmark.Setup();
Stopwatch sw = new();
sw.Restart();
testBenchmark.Publish_10000();
Console.WriteLine($"Publish 10000 messages in {sw.ElapsedMilliseconds} ms");
sw.Restart();
testBenchmark.Publish_10000_MessagePack();
Console.WriteLine($"Publish With MessagePack 10000 messages in {sw.ElapsedMilliseconds} ms");
sw.Restart();
testBenchmark.Publish_WithoutSerialization_10000();
Console.WriteLine($"Publish Without Serialization 10000 messages in {sw.ElapsedMilliseconds} ms");
sw.Restart();
testBenchmark.StreamAdd_10000();
Console.WriteLine($"StreamAdd 10000 messages in {sw.ElapsedMilliseconds} ms");
sw.Restart();
testBenchmark.StreamAdd_50000_5Con();
Console.WriteLine($"StreamAdd 50000 messages in 5 concurrent tasks and connections in {sw.ElapsedMilliseconds} ms");
sw.Restart();
#else
var summary = BenchmarkRunner.Run<TestBenchmarks>();
#endif

Console.ReadLine();