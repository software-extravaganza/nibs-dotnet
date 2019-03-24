using System.Collections.Generic;

namespace Nibs.NativeHost
{
    public class NativeNamespaceDescription : INativeNamespaceDescription {
        public NativeNamespaceDescription (string name) {
            Name = name;
        }

        public string Name { get; set; }

        public IDictionary<string, NativeClassDescription> Classes { get; set; } = new Dictionary<string, NativeClassDescription> ();

        public IDictionary<string, NativeNamespaceDescription> Namespaces { get; set; } = new Dictionary<string, NativeNamespaceDescription> ();

    }
}