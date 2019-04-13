namespace Nibs.NativeHost
{
	public interface ILoadedAssembliesResult
    {
        System.Collections.Generic.IList<NativeAssemblyDescription> AssembliesLoaded { get; }
        System.Collections.Generic.IList<string> AssembliesNotFound { get; }
        bool HasError { get; }
        string Error { get; }
    }
}