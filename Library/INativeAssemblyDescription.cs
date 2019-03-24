using System.Collections.Generic;

namespace Library
{
    public interface INativeAssemblyDescription
    {
        string FileName { get; set; }
        string Directory { get; set; }
        string FullPath { get; }
        IList<NativeNamespaceDescription> Namespaces { get; set; }
    }
}