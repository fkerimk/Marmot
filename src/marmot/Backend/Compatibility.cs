namespace Marmot.Backend;

public static class Compatibility {

    public static bool NativeRl;

    internal static void Check() {

        NativeRl = !File.Exists(Path.Join(AppContext.BaseDirectory + "libraylib.so"));
        if (NativeRl) Log.Warning("Falling back to native raylib");
    }
}