using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Nibs.NativeHost
{
    public class LoadedAssembliesResult : ILoadedAssembliesResult {
        public LoadedAssembliesResult(IList<NativeAssemblyDescription>? assembliesLoaded, IList<string>? assembliesNotFound)
        {
            AssembliesLoaded = assembliesLoaded ?? new List<NativeAssemblyDescription>();
            AssembliesNotFound = assembliesNotFound ?? new List<string>();
        }
        public IList<NativeAssemblyDescription> AssembliesLoaded { get; private set; }
        public IList<string> AssembliesNotFound { get; private set; }
        public bool HasError => AssembliesNotFound.Count > 0;
        public string Error => $"The following assemblies were not found (defined in conifg.json): {(string.Join(", ", AssembliesNotFound))}";

    }
}