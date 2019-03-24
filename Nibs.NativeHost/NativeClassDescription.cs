using System.Collections.Generic;

namespace Nibs.NativeHost
{
    public class NativeClassDescription : INativeClassDescription {
        public NativeClassDescription (string name) {
            Name = name;
        }
        public string Name { get; set; }

        public IDictionary<string, NativeMethodDescription> Methods { get; set; } = new Dictionary<string, NativeMethodDescription> ();

    
    }
}