# Download the prebuilt binaries for TBB, Embree, and OpenPGL from GitHub
if (-not(Test-Path -Path "prebuilt" -PathType Container))
{
    $renderLibVersion = "0.1.3"
    if ([environment]::OSVersion::IsWindows())
    {
        Invoke-WebRequest -Uri "https://github.com/pgrit/RenderLibs/releases/download/v$renderLibVersion/RenderLibs-v$renderLibVersion.zip" -OutFile "prebuilt.zip"
        Expand-Archive "prebuilt.zip" -DestinationPath ./prebuilt
        rm prebuilt.zip
    }
    else
    {
        wget -q "https://github.com/pgrit/RenderLibs/releases/download/v$renderLibVersion/RenderLibs-v$renderLibVersion.zip" -O "prebuilt.zip"
        Expand-Archive "prebuilt.zip" -DestinationPath ./prebuilt
        rm prebuilt.zip
    }
}

# Copy the shared libraries to the Runtimes folder for packaging in .NET
mkdir runtimes

mkdir runtimes/linux-x64
mkdir runtimes/linux-x64/native
cp prebuilt/linux/lib/libembree3.so.3 runtimes/linux-x64/native/
cp prebuilt/linux/lib/libtbb.so.12 runtimes/linux-x64/native/libtbb.so.12
cp prebuilt/linux/lib/libopenpgl.so.0.4.0 runtimes/linux-x64/native/

mkdir runtimes/win-x64
mkdir runtimes/win-x64/native
cp prebuilt/win/bin/embree3.dll runtimes/win-x64/native/
cp prebuilt/win/bin/tbb12.dll runtimes/win-x64/native/
cp prebuilt/win/bin/openpgl.dll runtimes/win-x64/native/

mkdir runtimes/osx-x64
mkdir runtimes/osx-x64/native
cp prebuilt/osx/lib/libembree3.3.dylib runtimes/osx-x64/native/
cp prebuilt/osx/lib/libtbb.12.dylib runtimes/osx-x64/native/libtbb.12.dylib
cp prebuilt/osx/lib/libopenpgl.0.4.0.dylib runtimes/linux-x64/native/

mkdir runtimes/osx-arm64
mkdir runtimes/osx-arm64/native
cp prebuilt/osx-arm64/lib/libembree3.3.dylib runtimes/osx-arm64/native/
cp prebuilt/osx/lib/libtbb.12.dylib runtimes/osx-arm64/native/libtbb.12.dylib
cp prebuilt/osx-arm64/lib/libopenpgl.0.4.0.dylib runtimes/linux-x64/native/

dotnet build
dotnet test