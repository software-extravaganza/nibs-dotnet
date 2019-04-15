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
}