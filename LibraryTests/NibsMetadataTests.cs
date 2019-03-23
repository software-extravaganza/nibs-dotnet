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
            var metadataPointer = NativeCore.GetNativeMetadata((int)ProgrammingPlatform.dotnet);
            var metadata = Marshal.PtrToStringUTF8 (metadataPointer);
            Assert.Equal("", metadata);
        }

        [Fact]
        public void MetaDataJsonIsLongerThanBufferSize(){
            var metadataPointer = NativeCore.GetNativeMetadata((int)ProgrammingPlatform.dotnet);
            var metadata = Marshal.PtrToStringUTF8 (metadataPointer, 8200);
        
        }

        
        [Fact]
        public void MetaDataExceptionWorks(){
            var metadataPointer = NativeCore.GetNativeMetadata(5000000);
            var metadata = Marshal.PtrToStringUTF8 (metadataPointer);
        
        }
    }
}
