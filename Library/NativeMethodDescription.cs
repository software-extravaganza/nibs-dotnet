using System.Collections.Generic;
using System.Text.Json;

namespace Library
{
    public class NativeMethodDescription : INativeMethodDescription {
        public string Name { get; set; } = string.Empty;
        public IList<INativeParameterDescription> Parameters { get; set; } = new List<INativeParameterDescription> ();
    
        public NativeTypeDescription? ReturnType { get; set; }
        public NativeTypeDescription? IntendedReturnType { get; set; }
    }
}