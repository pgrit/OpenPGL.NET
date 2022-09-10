# OpenPGL.NET

A C# wrapper for the [Intel® Open Path Guiding Library](https://github.com/OpenPathGuidingLibrary/openpgl), including sample code for a guided path tracer with [SeeSharp](https://github.com/pgrit/SeeSharp)

## Building

Precompiled binaries for openpgl and its dependencies can be downloaded by running the `make.ps1` (all platforms) or `make.sh` (Linux and OSX) scripts. If you want to supply your own binaries, check these scripts for details.

## Running the example

```
cd GuidedPathTracer
dotnet run -c Release
```

The results can be viewed by opening and running the [.NET interactive](https://github.com/dotnet/interactive) notebook [GuidedPathTracer/Results.dib](GuidedPathTracer/Results.dib), or by manually opening the .exr files in `GuidedPathTracer/Results`.

## Viewer

There is an experimental GUI tool in the `Viewer` folder. It can visualize the guiding distributions in a scene.

(It is a webapp with serverside Blazor that directly links the `GuidedPathTracer` project to run arbitrary code. Hardly tested at all, so expect bugs all over ;) )