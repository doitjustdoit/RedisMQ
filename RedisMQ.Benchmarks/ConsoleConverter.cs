using System.Text;
using Xunit.Abstractions;

namespace RedisMQ.Benchmarks;

internal class ConsoleConverter : TextWriter
{
    ITestOutputHelper _output;
    public ConsoleConverter(ITestOutputHelper output)
    {
        _output = output;
    }
    public override Encoding Encoding
    {
        get { return Encoding.Default; }
    }
    public override void WriteLine(string message)
    {
        _output.WriteLine(message);
    }
    public override void WriteLine(string format, params object[] args)
    {
        _output.WriteLine(format, args);
    }
}