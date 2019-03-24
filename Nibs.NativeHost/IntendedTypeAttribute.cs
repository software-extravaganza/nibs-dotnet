using System;
using System.Text.Json;

namespace Library
{
    [AttributeUsage (AttributeTargets.ReturnValue | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public class IntendedTypeAttribute : Attribute, IFastJsonConvertable {
        public IntendedTypeAttribute (Type type) {
            Type = new NativeTypeDescription (type);
        }

        public NativeTypeDescription Type { get; set; }

        public void ToJson (ref Utf8JsonWriter jsonWriter, string? name) {
            throw new NotImplementedException ();
        }

        public override string ToString () {
            return Type.ToString ();
        }
    }
}