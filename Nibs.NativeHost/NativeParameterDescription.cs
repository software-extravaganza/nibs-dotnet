using System;
using System.Text.Json;

namespace Nibs.NativeHost
{
    public class NativeParameterDescription : INativeParameterDescription {
        public NativeParameterDescription (string name, NativeTypeDescription? type, NativeTypeDescription? intendedType = null) {
            Name = name;
            Type = type;
            IntendedType = intendedType;
        }

        public NativeParameterDescription (string name, Type type) {
            Name = name;
            Type = new NativeTypeDescription (type);
        }

        public NativeParameterDescription (string name, string typeRepresentation) {
            if (string.IsNullOrWhiteSpace (typeRepresentation)) {
                throw new ArgumentOutOfRangeException (nameof (typeRepresentation), typeRepresentation, "Value of 'typeRepresentation' can't be null or whitepsace.");
            }

            var type = System.Type.GetType (typeRepresentation, false);
            Name = name;
            Type = type != null ? new NativeTypeDescription (type) : new NativeTypeDescription (typeRepresentation);
        }

        public string Name { get; set; }
        public NativeTypeDescription? Type { get; set; }
        public NativeTypeDescription? IntendedType { get; set; }
    }
}