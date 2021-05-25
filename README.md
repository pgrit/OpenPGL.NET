Requires the compiled libopenpgl.so.0.2.0 in the `Runtimes` folder.
Currently only set up for Linux and Windows and tested with Ubuntu 20.04 and Windows 10

Building and testing:

```
dotnet test
```

Running the rendering experiment (in Release mode):

```
cd GuidedPathTracer
dotnet run -c Release
```