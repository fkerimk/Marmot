# ##### BEGIN MIT LICENSE BLOCK #####
#
# blender/io_scene_m3d.py
#
# Copyright (C) 2019 - 2022 bzt (bztsrc@gitlab)
#
# Permission is hereby granted, free of charge, to any person
# obtaining a copy of this software and associated documentation
# files (the "Software"), to deal in the Software without
# restriction, including without limitation the rights to use, copy,
# modify, merge, publish, distribute, sublicense, and/or sell copies
# of the Software, and to permit persons to whom the Software is
# furnished to do so, subject to the following conditions:
#
# The above copyright notice and this permission notice shall be
# included in all copies or substantial portions of the Software.
#
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
# EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
# MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
# NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
# HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
# WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
# OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
# DEALINGS IN THE SOFTWARE.
#
# @brief Blender 2.80 Model 3D Exporter (and one day Importer too)
# https://gitlab.com/bztsrc/model3d
#
# ##### END MIT LICENSE BLOCK #####

import os, gzip, zlib, bpy, bmesh
from operator import itemgetter
from struct import pack
from mathutils import Matrix
from bpy_extras import node_shader_utils
from bpy.props import (BoolProperty, FloatProperty, StringProperty, IntProperty, EnumProperty)
from bpy_extras.io_utils import (ExportHelper, ImportHelper, axis_conversion)

matPropertyMap = {
    
    0   : [ "color"  , "base_color"            , "Kd"     ],
    1   : [ "gscale" , "metallic"              , "Ka"     ],
    2   : [ "gscale" , "specular"              , "Ks"     ],
    3   : [ "//float", "specular_tint"         , "Ns"     ],
    4   : [ "//color", "emissive"              , "Ke"     ],
    5   : [ "gscale" , "transmission"          , "Tf"     ],
    6   : [ "float"  , "normalmap_strength"    , "Km"     ],
    7   : [ "float"  , "alpha"                 , "d"      ],
    8   : [ "//byte" , "illumination"          , "il"     ],
    64  : [ "float"  , "roughness"             , "Pr"     ],
    65  : [ "float"  , "metallic"              , "Pm"     ],
    66  : [ "//float", "sheen"                 , "Ps"     ],
    67  : [ "float"  , "ior"                   , "Ni"     ],
    128 : [ "map"    , "base_color_texture"    , "map_Kd" ],
    130 : [ "//map"  , "specular_texture"      , "map_Ks" ],
    133 : [ "map"    , "transmission_texture"  , "map_Tf" ],
    134 : [ "map"    , "normalmap_texture"     , "map_Km" ],
    135 : [ "map"    , "alpha_texture"         , "map_D"  ],
    192 : [ "map"    , "roughness_texture"     , "map_Pr" ],
    193 : [ "map"    , "metallic_texture"      , "map_Pm" ],
    195 : [ "map"    , "ior_texture"           , "map_Ni" ],
}

