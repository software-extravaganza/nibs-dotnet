using System;
using System.Collections.Generic;

namespace Nibs.NativeHost
{
    internal interface INativeExporter
    {
        ReadOnlySpan<byte> Export(Func<INativeSourceSettingLoadResult> loadConfiguration, Func<INativeSourceSettings, ILoadedAssembliesResult> getAssemblyDescriptions);

        ReadOnlySpan<byte> ExportError(string? error, Exception? exception);
    }
}