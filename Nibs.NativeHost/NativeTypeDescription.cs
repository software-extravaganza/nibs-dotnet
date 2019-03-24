using System;
using System.Text.Json;

namespace Library
{
    public class NativeTypeDescription : INativeTypeDescription {

        public NativeTypeDescription (Type type) {
            if (type == null) {
                throw new ArgumentNullException (nameof (type));
            }

            Name = type.Name;
            Namespace = type.Namespace;
            AssemblyPath = type.Assembly.Location;
        }

        public NativeTypeDescription (string typeRepresentation) {
            if (string.IsNullOrWhiteSpace (typeRepresentation)) {
                throw new ArgumentOutOfRangeException (nameof (typeRepresentation), typeRepresentation, "Value of 'typeRepresentation' can't be null or whitepsace.");
            }

            Name = typeRepresentation;
        }

        public static NativeTypeDescription? FromString (string? typeRepresentation) {
            if (typeRepresentation == null) {
                return null;
            }

            if (string.IsNullOrWhiteSpace (typeRepresentation)) {
                throw new ArgumentOutOfRangeException (nameof (typeRepresentation), typeRepresentation, "Value of 'typeRepresentation' can't be null or whitepsace.");
            }

            var type = Type.GetType (typeRepresentation, false);
            return type != null ? new NativeTypeDescription (type) : new NativeTypeDescription (typeRepresentation);
        }
        public string Name { get; set; }
        public string? Namespace { get; set; }
        public string? AssemblyPath { get; set; }
        public string FullName { get => string.IsNullOrWhiteSpace (Namespace) ? Name : $"{Namespace}.{Name}"; }
    }
}