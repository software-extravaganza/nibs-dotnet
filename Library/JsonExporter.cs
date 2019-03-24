using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Library
{
    internal class JsonExporter : INativeExporter
    {
        public const int BUFFER_SIZE = 500;
        public ReadOnlySpan<byte> Export(Func<INativeSourceSettingLoadResult> loadConfiguration, Func<INativeSourceSettings, ILoadedAssembliesResult> getAssemblyDescriptions)
        {
            var output = new ArrayBufferWriter<byte>(BUFFER_SIZE);
            var json = new Utf8JsonWriter (output, state : default);
            var loadConfigurationResult = loadConfiguration();
            json.WriteStartObject ();

            if(loadConfigurationResult.HasError){
                json.WriteString("Error", loadConfigurationResult.Error);
                json.WriteString("Exception", loadConfigurationResult.Exception?.ToString());
            }
            else{
                try{
                    var assembliesResult = getAssemblyDescriptions(loadConfigurationResult.Settings);
                    if(assembliesResult.HasError){
                        json.WriteString("Error", assembliesResult.Error);
                    }
                    else{
                        ListToJsonArrayHelper(json, "Data", assembliesResult.AssembliesLoaded);
                    }
                }
                catch (Exception ex){
                    var catchOutput = new ArrayBufferWriter<byte>(BUFFER_SIZE);
                    var catchJson = new Utf8JsonWriter (catchOutput, state : default);
                    catchJson.WriteStartObject ();
                    catchJson.WriteString("Error", "Error processing or validating metadata.");
                    catchJson.WriteString("Exception", ex.ToString());
                    catchJson.WriteEndObject ();
                    catchJson.Flush();
                    return catchOutput.OutputAsSpan;
                }
            }

            json.WriteEndObject ();
            json.Flush();
            return output.OutputAsSpan;
        }

        public static void ListToJsonArrayHelper<T> (Utf8JsonWriter json, string name, IList<T> list) {
            json.WriteStartArray (name, escape: false);
            for (var count = 0; count < list.Count; count++) {
                var item = list[count];
                if (item is INativeAssemblyDescription assemblyDescription) {
                    JsonForNativeAssemblyDesciption(ref json, null, assemblyDescription);
                } else if (item is INativeNamespaceDescription namespaceDescription) {
                    JsonForNativeNamespaceDesciption(ref json, null, namespaceDescription);
                } else if (item is INativeClassDescription classDescription) {
                    JsonForNativeClassDesciption(ref json, null, classDescription);
                } else if (item is INativeMethodDescription methodDescription) {
                    JsonForNativeMethodDesciption(ref json, null, methodDescription);
                } else if (item is INativeParameterDescription paremeterDescription) {
                    JsonForNativeParameterDesciption(ref json, null, paremeterDescription);
                } else if (item is INativeTypeDescription typeDescription) {
                    JsonForNativeTypeDesciption(ref json, null, typeDescription);
                } else if (item is DateTime dt) {
                    json.WriteStringValue (dt);
                } else if (item is DateTimeOffset dto) {
                    json.WriteStringValue (dto);
                } else if (item is Guid g) {
                    json.WriteStringValue (g);
                } else if (item is bool b) {
                    json.WriteBooleanValue (b);
                } else if (item is int i) {
                    json.WriteNumberValue (i);
                } else if (item is long l) {
                    json.WriteNumberValue (l);
                } else if (item is decimal d) {
                    json.WriteNumberValue (d);
                } else if (item is float f) {
                    json.WriteNumberValue (f);
                } else if (item is string s) {
                    json.WriteStringValue (s, true);
                } else if (item is null) {
                    json.WriteNullValue ();
                } else {
                    throw new ArgumentException ($"Element #{count+1} was not a type that could be written to the json payload from the array", nameof (list));
                }
            }
            json.WriteEndArray ();
        }


        private static void JsonForNativeTypeDesciption(ref Utf8JsonWriter json, string? name, INativeTypeDescription? t)
        {
            if(t == null){
                return;
            }

            if(name != null ) {json.WriteStartObject (name); } else { json.WriteStartObject();}
            json.WriteString (nameof(t.Name), t.Name);
            json.WriteString (nameof(t.Namespace), t.Namespace);
            json.WriteString (nameof(t.AssemblyPath), t.AssemblyPath);
            json.WriteEndObject ();
        }

        private static void JsonForNativeParameterDesciption(ref Utf8JsonWriter json, string? name, INativeParameterDescription p)
        {
            if(name != null ) {json.WriteStartObject (name); } else { json.WriteStartObject();}
            json.WriteString (nameof(p.Name), p.Name);
            JsonForNativeTypeDesciption(ref json, nameof(p.Type), p.Type);
            JsonForNativeTypeDesciption(ref json, nameof(p.IntendedType), p.IntendedType);
            json.WriteEndObject ();
        }

        private static void JsonForNativeMethodDesciption(ref Utf8JsonWriter json, string? name, INativeMethodDescription m)
        {
            if(name != null ) {json.WriteStartObject (name); } else { json.WriteStartObject();}
            json.WriteString (nameof(m.Name), m.Name); 
            JsonForNativeTypeDesciption(ref json, nameof(m.ReturnType), m.ReturnType);
            JsonForNativeTypeDesciption(ref json, nameof(m.IntendedReturnType), m.IntendedReturnType);
            ListToJsonArrayHelper (json, nameof (m.Parameters), m.Parameters);
            json.WriteEndObject ();
        }

        private static void JsonForNativeClassDesciption(ref Utf8JsonWriter json, string? name, INativeClassDescription c)
        {
            if(name != null ) {json.WriteStartObject (name); } else { json.WriteStartObject();}
            json.WriteString (nameof(c.Name), c.Name);
            ListToJsonArrayHelper (json, nameof (c.Methods), c.Methods.Values.ToList ());
            json.WriteEndObject ();
        }

        private static void JsonForNativeNamespaceDesciption(ref Utf8JsonWriter json, string? name, INativeNamespaceDescription n)
        {
            if(name != null ) {json.WriteStartObject (name); } else { json.WriteStartObject();}
            json.WriteString (nameof(n.Name), n.Name);
            ListToJsonArrayHelper (json, nameof (n.Classes), n.Classes.Values.ToList ());
            ListToJsonArrayHelper (json, nameof (n.Namespaces), n.Namespaces.Values.ToList ());
            json.WriteEndObject ();
        }
        private static void JsonForNativeAssemblyDesciption(ref Utf8JsonWriter json, string? name, INativeAssemblyDescription a)
        {
            if(name != null) { json.WriteStartObject (name); } else { json.WriteStartObject(); }
            json.WriteString (nameof(a.FullPath), a.FullPath);
            ListToJsonArrayHelper (json, nameof(a.Namespaces), a.Namespaces);
            json.WriteEndObject ();
        }

        public ReadOnlySpan<byte> ExportError(string? error, Exception? exception)
        {
            var output = new ArrayBufferWriter<byte>(BUFFER_SIZE);
            var json = new Utf8JsonWriter (output, state : default);
            json.WriteStartObject ();
            json.WriteString("Error", error);
            json.WriteString("Exception", exception?.ToString());
            json.WriteEndObject ();
            json.Flush();
            return output.OutputAsSpan;
        }
    }
}