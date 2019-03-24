namespace Nibs.NativeHost
{
    public interface INativeSourceSettingLoadResult
    {
        bool HasError { get; }
        string? Error { get; }
        System.Exception? Exception { get; }
        INativeSourceSettings Settings { get; }
    }
}