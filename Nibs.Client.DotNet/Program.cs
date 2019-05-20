using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace Nibs.Client.DotNet {
	class Program {

		static void Main(string[] args) {
			#region methods
			var answer = NativeBridge.subtract(2, 6);
			Console.WriteLine($"Answer {answer}");
			NativeBridge.Process("NativeHostA.dll");
			#endregion
		}
	}

	public class NativeBridge {
		[DllImport("NativeHostA.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int subtract(int a, int b);

		[DllImport("NativeHostA.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int fibonacci_recursive(int n);
		public static string GetExecutingDirectoryByAssemblyLocation() {
			string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			return path;
		}

		public static void Process(string entryAssembly) {
			var entryAssemblyPath = Path.Combine(GetExecutingDirectoryByAssemblyLocation(), entryAssembly);
			if (!File.Exists(entryAssembly) && !File.Exists(entryAssemblyPath)) {
				throw new FileNotFoundException($"Could not find the {nameof(entryAssembly)} for the {nameof(NativeBridge)}.{nameof(Process)} method.", entryAssembly);
			}

			AssemblyName asmName = new AssemblyName(entryAssembly);
			AssemblyBuilder dynamicAsm = AssemblyBuilder.DefineDynamicAssembly(
				asmName,
				AssemblyBuilderAccess.RunAndCollect
			);

			// Create the module.
			ModuleBuilder dynamicMod = dynamicAsm.DefineDynamicModule(asmName.Name);

			// Create the TypeBuilder for the class that will contain the
			// signature for the PInvoke call.
			TypeBuilder nativeBridgeInternalBuilder = dynamicMod.DefineType("__NativeBridgeInternal", TypeAttributes.Public | TypeAttributes.UnicodeClass);

			MethodBuilder getNativeMetadataBuilder = nativeBridgeInternalBuilder.DefinePInvokeMethod(
				"nibs__get_native_metadata",
				entryAssembly,
				MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.PinvokeImpl,
				CallingConventions.Standard,
				typeof(IntPtr),
				new Type[] { typeof(int), typeof(int) }, //Type.EmptyTypes,
				CallingConvention.Cdecl,
				CharSet.Unicode);

			getNativeMetadataBuilder.SetImplementationFlags(getNativeMetadataBuilder.GetMethodImplementationFlags() | MethodImplAttributes.PreserveSig);

			MethodBuilder freePointerBuilder = nativeBridgeInternalBuilder.DefinePInvokeMethod(
				"nibs__free_ptr",
				entryAssembly,
				MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.PinvokeImpl,
				CallingConventions.Standard,
				null,
				new Type[] { typeof(IntPtr) }, //Type.EmptyTypes,
				CallingConvention.Cdecl,
				CharSet.Unicode);

			freePointerBuilder.SetImplementationFlags(freePointerBuilder.GetMethodImplementationFlags() | MethodImplAttributes.PreserveSig);

			// The PInvoke method does not have a method body.

			// Create the class and test the method.
			Type nativeBridgeInternal = nativeBridgeInternalBuilder.CreateType();
			MethodInfo getNativeMetadata = nativeBridgeInternal.GetMethod("nibs__get_native_metadata");
			MethodInfo freePointer = nativeBridgeInternal.GetMethod("nibs__free_ptr");

			IntPtr responsePointer = (IntPtr)getNativeMetadata.Invoke(null, new object[] { 0, 0 });
			var response = Marshal.PtrToStringUTF8(responsePointer);
			freePointer.Invoke(null, new object[] { responsePointer });

			Console.WriteLine("subtract returned: {0}", response);

			// Produce the .dll file.
			//Console.WriteLine("Saving: " + asmName.Name + ".dll");
			//dynamicAsm.Save(asmName.Name + ".dll");
		}

	}
}