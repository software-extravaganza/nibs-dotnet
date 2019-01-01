using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace Library2
{

    public class NativeSourceSettings
    {
        public List<string> Assemblies { get; set; }
    }
    public class NativeCore {
        public const string NULL_JSON_VALUE = "null";

        [NativeCallable(EntryPoint = "add", CallingConvention=CallingConvention.Cdecl)]
        public static int Add(int a, int b){
            return a+b;
        }

        [NativeCallable(EntryPoint = "subtract", CallingConvention=CallingConvention.Cdecl)]
        public static int Subtract(int a, int b){
            return a-b;
        }

        [NativeCallable(EntryPoint = "append", CallingConvention=CallingConvention.Cdecl)]
        [return: IntendedType(typeof(string))]
        public static IntPtr Append([IntendedType(typeof(string))]IntPtr a, [IntendedType(typeof(string))]IntPtr b){
            return Marshal.StringToCoTaskMemUTF8(Marshal.PtrToStringUTF8(a) + Marshal.PtrToStringUTF8(b));
        }

        // [NativeCallable(EntryPoint = "add", CallingConvention=CallingConvention.Cdecl)]
        // public static int Add(int a, int b){
        //     return a+b;
        // }
        [NativeCallable(EntryPoint = "get_native_metadata", CallingConvention = CallingConvention.Cdecl)]
        [return: IntendedType(typeof(string))]
        public static IntPtr GetNativeMetadata([IntendedType(typeof(ProgrammingPlatform))]int progammingPlaformNumber){
            if(File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "config.json"))){
                try{
                    var configurationBuilder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("config.json", true);
                    var config = configurationBuilder.Build();
                    var nativeSourceSettings =  config.GetSection("NativeSources").Get<NativeSourceSettings>();
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
                    foreach(var assembly in new [] { typeof(NativeCore).Assembly}){
                        
                        var assemblyFile = new FileInfo($"{Assembly.GetExecutingAssembly().FullName.Split(",").FirstOrDefault()}.dll");
                        var namespaces = ProcessAssembly(assembly, (ProgrammingPlatform)progammingPlaformNumber);
                        assemblies.Add(assemblyFile.FullName, new NativeAssemblyDescription(assemblyFile.Name, assemblyFile.DirectoryName.Replace("\\", "/")){ Namespaces = namespaces });
                    }

                    var assembliesArrayJson = NativeCore.ListToJsonArrayHelper(assemblies.Values.ToList());
                    return Marshal.StringToCoTaskMemUTF8($"{{\"Data\":{assembliesArrayJson}}}");
                }
                catch(InvalidOperationException ioex){
                    return Marshal.StringToCoTaskMemUTF8("{\"Error\": \"Config.json does not have a valid configuration.\"}");
                }
            }

            return Marshal.StringToCoTaskMemUTF8("{\"Error\": \"No config found config.json\"}");
        }


        public static Dictionary<string, NativeNamespaceDescription> ProcessAssembly(Assembly assembly, ProgrammingPlatform progammingPlaformNumber) 
        {
            //var methodsAndAttributes = from methodInfo in typeof(Library).GetMethods(BindingFlags.Static | BindingFlags.Public) let nativeAttribute = methodInfo.GetCustomAttribute(typeof(NativeCallableAttribute), true) as NativeCallableAttribute  where nativeAttribute != null select GetNativeMethodFrom(methodInfo, nativeAttribute);
            var namespaces = new Dictionary<string, NativeNamespaceDescription>();
            foreach(var classFound in assembly.GetTypes()){
                foreach(var methodInfo in classFound.GetMethods(BindingFlags.Static | BindingFlags.Public)){
                    var nativeAttribute = methodInfo.GetCustomAttribute(typeof(NativeCallableAttribute), true) as NativeCallableAttribute;
                    if(nativeAttribute == null){
                        continue;
                    }

                    var currentMethodName = nativeAttribute.EntryPoint;
                    var currentClassName = methodInfo?.DeclaringType?.Name;
                    var currentNamespaceName = methodInfo?.DeclaringType?.Namespace;
                    var currentNamespace = GetNameSpaceDesciptorForPath(namespaces, currentNamespaceName, true);
                    if(!currentNamespace.Classes.ContainsKey(currentClassName)){
                        currentNamespace.Classes.Add(currentClassName, new NativeClassDescription(currentClassName));
                    }

                    var currentClass = currentNamespace.Classes[currentClassName];
                    if(!currentClass.Methods.ContainsKey(currentMethodName)){
                        currentClass.Methods.Add(currentMethodName, GetNativeMethodFrom(progammingPlaformNumber, methodInfo, nativeAttribute));
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

        private static NativeNamespaceDescription GetNameSpaceDesciptorForPath(Dictionary<string, NativeNamespaceDescription> namespaces, string namespacePath, bool createMissingPath = false, bool throwExceptionIfNotFound = false){
            var namespaceExpression = new Regex(@"^(@?[a-z_A-Z]\w+(?:\.@?[a-z_A-Z]\w+)*)$");
            if(!namespaceExpression.IsMatch(namespacePath)){
                throw new ArgumentOutOfRangeException(nameof(namespacePath), namespacePath, "Value must be a valid namespace path");
            }
            
            var namespaceSegments = new Span<string>(namespacePath.Split('.'));
            var currentPath = string.Empty;
            var currentCollection = namespaces;
            for(var i = 0; i < namespaceSegments.Length; i++){
                var segmentName = namespaceSegments[i];
                var segment = currentCollection.ContainsKey(segmentName) ?  currentCollection[segmentName] : null;

                if(i > 0){
                    currentPath += ".";
                }

                currentPath += segmentName;
                if(segment == null){
                    if(createMissingPath){
                        segment = new NativeNamespaceDescription(segmentName);
                        currentCollection.Add(segmentName, segment);
                    }
                    else if(throwExceptionIfNotFound){
                        throw new KeyNotFoundException($"Could not find the following namespace path in cache '{currentPath}'.");
                    }
                    else{
                        break;
                    }
                }
                currentCollection = segment.Namespaces;

                if(i == namespaceSegments.Length - 1){
                    return segment;
                }
            }

            return null;
        }

        [NativeCallable(EntryPoint = "free_ptr", CallingConvention = CallingConvention.Cdecl)]
        public static void Free(IntPtr ptr) {
            Marshal.FreeCoTaskMem(ptr);
        }

        public static NativeMethodDescription GetNativeMethodFrom(ProgrammingPlatform programmingPlatform, MethodInfo methodInfo, NativeCallableAttribute nativeAttribute){
            // Passed through GetNativeType twice, once for hte native typecoversion in C# and another for the destination type (e.g. ruby, python)
            return new NativeMethodDescription{ 
                Name = nativeAttribute.EntryPoint, 
                ReturnType = NativeTypeDescription.FromString(GetNativeType(programmingPlatform, GetNativeType(methodInfo.ReturnType.FullName))), 
                IntendedReturnType = NativeTypeDescription.FromString(GetNativeType(programmingPlatform, GetNativeType(methodInfo.ReturnTypeCustomAttributes.GetCustomAttributes(true).Where(c => c is IntendedTypeAttribute).Select(c => c as IntendedTypeAttribute).FirstOrDefault()?.Type.FullName))),
                Parameters = GetNativeParametersFrom(programmingPlatform, methodInfo) };
        }

        public static List<NativeParameterDescription> GetNativeParametersFrom(ProgrammingPlatform programmingPlatform, MethodInfo methodInfo){
            var parameters = new List<NativeParameterDescription>();
            foreach(var parameter in methodInfo.GetParameters()){
                // Passed through GetNativeType twice, once for hte native typecoversion in C# and another for the destination type (e.g. ruby, python)
                parameters.Add(new NativeParameterDescription(parameter.Name, NativeTypeDescription.FromString(GetNativeType(programmingPlatform, GetNativeType(parameter.ParameterType.FullName))), NativeTypeDescription.FromString(GetNativeType(programmingPlatform,  GetNativeType(parameter.GetCustomAttribute<IntendedTypeAttribute>(true)?.Type.FullName)))));
            }
 
            return parameters;
        }

        public static string ListToJsonArrayHelper<T>(IList<T> list){
            var arrayStringbuilder = new StringBuilder("[");
            ListToJsonArrayInternalHelper(list, arrayStringbuilder);
            arrayStringbuilder.Append("]");
            return arrayStringbuilder.ToString();
        }

        public static StringBuilder ListToJsonArrayInternalHelper<T>(IList<T> list, StringBuilder arrayStringbuilder = null){
            if(arrayStringbuilder == null){
                arrayStringbuilder = new StringBuilder();
            }
            
            for(var i = 0;  i < list.Count; i++){
                arrayStringbuilder.Append(list[i]);
                if(i < list.Count - 1){
                    arrayStringbuilder.Append(", ");
                }
            }

            return arrayStringbuilder;
        }

        [NativeCallable(EntryPoint = "get_type", CallingConvention = CallingConvention.Cdecl)]
        [return: IntendedType(typeof(string))]
        public static IntPtr GetNativeType(int progammingPlaformNumber, [IntendedType(typeof(string))]IntPtr typeNamePtr) {
            var typeName = Marshal.PtrToStringUTF8(typeNamePtr)?.ToLower();
            var returnType = GetNativeType((ProgrammingPlatform)progammingPlaformNumber, typeName);
            return Marshal.StringToCoTaskMemUTF8(returnType);
        }

        public static string GetNativeType(string type) {
            return GetNativeType(ProgrammingPlatform.dotnet, type);
        }
        
        public static string GetNativeType(int progammingPlaformNumber, string typeName) {
            return GetNativeType((ProgrammingPlatform)progammingPlaformNumber, typeName);
        }

        public static string GetNativeType(ProgrammingPlatform programmingPlatform, string typeName) {
            if(typeName == null){
                return null;
            }

            switch(programmingPlatform){
                case ProgrammingPlatform.dotnet:{
                    // .NET Case
                    switch(typeName){
                        case var t when t.CompareTo("System.Int32") == 0 || t.CompareTo("int") == 0:
                            return "int";
                        case var t when t.CompareTo("pointer") == 0 || t.CompareTo("System.IntPtr") == 0:
                            return "pointer";
                        case var t when t.CompareTo("string") == 0 || t.CompareTo("System.String") == 0:
                            return "string";
                    }
                    break;
                }

                 case ProgrammingPlatform.ruby:{
                    // .NET Case
                    switch(typeName.ToLower()){
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

    public enum ProgrammingPlatform{
        dotnet = 0,
        python = 1,
        ruby = 2,
    }

    public class NativeAssemblyDescription {
        public NativeAssemblyDescription(string fileName, string directory){
            FileName = fileName;
            Directory = directory;
        }
        public string FileName {get;set;}

        public string Directory {get;set;}

        public string FullPath => Path.Combine(Directory, FileName).Replace("\\", "/");

        public Dictionary<string, NativeNamespaceDescription> Namespaces {get;set;} = new Dictionary<string, NativeNamespaceDescription>();

        public override string ToString(){
            var namespaceArrayJson = NativeCore.ListToJsonArrayHelper(Namespaces.Values.ToList());
            return $"{{\"FullPath\": \"{FullPath}\", \"Namespaces\": {namespaceArrayJson}}}";
        }
    }

    public class NativeNamespaceDescription {
        public NativeNamespaceDescription(string name){
            Name = name;
        }

        public string Name {get;set;}

        public Dictionary<string, NativeClassDescription> Classes {get;set;} = new Dictionary<string, NativeClassDescription>();

        public Dictionary<string, NativeNamespaceDescription> Namespaces {get;set;} = new Dictionary<string, NativeNamespaceDescription>();

        public override string ToString(){
            var classesArrayJson = NativeCore.ListToJsonArrayHelper(Classes.Values.ToList());
            var namespaceArrayJson = NativeCore.ListToJsonArrayHelper(Namespaces.Values.ToList());
            return $"{{\"Name\": \"{Name}\", \"Classes\": {classesArrayJson}, \"Namespaces\": {namespaceArrayJson}}}";
        }
    }

    public class NativeClassDescription{
        public NativeClassDescription(string name){
            Name = name;
        }
        public string Name {get;set;}

        public Dictionary<string, NativeMethodDescription> Methods {get;set;} = new Dictionary<string, NativeMethodDescription>();

        public override string ToString(){
            var methodsArray = NativeCore.ListToJsonArrayHelper(Methods.Values.ToList());
            return $"{{\"Name\": \"{Name}\", \"Methods\": {methodsArray}}}";
        }
    }

    public class NativeMethodDescription{
        public string Name {get;set;}
        public List<NativeParameterDescription> Parameters {get;set;} = new List<NativeParameterDescription>();
        public NativeTypeDescription ReturnType {get;set;}
        public NativeTypeDescription IntendedReturnType {get;set;}

        public override string ToString(){
            var parameterArray = NativeCore.ListToJsonArrayHelper(Parameters);
            return $"{{\"Name\": \"{Name}\", \"ReturnType\": {ReturnType?.ToString() ?? NativeCore.NULL_JSON_VALUE}, \"IntendedReturnType\": {IntendedReturnType?.ToString() ?? NativeCore.NULL_JSON_VALUE}, \"Parameters\": {parameterArray}}}";
        }
    }

    public class NativeParameterDescription{
        public NativeParameterDescription(string name, NativeTypeDescription type, NativeTypeDescription intendedType = null){
            Name = name;
            Type = type;
            IntendedType = intendedType;
        }

        public NativeParameterDescription(string name, Type type){
            Name = name;
            Type = new NativeTypeDescription(type);
        }

        public NativeParameterDescription(string name, string typeRepresentation){
            if (string.IsNullOrWhiteSpace(typeRepresentation)) {
                throw new ArgumentOutOfRangeException(nameof(typeRepresentation), typeRepresentation, "Value of 'typeRepresentation' can't be null or whitepsace.");
            }
            
            var type = System.Type.GetType(typeRepresentation, false);
            Name = name;
            Type =  type != null ? new NativeTypeDescription(type) : new NativeTypeDescription(typeRepresentation);
        }

        public string Name {get;set;}    
        public NativeTypeDescription Type {get;set;}
        public NativeTypeDescription IntendedType {get;set;}

        public override string ToString(){
            return $"{{\"Name\": \"{Name}\", \"Type\": {Type}, \"IntendedType\": {IntendedType?.ToString() ?? NativeCore.NULL_JSON_VALUE}}}";
        }
    }

    public class NativeTypeDescription{
        
        public NativeTypeDescription(Type type){
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }

            Name = type.Name;
            Namespace = type.Namespace;
            AssemblyPath = type.Assembly.Location;
        }

        public NativeTypeDescription(string typeRepresentation){
            if (string.IsNullOrWhiteSpace(typeRepresentation)) {
                throw new ArgumentOutOfRangeException(nameof(typeRepresentation), typeRepresentation, "Value of 'typeRepresentation' can't be null or whitepsace.");
            }

            Name = typeRepresentation;
        }

        public static NativeTypeDescription FromString(string typeRepresentation){
            if(typeRepresentation == null){
                return null;
            }

            if (string.IsNullOrWhiteSpace(typeRepresentation)) {
                throw new ArgumentOutOfRangeException(nameof(typeRepresentation), typeRepresentation, "Value of 'typeRepresentation' can't be null or whitepsace.");
            }

            var type = Type.GetType(typeRepresentation, false);
            return type != null ? new NativeTypeDescription(type) : new NativeTypeDescription(typeRepresentation);
        }
        public string Name {get;set;}    
        public string Namespace {get;set;}
        public string AssemblyPath {get;set;}
        public string FullName { get => string.IsNullOrWhiteSpace(Namespace) ? Name : $"{Namespace}.{Name}"; }

        public override string ToString(){
            return $"{{\"Name\": \"{Name}\", \"Namespace\": \"{Namespace}\", \"AssemblyPath\": \"{AssemblyPath}\"}}";
        }
    }

    [AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true) ]
    public class IntendedTypeAttribute: Attribute{
        public IntendedTypeAttribute(Type type){
            Type = new NativeTypeDescription(type);
        }

        public NativeTypeDescription Type {get;set;}

        public override string ToString(){
            return Type.ToString();
        }
    }
}