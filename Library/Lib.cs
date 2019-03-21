using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace Library {
    public interface IFastJsonConvertable {
        void ToJson (ref Utf8JsonWriter jsonWriter, string? name = null);
    }



    public class NativeSourceSettings {
        public List<string> Assemblies { get; set; } = new List<string>();
    }
    public class NativeCore {
        //public const string NULL_JSON_VALUE = "null";
        //public static IDictionary<IntPtr, ArrayBufferWriter> arrayBufferHold = new Dictionary<IntPtr, ArrayBufferWriter>();
        [NativeCallable (EntryPoint = "add", CallingConvention = CallingConvention.Cdecl)]
        public static int Add (int a, int b) {
            return a + b - 50;
        }

        [NativeCallable (EntryPoint = "subtract", CallingConvention = CallingConvention.Cdecl)]
        public static int Subtract (int a, int b) {
            return a - b - 50;
        }

        [NativeCallable (EntryPoint = "append", CallingConvention = CallingConvention.Cdecl)]
        [
            return :IntendedType (typeof (string))
        ]
        public static IntPtr Append ([IntendedType (typeof (string))] IntPtr a, [IntendedType (typeof (string))] IntPtr b) {
            return Marshal.StringToCoTaskMemUTF8 (Marshal.PtrToStringUTF8 (a) + Marshal.PtrToStringUTF8 (b));
        }

        // [NativeCallable(EntryPoint = "add", CallingConvention=CallingConvention.Cdecl)]
        // public static int Add(int a, int b){
        //     return a+b;
        // }
        [NativeCallable (EntryPoint = "get_native_metadata", CallingConvention = CallingConvention.Cdecl)]
        [
            return :IntendedType (typeof (string))
        ]
        public static IntPtr GetNativeMetadata ([IntendedType (typeof (ProgrammingPlatform))] int progammingPlaformNumber) {
            var utf8 = new UTF8Encoding ();
            try{
                var output = new ArrayBufferWriter<byte>();
                var json = new Utf8JsonWriter (output, state : default);
                json.WriteStartObject ();

                if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "config.json")))
                {
                    json.WriteString("Error", "No config found config.json");
                }
                else
                {
                    try
                    {
                        var configurationBuilder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("config.json", true);
                        var config = configurationBuilder.Build();
                        var nativeSourceSettings = config.GetSection("NativeSources").Get<NativeSourceSettings>();
                        var assembliesNotFound = new List<string>();
                        var assemblyFiles = new List<FileInfo>();
                        //foreach(var assembly in nativeSourceSettings.Assemblies.Distinct()){
                        // foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies().Distinct()){    
                        //     var filePath = Path.GetFullPath(assembly.FullName);
                        //     var assemblyFile = new FileInfo(filePath);
                        //     if(!assemblyFile.Exists){
                        //         assembliesNotFound.Add(assembly.FullName);
                        //     }
                        //     else{
                        //         assemblyFiles.Add(assemblyFile);
                        //     }
                        // }

                        // if(assembliesNotFound.Count > 0){
                        //     var assembliesNotFoundArrayJson = NativeCore.ListToJsonArrayHelper(assembliesNotFound);
                        //     return Marshal.StringToCoTaskMemUTF8($"{{\"Error\": \"The following assemblies were not found (defined in conifg.json): {assembliesNotFoundArrayJson}\"}}");
                        // }
                        var assemblies = new Dictionary<string, NativeAssemblyDescription>();
                        foreach (var assembly in new[] { typeof(NativeCore).Assembly })
                        {
                            var assemblyFile = new FileInfo($"{Assembly.GetExecutingAssembly().FullName.Split(",").FirstOrDefault()}.dll");
                            var namespaces = ProcessAssembly(assembly, (ProgrammingPlatform)progammingPlaformNumber);
                            assemblies.Add(assemblyFile.FullName, new NativeAssemblyDescription(assemblyFile.Name, assemblyFile.DirectoryName.Replace("\\", "/")) { Namespaces = namespaces });
                        }

                        NativeCore.ListToJsonArrayHelper(json, "Data", assemblies.Values.ToList());
                    }
                    catch (InvalidOperationException ioex)
                    {
                        json.WriteString("Error", "Error build metadata.");
                        json.WriteString("Exception", ioex.ToString());
                    }
                }

                json.WriteNumber(nameof(json.BytesWritten), json.BytesWritten);
                json.WriteNumber(nameof(json.BytesCommitted), json.BytesCommitted);
                json.WriteEndObject ();
                json.Flush();
                var holdPointer = Marshal.StringToCoTaskMemUTF8 (utf8.GetString (output.OutputAsSpan));
                //arrayBufferHold.Add(holdPointer, output);
                return holdPointer;
            }
            catch (InvalidOperationException ioex){
                var catchOutput = new ArrayBufferWriter<byte>(1024);
                var catchJson = new Utf8JsonWriter (catchOutput, state : default);
                catchJson.WriteStartObject ();
                catchJson.WriteString("Error", "Error processing or validating metadata.");
                catchJson.WriteString("Exception", ioex.ToString());
                catchJson.WriteEndObject ();
                return Marshal.StringToCoTaskMemUTF8 (utf8.GetString (catchOutput.OutputAsSpan));
            }
        }

        public static Dictionary<string, NativeNamespaceDescription> ProcessAssembly (Assembly assembly, ProgrammingPlatform progammingPlaformNumber) {
            //var methodsAndAttributes = from methodInfo in typeof(Library).GetMethods(BindingFlags.Static | BindingFlags.Public) let nativeAttribute = methodInfo.GetCustomAttribute(typeof(NativeCallableAttribute), true) as NativeCallableAttribute  where nativeAttribute != null select GetNativeMethodFrom(methodInfo, nativeAttribute);
            var namespaces = new Dictionary<string, NativeNamespaceDescription> ();
            foreach (var classFound in assembly.GetTypes ()) {
                foreach (var methodInfo in classFound.GetMethods (BindingFlags.Static | BindingFlags.Public)) {
                    var nativeAttribute = methodInfo.GetCustomAttribute (typeof (NativeCallableAttribute), true) as NativeCallableAttribute;
                    if (nativeAttribute == null) {
                        continue;
                    }

                    var currentMethodName = nativeAttribute.EntryPoint;
                    var currentClassName = methodInfo?.DeclaringType?.Name;
                    var currentNamespaceName = methodInfo?.DeclaringType?.Namespace;
                    var currentNamespace = GetNameSpaceDesciptorForPath (namespaces, currentNamespaceName, true);
                    if (!currentNamespace.Classes.ContainsKey (currentClassName)) {
                        currentNamespace.Classes.Add (currentClassName, new NativeClassDescription (currentClassName));
                    }

                    var currentClass = currentNamespace.Classes[currentClassName];
                    if (!currentClass.Methods.ContainsKey (currentMethodName)) {
                        currentClass.Methods.Add (currentMethodName, GetNativeMethodFrom (progammingPlaformNumber, methodInfo, nativeAttribute));
                    }
                }
            }

            return namespaces;
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

        private static NativeNamespaceDescription GetNameSpaceDesciptorForPath (Dictionary<string, NativeNamespaceDescription> namespaces, string namespacePath, bool createMissingPath = false, bool throwExceptionIfNotFound = false) {
            var namespaceExpression = new Regex (@"^(@?[a-z_A-Z]\w+(?:\.@?[a-z_A-Z]\w+)*)$");
            if (!namespaceExpression.IsMatch (namespacePath)) {
                throw new ArgumentOutOfRangeException (nameof (namespacePath), namespacePath, "Value must be a valid namespace path");
            }

            var namespaceSegments = new Span<string> (namespacePath.Split ('.'));
            var currentPath = string.Empty;
            var currentCollection = namespaces;
            for (var i = 0; i < namespaceSegments.Length; i++) {
                var segmentName = namespaceSegments[i];
                var segment = currentCollection.ContainsKey (segmentName) ? currentCollection[segmentName] : null;

                if (i > 0) {
                    currentPath += ".";
                }

                currentPath += segmentName;
                if (segment == null) {
                    if (createMissingPath) {
                        segment = new NativeNamespaceDescription (segmentName);
                        currentCollection.Add (segmentName, segment);
                    } else if (throwExceptionIfNotFound) {
                        throw new KeyNotFoundException ($"Could not find the following namespace path in cache '{currentPath}'.");
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

        [NativeCallable (EntryPoint = "free_ptr", CallingConvention = CallingConvention.Cdecl)]
        public static void Free (IntPtr ptr) {
            // if(arrayBufferHold.ContainsKey(ptr)){
            //     arrayBufferHold.Remove(ptr);
            // }
            Marshal.FreeCoTaskMem (ptr);
        }

        public static NativeMethodDescription GetNativeMethodFrom (ProgrammingPlatform programmingPlatform, MethodInfo methodInfo, NativeCallableAttribute nativeAttribute) {
            // Passed through GetNativeType twice, once for hte native typecoversion in C# and another for the destination type (e.g. ruby, python)
            return new NativeMethodDescription {
                Name = nativeAttribute.EntryPoint,
                    ReturnType = NativeTypeDescription.FromString (GetNativeType (programmingPlatform, GetNativeType (methodInfo.ReturnType.FullName))),
                    IntendedReturnType = NativeTypeDescription.FromString (GetNativeType (programmingPlatform, GetNativeType (methodInfo.ReturnTypeCustomAttributes.GetCustomAttributes (true).Where (c => c is IntendedTypeAttribute).Select (c => c as IntendedTypeAttribute).FirstOrDefault ()?.Type.FullName))),
                    Parameters = GetNativeParametersFrom (programmingPlatform, methodInfo)
            };
        }

        public static List<NativeParameterDescription> GetNativeParametersFrom (ProgrammingPlatform programmingPlatform, MethodInfo methodInfo) {
            var parameters = new List<NativeParameterDescription> ();
            foreach (var parameter in methodInfo.GetParameters ()) {
                // Passed through GetNativeType twice, once for hte native typecoversion in C# and another for the destination type (e.g. ruby, python)
                parameters.Add (new NativeParameterDescription (parameter.Name, NativeTypeDescription.FromString (GetNativeType (programmingPlatform, GetNativeType (parameter.ParameterType.FullName))), NativeTypeDescription.FromString (GetNativeType (programmingPlatform, GetNativeType (parameter.GetCustomAttribute<IntendedTypeAttribute> (true)?.Type.FullName)))));
            }

            return parameters;
        }

        public static void ListToJsonArrayHelper<T> (Utf8JsonWriter json, string name, IList<T> list) {
            json.WriteStartArray (name, escape: false);
            for (var count = 0; count < list.Count; count++) {
                var item = list[count];
                if (item is IFastJsonConvertable jsonObject) {
                    jsonObject.ToJson(ref json);
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

        [NativeCallable (EntryPoint = "get_type", CallingConvention = CallingConvention.Cdecl)]
        [
            return :IntendedType (typeof (string))
        ]
        public static IntPtr GetNativeType (int progammingPlaformNumber, [IntendedType (typeof (string))] IntPtr typeNamePtr) {
            var typeName = Marshal.PtrToStringUTF8 (typeNamePtr)?.ToLower ();
            var returnType = GetNativeType ((ProgrammingPlatform) progammingPlaformNumber, typeName);
            return Marshal.StringToCoTaskMemUTF8 (returnType);
        }

        public static string? GetNativeType (string? type) {
            return GetNativeType (ProgrammingPlatform.dotnet, type);
        }

        public static string? GetNativeType (int progammingPlaformNumber, string? typeName) {
            return GetNativeType ((ProgrammingPlatform) progammingPlaformNumber, typeName);
        }

        public static string? GetNativeType (ProgrammingPlatform programmingPlatform, string? typeName) {
            if (typeName == null) {
                return null;
            }

            switch (programmingPlatform) {
                case ProgrammingPlatform.dotnet:
                    {
                        // .NET Case
                        switch (typeName) {
                            case var t when t.CompareTo ("System.Int32") == 0 || t.CompareTo ("int") == 0:
                                return "int";
                            case var t when t.CompareTo ("pointer") == 0 || t.CompareTo ("System.IntPtr") == 0:
                                return "pointer";
                            case var t when t.CompareTo ("string") == 0 || t.CompareTo ("System.String") == 0:
                                return "string";
                        }
                        break;
                    }

                case ProgrammingPlatform.ruby:
                    {
                        // .NET Case
                        switch (typeName.ToLower ()) {
                            case var t when t.CompareTo ("int") == 0:
                                return "int";
                            case var t when t.CompareTo ("pointer") == 0:
                                return "pointer";
                            case var t when t.CompareTo ("string") == 0:
                                return "string";
                            default:
                                return "void";
                        }
                    }
            }

            return "none";
        }
    }

    public enum ProgrammingPlatform {
        dotnet = 0,
        python = 1,
        ruby = 2,
    }

    public class NativeAssemblyDescription : IFastJsonConvertable {
        public NativeAssemblyDescription (string fileName, string directory) {
            FileName = fileName;
            Directory = directory;
        }
        public string FileName { get; set; }

        public string Directory { get; set; }

        public string FullPath => Path.Combine (Directory, FileName).Replace ("\\", "/");

        public Dictionary<string, NativeNamespaceDescription> Namespaces { get; set; } = new Dictionary<string, NativeNamespaceDescription> ();

        public void ToJson (ref Utf8JsonWriter json, string? name) {
            if(name != null ) {json.WriteStartObject (name); } else { json.WriteStartObject();}
            json.WriteString (nameof(FullPath), FullPath);
            NativeCore.ListToJsonArrayHelper (json, nameof(Namespaces), Namespaces.Values.ToList());
            json.WriteEndObject ();
        }
    }

    public class NativeNamespaceDescription : IFastJsonConvertable {
        public NativeNamespaceDescription (string name) {
            Name = name;
        }

        public string Name { get; set; }

        public Dictionary<string, NativeClassDescription> Classes { get; set; } = new Dictionary<string, NativeClassDescription> ();

        public Dictionary<string, NativeNamespaceDescription> Namespaces { get; set; } = new Dictionary<string, NativeNamespaceDescription> ();

        public void ToJson (ref Utf8JsonWriter json, string? name) {
            if(name != null ) {json.WriteStartObject (name); } else { json.WriteStartObject();}
            json.WriteString (nameof(Name), Name);
            NativeCore.ListToJsonArrayHelper (json, nameof (Classes), Classes.Values.ToList ());
            NativeCore.ListToJsonArrayHelper (json, nameof (Namespaces), Namespaces.Values.ToList ());
            json.WriteEndObject ();
        }
    }

    public class NativeClassDescription : IFastJsonConvertable {
        public NativeClassDescription (string name) {
            Name = name;
        }
        public string Name { get; set; }

        public Dictionary<string, NativeMethodDescription> Methods { get; set; } = new Dictionary<string, NativeMethodDescription> ();

        public void ToJson (ref Utf8JsonWriter json, string? name) {
            if(name != null ) {json.WriteStartObject (name); } else { json.WriteStartObject();}
            json.WriteString (nameof(Name), Name);
            NativeCore.ListToJsonArrayHelper (json, nameof (Methods), Methods.Values.ToList ());
            json.WriteEndObject ();
        }
    }

    public class NativeMethodDescription : IFastJsonConvertable {
        public string Name { get; set; } = string.Empty;
        public List<NativeParameterDescription> Parameters { get; set; } = new List<NativeParameterDescription> ();
    
        public NativeTypeDescription? ReturnType { get; set; }
        public NativeTypeDescription? IntendedReturnType { get; set; }
        
        public void ToJson (ref Utf8JsonWriter json, string? name) {
            if(name != null ) {json.WriteStartObject (name); } else { json.WriteStartObject();}
            json.WriteString (nameof(Name), Name);
            ReturnType?.ToJson(ref json, nameof(ReturnType));
            IntendedReturnType?.ToJson(ref json, nameof(IntendedReturnType));
            NativeCore.ListToJsonArrayHelper (json, nameof (Parameters), Parameters);
            json.WriteEndObject ();
        }
    }

    public class NativeParameterDescription : IFastJsonConvertable {
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

        public void ToJson (ref Utf8JsonWriter json, string? name) {
            if(name != null ) {json.WriteStartObject (name); } else { json.WriteStartObject();}
            json.WriteString (nameof(Name), Name);
            Type?.ToJson(ref json, nameof(Type));
            IntendedType?.ToJson(ref json, nameof(IntendedType));
            json.WriteEndObject ();
        }
    }

    public class NativeTypeDescription : IFastJsonConvertable {

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

        public void ToJson (ref Utf8JsonWriter json, string? name) {
            if(name != null ) {json.WriteStartObject (name); } else { json.WriteStartObject();}
            json.WriteString (nameof(Name), Name);
            json.WriteString (nameof(Namespace), Namespace);
            json.WriteString (nameof(AssemblyPath), AssemblyPath);
            json.WriteEndObject ();
        }
    }

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