def WriteM3D(context, filepath, report, *, use_name='', use_license='', use_author='', use_comment='', use_scale=1.0, use_selection=True, use_mesh_modifiers=True, use_normals=False, use_uvs=True, use_colors=True, use_materials=True, use_skeleton=True, use_animation=True, use_markers=False, use_fps=25, use_quality='-1', use_strmcompress=True, use_relbones=True, global_matrix=None, check_existing=True):
    def safestr(name, morelines=0):
        if name is None: return ''
        elif morelines == 3: return name.replace('\r', '').strip()
        elif morelines == 2: return name.replace('\r', '').replace('\n', ' ').strip()
        elif morelines == 1: return name.replace('\r', '').replace('\n', '\r\n').strip()
        else: return name.replace(' ', '_').replace('/', '_').replace('\\', '_').replace('\r', '').replace('\n', ' ').strip()

    def uniquedict(l, e):
        h = hash(str(e))
        try: return l[h][0]
        except KeyError:
            i = len(l)
            l[h] = [i, e]
        return i

    def dict2list(l):
        r = []
        for i, v in l.items(): r.insert(v[0], v[1])
        return r

    def idxsize(cnt):
        if cnt == 0: return 3
        elif cnt < 254: return 0
        elif cnt < 65534: return 1
        return 2

    def addidx(fmt, idx):
        if fmt == 0:
            if idx < 0: idx = 256 + idx
            return pack("<B", idx)
        elif fmt == 1:
            if idx < 0: idx = 65536 + idx
            return pack("<H", idx)
        elif fmt == 2:
            if idx < 0: idx = 4294967296 + idx
            return pack("<I", idx)
        return b''

    def vert(x,y,z,w,c,s):
        if x == -0.0: x = 0.0
        if y == -0.0: y = 0.0
        if z == -0.0: z = 0.0
        if w == -0.0: w = 0.0
        return [x,y,z,w,c,s]

    def matnorm(a):
        p, q, s = a.decompose()
        q.normalize()
        return Matrix.Translation(p) @ q.to_matrix().to_4x4()

    def gettexture(fn):
        data = b''
        if fn[0:2] == "//": fn = fn[2:]
        imgpath = repr(os.path.basename(fn))[1:-1]
        imgpath = os.path.splitext(imgpath)[0]
        if imgpath != "":
            try: data = open(os.path.join(os.path.dirname(filepath), fn), 'rb').read()
            except:
                try: data = open(os.path.join(os.path.dirname(filepath), os.path.basename(fn)), 'rb').read()
                except:
                    try: data = open(os.path.join(os.path.dirname(filepath), imgpath + ".png"), 'rb').read()
                    except:
                        try: data = open(imgpath + ".png", 'rb').read()
                        except:
                            try: data = open(fn, 'rb').read()
                            except: data = b''
            if len(data) < 8 or data[0:4] != b'\x89PNG':
                report({"WARNING"}, "Texture file '" + fn + "' not found or not a valid PNG. Cannot be inlined.")
                data = b''
        return [ imgpath, data ]

    def bonestr(strs, bones, parent, level):
        ret = ""
        for i,b in enumerate(bones):
            if b[0] == parent:
                ret += "/"*level + str(b[2]) + " " + str(b[3]) + " " + strs[b[1]] + "\r\n"
                ret += bonestr(strs, bones, i, level+1)
        return ret

    if True:
        if global_matrix is None:
            global_matrix = axis_conversion(from_forward='-Y', from_up='Z',to_forward='Z', to_up='Y').to_4x4()
        if use_animation: use_skeleton = True
        if use_fps < 1 or use_fps > 120: use_fps = 25

        depsgraph = context.evaluated_depsgraph_get()
        scene = context.scene
        if use_selection: objects = context.selected_objects
        else: objects = context.scene.objects

        use_quality = int(use_quality)
        if use_quality < 0 or use_quality > 3:
            n = 0
            for i, ob_main in enumerate(objects):
                if ob_main.parent and ob_main.parent.instance_type in {'VERTS', 'FACES'}: continue
                try:
                    me = ob_main.original.to_mesh()
                    n += len(me.polygons)
                except: continue
            if n < 1024: use_quality = 0
            else: use_quality = 1
        
        if use_quality < 2: use_quality = 2
        
        if use_quality == 3: digits = 15
        elif use_quality == 2: digits = 7
        else: digits = 4

        cmap, strs, verts, tmaps = {}, {}, {}, {}
        faces, shapes, labels, materials = [], [], [], []
        bones, skins, actions, inlined, extras = {}, {}, [], {}, []
        
        refmats = {}
        nb_m = 0
        fi_m = 0

        oldaction = None
        oldframe = context.scene.frame_current
        oldpose = {}
        oldnlamute = {}
        
        for i,ob_main in enumerate(objects):
            if ob_main.type == "ARMATURE":
                oldpose[i] = ob_main.data.pose_position
                ob_main.data.pose_position = "REST"
                ob_main.data.update_tag()
                if oldaction == None and ob_main.animation_data and ob_main.animation_data.action:
                    oldaction = ob_main.animation_data.action
                if ob_main.animation_data:
                    for track in ob_main.animation_data.nla_tracks:
                        oldnlamute[(ob_main.name, track.name)] = track.mute
                        track.mute = True
        context.scene.frame_set(0)

        if use_skeleton:
            idx = 0
            for i,ob_main in enumerate(objects):
                if ob_main.type != "ARMATURE": continue
                for b in ob_main.data.bones:
                    m = matnorm(global_matrix @ ob_main.matrix_world @ b.matrix_local)
                    a = -1
                    if b.parent:
                        for j,p in enumerate(ob_main.data.bones):
                            if p == b.parent:
                                a = j
                                break
                        if use_relbones == True:
                            p = matnorm(global_matrix @ ob_main.matrix_world @ b.parent.matrix_local)
                            m = p.inverted() @ m
                    p = m.to_translation()
                    q = m.to_quaternion()
                    q.normalize()
                    n = safestr(b.name)
                    try:
                        ni = strs[hash(str(n))][0]
                        name = "'" + b.name + "'"
                        if b.name != n: name += " (" + n + ")"
                        report({"WARNING"}, "Bone name " + name + " not unique.")
                        use_skeleton = False
                        use_animation = False
                        bones = {}
                        break
                    except: pass
                    
                    bones[b.name] = [idx, [a, uniquedict(strs, n),
                        uniquedict(verts, vert(
                            round(p[0], digits),
                            round(p[1], digits),
                            round(p[2], digits), 1.0, 0, -1)),
                        uniquedict(verts, vert(
                            round(q.x, digits),
                            round(q.y, digits),
                            round(q.z, digits),
                            round(q.w, digits), 0, -2))]]
                    idx = idx + 1
            if len(bones) < 1 and use_animation:
                use_animation = False

        for i, ob_main in enumerate(objects):
            if ob_main.parent and ob_main.parent.instance_type in {'VERTS', 'FACES'}: continue
            obs = [(ob_main, ob_main.matrix_world)]
            if ob_main.is_instancer:
                obs += [(dup.instance_object.original, dup.matrix_world.copy())
                        for dup in depsgraph.object_instances
                        if dup.parent and dup.parent.original == ob_main]
            for ob, ob_mat in obs:
                try:
                    o = ob.evaluated_get(depsgraph) if use_mesh_modifiers else ob.original
                    me = o.to_mesh()
                except: me = None
                
                if me is None or len(me.polygons) < 1: continue
                if use_name is None or use_name == '': use_name = ob.name

                r = False
                for poly in me.polygons:
                    if len(poly.loop_indices) != 3:
                        r = True
                        break
                if r == True:
                    bm = bmesh.new()
                    bm.from_mesh(me)
                    bmesh.ops.triangulate(bm, faces=bm.faces[:])
                    bm.to_mesh(me)
                    bm.free()

                me.transform(global_matrix @ ob_mat)
                if ob_mat.determinant() < 0.0: me.flip_normals()
                if use_normals:
                    try: me.calc_normals_split()
                    except: pass

                if use_skeleton and len(ob.vertex_groups) > 0: vg = ob.vertex_groups
                else:
                    vg = []
                    if use_skeleton == True and use_animation == True:
                        report({"WARNING"}, "Mesh '" + me.name + "' in object '" + ob.name + "' has no vertex groups, no skeletal animation possible!")

                if use_uvs and len(me.uv_layers) > 0: uv_layer = me.uv_layers.active.data[:]
                else: uv_layer = []

                if use_colors and len(me.vertex_colors) > 0:
                    if me.vertex_colors.active_index >= 0 and me.vertex_colors.active_index < len(me.vertex_colors) and len(me.vertex_colors[me.vertex_colors.active_index].data) > 0:
                        vcol = me.vertex_colors[me.vertex_colors.active_index].data
                    else:
                        report({"WARNING"}, "Vertex color in mesh '" + me.name + "' in object '" + ob.name + "' has invalid out-of-bounds index.")
                        if len(me.vertex_colors[0].data) > 0: vcol = me.vertex_colors[0].data
                        else:
                            report({"WARNING"}, "Vertex color in mesh '" + me.name + "' in object '" + ob.name + "' unable to fallback to vertex_colors[0], no data.")
                            vcol = []
                else: vcol = []

                matnames = []
                if use_materials:
                    for m in me.materials[:]:
                        if m and m.name: matnames.append(uniquedict(strs, safestr(m.name)))
                        else: matnames.append(-1)

                badref = {}
                for pi,poly in enumerate(me.polygons):
                    face = [ -1, [-1,-1,-1], [-1,-1,-1], [-1,-1,-1], -1 ]
                    if len(matnames) > 0:
                        if poly.material_index < len(matnames): i = poly.material_index
                        else:
                            i = 0
                            try: dummy = badref[poly.material_index]
                            except:
                                badref[poly.material_index] = 1
                                report({"WARNING"}, "Polygon face in mesh '" + me.name + "' referencing a non-existent material.")
                        if i >= 0:
                            face[0] = matnames[i]
                            uniquedict(refmats, me.materials[i])
                    for i, li in enumerate(poly.loop_indices):
                        if len(vcol) > 0: c = uniquedict(cmap, [vcol[li].color[0], vcol[li].color[1], vcol[li].color[2], vcol[li].color[3]])
                        else: c = 0
                        v = me.vertices[poly.vertices[i]]
                        if use_skeleton and len(vg) > 0 and len(v.groups) > 0:
                            wf = 0.0
                            for g in v.groups: wf += g.weight
                            if wf > 0.0:
                                skin = []
                                w = wi = wm = 0
                                for g in v.groups:
                                    try:
                                        s = round(g.weight / wf * 255.0)
                                        if s > wm:
                                            wm = s
                                            si = len(skin)
                                        if s < 1: s = 1
                                        if s > 255: s = 255
                                        skin.append([bones[vg[g.group].name][0], s])
                                        w = w + s
                                    except:
                                        report({"WARNING"}, "Vertex group name '" + vg[g.group].name + "' does not match any bone.")
                                        use_skeleton = False
                                        vg = []
                                        s = -1
                                        break
                                try:
                                    if w != 255: skin[si][1] += 255 - w
                                except: pass
                                s = uniquedict(skins, skin)
                                if len(skin) > nb_m: nb_m = len(skin)
                            else: s = -1
                        else: s = -1
                        face[1][i] = uniquedict(verts, vert(
                            round(v.co.x, digits),
                            round(v.co.y, digits),
                            round(v.co.z, digits), 1.0, c, s))
                        if use_normals:
                            try: no = v.normal.copy()
                            except: no = poly.loops[i].normal.copy()
                            no.normalize()
                            face[3][i] = uniquedict(verts, vert(
                                round(no.x, digits),
                                round(no.y, digits),
                                round(no.z, digits), 1.0, 0, -1))
                            del no
                        if use_uvs and len(uv_layer) > 0:
                            face[2][i] = uniquedict(tmaps, list(uv_layer[li].uv[:]))
                    faces.append(face)
                del me

        if use_materials:
            matopa = {}
            matopa[-1] = -1
            for i,v in refmats.items():
                mi = v[0]
                mat = v[1]
                if mat is not None:
                    props = {}
                    d = 1.0
                    if mat.node_tree:
                        for n in mat.node_tree.nodes:
                            if n.type == 'TEX_IMAGE' and n.image and n.image.filepath and n.image.filepath != "" and n.image.filepath != "//":
                                imgpath, data = gettexture(n.image.filepath)
                                if imgpath != "":
                                    s = uniquedict(strs, imgpath)
                                    if len(data) > 8:
                                        uniquedict(inlined, [s, data])
                                    props[128] = [128, s]
                                break
                    mat_wrap = node_shader_utils.PrincipledBSDFWrapper(mat)
                    if mat_wrap:
                        for key, mat_wrap_key in matPropertyMap.items():
                            if key == 0:
                                if mat_wrap.alpha != 0.0 and mat_wrap.alpha != 1.0: d = mat_wrap.alpha
                                elif mat_wrap.base_color and len(mat_wrap.base_color) > 3: d = mat_wrap.base_color[3]
                                else: d = 0.0
                                if d != 0.0: props[0] = [0, uniquedict(cmap, [mat_wrap.base_color[0], mat_wrap.base_color[1], mat_wrap.base_color[2], d])]
                            elif key == 8:
                                il = 0
                                if mat_wrap.specular == 0: il = 1
                                elif mat_wrap.metallic != 0.0:
                                    if d != 1.0: il = 6
                                    else: il = 3
                                elif d != 1.0: il = 9
                                else: il = 2
                                if il != 0: props[8] = [8, il]
                            elif mat_wrap_key[0][0:2] == "//": continue
                            try: val = getattr(mat_wrap, mat_wrap_key[1], None)
                            except: continue
                            if val is None: continue
                            if key >= 128:
                                if val.image is None or val.image.filepath is None or val.image.filepath == "" or val.image.filepath == "//": continue
                                imgpath, data = gettexture(val.image.filepath)
                                if imgpath == "": continue
                                s = uniquedict(strs, imgpath)
                                props[key] = [key, s]
                                if len(data) > 8: uniquedict(inlined, [s, data])
                            elif mat_wrap_key[0] == "gscale" and val != 0.0: props[key] = [key, uniquedict(cmap, [val, val, val, 1.0])]
                            elif mat_wrap_key[0] == "color" and len(val) == 3: props[key] = [key, uniquedict(cmap, [val[0], val[1], val[2], 1.0])]
                            elif mat_wrap_key[0] == "color" and len(val) == 4: props[key] = [key, uniquedict(cmap, val)]
                            elif mat_wrap_key[0] == "float" and val != 0.0: props[key] = [key, val]
                            elif (mat_wrap_key[0] == "byte" or mat_wrap_key[0] == "int") and val != 0: props[key] = [key, val]
                    else:
                        report({"WARNING"}, "Material '" + mat.name + "' does not use PrincipledBSDF surface, not parsing.")
                    if len(props) > 0:
                        ni = uniquedict(strs, safestr(mat.name))
                        matopa[ni] = 255 - int(255.0 * d)
                        materials.append([ni, props])
            for i,v in enumerate(faces):
                try: faces[i][4] = matopa[faces[i][0]]
                except: faces[i][4] = 255
            faces.sort(key=itemgetter(4,0))
        else:
            faces.sort(key=itemgetter(0))

        if use_animation:
            if use_skeleton and len(bones) > 0:
                mpf = 1000.0/use_fps
                acts = []
                nf = 0
                if use_markers == True:
                    if len(scene.timeline_markers) > 0:
                        tlm = sorted(scene.timeline_markers, key=lambda tl: tl.frame)
                        for i,t in enumerate(tlm):
                            if i + 1 >= len(tlm): et = scene.frame_end
                            else: et = tlm[i+1].frame - 1
                            if et > t.frame:
                                acts.append([safestr(t.name), -1, t.frame, et])
                                nf = nf + et - t.frame
                        del tlm
                else:
                    for i,a in enumerate(bpy.data.actions):
                        st = et = 0
                        try:
                            st = int(a.curve_frame_range[0])
                            et = int(a.curve_frame_range[1])
                        except:
                            st = int(a.frame_range[0])
                            et = int(a.frame_range[1])
                        if et > 0: acts.append([safestr(a.name), i, st, et])
                        nf += et - st
                if nf == 0:
                    acts.append(["Anim", -1, scene.frame_start, scene.frame_end])
                    nf = scene.frame_end - scene.frame_start

                for a in acts:
                    scene.frame_set(0, subframe=0.0)
                    for i,ob_main in enumerate(objects):
                        if ob_main.type != "ARMATURE": continue
                        if a[1] != -1:
                            _act = bpy.data.actions[a[1]]
                            ob_main.animation_data.action = _act
                            try:
                                if _act.slots: ob_main.animation_data.action_slot = _act.slots[0]
                            except AttributeError: pass
                        ob_main.data.pose_position = "POSE"
                        ob_main.data.update_tag()
                    
                    lf = 0
                    frames = []
                    lastpose = {}
                    for n,b in bones.items(): lastpose[n] = [b[1][2], b[1][3]]
                    
                    for frame in range(a[2], a[3] + 1):
                        scene.frame_set(frame, subframe=0.0)
                        changed = []
                        for i,ob_main in enumerate(objects):
                            if ob_main.type != "ARMATURE": continue
                            for i, b in enumerate(ob_main.pose.bones):
                                try: idx = bones[b.name][0]
                                except:
                                    report({"WARNING"}, "Animated bone name '" + b.name + "' does not match any bind-pose bone???")
                                    break
                                m = matnorm(global_matrix @ ob_main.matrix_world @ b.matrix)
                                if use_relbones == True and b.parent:
                                    p = matnorm(global_matrix @ ob_main.matrix_world @ b.parent.matrix)
                                    m = p.inverted() @ m
                                p = m.to_translation()
                                q = m.to_quaternion()
                                q.normalize()
                                pos = uniquedict(verts, vert(
                                        round(p[0], digits),
                                        round(p[1], digits),
                                        round(p[2], digits), 1.0, 0, -1))
                                ori = uniquedict(verts, vert(
                                        round(q.x, digits),
                                        round(q.y, digits),
                                        round(q.z, digits),
                                        round(q.w, digits), 0, -2))
                                if lastpose[b.name][0] != pos or lastpose[b.name][1] != ori:
                                    changed.append([idx, pos, ori])
                                    lastpose[b.name][0] = pos
                                    lastpose[b.name][1] = ori
                        if len(changed) > 0:
                            if len(frames) < 1: a[2] = frame
                            frames.append([int((frame-a[2]) * mpf), changed])
                            lf = frame
                            if len(changed) > fi_m: fi_m = len(changed)
                    if len(frames) > 0:
                        actions.append([uniquedict(strs, safestr(a[0])), int((lf-a[2]+1) * mpf), frames])
            else:
                report({"WARNING"}, "Trying to export animations without armature and skin")

        for i,ob_main in enumerate(objects):
            if ob_main.type == "ARMATURE":
                if oldaction != None and ob_main.animation_data:
                    try:
                        ob_main.animation_data.action = oldaction
                        try:
                            if oldaction.slots: ob_main.animation_data.action_slot = oldaction.slots[0]
                        except AttributeError: pass
                    except: continue
                ob_main.data.pose_position = oldpose[i]
                ob_main.data.update_tag()
                if ob_main.animation_data:
                    for track in ob_main.animation_data.nla_tracks:
                        key = (ob_main.name, track.name)
                        if key in oldnlamute: track.mute = oldnlamute[key]
        context.scene.frame_set(oldframe)

        cmap = dict2list(cmap)
        strs = dict2list(strs)
        verts = dict2list(verts)
        tmaps = dict2list(tmaps)
        bones = dict2list(bones)
        skins = dict2list(skins)
        inlined = dict2list(inlined)

        if use_scale <= 0.0: use_scale = 1.0

        if use_author is None or use_author == "": use_author = os.getenv("LOGNAME", "")

        stridx = [0] * (len(strs))
        st = bytes(safestr(use_name, 2), 'utf-8') + pack("<b", 0)
        st = st + bytes(safestr(use_license, 2), 'utf-8') + pack("<b", 0)
        st = st + bytes(safestr(use_author, 2), 'utf-8') + pack("<b", 0)
        st = st + bytes(safestr(use_comment, 1), 'utf-8') + pack("<b", 0)
        o = len(st)
        for i, s in enumerate(strs):
            s = bytes(s, 'utf-8') + pack("<b", 0)
            st = st + s
            stridx[i] = o
            o = o + len(s)

        ci_s = idxsize(len(cmap))
        ti_s = idxsize(len(tmaps))
        vi_s = idxsize(len(verts))
        si_s = idxsize(o)
        bi_s = idxsize(len(bones))
        sk_s = idxsize(len(skins))
        hi_s = idxsize(len(shapes))
        fi_s = idxsize(len(faces))
        if nb_m < 2: nb_s = 0
        elif nb_m == 2: nb_s = 1
        elif nb_m <= 4: nb_s = 2
        else: nb_s = 3
        fc_s = idxsize(fi_m)
        flags = (use_quality << 0) | (vi_s << 2) | (si_s << 4) | (ci_s << 6) | (ti_s << 8) | (bi_s << 10) | (nb_s << 12)
        flags |= (sk_s << 14) | (fc_s << 16) | (hi_s << 18) | (fi_s << 20)
        buf = pack("<f", use_scale) + pack("<I", flags) + st
        buf = b'HEAD' + pack("<I",len(buf) + 8) + buf

        if len(cmap) > 0 and ci_s < 4:
            buf = buf + b'CMAP' + pack("<I", len(cmap) * 4 + 8)
            for col in cmap:
                for i in range(0, 4): buf = buf + pack("<B", int(col[i] * 255))

        if len(tmaps) > 0:
            buf = buf + b'TMAP' + pack("<I", len(tmaps) * 2 * (1 << use_quality) + 8)
            r = True
            for t in tmaps:
                if t[0] < 0.0 or t[0] > 1.0 or t[1] < 0.0 or t[1] > 1.0:
                    r = False
                    if t[0] > 1.0: t[0] = 1.0
                    if t[0] < 0.0: t[0] = 0.0
                    if t[1] > 1.0: t[1] = 1.0
                    if t[1] < 0.0: t[1] = 0.0
                if use_quality == 0: buf = buf + pack("<BB", int(t[0] * 255), int(t[1] * 255))
                elif use_quality == 1: buf = buf + pack("<HH", int(t[0] * 65535), int(t[1] * 65535))
                elif use_quality == 3: buf = buf + pack("<dd", t[0], t[1])
                else: buf = buf + pack("<ff", t[0], t[1])

        if len(verts) > 0:
            o = b''
            for v in verts:
                for i in range(0, 4):
                    if use_quality == 0: o = o + pack("<b", int(v[i] * 127))
                    elif use_quality == 1: o = o + pack("<h", int(v[i] * 32767))
                    elif use_quality == 3: o = o + pack("<d", v[i])
                    else: o = o + pack("<f", v[i])
                if ci_s < 4: o = o + addidx(ci_s, v[4])
                else: o = o + pack("<I", cmap[v[4]])
                o = o + addidx(sk_s, v[5])
            buf = buf + b'VRTS' + pack("<I", len(o) + 8) + o

        if len(bones) > 0 or len(skins) > 0:
            o = addidx(bi_s, len(bones)) + addidx(sk_s, len(skins))
            for b in bones:
                o = o + addidx(bi_s, b[0]) + addidx(si_s, stridx[b[1]]) + addidx(vi_s, b[2]) + addidx(vi_s, b[3])
            for s in skins:
                if nb_s > 0:
                    for i in range(0, 1 << nb_s):
                        if i >= len(s): o = o + pack("<B", 0)
                        else: o = o + pack("<B", s[i][1])
                for i in range(0, min(len(s), 1 << nb_s)):
                    if s[i][1] != 0: o = o + addidx(bi_s, s[i][0])
            buf = buf + b'BONE' + pack("<I", len(o) + 8) + o

        if len(materials) > 0:
            for m in materials:
                o = addidx(si_s, stridx[m[0]])
                for pi,p in m[1].items():
                    o = o + pack("<B", p[0])
                    t = matPropertyMap[p[0]]
                    if t[0] == "color" or t[0] == "gscale":
                        if ci_s < 4: o = o + addidx(ci_s, p[1])
                        else: o = o + pack("<I", cmap[p[1]])
                    elif t[0] == "byte" or t[0] == "//byte": o = o + pack("<B", p[1])
                    elif p[0] >= 128: o = o + addidx(si_s, stridx[p[1]])
                    else: o = o + pack("<f", p[1])
                buf = buf + b'MTRL' + pack("<I", len(o) + 8) + o

        if len(faces) > 0:
            l = -1
            o = b''
            for f in faces:
                if l != f[0]:
                    l = f[0]
                    o = o + pack("<b", 0) + addidx(si_s, stridx[l])
                o = o + pack("<b", (len(f[1]) << 4) | (use_uvs) | (use_normals << 1))
                for i,v in enumerate(f[1]):
                    o = o + addidx(vi_s, v)
                    if use_uvs: o = o + addidx(ti_s, f[2][i])
                    if use_normals: o = o + addidx(vi_s, f[3][i])
            buf = buf + b'MESH' + pack("<I", len(o) + 8) + o

        if len(shapes) > 0:
            l = -1
            o = b''
            for f in shapes: o = o + b''
            buf = buf + b'SHPE' + pack("<I", len(o) + 8) + o

        if len(labels) > 0:
            l = -1
            o = b''
            for f in labels: o = o + b''
            buf = buf + b'LBLS' + pack("<I", len(o) + 8) + o

        if len(actions) > 0:
            for a in actions:
                if len(a[2]) < 1: continue
                o = addidx(si_s, stridx[a[0]]) + pack("<H", len(a[2])) + pack("<I", a[1])
                for f in a[2]:
                    o = o + pack("<I", f[0]) + addidx(fc_s, len(f[1]))
                    for t in f[1]: o = o + addidx(bi_s, t[0]) + addidx(vi_s, t[1]) + addidx(vi_s, t[2])
                buf = buf + b'ACTN' + pack("<I", len(o) + 8) + o

        if len(inlined) > 0:
            for i in inlined:
                o = addidx(si_s, stridx[i[0]]) + i[1]
                buf = buf + b'ASET' + pack("<I", len(o) + 8) + o

        if len(extras) > 0:
            for e in extras:
                buf = buf + e[0][0:3] + pack("<I", len(e[1]) + 8) + e[1]

        buf = buf + b'OMD3'
        if use_strmcompress:
            buf = zlib.compress(buf, 9)

        f = open(filepath, 'wb')
        s = len(buf) + 8
        f.write(b'3DMO' + pack("<L", s) + buf)
        f.close()

        report({"INFO"}, "Model 3D " + filepath + " (" + str(s) + " bytes) exported.")
    return {'FINISHED'}

