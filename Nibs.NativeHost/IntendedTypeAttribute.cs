using System;
using System.Text.Json;

namespace Nibs.NativeHost.Attributes {
	[AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
	public class IntendedTypeAttribute : Attribute {
		public IntendedTypeAttribute(Type type) {
			Type = type;
		}

		public Type Type { get; set; }
	}
}