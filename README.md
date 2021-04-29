Requires the compiled libopenpgl.so.0.1.0 in the `Runtimes` folder.
Currently only set up for Linux and tested with Ubuntu 20.04

Building and testing:

```
dotnet test
```

Running the rendering experiment (in Release mode):

```
cd GuidedPathTracer
dotnet run -c Release
```