using System.Reflection;

namespace OpenPGL.NET;

internal static partial class OpenPGL {
    const string LibName = "openpgl";

    static OpenPGL() {
        NativeLibrary.SetDllImportResolver(typeof(OpenPGL).Assembly, ImportResolver);
    }

    private static IntPtr ImportResolver(string libraryName, Assembly assembly,
                                         DllImportSearchPath? dllImportSearchPath) {
        string mappedName = libraryName;

        // Linking on OS X only works correctly if the file contains the version number.
        if (libraryName == LibName && RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            mappedName = $"lib{LibName}.so.0.3.1";
        }

        return NativeLibrary.Load(mappedName, assembly, dllImportSearchPath);
    }
}
