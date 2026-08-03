using static Raylib_cs.Raylib;

using Marmot.Backend.Resources;
using Marmot.Backend.Resources.Types;

namespace Marmot;

public class Shader : Resource {

    public override bool RawImportPath => true;

    internal Raylib_cs.Shader? RlShader;

    public override void Import(string path) {

        var vsPath = ResMan.FindResourcePath(path + ".vs", true);
        var fsPath = ResMan.FindResourcePath(path + ".fs", true);

        if (string.IsNullOrEmpty(vsPath) && string.IsNullOrEmpty(fsPath))
            throw new FileNotFoundException($"No shaders found as {path}");

        RlShader = LoadShader(vsPath, fsPath);
    }

    public override void Unload() {

        if (RlShader == null) return;
        UnloadShader(RlShader.Value);
    }
}