import bpy, os

CHAR_ARMATURE, FALLBACK_ROOT = None, "root"

def get_armature():
    if CHAR_ARMATURE and (ob := bpy.data.objects.get(CHAR_ARMATURE)):
        return ob
    for ob in bpy.data.objects:
        if ob.type == 'ARMATURE':
            return ob
    raise RuntimeError("There is no Armature on scene.")

def process_animations(anim_files):
    if not anim_files:
        return
    target_arm = get_armature()

    for f in anim_files:
        actions_before = set(bpy.data.actions.keys())

        before = set(bpy.data.objects)
        ext = f.lower()
        if ext.endswith('.fbx'):
            bpy.ops.import_scene.fbx(filepath=f, automatic_bone_orientation=True)
        elif ext.endswith('.bvh'):
            bpy.ops.import_anim.bvh(filepath=f)
        elif ext.endswith(('.glb', '.gltf')):
            bpy.ops.import_scene.gltf(filepath=f)

        imported = [o for o in bpy.data.objects if o not in before]
        src_arm = next((o for o in imported if o.type == 'ARMATURE'), None)
        if not src_arm:
            continue

        if src_arm.animation_data:
            src_arm.animation_data.use_nla = False

        base_name = os.path.splitext(os.path.basename(f))[0].split('@')[-1]
        new_act = bpy.data.actions.new(name=base_name)

        if not target_arm.animation_data:
            target_arm.animation_data_create()

        target_arm.animation_data.action = new_act

        if hasattr(new_act, "slots"):
            if len(new_act.slots) == 0:
                new_act.slots.new(id_type='OBJECT', name=target_arm.name)
            if hasattr(target_arm.animation_data, "action_suitable_slots") and target_arm.animation_data.action_suitable_slots:
                target_arm.animation_data.action_slot = target_arm.animation_data.action_suitable_slots[0]

        old_use_nla = target_arm.animation_data.use_nla
        target_arm.animation_data.use_nla = False

        # Make context-safe active
        for o in bpy.context.view_layer.objects:
            o.select_set(False)
        target_arm.select_set(True)
        bpy.context.view_layer.objects.active = target_arm

        bpy.ops.object.mode_set(mode='POSE')

        # Clear Constraints + Reset Transform
        for pb in target_arm.pose.bones:
            while pb.constraints:
                pb.constraints.remove(pb.constraints[0])
            pb.location = (0.0, 0.0, 0.0)
            pb.rotation_quaternion = (1.0, 0.0, 0.0, 0.0)
            pb.rotation_euler = (0.0, 0.0, 0.0)
            pb.scale = (1.0, 1.0, 1.0)

        # Matching
        src_map = {b.name.split(':')[-1].lower(): b.name for b in src_arm.pose.bones}

        for pb in target_arm.pose.bones:
            clean_name = pb.name.replace("DEF-", "").replace("ORG-", "").split(':')[-1].lower()
            match_name = src_map.get(clean_name)
            if match_name:
                con = pb.constraints.new('COPY_TRANSFORMS')
                con.target = src_arm
                con.subtarget = match_name

        root_bone = target_arm.pose.bones.get(FALLBACK_ROOT)
        if root_bone:
            src_hips = next((b for b in src_arm.pose.bones if b.name.lower().startswith(("hip", "pelvis"))), None)
            if src_hips:
                rm_con = root_bone.constraints.new('COPY_LOCATION')
                rm_con.target = src_arm
                rm_con.subtarget = src_hips.name
                rm_con.use_z = False

        # Bake
        src_act = src_arm.animation_data.action if src_arm.animation_data else None
        frame_start = int(src_act.frame_range[0]) if src_act and hasattr(src_act, 'frame_range') else bpy.context.scene.frame_start
        frame_end   = int(src_act.frame_range[1]) if src_act and hasattr(src_act, 'frame_range') else bpy.context.scene.frame_end

        bpy.ops.nla.bake(
            frame_start=frame_start,
            frame_end=frame_end,
            visual_keying=True,
            clear_constraints=True,
            use_current_action=True,
            only_selected=False,
            bake_types={'POSE'}
        )
        bpy.ops.object.mode_set(mode='OBJECT')

        act = target_arm.animation_data.action
        if act:
            fcurves = getattr(act, "fcurves", [])
            if not fcurves and hasattr(act, "bindings"):
                fcurves = [c for b in act.bindings for c in getattr(b, "curves", getattr(b, "fcurves", []))]
            elif not fcurves and hasattr(act, "slots"):
                fcurves = [c for s in act.slots for c in getattr(s, "fcurves", getattr(s, "curves", []))]

            for fc in fcurves:
                if hasattr(fc, "modifiers") and not any(m.type == 'CYCLES' for m in fc.modifiers):
                    fc.modifiers.new('CYCLES')

            # Add NLA track (no move)
            tracks = target_arm.animation_data.nla_tracks
            track = tracks.new()
            track.name = act.name
            track.lock = True
            track.mute = True
            track.strips.new(act.name, frame_start, act)

            target_arm.animation_data.action = None

        target_arm.animation_data.use_nla = old_use_nla

        # Delete excess (invalid) actions
        actions_after = set(bpy.data.actions.keys())
        new_actions = actions_after - actions_before
        keep_name = act.name if act else base_name
        for name in new_actions:
            if name != keep_name:
                bad = bpy.data.actions.get(name)
                if bad:
                    bpy.data.actions.remove(bad)

        for o in imported:
            bpy.data.objects.remove(o, do_unlink=True)

    # Final
    for o in bpy.context.view_layer.objects:
        o.select_set(False)
    target_arm.select_set(True)
    bpy.context.view_layer.objects.active = target_arm

    b_names = target_arm.data.bones.keys()
    for o in bpy.data.objects:
        if o.type == 'MESH' and o.vertex_groups:
            for vg in list(o.vertex_groups):
                if vg.name not in b_names:
                    o.vertex_groups.remove(vg)

    bpy.ops.outliner.orphans_purge(do_local_ids=True, do_recursive=True)