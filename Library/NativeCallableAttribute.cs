namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class NativeCallableAttribute : Attribute
    {
        public string EntryPoint = string.Empty;
        public CallingConvention CallingConvention;
        public NativeCallableAttribute() { }
    }
}