using BenchmarkDotNet.Running;

namespace AlgoTrendy.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        // Run all benchmarks
        var summary = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
