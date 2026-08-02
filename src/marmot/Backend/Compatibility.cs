namespace Marmot.Backend;

public static class Compatibility {

    public static bool DebugMode;
    public static bool IsCpuSkinned { get; private set; }

    internal static void Check(bool debugMode) {

        #if DEBUG
        DebugMode = true;
        #else
        DebugMode = debugMode;
        #endif

        IsCpuSkinned = false;
    }
}