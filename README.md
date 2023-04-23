# NSW SD files builder - GUI Version

You should already know what's this for. Made with .NET Framework 4.8.

### How to build (without Visual Studio)

1. Download the [.NET Framework 4.8 Developer Pack](https://dotnet.microsoft.com/en-us/download/dotnet-framework/thank-you/net48-developer-pack-offline-installer) and install it.
2. Download the [Visual Studio Build Tools 2017 installer](https://download.visualstudio.microsoft.com/download/pr/653e10c9-d650-464b-a0b0-f211bb0c7c32/ce78a99572710c75aa8a209d771c54f98513c8f5cfe4bad9a661fb1a3298bf50/vs_BuildTools.exe) and run it. Don't select anything and just press the **Next** button, should be a 47 MB install.
3. Go to _Start_ → _Visual Studio 2017_ and open the _Developer Command Prompt for VS 2017_ shortcut.
4. In the opened command prompt window, `cd` to the repo's folder.
5. Run: `msbuild MakeNSWSD.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"`
6. Check the _bin\Release_ folder for the compiled exe.
