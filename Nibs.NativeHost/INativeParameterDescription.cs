namespace Library
{
    public interface INativeParameterDescription
    {
        string Name { get; set; }
        NativeTypeDescription? Type { get; set; }
        NativeTypeDescription? IntendedType { get; set; }
    }
}