class ExportM3D(bpy.types.Operator, ExportHelper):
    bl_idname = "export_scene.m3d"
    bl_label = 'Export M3D'
    bl_options = {'PRESET'}
    filename_ext = ".m3d"
    filter_glob: StringProperty(default="*.m3d", options={'HIDDEN'}) # type: ignore
    use_name: StringProperty(name="Model Name", description="Name of the exported model", default="") # type: ignore
    use_license: StringProperty(name="License", description="Licensing, copyright notice", default="") # type: ignore
    use_author: StringProperty(name="Author", description="Your name and contact", default="") # type: ignore
    use_comment: StringProperty(name="Comment", description="Any description or comment on the model", default="") # type: ignore
    use_scale: FloatProperty(name="Scale (meter)", description="Specify model space 1.0 in SI meters", min=0.0, max=1000.0, default=1.0) # type: ignore
    use_selection: BoolProperty(name="Selection Only", description="Export selected objects only", default=False) # type: ignore
    use_mesh_modifiers: BoolProperty(name="Apply Modifiers", description="Apply modifiers", default=True) # type: ignore
    use_normals: BoolProperty(name="Include Normals", description="Export one normal per vertex and per face", default=True) # type: ignore
    use_uvs: BoolProperty(name="Include UVs", description="Write out the active UV coordinates", default=True) # type: ignore
    use_colors: BoolProperty(name="Include Vertex Colors", description="Write out individual vertex colors", default=True) # type: ignore
    use_materials: BoolProperty(name="Write Materials", description="Write out the materials", default=True) # type: ignore
    use_skeleton: BoolProperty(name="Write Armature", description="Write out armature", default=True) # type: ignore
    use_animation: BoolProperty(name="Write Animation", description="Write out actions", default=True) # type: ignore
    use_markers: BoolProperty(name="Use Markers", description="Use timeline markers for animations", default=False) # type: ignore
    use_fps: IntProperty(name="FPS", description="Specify frame per second", min=1, max=120, default=25) # type: ignore
    use_quality: EnumProperty(name="Precision", items=(('-1','auto',''),('0','8 bits',''),('1','16 bits',''),('2','32 bits',''),('3','64 bits','')), description="Coordinate grid system's size and precision", default='-1') # type: ignore
    use_strmcompress: BoolProperty(name="Use Streamcompression", description="Use lossless deflate", default=True) # type: ignore

    def execute(self, context):
        if bpy.ops.object.mode_set.poll():
            bpy.ops.object.mode_set(mode='OBJECT')
        keywords = self.as_keywords(ignore=("filepath", "filter_glob"))
        return WriteM3D(context, self.filepath, self.report, **keywords)

def Reg(): bpy.utils.register_class(ExportM3D)
def UnReg(): bpy.utils.unregister_class(ExportM3D)

if __name__ == "__main__": Reg()