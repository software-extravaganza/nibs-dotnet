using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Nibs.NativeHost.Attributes;

namespace Nibs.NativeHost {

	public class Host {
		private static INativeExporter chooseExporter(int exportType) {
			switch ((ExportType)exportType) {
				case ExportType.UnsafeJson:
					return new UnsafeJsonExporter();
				default:
					return new JsonExporter();
			}
		}

		[NativeCallable(EntryPoint = "nibs__free_ptr", CallingConvention = CallingConvention.Cdecl)]
		public static void Free(IntPtr ptr) {
			// if(arrayBufferHold.ContainsKey(ptr)){
			//     arrayBufferHold.Remove(ptr);
			// }
			Marshal.FreeCoTaskMem(ptr);
		}

		[NativeCallable(EntryPoint = "nibs__get_native_metadata", CallingConvention = CallingConvention.Cdecl)]
		[
			return :IntendedType(typeof(string))
		]
		public static IntPtr GetNativeMetadata([IntendedType(typeof(ProgrammingPlatform))] int programmingPlaformNumber, [IntendedType(typeof(ExportType))] int exportType) {
			var exporter = chooseExporter(exportType);
			var output = exporter.Export(loadConfiguration, (nativeSourceSettings) => processAssemblies(exporter, programmingPlaformNumber, nativeSourceSettings));
			var utf8 = new UTF8Encoding();

			//MemoryMarshal.AsMemory<byte>(output.OutputAsMemory);
			//var test = MemoryMarshal.AsRef(output.OutputAsSpan);
			//arrayBufferHold.Add(holdPointer, output);
			return Marshal.StringToCoTaskMemUTF8(utf8.GetString(output));
		}

		private static ILoadedAssembliesResult processAssemblies(INativeExporter exporter, int programmingPlaformNumber, INativeSourceSettings nativeSourceSettings) {
			var assemblies = new Dictionary<string, NativeAssemblyDescription>();
			var assembliesNotFound = new List<string>();

			var assembliesLoaded = AppDomain.CurrentDomain.GetAssemblies().ToList();
			assembliesLoaded.Add(typeof(Host).Assembly);
			var assemblyNames = assembliesLoaded.Select(a => a.FullName);
			//throw new Exception(string.Join(";", assemblyNames));

			// foreach (var assemblyPath in nativeSourceSettings.Assemblies.Distinct()) {
			// 	string exactAssemblyPath = Path.GetFullPath(assemblyPath);
			// 	if (File.Exists(exactAssemblyPath)) {

			// 		var resolver = new AssemblyDependencyResolver(exactAssemblyPath);
			// 		var assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
			// 		var assemblyBytes = File.ReadAllBytes(exactAssemblyPath);
			// 		var assemblyLoaded = Assembly.Load(assemblyBytes);
			// 		var assemblyLoaded = AssemblyLoadContext.Default.LoadFromAssemblyPath(exactAssemblyPath);
			// 		assembliesLoaded.Add(assemblyLoaded);
			// 	} else {
			// 		assembliesNotFound.Add(exactAssemblyPath);
			// 	}
			// }

			foreach (var assembly in assembliesLoaded.GroupBy(a => a.IsDynamic ? a.FullName : a.Location).Select(g => g.First()).ToList()) {
				var assemblyFile = new FileInfo($"{assembly.FullName.Split(",").FirstOrDefault()}.dll");
				var namespaces = ProcessAssembly(assembly, (ProgrammingPlatform)programmingPlaformNumber);
				if (namespaces.Count > 0) {
					assemblies.Add(assemblyFile.FullName, new NativeAssemblyDescription(assemblyFile.Name, assemblyFile.DirectoryName.Replace("\\", "/")) { Namespaces = namespaces });
				}
			}

			throw new Exception(assemblies.Count <= 0 ? "No assemblies" : string.Join(";", assemblies.Values.Select(a => a.FullPath)));
			return new LoadedAssembliesResult(assemblies.Values.ToList(), assembliesNotFound);
		}

