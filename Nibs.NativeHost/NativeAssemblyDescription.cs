using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Nibs.NativeHost {
	public class NativeAssemblyDescription : INativeAssemblyDescription {
		public NativeAssemblyDescription(string fileName, string directory) {
			FileName = fileName;
			Directory = directory;
		}
		public string FileName { get; set; }

		public string Directory { get; set; }

		public string FullPath => Path.Combine(Directory, FileName).Replace("\\", "/");

		public IList<NativeNamespaceDescription> Namespaces { get; set; } = new List<NativeNamespaceDescription>();
	}

}