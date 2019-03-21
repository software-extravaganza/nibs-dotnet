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
    }
}
