# Try out examples #
**Notes:**
> ![dotnet try Enabled](https://img.shields.io/badge/Try_.NET-Enabled-501078.svg) <br />
> 1) These examples are all in C#. This file is intended to be used with the "**dotnet try**" tool. <br />
> Don't have the **try** tool setup? [Help is here](INSTALL-TRY.md).
> 2) The first run may take a while. This is due to the native project compliation.
> 3) If these examples don't work:
>     1) Try to hit the run button again (try a second time).
>     2) Try building the project first. In a terminal (at the solution directory), run:
>     ``` bash
>	    dotnet restore; dotnet build .\Nibs.Client.DotNet\
>     ```


Use **NativeBridge** as your static entry into the sample native functions
``` cs --region methods --source-file ../Nibs.Client.DotNet/Program.cs --project ../Nibs.Client.DotNet/Nibs.Client.DotNet.csproj
```