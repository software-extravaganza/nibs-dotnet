using System.Collections.Generic;

namespace Nibs.NativeHost
{
    public class NativeSourceSettings: INativeSourceSettings {
        public List<string> Assemblies { get; set; } = new List<string>();
    }


}