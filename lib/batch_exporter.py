import bpy
import json
import sys
import os

def check_node_inputs(mat):
    
    matInfo = {
        
        "name": mat.name,
        "hasAlbedo": False,
        "hasNormal": False,
        "hasRoughness": False,
        "hasMetallic": False,
        "hasEmission": False,
        "hasAo": False
    }

    if not mat.use_nodes or not mat.node_tree: return matInfo

    # Principled BSDF node
    bsdfNode = None
    for node in mat.node_tree.nodes:
        if node.type == 'BSDF_PRINCIPLED':
            bsdfNode = node
            break

    if not bsdfNode: return matInfo

    # Principled BSDF input sockets
    socketMappings = {
        
        "hasAlbedo"    : ["Base Color", "Color", "Diffuse"],
        "hasRoughness" : ["Roughness"],
        "hasMetallic"  : ["Metallic"],
        "hasEmission"  : ["Emission", "Emission Color"],
        "hasNormal"    : ["Normal"]
    }

    for key, socketNames in socketMappings.items():
        for socket_name in socketNames:
            if socket_name in bsdfNode.inputs and bsdfNode.inputs[socket_name].is_linked:
                
                link = bsdfNode.inputs[socket_name].links[0]
                fromNode = link.from_node
                
                if fromNode.type == 'NORMAL_MAP' and fromNode.inputs['Color'].is_linked:
                    fromNode = fromNode.inputs['Color'].links[0].from_node

                if fromNode.type == 'TEX_IMAGE' and fromNode.image:
                    matInfo[key] = True
                    break

    # Check Ambient Occlusion/TEX_IMAGE
    for node in mat.node_tree.nodes:
        if node.type == 'AMBIENT_OCCLUSION':
            matInfo["hasAo"] = True
            break

    return matInfo

def export_file(blend_path, m3d_output_path):
    
    absBlend = os.path.abspath(blend_path)
    absM3D = os.path.abspath(m3d_output_path)
    
    # Import the working directory
    os.chdir(os.path.dirname(absBlend))
    bpy.ops.wm.open_mainfile(filepath=absBlend)
    jsonOutputPath = f"{os.path.splitext(absM3D)[0]}.m3d.json"

    try: bpy.ops.preferences.addon_enable(module="io_scene_m3d")
    except Exception: pass

    # Export M3D
    try: bpy.ops.export_scene.m3d(filepath=absM3D, use_inline=True, use_gridcompress=False)
    except Exception: pass

    materialsData = []
    
    for mat in bpy.data.materials: materialsData.append(check_node_inputs(mat))

    with open(jsonOutputPath, "w", encoding="utf-8") as f:
        json.dump(materialsData, f, indent=4)
        
    print(f"Tamamlandi: {blend_path} -> {m3d_output_path}")

if __name__ == "__main__":
    
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    
    if args and os.path.exists(args[0]):
        
        with open(args[0], "r", encoding="utf-8") as f:
            targets = json.load(f)
            
        for blendFile, m3dFile in targets.items():
            if os.path.exists(blendFile):
                export_file(blendFile, m3dFile)