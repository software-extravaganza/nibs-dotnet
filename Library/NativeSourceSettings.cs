using System.Collections.Generic;

namespace Library
{
    public class NativeSourceSettings: INativeSourceSettings {
        public List<string> Assemblies { get; set; } = new List<string>();
    }


}