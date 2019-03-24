using System;
using Xunit;
using Library;
using System.Runtime.InteropServices;

namespace Library_Tests
{
    public class NibsMetadataTests
    {
        [Fact]
        public void MetaDataJsonMatches(){
            var metadataPointer = NativeCore.GetNativeMetadata((int)ProgrammingPlatform.dotnet, (int)ExportType.Json);
            var metadata = Marshal.PtrToStringUTF8 (metadataPointer);
            Assert.Equal("", metadata);
        }

        [Fact]
        public void MetaDataJsonIsLongerThanBufferSize(){
            var metadataPointer = NativeCore.GetNativeMetadata((int)ProgrammingPlatform.dotnet, (int)ExportType.UnsafeJson);
            var metadata = Marshal.PtrToStringUTF8 (metadataPointer);
        
        }
        
        [Fact]
        public void MetaDataExceptionWorks(){
            var metadataPointer = NativeCore.GetNativeMetadata(5000000, (int)ExportType.Json);
            var metadata = Marshal.PtrToStringUTF8 (metadataPointer);
        
        }
    }
}
