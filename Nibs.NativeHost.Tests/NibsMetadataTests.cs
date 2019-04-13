using System;
using System.Runtime.InteropServices;
using HostA;
using Nibs.NativeHost;
using Xunit;

namespace Nibs.NativeHost.Tests {
	public class NibsMetadataTests {
		[Fact]
		public void MetaDataJsonMatches () {
			var metadataPointer = Host.GetNativeMetadata ((int) ProgrammingPlatform.dotnet, (int) ExportType.Json);
			var metadata = Marshal.PtrToStringUTF8 (metadataPointer);
			Assert.Equal ("", metadata);
		}

		[Fact]
		public void MetaDataJsonIsLongerThanBufferSize () {
			var metadataPointer = Host.GetNativeMetadata ((int) ProgrammingPlatform.dotnet, (int) ExportType.UnsafeJson);
			var metadata = Marshal.PtrToStringUTF8 (metadataPointer);

		}

		[Fact]
		public void MetaDataExceptionWorks () {
			var metadataPointer = Host.GetNativeMetadata (5000000, (int) ExportType.Json);
			var metadata = Marshal.PtrToStringUTF8 (metadataPointer);

		}

	}
}