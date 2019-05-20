 # nibs-dotnet #
Dot net core implementation of NIBS (Native Interfacing Bridge Standard)

----
## Building the .NET Native Library ##
To build the library, make sure to navigate to the 'Library' directory:
``` bash
cd ./Library
```
Then run the build command, replacing the `-r` flag with the runtime indetifier (see also: [Runtime identifier catalog](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog)):
``` bash
dotnet publish /p:NativeLib=Shared -r <runtime-indetifier> -c release
```

> ### Windows Example ###
> ``` bash
> dotnet publish /p:NativeLib=Shared -r win-x64 -c release
> ```
>
> ### Linux Example ###
> ``` bash
> dotnet publish /p:NativeLib=Shared -r linux-x64 -c release
> ``` 
>
> ### macOS Example ###
> ``` bash
> dotnet publish /p:NativeLib=Shared -r osx-x64 -c release
> ```

----
## Running the Ruby Tester ##
To run the tester, navigate to the 'Ruby Tester' directory:
``` bash
cd './Ruby Tester'
```
Then run the tester:
``` bash
ruby test.rb
```

## Try it out ![dotnet try Enabled](https://img.shields.io/badge/Try_.NET-Enabled-501078.svg) ##
[Run live examples here](Documentation/TRY.md)