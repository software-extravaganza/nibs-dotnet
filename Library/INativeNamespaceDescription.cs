using System.Collections.Generic;

namespace Library
{
    public interface INativeNamespaceDescription
    {
        string Name { get; set; }
        IDictionary<string, NativeClassDescription> Classes { get; set; }
        IDictionary<string, NativeNamespaceDescription> Namespaces { get; set; }
    }
}