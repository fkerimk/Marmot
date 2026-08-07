import os, sys, json, glob, importlib, bpy

def TraceForTexture(socket, visited=None):
    
    visited = visited or set()
    
    if not getattr(socket, 'is_linked', False): return False
    
    for link in socket.links:
        
        node = link.from_node
        
        if node in visited: continue
        visited.add(node)
        
        if node.type == 'TEX_IMAGE' and getattr(node, 'image', None):
            return True
        
        if any(TraceForTexture(i, visited)
               for i in node.inputs): return True
        
    return False

def CheckNodeInputs(mat):
    
    info = {k: False for k in ("hasAlbedo", "hasNormal", "hasRoughness", "hasMetallic", "hasEmission", "hasAo")}
    info["name"] = mat.name
    
    if not mat.node_tree: return info

    bsdf = next((n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
    
    if bsdf:
        
        maps = {
            
            "hasAlbedo"   : ["Base Color", "Color", "Diffuse"],
            "hasRoughness": ["Roughness"],
            "hasMetallic" : ["Metallic"],
            "hasEmission" : ["Emission", "Emission Color"],
            "hasNormal"   : ["Normal"]
        }
        
        for k, sockets in maps.items():
            info[k] = any(s in bsdf.inputs and TraceForTexture(bsdf.inputs[s]) for s in sockets)

    info["hasAo"] = any(n.type == 'AMBIENT_OCCLUSION' for n in mat.node_tree.nodes)
    
    return info

def ExportFile(blendPath, m3dPath, libPath):
    
    blendPath, m3dPath = os.path.abspath(blendPath), os.path.abspath(m3dPath)
    
    os.chdir(os.path.dirname(blendPath))
    bpy.ops.wm.open_mainfile(filepath=blendPath)
    
    anims = [f for f in glob.glob(f"{blendPath}@*") if f.lower().endswith(('.fbx'))]
    
    if anims:
        sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
        import RetargetPipeline; importlib.reload(RetargetPipeline)
        RetargetPipeline.process_animations(anims)

    if libPath and libPath not in sys.path: sys.path.insert(0, libPath)
    
    import M3DExporter; importlib.reload(M3DExporter)
    
    try: M3DExporter.Reg()
    except: pass

    bpy.ops.export_scene.m3d(filepath=m3dPath)

    with open(f"{os.path.splitext(m3dPath)[0]}.m3d.json", "w") as f:
        json.dump([CheckNodeInputs(m) for m in bpy.data.materials], f, indent=4)

if __name__ == "__main__":
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if args and os.path.exists(args[0]):
        lib_path = args[1] if len(args) > 1 else ""
        for blend, m3d in json.load(open(args[0])).items():
            if os.path.exists(blend): ExportFile(blend, m3d, lib_path)