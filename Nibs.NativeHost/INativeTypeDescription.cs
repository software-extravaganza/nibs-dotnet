namespace Library
{
    public interface INativeTypeDescription
    {
        string Name { get; set; }
        string? Namespace { get; set; }
        string? AssemblyPath { get; set; }
        string FullName { get; }
    }
}