using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nibs.NativeHost {
	public class UnsafeJsonExporter : INativeExporter {
		public const string NULL_JSON_VALUE = "null";
		public ReadOnlySpan<byte> Export(Func<INativeSourceSettingLoadResult> loadConfiguration, Func<INativeSourceSettings, ILoadedAssembliesResult> getAssemblyDescriptions) {
			var json = new StringBuilder();
			var loadConfigurationResult = loadConfiguration();
			if (loadConfigurationResult.HasError) {
				json.Append($"{{\"Error\": \"Config.json does not have a valid configuration.\", \"Exception\":\"{loadConfigurationResult.Exception}\"}}");
			} else {
				try {
					var assembliesResult = getAssemblyDescriptions(loadConfigurationResult.Settings);
					if (assembliesResult.HasError) {
						json.Append($"{{\"Error\": \"{assembliesResult.Error}\"}}");
					} else {
						json.Append($"{{\"Data\": {ListToJsonArrayHelper(assembliesResult.AssembliesLoaded)}}}");
					}
				} catch (Exception ex) {
					json = new StringBuilder();
					json.Append($"{{\"Error\": \"Error processing or validating metadata.\", \"Exception\":\"{ex}\"}}");
				}
			}

			var utf8 = new UTF8Encoding();
			return utf8.GetBytes(json.ToString()).AsSpan();
		}

		public static string ListToJsonArrayHelper<T>(IList<T> list) {
			var arrayStringbuilder = new StringBuilder("[");
			ListToJsonArrayInternalHelper(list, arrayStringbuilder);
			arrayStringbuilder.Append("]");
			return arrayStringbuilder.ToString();
		}

		public static StringBuilder ListToJsonArrayInternalHelper<T>(IList<T> list, StringBuilder? arrayStringbuilder = null) {
			if (arrayStringbuilder == null) {
				arrayStringbuilder = new StringBuilder();
			}

			for (var i = 0; i < list.Count; i++) {
				var item = list[i];
				if (item is INativeAssemblyDescription assemblyDescription) {
					arrayStringbuilder.Append(JsonForNativeAssemblyDesciption(assemblyDescription));
				} else if (item is INativeNamespaceDescription namespaceDescription) {
					arrayStringbuilder.Append(JsonForNativeNamespaceDesciption(namespaceDescription));
				} else if (item is INativeClassDescription classDescription) {
					arrayStringbuilder.Append(JsonForNativeClassDesciption(classDescription));
				} else if (item is INativeMethodDescription methodDescription) {
					arrayStringbuilder.Append(JsonForNativeMethodDesciption(methodDescription));
				} else if (item is INativeParameterDescription paremeterDescription) {
					arrayStringbuilder.Append(JsonForNativeParameterDesciption(paremeterDescription));
				} else if (item is INativeTypeDescription typeDescription) {
					arrayStringbuilder.Append(JsonForNativeTypeDesciption(typeDescription));
				}

				if (i < list.Count - 1) {
					arrayStringbuilder.Append(", ");
				}
			}

			return arrayStringbuilder;
		}

		private static string JsonForNativeTypeDesciption(INativeTypeDescription? t) {
			if (t == null) {
				return NULL_JSON_VALUE;
			}

			return $"{{\"Name\": \"{t.Name}\", \"Namespace\": \"{t.Namespace}\", \"AssemblyPath\": \"{t.AssemblyPath}\"}}";
		}

		private static string JsonForNativeParameterDesciption(INativeParameterDescription p) {
			return $"{{\"Name\": \"{p.Name}\", \"Type\": {JsonForNativeTypeDesciption(p.Type)}, \"IntendedType\": {JsonForNativeTypeDesciption(p.IntendedType)}}}";
		}

		private static string JsonForNativeMethodDesciption(INativeMethodDescription m) {
			var parameterArray = ListToJsonArrayHelper(m.Parameters);
			return $"{{\"Name\": \"{m.Name}\", \"ReturnType\": {JsonForNativeTypeDesciption(m.ReturnType)}, \"IntendedReturnType\": {JsonForNativeTypeDesciption(m.IntendedReturnType)}, \"Parameters\": {parameterArray}}}";
		}

		private static string JsonForNativeClassDesciption(INativeClassDescription c) {
			var methodsArray = ListToJsonArrayHelper(c.Methods.Values.ToList());
			return $"{{\"Name\": \"{c.Name}\", \"Methods\": {methodsArray}}}";
		}

		private static string JsonForNativeNamespaceDesciption(INativeNamespaceDescription n) {
			var classesArrayJson = ListToJsonArrayHelper(n.Classes.Values.ToList());
			var namespaceArrayJson = ListToJsonArrayHelper(n.Namespaces.Values.ToList());
			return $"{{\"Name\": \"{n.Name}\", \"Classes\": {classesArrayJson}, \"Namespaces\": {namespaceArrayJson}}}";
		}

		private static string JsonForNativeAssemblyDesciption(INativeAssemblyDescription a) {
			var namespaceArrayJson = ListToJsonArrayHelper(a.Namespaces);
			return $"{{\"FullPath\": \"{a.FullPath}\", \"Namespaces\": {namespaceArrayJson}}}";
		}

		public ReadOnlySpan<byte> ExportError(string? error, Exception? exception) {
			var json = $"{{\"Error\": \"{sanitizeResult(error)}\", \"Exception\":\"{sanitizeResult(exception?.ToString())}\"}}";
			var utf8 = new UTF8Encoding();
			return utf8.GetBytes(json.ToString()).AsSpan();
		}

		private string sanitizeResult(string? result) {
			return result == null ? string.Empty : result?.Replace("\"", "\\\"");
		}
	}
}