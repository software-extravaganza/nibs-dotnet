using System;
using System.Runtime.InteropServices;
using Nibs.NativeHost;
using Nibs.NativeHost.Attributes;

namespace HostA {
	public class NativeCode {
		[NativeCallable(EntryPoint = "add", CallingConvention = CallingConvention.Cdecl)]
		public static int Add(int a, int b) {
			return a + b;
		}

		[NativeCallable(EntryPoint = "subtract", CallingConvention = CallingConvention.Cdecl)]
		public static int Subtract(int a, int b) {
			return a - b;
		}

		[NativeCallable(EntryPoint = "append", CallingConvention = CallingConvention.Cdecl)]
		[
			return :IntendedType(typeof(string))
		]
		public static IntPtr Append([IntendedType(typeof(string))] IntPtr a, [IntendedType(typeof(string))] IntPtr b) {
			return Marshal.StringToCoTaskMemUTF8(Marshal.PtrToStringUTF8(a) + Marshal.PtrToStringUTF8(b));
		}
	}

	public class Fibonacci {
		[NativeCallable(EntryPoint = "fibonacci_recursive", CallingConvention = CallingConvention.Cdecl)]
		public static int Recursive(int n) {
			return recursiveInternal(n);
		}

		public static int recursiveInternal(int n) {
			if (n < 2) {
				return n;
			} else {
				return recursiveInternal(n - 2) + recursiveInternal(n - 1);
			}
		}

		[NativeCallable(EntryPoint = "fibonacci_iterative", CallingConvention = CallingConvention.Cdecl)]
		public static int Iterative(int n) {
			if (n < 2)
				return n;

			int second_fib = 0, first_fib = 1, current_fib = 0;
			for (int i = 2; i <= n; i++) {
				current_fib = second_fib + first_fib;
				second_fib = first_fib;
				first_fib = current_fib;
			}
			return current_fib;
		}

		[NativeCallable(EntryPoint = "fibonacci_tail_recursion", CallingConvention = CallingConvention.Cdecl)]
		public static int TailRecursion(int n, int a = 0, int b = 1) {
			return tailRecursionInternal(n, a, b);
		}

		public static int tailRecursionInternal(int n, int a = 0, int b = 1) {
			if (n == 0)
				return a;
			if (n == 1)
				return b;
			return tailRecursionInternal(n - 1, b, a + b);
		}
	}
}