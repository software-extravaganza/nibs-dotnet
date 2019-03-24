using System.Collections.Generic;

namespace Library
{
    public interface INativeClassDescription
    {
        string Name { get; set; }
        IDictionary<string, NativeMethodDescription> Methods { get; set; }
    }
}