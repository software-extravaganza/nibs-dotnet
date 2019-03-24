using System.Collections.Generic;

namespace Nibs.NativeHost
{
    public interface INativeMethodDescription
    {
        string Name { get; set; }
        IList<INativeParameterDescription> Parameters { get; set; }
        NativeTypeDescription? ReturnType { get; set; }
        NativeTypeDescription? IntendedReturnType { get; set; }
    }
}