"""Builds the CH03 East Texas modular kit, exports FBXs to Assets/_Game/Art/Models/EastTexas/ and saves ArtSource/EastTexas/KIT_EastTexas.blend.

Run from the project root:
  /Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup --python Tools/blender/build_easttexas_kit.py

Coordinates: Z up, FRONT = -Y (Unity +Z), width along X. Pivot = bottom-center (X -2..2, Z 0..4), thickness toward +Y (same as SM_AT_Wall_Siding_4x4).
Materials are matched by NAME in Unity (see Editor/KitModelImport.cs), so pieces carry no UVs; relief is geometry.
Rules: no thin (<4 cm) upward-facing flat faces (sub-pixel specular glints) -> slope or chamfer sills/caps/rails/muntins; no coincident faces,
overlap trim INTO the boards instead. Wall bodies are one closed sawtooth profile (boolean-cut for openings); trim/sashes are overlapping solids.

Special pivots:
  SM_ET_Door_6Panel: origin = bottom of the hinge edge; leaf spans local x 0..1, y -.025..+.025, z 0..2.2. Hang closed in SM_ET_Wall_Clapboard_Door_4x4
    at tile-local position (-0.5, 0.095, 0.10) (leaf then fills x -.5..+.5, y .07..0.12, z .1..2.3).
    Unity mirrors X on FBX import (the leaf comes in as x -1..0), so in Unity hang it at tile-local (+0.5, 0.10, -0.095).
  SM_ET_Corner_Clapboard(_Upper): origin = outside corner point where the two walls' y=0 front planes meet. Wall A runs along -X from the corner
    (front -Y, thickness +Y); wall B runs along +Y from the corner (front +X, thickness -X) - B is a tile rotated +90 deg about Z. Legs are
    mirrored across the x+y=0 diagonal, 0.15 wide x ~0.065 deep; Z 0..4.

Kit plan (append pieces to PIECES below):
  Phase 1 funeral home shell: Wall_Clapboard_4x4 (done), Wall_Clapboard_Upper_4x4 (done; no plinth/water table, mirrors SM_NO_Wall_Upper_4x4),
    Wall_Clapboard_Window_4x4 + Window_Upper_4x4 (done; double-hung + shutters), Wall_Clapboard_Door_4x4 + Door_6Panel leaf (done; transom),
    Corner_Clapboard + _Upper (done), Wall_Clapboard_Damaged_4x4, Cornice_4m, Portico, Roof_Gable_Shingle.
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
for _n, _c in (("CH03 Clapboard", (.78, .76, .70, 1)), ("Trim limestone", (.62, .60, .54, 1)), ("Aged stucco", (.66, .62, .55, 1)),
               ("Window pane", (.30, .40, .45, 1)), ("Walnut", (.25, .15, .08, 1)), ("CH03 Shutter", (.16, .24, .19, 1))):
    MATS[_n] = bpy.data.materials.new(_n)
    MATS[_n].diffuse_color = _c
CLAP, STONE, STUCCO, PANE, WALNUT, SHUT = "CH03 Clapboard", "Trim limestone", "Aged stucco", "Window pane", "Walnut", "CH03 Shutter"


def _tri_caps(bm, caps):
    bmesh.ops.triangulate(bm, faces=caps, quad_method="BEAUTY", ngon_method="EAR_CLIP")


def _to_obj(bm, name, slots):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    for s in slots:
        me.materials.append(MATS[s])
    o = bpy.data.objects.new(name, me)
    kit.objects.link(o)
    return o


def ext(name, plane, pts, a0, a1, mat):
    """Extrude a closed 2D profile: 'yz' along X, 'xy' along Z, 'xz' along Y. Single material; caps triangulated."""
    bm = bmesh.new()

    def v(p, a):
        c = {"yz": (a, p[0], p[1]), "xy": (p[0], p[1], a), "xz": (p[0], a, p[1])}[plane]
        return bm.verts.new(c)

    A = [v(p, a0) for p in pts]
    B = [v(p, a1) for p in pts]
    caps = [bm.faces.new(A[::-1]), bm.faces.new(B)]
    for i in range(len(pts)):
        j = (i + 1) % len(pts)
        bm.faces.new((A[i], A[j], B[j], B[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    _tri_caps(bm, caps)
    return _to_obj(bm, name, [mat])


def box(name, mn, mx, mat):
    return ext(name, "yz", [(mn[1], mn[2]), (mx[1], mn[2]), (mx[1], mx[2]), (mn[1], mx[2])], mn[0], mx[0], mat)


def hbar(name, x0, x1, z0, z1, ya, yb, mat, d=.02):
    """Horizontal bar with a roofed (two sloped faces) top, so it has no flat upward face."""
    d = min(d, (z1 - z0) / 2)
    ym = (ya + yb) / 2
    return ext(name, "yz", [(ya, z0), (yb, z0), (yb, z1 - d), (ym, z1), (ya, z1 - d)], x0, x1, mat)


def frustum(name, x0, x1, z0, z1, yb, yt, inset, mat):
    bm = bmesh.new()
    b = [bm.verts.new((x, yb, z)) for x, z in ((x0, z0), (x1, z0), (x1, z1), (x0, z1))]
    t = [bm.verts.new((x, yt, z)) for x, z in ((x0 + inset, z0 + inset), (x1 - inset, z0 + inset), (x1 - inset, z1 - inset), (x0 + inset, z1 - inset))]
    bm.faces.new(b[::-1])
    bm.faces.new(t)
    for i in range(4):
        j = (i + 1) % 4
        bm.faces.new((b[i], b[j], t[j], t[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return _to_obj(bm, name, [mat])


def knob(name, c, r, mat):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=8, v_segments=6, radius=r)
    bmesh.ops.translate(bm, verts=bm.verts, vec=c)
    return _to_obj(bm, name, [mat])


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
    return o


def mirror_diag(parts):
    """Reflect across the x+y=0 plane: (x, y) -> (-y, -x)."""
    for o in parts:
        for v in o.data.vertices:
            v.co.x, v.co.y = -v.co.y, -v.co.x
        bm = bmesh.new()
        bm.from_mesh(o.data)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        bm.to_mesh(o.data)
        bm.free()


def open_edges(o):
    bm = bmesh.new()
    bm.from_mesh(o.data)
    n = len([e for e in bm.edges if len(e.link_faces) != 2])
    bm.free()
    return n


# ---------------------------------------------------------------- wall body (one closed sawtooth profile)
BACK, YP, YT = .20, .02, .06  # back plane; board proud bottom edge / tucked top edge


def profile_extrude(name, x0, x1, pts, edge_mats, cap_mat):
    """Extrude ONE closed (y, z) profile along X. edge_mats[i] = material of the side face on edge pts[i]->pts[i+1]. Caps are concave -> triangulated."""
    slots = [m for m in MATS if m in set(edge_mats) | {cap_mat}]
    bm = bmesh.new()
    a = [bm.verts.new((x0, y, z)) for y, z in pts]
    b = [bm.verts.new((x1, y, z)) for y, z in pts]
    caps = [bm.faces.new(a[::-1]), bm.faces.new(b)]
    for c in caps:
        c.material_index = slots.index(cap_mat)
    for i in range(len(pts)):
        j = (i + 1) % len(pts)
        bm.faces.new((a[i], a[j], b[j], b[i])).material_index = slots.index(edge_mats[i])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    _tri_caps(bm, caps)
    o = _to_obj(bm, name, slots)
    assert open_edges(o) == 0
    return o


def wall_profile(upper):
    H = 4.0
    if upper:
        N, ZD = 22, 0.0
        pts, em = [(YP, 0)], []
    else:
        N, ZD = 19, .61  # drip cap top = first board start
        pts = [(-.02, 0), (-.02, .44), (-.01, .45), (-.01, .57), (YP, ZD)]
        em = [STONE, STONE, CLAP, CLAP]  # plinth face, plinth chamfer, water table, drip cap
    exp = (H - ZD) / N
    for i in range(N):
        top = H if i == N - 1 else ZD + (i + 1) * exp
        pts.append((YT, top))
        em.append(CLAP)  # board face
        if i < N - 1:
            pts.append((YP, top))
            em.append(CLAP)  # step out to next board's proud bottom edge
    pts += [(BACK, H), (BACK, 0)]
    em += [CLAP, STUCCO, CLAP]  # top, rear plane, bottom
    return pts, em, exp, N


def wall_body(name, upper, cuts=()):
    pts, em, exp, N = wall_profile(upper)
    print(name, "boards", N, "exposure", round(exp, 5))
    o = profile_extrude(name, -2.0, 2.0, pts, em, CLAP)
    for x0, x1, z0, z1 in cuts:  # boolean-cut openings; cutter faces take slot 0 (= CH03 Clapboard)
        c = box("cut", (x0, -1, z0), (x1, 1, z1), CLAP)
        m = o.modifiers.new("cut", "BOOLEAN")
        m.operation, m.solver, m.object = "DIFFERENCE", "EXACT", c
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.modifier_apply(modifier=m.name)
        bpy.data.objects.remove(c)
    print(name, "wall body open edges:", open_edges(o))
    assert open_edges(o) == 0
    return o


def wall_clapboard():
    return wall_body("SM_ET_Wall_Clapboard_4x4", False)


def wall_clapboard_upper():
    return wall_body("SM_ET_Wall_Clapboard_Upper_4x4", True)


# ---------------------------------------------------------------- window
Z0, Z1 = .95, 2.65  # opening sill / head heights (opening x +-0.5)


def sash(xh, z0, z1, y0, y1):
    ym = (y0 + y1) / 2
    p = [hbar("rail", -xh, xh, z0, z0 + .05, y0, y1, CLAP), hbar("rail", -xh, xh, z1 - .05, z1, y0, y1, CLAP),
         box("stile", (-xh, y0, z0 + .03), (-xh + .05, y1, z1 - .03), CLAP), box("stile", (xh - .05, y0, z0 + .03), (xh, y1, z1 - .03), CLAP),
         box("muntinV", (-.01, ym - .01, z0 + .03), (.01, ym + .01, z1 - .03), CLAP),
         box("glass", (-xh + .04, ym + .004, z0 + .04), (xh - .04, ym + .008, z1 - .04), PANE)]
    zi0, zi1 = z0 + .05, z1 - .05
    for k in (1, 2):
        zc = zi0 + (zi1 - zi0) * k / 3
        p.append(hbar("muntinH", -xh + .04, xh - .04, zc - .01, zc + .01, ym - .01, ym + .01, CLAP, d=.008))
    return p


def shutter(x0, x1):
    y0, y1 = 0.0, .03
    p = [hbar("rail", x0, x1, Z1 - .06, Z1, y0, y1, SHUT, d=.015), hbar("rail", x0, x1, Z0, Z0 + .06, y0, y1, SHUT, d=.015),
         box("stile", (x0, y0, Z0 + .03), (x0 + .06, y1, Z1 - .03), SHUT), box("stile", (x1 - .06, y0, Z0 + .03), (x1, y1, Z1 - .03), SHUT)]
    n = 24
    pitch = (Z1 - Z0 - .12) / n
    for k in range(n):
        zc = Z0 + .06 + (k + .5) * pitch
        # slanted louver: back edge high, front edge low (sheds water outward); thickness .01
        p.append(ext("slat", "yz", [(.025, zc + .014), (.005, zc - .016), (.005, zc - .026), (.025, zc + .004)], x0 + .05, x1 - .05, SHUT))
    return p


def wall_window(upper):
    name = "SM_ET_Wall_Clapboard_Window_Upper_4x4" if upper else "SM_ET_Wall_Clapboard_Window_4x4"
    p = [wall_body(name, upper, [(-.5, .5, Z0, Z1)])]
    for s in (-1, 1):
        x0, x1 = sorted((s * .48, s * .60))
        p.append(box("casing", (x0, -.01, Z0 - .03), (x1, .07, Z1 + .01), CLAP))
        l0, l1 = sorted((s * .47, s * .53))
        p.append(box("liner", (l0, .05, Z0 - .02), (l1, .19, Z1 + .02), CLAP))
        a, b = sorted((s * .62, s * 1.12))
        p += shutter(a, b)
    p.append(ext("head", "yz", [(-.02, Z1 - .02), (-.02, Z1 + .16), (.07, Z1 + .24), (.07, Z1 - .02)], -.61, .61, CLAP))
    p.append(ext("sill", "yz", [(-.06, Z0 - .06), (-.06, Z0 - .02), (.07, Z0 + .02), (.07, Z0 - .06)], -.61, .61, CLAP))
    p.append(ext("apron", "yz", [(-.03, Z0 - .03), (-.03, Z0 - .15), (.01, Z0 - .19), (.07, Z0 - .19), (.07, Z0 - .03)], -.5, .5, CLAP))
    p.append(box("stool", (-.53, .05, Z0 - .02), (.53, .184, Z0 + .02), CLAP))
    p.append(box("headliner", (-.53, .05, Z1 - .02), (.53, .187, Z1 + .02), CLAP))
    zc = (Z0 + Z1) / 2 + .05
    p += sash(.46, Z0 + .015, zc + .025, .075, .12)  # lower sash
    p += sash(.46, zc - .025, Z1 - .015, .065, .11)  # upper sash (offset in y so rails don't share planes)
    return join(name, p)


# ---------------------------------------------------------------- door
def wall_door():
    name = "SM_ET_Wall_Clapboard_Door_4x4"
    p = [wall_body(name, False, [(-.5, .5, -1, 2.3), (-.5, .5, 2.35, 2.76)])]
    p.append(hbar("transomBar", -.52, .52, 2.29, 2.37, 0, .07, CLAP, d=.02))
    p.append(ext("threshold", "yz", [(-.07, 0), (.16, 0), (.16, .10), (-.035, .10), (-.07, .065)], -.55, .55, STONE))
    for s in (-1, 1):
        x0, x1 = sorted((s * .48, s * .63))
        p.append(box("pilaster", (x0, -.03, 0), (x1, .07, 2.80), CLAP))
        l0, l1 = sorted((s * .47, s * .53))
        p.append(box("jamb", (l0, .13, .10), (l1, .19, 2.28), CLAP))
        p.append(box("transomJamb", (l0, .05, 2.33), (l1, .19, 2.77), CLAP))
    p.append(ext("entablature", "yz", [(-.05, 2.74), (-.05, 2.94), (.07, 3.04), (.07, 2.74)], -.65, .65, CLAP))
    p.append(box("transomHead", (-.53, .05, 2.74), (.53, .187, 2.78), CLAP))
    p.append(box("transomGlass", (-.5, .088, 2.34), (.5, .092, 2.76), PANE))
    for x in (-.25, 0, .25):
        p.append(box("transomMuntin", (x - .01, .08, 2.33), (x + .01, .10, 2.77), CLAP))
    return join(name, p)


def door_leaf():
    p = [box("leaf", (0, -.025, 0), (1.0, .025, 2.2), WALNUT)]
    rows = ((.12, .68), (.80, 1.54), (1.66, 2.08))  # bottom medium, middle tall, top short
    for x0, x1 in ((.12, .44), (.56, .88)):
        for z0, z1 in rows:
            p.append(frustum("panel", x0, x1, z0, z1, -.02, -.035, .025, WALNUT))
    p.append(knob("knob", (.94, -.05, 1.05), .03, WALNUT))
    return join("SM_ET_Door_6Panel", p)


# ---------------------------------------------------------------- corner
def corner(upper):
    def leg():
        p = [box("trim", (-.15, -.02, 0 if upper else .60), (.025, .045, 4.0), CLAP)]
        if not upper:
            p.append(ext("plinthBlock", "yz", [(.04, 0), (-.02, 0), (-.02, .44), (-.01, .45), (.04, .45)], -.15, .025, STONE))
            p.append(ext("tableReturn", "yz", [(.042, .44), (-.01, .44), (-.01, .57), (.02, .61), (.042, .61)], -.15, .025, CLAP))
        return p

    a, b = leg(), leg()
    mirror_diag(b)
    return join("SM_ET_Corner_Clapboard" + ("_Upper" if upper else ""), a + b)


def bounds(o):
    v = [o.matrix_world @ Vector(c) for c in o.bound_box]
    return tuple((round(min(c[i] for c in v), 3), round(max(c[i] for c in v), 3)) for i in range(3))


OUT.mkdir(parents=True, exist_ok=True)
PIECES = [wall_clapboard, wall_clapboard_upper, lambda: wall_window(False), lambda: wall_window(True), wall_door, door_leaf,
          lambda: corner(False), lambda: corner(True)]
pieces = [f() for f in PIECES]
for i, o in enumerate(pieces):
    bb = bounds(o)
    print(o.name, "bounds x,y,z:", bb, "verts", len(o.data.vertices))
    if o.name.startswith("SM_ET_Wall") or o.name.startswith("SM_ET_Corner"):
        assert bb[2] == (0.0, 4.0), bb
    if o.name.startswith("SM_ET_Wall"):
        assert bb[0] == (-2.0, 2.0), bb
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    bpy.ops.export_scene.fbx(filepath=str(OUT / (o.name + ".fbx")), use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL",
                             axis_forward="-Z", axis_up="Y", bake_space_transform=True, mesh_smooth_type="FACE", add_leaf_bones=False, use_mesh_modifiers=True)
    o.location = (i * 6, 0, 0)  # display spacing only; exported at the origin above
BLEND.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(BLEND))