		private static NativeSourceSettingLoadResult loadConfiguration() {
			if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "config.json"))) {
				//json.WriteString("Error", "No config found config.json");

				try {
					var configurationBuilder = new ConfigurationBuilder()
						.SetBasePath(Directory.GetCurrentDirectory())
						.AddJsonFile("config.json", true);
					var config = configurationBuilder.Build();
					return new NativeSourceSettingLoadResult(config.GetSection("NativeSources").Get<NativeSourceSettings>());
				} catch (Exception ex) {
					//json.WriteString("Error", "Error build metadata.");
					//json.WriteString("Exception", ioex.ToString());
					return new NativeSourceSettingLoadResult(new NativeSourceSettings(), "Error building metadata", ex);
				}
			}

			return new NativeSourceSettingLoadResult(new NativeSourceSettings(), "No config found config.json");
		}

		public static List<NativeNamespaceDescription> ProcessAssembly(Assembly assembly, ProgrammingPlatform progammingPlaformNumber) {
			var namespaces = new Dictionary<string, NativeNamespaceDescription>();

			//var methodsAndAttributes = from methodInfo in typeof(Library).GetMethods(BindingFlags.Static | BindingFlags.Public) let nativeAttribute = methodInfo.GetCustomAttribute(typeof(NativeCallableAttribute), true) as NativeCallableAttribute  where nativeAttribute != null select GetNativeMethodFrom(methodInfo, nativeAttribute);
			Type[] types;
			try {
				types = assembly.GetTypes();
			} catch (ReflectionTypeLoadException rtlex) {
				types = rtlex.Types.Where(t => t != null).ToArray();
			}

			foreach (var typeFound in types) {
				//try {
				foreach (var methodInfo in typeFound.GetMethods(BindingFlags.Static | BindingFlags.Public)) {
					var nativeAttribute = methodInfo.GetCustomAttribute(typeof(NativeCallableAttribute), true)as NativeCallableAttribute;
					if (nativeAttribute == null) {
						continue;
					}

					var currentMethodName = nativeAttribute.EntryPoint;
					var currentTypeName = methodInfo?.DeclaringType?.Name;
					var currentNamespaceName = methodInfo?.DeclaringType?.Namespace;
					var currentNamespace = GetNameSpaceDesciptorForPath(namespaces, currentNamespaceName, true);
					if (!currentNamespace.Classes.ContainsKey(currentTypeName)) {
						currentNamespace.Classes.Add(currentTypeName, new NativeClassDescription(currentTypeName));
					}

					var currentClass = currentNamespace.Classes[currentTypeName];
					if (!currentClass.Methods.ContainsKey(currentMethodName)) {
						currentClass.Methods.Add(currentMethodName, GetNativeMethodFrom(progammingPlaformNumber, methodInfo, nativeAttribute));
					}
				}
				//} catch (TypeLoadException tlex) {}
			}

			return namespaces.Values.ToList();
		}

		// private static NativeNamespaceDescription CreateNamespaceTreeAndReturnDeepestDescriptor(Dictionary<string, NativeNamespaceDescription> namespaces, Span<string> namespaceSegments, string namespaceBasePath = null) {
		//     if(namespaceSegments.Length <= 0){
		//         throw new ArgumentOutOfRangeException(nameof(namespaceSegments), "Value needs to contains a Span<string> with at least one item.");
		//     }

		//     var currentNamespaceName = namespaceSegments[0];
		//     NativeNamespaceDescription currentNamespace = null;
		//     if(!namespaces.ContainsKey(currentNamespaceName)){
		//         currentNamespace = new NativeNamespaceDescription(currentNamespaceName);
		//         namespaces.Add(currentNamespaceName, currentNamespace);
		//     }
		//     else{
		//         currentNamespace = namespaces
		//     }

		//     var remainingSegments = namespaceSegments.Slice(1);
		//     return remainingSegments.Length <= 0 ? namespaces.Last
		// }

		private static NativeNamespaceDescription GetNameSpaceDesciptorForPath(IDictionary<string, NativeNamespaceDescription> namespaces, string namespacePath, bool createMissingPath = false, bool throwExceptionIfNotFound = false) {
			var namespaceExpression = new Regex(@"^(@?[a-z_A-Z]\w+(?:\.@?[a-z_A-Z]\w+)*)$");
			if (!namespaceExpression.IsMatch(namespacePath)) {
				throw new ArgumentOutOfRangeException(nameof(namespacePath), namespacePath, "Value must be a valid namespace path");
			}

			var namespaceSegments = new Span<string>(namespacePath.Split('.'));
			var currentPath = string.Empty;
			var currentCollection = namespaces;
			for (var i = 0; i < namespaceSegments.Length; i++) {
				var segmentName = namespaceSegments[i];
				var segment = currentCollection.ContainsKey(segmentName) ? currentCollection[segmentName] : null;

				if (i > 0) {
					currentPath += ".";
				}

				currentPath += segmentName;
				if (segment == null) {
					if (createMissingPath) {
						segment = new NativeNamespaceDescription(segmentName);
						currentCollection.Add(segmentName, segment);
					} else if (throwExceptionIfNotFound) {
						throw new KeyNotFoundException($"Could not find the following namespace path in cache '{currentPath}'.");
					} else {
						break;
					}
				}
				currentCollection = segment.Namespaces;

				if (i == namespaceSegments.Length - 1) {
					return segment;
				}
			}

			throw new Exception($"Could not generate {nameof(NativeNamespaceDescription)}");
		}

		public static NativeMethodDescription GetNativeMethodFrom(ProgrammingPlatform programmingPlatform, MethodInfo methodInfo, NativeCallableAttribute nativeAttribute) {
			// Passed through GetNativeType twice, once for hte native typecoversion in C# and another for the destination type (e.g. ruby, python)
			return new NativeMethodDescription {
				Name = nativeAttribute.EntryPoint,
					ReturnType = NativeTypeDescription.FromString(GetNativeType(programmingPlatform, GetNativeType(methodInfo.ReturnType.FullName))),
					IntendedReturnType = NativeTypeDescription.FromString(GetNativeType(programmingPlatform, GetNativeType(methodInfo.ReturnTypeCustomAttributes.GetCustomAttributes(true).Where(c => c is IntendedTypeAttribute).Select(c => c as IntendedTypeAttribute).FirstOrDefault()?.Type.FullName))),
					Parameters = GetNativeParametersFrom(programmingPlatform, methodInfo)
			};
		}

		public static IList<INativeParameterDescription> GetNativeParametersFrom(ProgrammingPlatform programmingPlatform, MethodInfo methodInfo) {
			var parameters = new List<INativeParameterDescription>();
			foreach (var parameter in methodInfo.GetParameters()) {
				// Passed through GetNativeType twice, once for hte native typecoversion in C# and another for the destination type (e.g. ruby, python)
				parameters.Add(new NativeParameterDescription(parameter.Name, NativeTypeDescription.FromString(GetNativeType(programmingPlatform, GetNativeType(parameter.ParameterType.FullName))), NativeTypeDescription.FromString(GetNativeType(programmingPlatform, GetNativeType(parameter.GetCustomAttribute<IntendedTypeAttribute>(true)?.Type.FullName)))));
			}

			return parameters;
		}

		[NativeCallable(EntryPoint = "nibs__get_type", CallingConvention = CallingConvention.Cdecl)]
		[
			return :IntendedType(typeof(string))
		]
		public static IntPtr GetNativeType(int progammingPlaformNumber, [IntendedType(typeof(string))] IntPtr typeNamePtr) {
			var typeName = Marshal.PtrToStringUTF8(typeNamePtr)?.ToLower();
			var returnType = GetNativeType((ProgrammingPlatform)progammingPlaformNumber, typeName);
			return Marshal.StringToCoTaskMemUTF8(returnType);
		}

		public static string? GetNativeType(string? type) {
			return GetNativeType(ProgrammingPlatform.dotnet, type);
		}

		public static string? GetNativeType(int progammingPlaformNumber, string? typeName) {
			return GetNativeType((ProgrammingPlatform)progammingPlaformNumber, typeName);
		}

		public static string? GetNativeType(ProgrammingPlatform programmingPlatform, string? typeName) {
			if (typeName == null) {
				return null;
			}

			switch (programmingPlatform) {
				case ProgrammingPlatform.dotnet:
					{
						// .NET Case
						switch (typeName) {
							case var t when t.CompareTo("System.Int32") == 0 || t.CompareTo("int") == 0:
								return "int";
							case var t when t.CompareTo("pointer") == 0 || t.CompareTo("System.IntPtr") == 0:
								return "pointer";
							case var t when t.CompareTo("string") == 0 || t.CompareTo("System.String") == 0:
								return "string";
						}
						break;
					}

				case ProgrammingPlatform.ruby:
					{
						// .NET Case
						switch (typeName.ToLower()) {
							case var t when t.CompareTo("int") == 0:
								return "int";
							case var t when t.CompareTo("pointer") == 0:
								return "pointer";
							case var t when t.CompareTo("string") == 0:
								return "string";
							default:
								return "void";
						}
					}
			}

			return "none";
		}
	}

}