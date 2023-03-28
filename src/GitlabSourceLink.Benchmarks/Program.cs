using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace Genbox.GitlabSourceLink.Benchmarks;

internal static class Program
{
    private static void Main(string[] args)
    {
        IConfig config = ManualConfig.CreateMinimumViable().AddJob(Job.InProcess);
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
    }
}
