using System.Collections.Generic;

namespace Library
{
    public interface INativeMethodDescription
    {
        string Name { get; set; }
        IList<INativeParameterDescription> Parameters { get; set; }
        NativeTypeDescription? ReturnType { get; set; }
        NativeTypeDescription? IntendedReturnType { get; set; }
    }
}