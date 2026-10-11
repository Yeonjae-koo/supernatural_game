"""Builds the CH03 East Texas modular kit, exports FBXs to Assets/_Game/Art/Models/EastTexas/ and saves ArtSource/EastTexas/KIT_EastTexas.blend.

Run from the project root:
  /Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup --python Tools/blender/build_easttexas_kit.py

Coordinates: Z up, FRONT = -Y (Unity +Z), width along X. Pivot = bottom-center (X -2..2, Z 0..4), thickness toward +Y (same as SM_AT_Wall_Siding_4x4).
Materials are matched by NAME in Unity (see Editor/KitModelImport.cs), so pieces carry no UVs; relief is geometry.

Kit plan (append pieces to PIECES below):
  Phase 1 funeral home shell: Wall_Clapboard_4x4 (done),
    Wall_Clapboard_Upper_4x4 (no plinth/water table, for 2nd storey; mirrors SM_NO_Wall_Upper_4x4), Wall_Clapboard_Window_4x4 (double-hung + shutters), Wall_Clapboard_Door_4x4 (6-panel + transom),
    Wall_Clapboard_Damaged_4x4, Corner_Clapboard, Cornice_4m, Portico, Roof_Gable_Shingle.
  Phase 2 cemetery: reuse SM_NO_IronFence_4m / FencePier / CemeteryGate / Tomb first; add SM_ET_Mausoleum only if reuse falls short.
  Phase 3 Main Street: Wall_Block_4x4, Wall_Block_Door_4x4, Wall_Block_Utility_4x4, Wall_RollUpDoor_4x4, Diner_Front_4m, Diner_Corner,
    Parapet_Band_4m (gas canopy/pump: reuse SM_AT_GasCanopy / FuelPump).
  Phase 4 service/basement: Bulkhead_CellarDoor, Stair_Concrete, PipeManifold_Meter, Bollard.
"""
from pathlib import Path

import bpy
import bmesh
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets/_Game/Art/Models/EastTexas"
BLEND = ROOT / "ArtSource/EastTexas/KIT_EastTexas.blend"

bpy.ops.wm.read_homefile(use_empty=True)
scene = bpy.context.scene
kit = bpy.data.collections.new("KIT")
scene.collection.children.link(kit)
bpy.context.view_layer.active_layer_collection = bpy.context.view_layer.layer_collection.children["KIT"]

MATS = {}
for _n, _c in (("CH03 Clapboard", (.78, .76, .70, 1)), ("Trim limestone", (.62, .60, .54, 1)), ("Aged stucco", (.66, .62, .55, 1))):
    MATS[_n] = bpy.data.materials.new(_n)
    MATS[_n].diffuse_color = _c
CLAP, STONE, STUCCO = "CH03 Clapboard", "Trim limestone", "Aged stucco"


def prism(name, x0, x1, profile, mat):
    """Extrude a closed (y, z) profile (counter-clockwise seen from +X) along X from x0 to x1."""
    bm = bmesh.new()
    a = [bm.verts.new((x0, y, z)) for y, z in profile]
    b = [bm.verts.new((x1, y, z)) for y, z in profile]
    bm.faces.new(a[::-1])
    bm.faces.new(b)
    n = len(profile)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((a[i], a[j], b[j], b[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    o = bpy.data.objects.new(name, me)
    kit.objects.link(o)
    me.materials.append(MATS[mat])
    return o


def box(name, mn, mx, mat):
    return prism(name, mn[0], mx[0], [(mn[1], mn[2]), (mx[1], mn[2]), (mx[1], mx[2]), (mn[1], mx[2])], mat)


def join(name, parts):
    bpy.ops.object.select_all(action="DESELECT")
    for o in parts:
        o.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    o = bpy.context.object
    o.name = o.data.name = name
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    bm = bmesh.new()
    bm.from_mesh(o.data)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(o.data)
    bm.free()
    return o


def profile_extrude(name, x0, x1, pts, edge_mats, cap_mat):
    """Extrude ONE closed (y, z) profile along X. edge_mats[i] is the material of the side face on edge pts[i]->pts[i+1].
    End caps are concave n-gons, so they are triangulated here."""
    bm = bmesh.new()
    a = [bm.verts.new((x0, y, z)) for y, z in pts]
    b = [bm.verts.new((x1, y, z)) for y, z in pts]
    names = list(MATS)
    caps = [bm.faces.new(a[::-1]), bm.faces.new(b)]
    for c in caps:
        c.material_index = names.index(cap_mat)
    n = len(pts)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((a[i], a[j], b[j], b[i])).material_index = names.index(edge_mats[i])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bmesh.ops.triangulate(bm, faces=caps, quad_method="BEAUTY", ngon_method="EAR_CLIP")
    bad = [e for e in bm.edges if len(e.link_faces) != 2]
    print(name, "non-manifold/boundary edges:", len(bad))
    assert not bad
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    for m in MATS.values():
        me.materials.append(m)
    o = bpy.data.objects.new(name, me)
    kit.objects.link(o)
    return o


def wall_clapboard():
    W, H, N = 2.0, 4.0, 19
    BACK, YP, YT = .20, .02, .06  # back plane; board proud bottom edge / tucked top edge
    ZD = .61                     # top of drip cap = first board start
    exp = (H - ZD) / N
    pts = [(-.02, 0), (-.02, .44), (-.01, .45), (-.01, .57), (YP, ZD)]
    em = [STONE, STONE, CLAP, CLAP]  # plinth face, plinth top chamfer, water table, drip cap
    for i in range(N):
        z = ZD + i * exp
        top = H if i == N - 1 else z + exp
        pts.append((YT, top))
        em.append(CLAP)                   # board face
        if i < N - 1:
            pts.append((YP, top))
            em.append(CLAP)               # step out to next board's bottom edge
    pts += [(BACK, H), (BACK, 0)]
    em += [CLAP, STUCCO, CLAP]            # top, rear plane, bottom
    print("boards", N, "exposure", round(exp, 5))
    return profile_extrude("SM_ET_Wall_Clapboard_4x4", -W, W, pts, em, CLAP)


def bounds(o):
    v = [o.matrix_world @ Vector(c) for c in o.bound_box]
    return tuple((round(min(c[i] for c in v), 3), round(max(c[i] for c in v), 3)) for i in range(3))


OUT.mkdir(parents=True, exist_ok=True)
PIECES = [wall_clapboard]
pieces = [f() for f in PIECES]
for i, o in enumerate(pieces):
    bb = bounds(o)
    print(o.name, "bounds x,y,z:", bb, "verts", len(o.data.vertices))
    assert bb[0] == (-2.0, 2.0) and bb[2] == (0.0, 4.0), bb
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    bpy.ops.export_scene.fbx(filepath=str(OUT / (o.name + ".fbx")), use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL",
                             axis_forward="-Z", axis_up="Y", bake_space_transform=True, mesh_smooth_type="FACE", add_leaf_bones=False, use_mesh_modifiers=True)
    o.location = (i * 6, 0, 0)  # display spacing only; exported at the origin above
BLEND.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(BLEND))
