using System;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;
using ClientNet = HostA;
using ClientNibs = Nibs.Client.DotNet;
using Nibs.NativeHost;

namespace Nibs.NativeHost.Benchmarks {

	[IterationCount(100)]
	[InvocationCount(1,1)]
	public class FibonacciBenchmarks {
		[Params(20, 25, 30, 35)]
		public int N;

		[GlobalSetup]
		public void Setup() {}

		[BenchmarkCategory("Fibonacci"), Benchmark(Baseline = true)]
		public int Fibonacci() => ClientNet.Fibonacci.Recursive(N);

		[BenchmarkCategory("Fibonacci"), Benchmark]
		public int FibonacciNibs() => ClientNibs.NativeBridge.fibonacci_recursive(N);
	}
}