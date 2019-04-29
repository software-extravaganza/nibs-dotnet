using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace Nibs.NativeHost.Benchmarks {
	public class Program {
		static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, (
			ManualConfig
			.Create(DefaultConfig.Instance)
			//.With(Job.RyuJitX64)
			.With(Job.Dry
				.With(Platform.X64)
				.With(Jit.RyuJit)
				.With(Runtime.Core)
				.WithLaunchCount(5)
				.WithMaxRelativeError(0.01)
				.WithId("netcore"))
			.With(MemoryDiagnoser.Default)
			.With(HtmlExporter.Default)
			//.With(RPlotExporter.Default)
			.With(MarkdownExporter.StackOverflow)
			.With(ExecutionValidator.FailOnError)
			.With(BaselineValidator.FailOnError)
			.With(JitOptimizationsValidator.FailOnError)
			.With(BenchmarkLogicalGroupRule.ByCategory)
		));
	}
}