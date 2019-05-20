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

	public class ArithmeticBenchmarks {
		[Params(100, 5000)]
		public int A;

		[GlobalSetup]
		public void Setup() {}

		[BenchmarkCategory("Arithmetic"), Benchmark(Baseline = true)]
		public int Subtract() => ClientNet.NativeCode.Subtract(A, 2);

		[BenchmarkCategory("Arithmetic"), Benchmark]
		public int SubtractNibs() => ClientNibs.NativeBridge.subtract(A, 2);

	}
}