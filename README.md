This code really only shows how one could implement a plugin system using `AssemblyLoadContext`.

Build the code using

```PS
cd src
dotnet build
```

Then run the sample application like so

```PS
cd SampleApplication\bin\Debug\netcoreapp3.1
.\SampleApplication.exe ..\..\..\..\SamplePlugin\bin\Debug\netcoreapp3.1\SamplePlugin.dll ..\..\..\..\SamplePlugin2\bin\Debug\netcoreapp3.1\SamplePlugin2.dll
```

This will load the two plugins as given by their assembly path. Each plugin references a different version of `Newtonsoft.JSON` (JSON.NET) and displays
which version it uses when the respective implementation class is created. You will see from the output that each one indeed has its distinct version.
The actuall dependencies (JSON.NET in this case) are looked up from the same directory as the plugin assembly is loaded from.

Then the sample will just print the names of the loaded plugins, and finally unload them all again.
