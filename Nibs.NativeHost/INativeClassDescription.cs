using System.Collections.Generic;

namespace Nibs.NativeHost
{
    public interface INativeClassDescription
    {
        string Name { get; set; }
        IDictionary<string, NativeMethodDescription> Methods { get; set; }
    }
}