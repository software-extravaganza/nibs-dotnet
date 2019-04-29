using System;
using System.Runtime.InteropServices;

namespace Nibs.Client.DotNet {
	class Program {

		static void Main(string[] args) {
			var answer = NativeBridge.subtract(2, 5);
			Console.WriteLine($"Answer {answer}");
		}
	}

	public class NativeBridge {
		[DllImport("NativeHostA.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int subtract(int a, int b);

		[DllImport("NativeHostA.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int fibonacci_recursive(int n);

	}
}