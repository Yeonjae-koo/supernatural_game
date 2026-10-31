"""Builds the CH03 East Texas modular kit, exports FBXs to Assets/_Game/Art/Models/EastTexas/ and saves ArtSource/EastTexas/KIT_EastTexas.blend.

Run from the project root:
  /Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup --python Tools/blender/build_easttexas_kit.py

Coordinates: Z up, FRONT = -Y (Unity +Z), width along X. Pivot = bottom-center (X -2..2, Z 0..4), thickness toward +Y (same as SM_AT_Wall_Siding_4x4).
Materials are matched by NAME in Unity (see Editor/KitModelImport.cs). Relief is geometry; only "CH03 Siding" (clapboard board + butt faces) and
"CH03 Shingle" faces carry meaningful UVs (UV map "UVMap", only layer), everything else in those pieces is (0,0) (CH03 Clapboard stays triplanar).
Siding UV: U=(x+2)/4 (gables: x/4), V=z_building/8 (upper pieces z+4; gables 0.97+0.02*z/apex = clean top band). Shingle UV: U=metres along ridge
(roof: x; portico roof: y), V=0.5 per course: exposed face B_i->T_i spans i*.5..(i+1)*.5, butt face of course i spans i*.5-.02..i*.5 (2 courses per texture repeat).
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
    Corner_Clapboard + _Upper (done), Wall_Clapboard_Damaged_4x4, and (phase 3b, done) Cornice_4m, Cornice_Corner, Roof_Shingle_4m,
    Roof_Shingle_GableEnd_4m, Gable_Clapboard_8m, Portico - stacking rules below.
    16 m-deep building (the real funeral home): Roof_Shingle_Span16_4m, Roof_Shingle_Span16_GableEnd_4m, Gable_Clapboard_16m (same rules, wall planes at y=+-8).

Funeral-home stacking (building front wall plane y=0, x -6..6, depth 8 -> y 0..8, centre-line y=4; upper wall top z=8). The "front reference" of a
wall tile is its y=0 plane: boards are proud to y=.02 (tucked .06), corner trim/plinth sit at y=-.02, rear plane y=.20.
  SM_ET_Cornice_4m: pivot x centre, y=0 (wall front plane), z=0 = top of an Upper wall. Place at (cx, 0, 8) for the front tiles (cx -4,0,4); back run =
    same tiles rotated 180 deg about Z at y=8. Frieze z -.30..0 (front y=-.03), corona to y=-.38, cap top z=+.22 (roof bearing line), rear y=.19. Not on side walls.
  SM_ET_Cornice_Corner: origin = outside corner point (same convention as Corner_Clapboard): arm A along -X (to x=-.6), arm B along +Y (to y=+.6), mitred on
    x+y=0, profile 4 mm oversize so it never z-fights the 4 m cornices it overlaps. Place with the Corner_Clapboard transforms: (6,0,8) rot 0, (6,8,8) rot 90,
    (-6,8,8) rot 180, (-6,0,8) rot 270. The side arm is a .6 m return; side eaves are bare (rake + roof overhang).
  SM_ET_Roof_Shingle_4m / _GableEnd_4m: pivot x centre, y = building centre-line (4), z = top of upper wall (8). 35 deg pitch, eave fascia outer face at +-4.46 from
    centre-line (fascia back .02 inside the corona), soffit z=+.22 (= cornice bearing line) from y=+-4.36 to +-4.0, then rafter underside rises at 35 deg to the ridge
    (underside apex z=3.021, ridge cap apex z~3.43). Slices at x -4, 0, 4: GableEnd at x=-4 as-is (rake end outer face x=-6.30), middle at 0, GableEnd at x=+4
    rotated 180 deg about Z (symmetric front/back so it also lands correctly; rake end at x=+6.30). Unity X-mirror: pieces are X-symmetric except the GableEnd
    rake, which lands on the +X side after mirror - rotate it 180 to compensate (or use rot 0 at +4 when mirrored).
  SM_ET_Gable_Clapboard_8m: own frame like a wall tile (x -4..4, front -Y, z=0 = top of Upper wall, rear y=.20). Place on the side Upper wall run: east wall at (6,4,8) rot +90,
    west wall at (-6,4,8) rot -90. Apex z=3.051 (roof underside + .03 tuck), ends z=.25.
  SM_ET_Roof_Shingle_Span16_4m / _Span16_GableEnd_4m: as the 8 m roofs but for a 16 m deep building: pivot x centre, y = building centre-line (8), z = top of upper wall;
    wall planes at y=+-8 from the centre-line, fascia outer face at +-8.46, soffit z=+.22 from +-8.36 to +-8.0, underside apex z=5.82. Same 35 deg pitch/placement/rotation rules.
  SM_ET_Gable_Clapboard_16m: as Gable_Clapboard_8m but x -8..8 (4 tiles of siding UV), apex z=5.852; place on the side Upper wall run at the centre-line of the 16 m side.
  SM_ET_Portico: pivot x centre of door tile, y=0 front plane, z=0 ground; slab y -2.6..+.10, x +-2.2; columns at (+-2.0,-2.4); everything below z 4.9 (top 4.81).
  Phase 2 cemetery: reuse SM_NO_IronFence_4m / FencePier / CemeteryGate / Tomb first; add SM_ET_Mausoleum only if reuse falls short.
  Phase 3 Main Street (painted CMU + 1950s diner; "CH03 Block" is world-space triplanar, no UVs; all faces/ribs/louvers are geometry or material only):
    Wall_Block_4x4 / Wall_Block_Door_4x4 / Wall_Block_Utility_4x4 / Wall_RollUpDoor_4x4: same frame as the clapboard walls (x -2..2, z 0..4, front plane y=0, rear
      y=.20, tile-local Blender coordinates) with a concrete plinth z 0..0.3 proud 4 cm (chamfered, "Trim limestone"). Door: opening x -1.2..-.25 (steel frame, recessed
      leaf with wired-glass window, gooseneck lamp with "Window glow" bulb at x -.725, meter + conduit at x .7, concrete step y -.5..0 top z .15). Utility: louvered vent at
      x -1.0 z 2.6..3.2, two electrical boxes + vertical conduits at x .4..1.4. RollUpDoor: opening x +-1.5 z 0..3.2, corrugated door recessed to y .105..0.17, hood box above
      inside the head, steel jamb angles, two yellow bollards at (+-1.4, -0.35) 1 m tall (tile y min -0.44).
    Block_Pilaster: corner cover, origin = outside corner point (Corner_Clapboard convention: wall A along -X, wall B = tile rotated +90 about Z along +Y). Footprint x -.26..+.06,
      y -.06..+.26 (0.32 square, chamfered), z 0..4.0, concrete base z 0..0.3. Also caps an open run end. Block_Pilaster_Parapet: same footprint, red enamel + stainless, z 0..0.62,
      place at the same (x, y) at z = 4 (same rotation).
    Parapet_Band_4m: pivot x centre, y=0 = wall front plane, z=0 = top of a 4 m block wall (place at (cx, 0, 4); other sides: same rotations as the walls). Red enamel z 0..0.58
      front y -.04, stainless strips (front y -.06), coping to y .24 at the back, top z .62; square ends. Total top z 4.62 ~ Diner cap 4.6.
    Diner_Front_4m: pivot x centre, y=0 reference plane, z 0..4.6, depth to y=.25 (closed back). Kick panel front y -.04, glass recessed (front y .02), stainless mullions (front
      y -.02) at x -2(half), -1, 0, 1, 2(half), red band front y -.04, cornice proud to y -.25, top z 4.6. Half mullions at both ends join into full ones across tiles.
    Diner_Corner: the same section swept 90 deg around radius 2 (16 segments, glass in 4 flat facets with mullions). Origin = virtual outside corner (Corner_Clapboard convention);
      curve centre (-2, 2): runs from (-2, 0) (joins a Diner_Front tile ending at x=-2 along -X) to (0, 2) (joins a wall running along +Y, e.g. a rotated +90 tile).
    (gas canopy/pump: reuse SM_AT_GasCanopy / FuelPump).
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
for _n, _c in (("CH03 Clapboard", (.78, .76, .70, 1)), ("CH03 Siding", (.78, .76, .70, 1)), ("Trim limestone", (.62, .60, .54, 1)), ("Aged stucco", (.66, .62, .55, 1)),
               ("Window pane", (.30, .40, .45, 1)), ("Walnut", (.25, .15, .08, 1)), ("CH03 Shutter", (.16, .24, .19, 1)),
               ("CH03 Shingle", (.20, .20, .22, 1)), ("Window glow", (1, .9, .7, 1)),
               ("CH03 Block", (.85, .83, .78, 1)), ("CH03 Diner red", (.55, .08, .07, 1)), ("CH03 Stainless", (.7, .7, .72, 1)),
               ("CH03 Cold steel", (.28, .38, .33, 1)), ("CH03 Safety yellow", (.95, .75, .05, 1))):
    MATS[_n] = bpy.data.materials.new(_n)
    MATS[_n].diffuse_color = _c
CLAP, STONE, STUCCO, PANE, WALNUT, SHUT = "CH03 Clapboard", "Trim limestone", "Aged stucco", "Window pane", "Walnut", "CH03 Shutter"
SHING, GLOW, SIDE = "CH03 Shingle", "Window glow", "CH03 Siding"
BLK, DRED, SS, STL, YEL = "CH03 Block", "CH03 Diner red", "CH03 Stainless", "CH03 Cold steel", "CH03 Safety yellow"


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


def profile_extrude(name, x0, x1, pts, edge_mats, cap_mat, slots=None):
    """Extrude ONE closed (y, z) profile along X. edge_mats[i] = material of the side face on edge pts[i]->pts[i+1]. Caps are concave -> triangulated."""
    slots = slots or [m for m in MATS if m in set(edge_mats) | {cap_mat}]
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
        em.append(SIDE)  # board face
        if i < N - 1:
            pts.append((YP, top))
            em.append(SIDE)  # step out to next board's proud bottom edge
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


# ================================================================ phase 3b: cornice / roof / gable / portico
import math

T35, T28 = math.tan(math.radians(35)), math.tan(math.radians(28))


def pe(name, plane, a0, a1, pts, edge_mats, cap_mat):
    """Like profile_extrude but plane 'yz' (along X) or 'xz' (along Y)."""
    slots = [m for m in MATS if m in set(edge_mats) | {cap_mat}]
    bm = bmesh.new()

    def v(p, a):
        return bm.verts.new((a, p[0], p[1]) if plane == "yz" else (p[0], a, p[1]))

    A = [v(p, a0) for p in pts]
    B = [v(p, a1) for p in pts]
    caps = [bm.faces.new(A[::-1]), bm.faces.new(B)]
    for c in caps:
        c.material_index = slots.index(cap_mat)
    for i in range(len(pts)):
        j = (i + 1) % len(pts)
        bm.faces.new((A[i], A[j], B[j], B[i])).material_index = slots.index(edge_mats[i])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    _tri_caps(bm, caps)
    o = _to_obj(bm, name, slots)
    assert open_edges(o) == 0, name
    return o


def loft(name, rings, mat, smooth=()):
    """Closed solid through rings of equal vertex count; gaps listed in `smooth` get smooth-shaded side faces; end caps flat."""
    bm = bmesh.new()
    R = [[bm.verts.new(p) for p in r] for r in rings]
    caps = [bm.faces.new(R[0][::-1]), bm.faces.new(R[-1])]
    for k in range(len(R) - 1):
        n = len(R[k])
        for i in range(n):
            j = (i + 1) % n
            bm.faces.new((R[k][i], R[k][j], R[k + 1][j], R[k + 1][i])).smooth = k in smooth
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    _tri_caps(bm, caps)
    o = _to_obj(bm, name, [mat])
    assert open_edges(o) == 0, name
    return o


def rect(x0, x1, y0, y1, z):
    return [(x0, y0, z), (x1, y0, z), (x1, y1, z), (x0, y1, z)]


def circ(cx, cy, r, z, n=24):
    return [(cx + r * math.cos(2 * math.pi * i / n), cy + r * math.sin(2 * math.pi * i / n), z) for i in range(n)]


def shear(name, pts, xa, xb, L, mat):
    """Prism whose (y, dz) profile rides a sloped line z=L(x) between x=xa and xb (sheared extrusion, plumb end cuts)."""
    bm = bmesh.new()
    A = [bm.verts.new((xa, y, L(xa) + d)) for y, d in pts]
    B = [bm.verts.new((xb, y, L(xb) + d)) for y, d in pts]
    caps = [bm.faces.new(A[::-1]), bm.faces.new(B)]
    for i in range(len(pts)):
        j = (i + 1) % len(pts)
        bm.faces.new((A[i], A[j], B[j], B[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    _tri_caps(bm, caps)
    o = _to_obj(bm, name, [mat])
    assert open_edges(o) == 0, name
    return o


# ---------------------------------------------------------------- cornice
CORN = [(.19, -.30), (-.03, -.30), (-.03, -.06), (-.07, -.02), (-.07, .04), (-.38, .04), (-.38, .09), (-.34, .13), (-.10, .22), (.19, .22)]
CORN_EM = [CLAP] * 9 + [STUCCO]  # last edge = rear plane


def cornice_4m():
    return pe("SM_ET_Cornice_4m", "yz", -2.0, 2.0, CORN, CORN_EM, CLAP)


def cornice_corner():
    g = .004  # oversize so the arms never z-fight the 4 m cornices
    prof = [(y - g if y < 0 else y, z + g if z > .2 else z) for y, z in CORN]
    L = .6
    bm = bmesh.new()
    n = len(prof)
    a = [bm.verts.new((-L, y, z)) for y, z in prof]
    m = [bm.verts.new((-y, y, z)) for y, z in prof]  # mitre line x+y=0, shared by both arms
    c = [bm.verts.new((-y, L, z)) for y, z in prof]  # arm B = arm A reflected across x+y=0
    caps = [bm.faces.new(a[::-1]), bm.faces.new(c)]
    for i in range(n):
        j = (i + 1) % n
        mi = 1 if CORN_EM[i] == STUCCO else 0
        bm.faces.new((a[i], a[j], m[j], m[i])).material_index = mi
        bm.faces.new((m[i], m[j], c[j], c[i])).material_index = mi
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    _tri_caps(bm, caps)
    o = _to_obj(bm, "SM_ET_Cornice_Corner", [CLAP, STUCCO])
    assert open_edges(o) == 0
    return o


# ---------------------------------------------------------------- shingled roofs
def courses(b0, theta, sx, e0, d, h_end):
    """Sawtooth shingle courses from butt point b0=(h,z) toward the ridge (sx=+1 rising in +h, -1 in -h) until h_end. Returns points B0,T0,B1,T1..T_last."""
    t = (sx * math.cos(theta), math.sin(theta))
    nr = (-sx * math.sin(theta), math.cos(theta))
    O = (b0[0] - nr[0] * d, b0[1] - nr[1] * d)
    stot = (h_end - O[0]) / t[0]
    N = max(1, round(stot / e0))
    e = stot / N
    P = []
    for i in range(N):
        P.append((O[0] + t[0] * i * e + nr[0] * d, O[1] + t[1] * i * e + nr[1] * d))
        P.append((O[0] + t[0] * (i + 1) * e, O[1] + t[1] * (i + 1) * e))
    return P, O, t, N, e


def roof_profile(H=4.0):
    th = math.radians(35)
    pf, O, t, N, e = courses((-(H + .42), .26), th, 1, .14, .02, -.18)
    zp = lambda y: O[1] + T35 * (y - O[0])  # shingle base plane
    yc = pf[-1][0]
    apex = (0, zp(0) + .07)
    front = [(-(H + .46), .08), (-(H + .46), .22)] + pf + [(yc, pf[-1][1] + .07), apex]
    # edge i is pts[i]->pts[i+1]: fascia face, chamfer to B0 (CLAP); courses, T_last->riser, riser->apex (SHING)
    mats = [CLAP, CLAP] + [SHING] * (len(pf) - 1) + [SHING, SHING]
    mir = [(-y, z) for y, z in reversed(front[:-1])]
    pts = front + mir + [(H + .36, .08), (H + .36, .22), (H, .22), (0, .22 + T35 * H), (-H, .22), (-(H + .36), .22), (-(H + .36), .08)]
    nf = len(front)
    em = []
    for i in range(len(pts)):
        if i < nf - 1:
            em.append(mats[i])
        elif i < nf + len(mir) - 1:  # apex->riser(m) ... through mirrored courses
            k = i - (nf - 1)  # 0: apex->riser, then riser->T_last ... ending at B0->chamfer pts
            em.append(SHING if k < len(mir) - 3 else CLAP)
        else:
            em.append(CLAP)
    return pts, em, (O, zp, N, e, apex)


SHINGLE_INFO = {}  # piece name -> (O, e, d, sx, theta, fold(h)) used by the shingle UV pass


def roof_slice(name, x0, x1, H=4.0):
    pts, em, info = roof_profile(H)
    SHINGLE_INFO[name] = (info[0], info[3], .02, 1, math.radians(35), lambda y: -abs(y))
    o = pe(name, "yz", x0, x1, pts, em, CLAP)
    return o, info


def roof_4m(H=4.0, name="SM_ET_Roof_Shingle_4m"):
    o, (O, zp, N, e, apex) = roof_slice(name, -2.0, 2.0, H)
    if H != 4.0:
        print(name, "courses", N, "exposure", round(e, 4), "ridge cap apex z", round(apex[1], 3), "underside apex z", round(.22 + T35 * H, 3))
    return o


def roof_gable_end(H=4.0, name="SM_ET_Roof_Shingle_GableEnd_4m"):
    body, (O, zp, N, e, apex) = roof_slice(name, -2.26, 2.0, H)
    print(name, "courses", N, "exposure", round(e, 4), "ridge cap apex z", round(apex[1], 3), "underside apex z", round(.22 + T35 * H, 3))
    # rake board: ~.18 m perpendicular band along both slopes, 10 cm thick, overlapping the slab end
    zt, hb = zp(-(H + .44)), .18 / math.cos(math.radians(35))
    y1 = -(H + .44) + (.08 - (zt - hb)) / T35  # bottom edge clamped to the fascia bottom (z .08)
    rk = [(-(H + .44), .08), (-(H + .44), zt + .045), (0, zp(0) + .045), (H + .44, zt + .045), (H + .44, .08), (-y1, .08), (0, zp(0) - hb), (y1, .08)]
    r = ext("rake", "yz", rk, -2.30, -2.20, CLAP)
    return join(name, [body, r])


# ---------------------------------------------------------------- gable wall
GABLE_APEX = {}
def gable_clapboard(half=4.0):
    name = "SM_ET_Gable_Clapboard_%dm" % round(2 * half)
    exp = .1818
    N = math.ceil((.22 + T35 * half + .03) / exp - 1e-9)
    pts, em = [(YP, 0)], []
    for i in range(N):
        top = (i + 1) * exp
        pts.append((YT, top))
        em.append(SIDE)
        if i < N - 1:
            pts.append((YP, top))
            em.append(SIDE)
    pts += [(BACK, top), (BACK, 0)]
    em += [CLAP, STUCCO, CLAP]
    o = profile_extrude(name, -half, half, pts, em, CLAP)
    GABLE_APEX[name] = za = .22 + T35 * half + .03  # roof underside at the centre + 3 cm tuck
    print(name, "boards", N, "exposure", exp, "apex z", round(za, 3))
    hc = half + .5
    c = ext("cut", "xz", [(-hc, za - T35 * hc), (0, za), (hc, za - T35 * hc), (hc, za + 2), (-hc, za + 2)], -1, 1, CLAP)
    m = o.modifiers.new("cut", "BOOLEAN")
    m.operation, m.solver, m.object = "DIFFERENCE", "EXACT", c
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.modifier_apply(modifier=m.name)
    bpy.data.objects.remove(c)
    assert open_edges(o) == 0
    return o


# ---------------------------------------------------------------- portico
def portico():
    name = "SM_ET_Portico"
    Lr = lambda x: 3.45 + T28 * (2.2 - abs(x))  # pediment base line (top of entablature at the eaves)
    P = []
    # stone floor slab with 45 deg chamfered front/side top edges
    c = .04
    P.append(loft("slab", [rect(-2.2, 2.2, -2.6, .10, 0), rect(-2.2, 2.2, -2.6, .10, .06), rect(-2.2 + c, 2.2 - c, -2.6 + c, .10, .10)], STONE))
    for sx in (-1, 1):
        cx, cy = sx * 2.0, -2.4
        P.append(loft("plinth", [rect(cx - .2, cx + .2, cy - .2, cy + .2, .05), rect(cx - .2, cx + .2, cy - .2, cy + .2, .16),
                                 rect(cx - .15, cx + .15, cy - .15, cy + .15, .22)], STONE))
        col = [(.20, .15), (.22, .15), (.24, .165), (.265, .165), (.285, .15), (2.85, .13), (2.89, .16), (2.93, .18), (2.95, .18)]
        P.append(loft("column", [circ(cx, cy, r, z) for z, r in col], CLAP, smooth=range(len(col) - 1)))
        P.append(loft("abacus", [rect(cx - .19, cx + .19, cy - .19, cy + .19, 2.93), rect(cx - .19, cx + .19, cy - .19, cy + .19, 3.0),
                                 rect(cx - .16, cx + .16, cy - .16, cy + .16, 3.06)], CLAP))
        P.append(box("pilaster", (cx - .15, -.14, .05), (cx + .15, .10, 2.98), CLAP))
        P.append(box("pilasterCap", (cx - .19, -.18, 2.98), (cx + .19, .10, 3.06), CLAP))
        # side entablature beam (x 1.8..2.2), runs front->wall; mirrored profile for the left side
        pr = [(sx * 1.8, 3.05), (sx * 2.2, 3.05), (sx * 2.2, 3.40), (sx * 2.15, 3.45), (sx * 1.8, 3.45)]
        P.append(ext("beamSide", "xz", pr, -2.2, .10, CLAP))
    # front entablature beam (full width, small chamfered cap)
    P.append(ext("beamFront", "yz", [(-2.6, 3.05), (-2.2, 3.05), (-2.2, 3.45), (-2.55, 3.45), (-2.6, 3.40)], -2.2, 2.2, CLAP))
    # soffit ceiling + two downlight panels
    P.append(box("ceiling", (-2.1, -2.5, 3.06), (2.1, .10, 3.30), CLAP))
    for gx in (-.7, .7):
        P.append(box("downlight", (gx - .125, -1.425, 3.04), (gx + .125, -1.175, 3.07), GLOW))
    # pediment: recessed flat tympanum + raking cornice along both slopes
    P.append(ext("tympanum", "xz", [(-2.2, 3.30), (2.2, 3.30), (2.2, Lr(2.2) - .08), (0, Lr(0) - .08), (-2.2, Lr(2.2) - .08)], -2.55, -2.38, CLAP))
    rk = [(-2.60, -.14), (-2.60, .14), (-2.55, .19), (-2.42, .19), (-2.42, -.14)]
    P.append(shear("rakeR", rk, 0, 2.26, lambda x: 3.45 + T28 * (2.2 - x), CLAP))
    P.append(shear("rakeL", rk, -2.26, 0, lambda x: 3.45 + T28 * (2.2 + x), CLAP))
    # small gable roof behind the pediment, ridge along Y, shingled
    th = math.radians(28)
    xe = 2.26
    b0 = (xe, Lr(xe) + .16)
    pf, O, t, N, e = courses(b0, th, -1, .14, .02, .15)
    zp = lambda x: O[1] + T28 * (O[0] - x)
    SHINGLE_INFO["SM_ET_Portico"] = (O, e, .02, -1, th, abs)
    xc = pf[-1][0]
    # polygon: bottom-right -> eave face -> shingles -> apex -> mirrored shingles -> bottom-left
    pts = [(xe, 3.43)] + [(x, z) for x, z in pf] + [(xc, pf[-1][1] + .05), (0, zp(0) + .05)]
    pts += [(-x, z) for x, z in reversed(pts[1:-1])] + [(-xe, 3.43)]
    # edges: (xe,3.43)->B0 eave face CLAP, everything on the top SHINGLE, left eave face CLAP, bottom CLAP
    em = []
    for i in range(len(pts)):
        j = (i + 1) % len(pts)
        if i == 0 or j == len(pts) - 1 or i == len(pts) - 1:
            em.append(CLAP)
        else:
            em.append(SHING)
    em[0] = CLAP
    P.append(pe("roof", "xz", -2.45, .12, pts, em, CLAP))
    print(name, "roof courses", N, "exposure", round(e, 4), "ridge apex z", round(zp(0) + .05, 3))
    return join(name, P)


# ================================================================ phase 3: Main Street (CMU block buildings + diner)
def poly_ring(x0, x1, y0, y1, c, z):
    """Octagon (square with chamfered vertical corners) at height z."""
    return [(x0 + c, y0, z), (x1 - c, y0, z), (x1, y0 + c, z), (x1, y1 - c, z), (x1 - c, y1, z), (x0 + c, y1, z), (x0, y1 - c, z), (x0, y0 + c, z)]


def rings_axis(name, axis, c, prof, mat, n=12):
    """Solid of revolution about an axis-aligned line through c (the other two coords); prof = [(a along the axis, radius)]."""
    rings = []
    for a, r in prof:
        pts = []
        for i in range(n):
            t = 2 * math.pi * i / n
            p, q = c[0] + r * math.cos(t), c[1] + r * math.sin(t)
            pts.append({"x": (a, p, q), "y": (p, a, q), "z": (p, q, a)}[axis])
        rings.append(pts)
    return loft(name, rings, mat)


def cyl(name, axis, c, a0, a1, r, mat, n=10, end0=False, end1=True):
    """Conduit/pipe. Ends taper to a small cap so no flat face is left (end1 = the a1 end tapers; end0 likewise)."""
    prof = ([(a0, r * .35), (a0 + .012 * (1 if a1 > a0 else -1), r)] if end0 else [(a0, r)]) + ([(a1 - .012 * (1 if a1 > a0 else -1), r), (a1, r * .35)] if end1 else [(a1, r)])
    return rings_axis(name, axis, c, prof, mat, n)


def cut_box(o, x0, x1, z0, z1):
    c = box("cut", (x0, -1, z0), (x1, 1, z1), BLK)  # cutter faces take slot 0 (= CH03 Block)
    m = o.modifiers.new("cut", "BOOLEAN")
    m.operation, m.solver, m.object = "DIFFERENCE", "EXACT", c
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.modifier_apply(modifier=m.name)
    bpy.data.objects.remove(c)


def block_body(name, cuts=()):
    """Painted CMU wall: one closed profile; concrete plinth z 0..0.3 proud 4 cm with a 45 deg chamfer. Openings are boolean cuts."""
    pts = [(BACK, 0), (-.04, 0), (-.04, .26), (0, .30), (0, 4), (BACK, 4)]
    em = [STONE, STONE, STONE, BLK, BLK, BLK]
    o = profile_extrude(name, -2.0, 2.0, pts, em, BLK, slots=[BLK, STONE])
    for c in cuts:
        cut_box(o, *c)
    assert open_edges(o) == 0, name
    return o


def conduit_run(x, z0, z1, y=-.01, r=.02):
    p = [cyl("conduit", "z", (x, y), z0, z1, r, STL, end0=z0 > .5)]
    for zc in (z0 + (z1 - z0) * k for k in (.3, .7)):  # standoff clips
        p.append(box("clip", (x - .03, y - .025, zc - .012), (x + .03, .005, zc + .012), STL))
    return p


def wall_block():
    return block_body("SM_ET_Wall_Block_4x4")


# ---- door wall
DX0, DX1 = -1.2, -.25  # steel door opening (x) z 0..2.15


def wall_block_door():
    name = "SM_ET_Wall_Block_Door_4x4"
    cx = (DX0 + DX1) / 2
    p = [block_body(name, [(DX0, DX1, -1, 2.15)])]
    # concrete step with threshold inside the opening, chamfered front edge
    p.append(ext("step", "yz", [(.14, 0), (-.5, 0), (-.5, .11), (-.46, .15), (.14, .15)], cx - .7, cx + .7, STONE))
    # steel frame (jambs + head overlap the wall by 7 cm and the opening by 3 cm)
    for x0, x1 in ((DX0 - .07, DX0 + .03), (DX1 - .03, DX1 + .07)):
        p.append(box("jamb", (x0, -.03, 0), (x1, .14, 2.22), STL))
    p.append(hbar("head", DX0 - .07, DX1 + .07, 2.12, 2.24, -.03, .14, STL, d=.03))
    # recessed leaf with wired-glass window, lever handle, hinges
    lx0, lx1 = DX0 + .03, DX1 - .03
    p.append(box("leaf", (lx0, .06, .17), (lx1, .11, 2.12), STL))
    p.append(box("glass", (cx - .13, .05, 1.30), (cx + .13, .09, 1.76), PANE))
    for s in (-1, 1):
        a, b = sorted((cx + s * .12, cx + s * .17))
        p.append(box("winStile", (a, .045, 1.26), (b, .09, 1.80), STL))
    p.append(hbar("winTop", cx - .17, cx + .17, 1.74, 1.80, .045, .09, STL, d=.03))
    p.append(hbar("winBot", cx - .17, cx + .17, 1.26, 1.32, .045, .09, STL, d=.03))
    p.append(box("rosette", (lx1 - .12, .045, .95), (lx1 - .06, .065, 1.10), STL))
    p.append(hbar("lever", lx1 - .22, lx1 - .07, 1.00, 1.04, .02, .05, STL, d=.015))
    for zh in (.35, 1.15, 1.95):
        p.append(box("hinge", (lx0 - .01, .03, zh - .06), (lx0 + .05, .07, zh + .06), STL))
    # gooseneck lamp above the door: wall plate, arm, domed shade, glowing bulb face under it
    ly = -.30
    p.append(box("lampPlate", (cx - .06, -.02, 2.50), (cx + .06, .02, 2.80), STL))
    p.append(cyl("lampArm", "y", (cx, 2.66), .0, ly, .018, STL, end1=False))
    p.append(rings_axis("lampShade", "z", (cx, ly), [(2.74, .03), (2.72, .05), (2.66, .12), (2.60, .18), (2.56, .205), (2.555, .205)], STL, n=16))
    p.append(rings_axis("bulb", "z", (cx, ly), [(2.52, .05), (2.54, .09), (2.57, .09)], GLOW, n=12))
    # electric meter box + dome, conduits
    mx = .70
    p.append(ext("meterBox", "yz", [(.02, 1.25), (-.13, 1.25), (-.13, 1.71), (.02, 1.75)], mx - .15, mx + .15, STL))
    p.append(rings_axis("meterFlange", "y", (mx, 1.52), [(-.12, .115), (-.14, .115)], STL, n=16))
    p.append(rings_axis("meterDome", "y", (mx, 1.52), [(-.13, .10), (-.15, .095), (-.17, .065), (-.18, .03)], PANE, n=16))
    p += conduit_run(mx, 1.74, 3.9) + conduit_run(mx, .15, 1.26)
    return join(name, p)


# ---- utility wall
def wall_block_utility():
    name = "SM_ET_Wall_Block_Utility_4x4"
    vx0, vx1, vz0, vz1 = -1.45, -.55, 2.55, 3.20  # vent opening
    p = [block_body(name, [(vx0, vx1, vz0, vz1)])]
    p.append(box("ventBack", (vx0 - .01, .12, vz0 - .01), (vx1 + .01, .16, vz1 + .01), PANE))  # dark void behind the louvers
    n = 9
    pitch = (vz1 - vz0) / n
    for k in range(n):
        zc = vz0 + (k + .5) * pitch
        # louver slats slope down toward the front; 1 cm thick
        p.append(ext("louver", "yz", [(.11, zc + .022), (.03, zc - .012), (.03, zc - .022), (.11, zc + .012)], vx0 - .01, vx1 + .01, STL))
    for s in (-1, 1):
        a, b = sorted((vx0 if s < 0 else vx1, (vx0 - .08) if s < 0 else (vx1 + .08)))
        p.append(box("ventFrameSide", (a - .02, -.025, vz0 - .07), (b + .0, .02, vz1 + .07), STL))
    p.append(hbar("ventFrameTop", vx0 - .08, vx1 + .08, vz1 - .02, vz1 + .07, -.025, .02, STL, d=.025))
    p.append(hbar("ventFrameBot", vx0 - .08, vx1 + .08, vz0 - .07, vz0 + .02, -.025, .02, STL, d=.025))
    # electrical boxes with sloped rain-hood tops
    p.append(ext("panelBox", "yz", [(.02, 1.50), (-.14, 1.50), (-.14, 2.06), (.02, 2.12)], .45, .85, STL))
    p.append(ext("junctionBox", "yz", [(.02, .90), (-.10, .90), (-.10, 1.12), (.02, 1.16)], 1.15, 1.40, STL))
    p.append(box("panelLatch", (.62, -.16, 1.74), (.68, -.12, 1.84), STL))
    p += conduit_run(.65, 2.11, 3.9) + conduit_run(.55, .15, 1.51) + conduit_run(1.275, 1.15, 3.9) + conduit_run(1.275, .15, .91)
    p.append(cyl("conduitH", "x", (-.01, 1.80), .84, 1.275, .02, STL, end1=False))
    return join(name, p)


# ---- roll-up door wall
def wall_rollup():
    name = "SM_ET_Wall_RollUpDoor_4x4"
    p = [block_body(name, [(-1.5, 1.5, -1, 3.2)])]
    p.append(ext("hood", "yz", [(.08, 3.00), (.12, 2.92), (.22, 2.92), (.22, 3.25), (.08, 3.25)], -1.46, 1.46, STL))
    # corrugated door: horizontal ribs (trapezoid profile, 7.5 cm pitch) recessed to y .105..0.13, back plane .17
    z1, per = 2.95, .075
    n = 38
    pts = [(.17, z1 - n * per), (.17, z1)]
    for k in range(n):
        zt = z1 - k * per
        pts += [(.13, zt), (.13, zt - .015), (.105, zt - .0375), (.105, zt - .0525)]
    pts.append((.13, z1 - n * per))
    p.append(ext("door", "yz", pts, -1.48, 1.48, STL))
    p.append(hbar("bottomRail", -1.48, 1.48, .02, .13, .07, .17, STL, d=.03))
    for s in (-1, 1):
        a, b = sorted((s * 1.45, s * 1.51))
        p.append(box("guide", (a, -.03, 0), (b, .18, 3.25), STL))
        a, b = sorted((s * 1.48, s * 1.62))
        p.append(box("jambAngle", (a, -.03, 0), (b, .02, 3.20), STL))
        p.append(rings_axis("bollard", "z", (s * 1.4, -.35),
                            [(0, .095), (.03, .095), (.045, .078), (.92, .078), (.955, .07), (.985, .05), (1.0, .022), (1.0, .012)], YEL, n=16))
        p.append(rings_axis("bollardBand", "z", (s * 1.4, -.35), [(.72, .079), (.88, .079)], STL, n=16))
    p.append(hbar("headAngle", -1.64, 1.64, 3.15, 3.27, -.03, .02, STL, d=.03))
    return join(name, p)


# ---- pilasters
PIL = (-.26, .06, -.06, .26)  # footprint x0, x1, y0, y1 (outside corner at x .06 / y -.06 side)


def block_pilaster():
    x0, x1, y0, y1 = PIL
    body = loft("body", [poly_ring(x0, x1, y0, y1, .03, .26), poly_ring(x0, x1, y0, y1, .03, 4.0)], BLK)
    g = .03
    base = loft("base", [poly_ring(x0 - g, x1 + g, y0 - g, y1 + g, .03, 0), poly_ring(x0 - g, x1 + g, y0 - g, y1 + g, .03, .26),
                         poly_ring(x0, x1, y0, y1, .03, .30)], STONE)
    return join("SM_ET_Block_Pilaster", [body, base])


def block_pilaster_parapet():
    x0, x1, y0, y1 = PIL
    red = loft("red", [poly_ring(x0, x1, y0, y1, .03, 0), poly_ring(x0, x1, y0, y1, .03, .56)], DRED)
    g = .012
    strips = [loft("strip", [poly_ring(x0 + .01, x1 - .01, y0 + .01, y1 - .01, .03, z0), poly_ring(x0 - g, x1 + g, y0 - g, y1 + g, .03, z0 + .015),
                             poly_ring(x0 - g, x1 + g, y0 - g, y1 + g, .03, z1 - .015), poly_ring(x0 + .01, x1 - .01, y0 + .01, y1 - .01, .03, z1)], SS)
              for z0, z1 in ((0, .06), (.50, .56))]
    cap = loft("cap", [poly_ring(x0 - .02, x1 + .02, y0 - .02, y1 + .02, .03, .54), poly_ring(x0 + .02, x1 - .02, y0 + .02, y1 - .02, .03, .62)], SS)
    return join("SM_ET_Block_Pilaster_Parapet", [red, cap] + strips)


# ---- parapet band
def strip_prof(d0, z0, z1, proud=.02, tilt=.015):
    """Stainless trim strip hugging a plane at depth d0 (front = negative d): chamfered both edges, protrudes `proud`, back overlaps `tilt*2` into the body."""
    return [(d0 + .02, z0), (d0 - proud, z0 + tilt), (d0 - proud, z1 - tilt), (d0 + .02, z1)]


def parapet_band():
    prof = lambda pts: pts
    p = [ext("band", "yz", [(.20, 0), (-.04, 0), (-.04, .56), (.20, .56)], -2, 2, DRED),
         ext("stripLow", "yz", strip_prof(-.04, 0, .06), -2, 2, SS), ext("stripHigh", "yz", strip_prof(-.04, .50, .56), -2, 2, SS),
         ext("coping", "yz", [(-.05, .56), (-.05, .60), (.02, .62), (.24, .58), (.24, .54)], -2, 2, SS)]
    return join("SM_ET_Parapet_Band_4m", p)


# ---- diner
DN = 24  # arc segments for the corner
ARC = [-math.pi / 2 + k * (math.pi / 2) / DN for k in range(DN + 1)]
FACETS = [-math.pi / 2 + k * (math.pi / 8) for k in range(5)]  # 4 glass facets of 22.5 deg


def frame(corner):
    """pt(s, a, d, z): s = x of the section (tile) or angle (corner); a = along the run, d = depth (front negative), z up."""
    if not corner:
        return lambda s, a, d, z: (s + a, d, z)

    def f(th, a, d, z):
        r = 2 - d
        return (-2 + r * math.cos(th) - a * math.sin(th), 2 + r * math.sin(th) + a * math.cos(th), z)
    return f


# (d, z) sections; back plane d=.25 (closed shell)
KICK = [(.25, 0), (-.03, 0), (-.04, .04), (-.04, .78), (-.03, .82), (.25, .82)]
BAND = [(.25, 2.55), (-.04, 2.55), (-.04, 4.12), (.25, 4.12)]
RUB = [(-.02, .36), (-.055, .372), (-.055, .408), (-.02, .42)]
KICKPLATE = [(-.02, 0), (-.05, .015), (-.05, .095), (-.02, .10)]
SILL = [(.06, .78), (-.075, .78), (-.075, .83), (.05, .885), (.06, .885)]
HEADER = [(.25, 2.46), (-.05, 2.46), (-.08, 2.49), (-.08, 2.60), (-.05, 2.64), (.25, 2.64)]
CAP = [(.25, 4.02), (-.075, 4.02), (-.095, 4.05), (-.095, 4.12), (-.25, 4.30), (-.25, 4.40)] + \
      [(-.05 - .2 * math.cos(math.pi / 2 * k / 6), 4.40 + .2 * math.sin(math.pi / 2 * k / 6)) for k in range(1, 7)] + [(.25, 4.58)]


def diner(corner):
    name = "SM_ET_Diner_Corner" if corner else "SM_ET_Diner_Front_4m"
    f = frame(corner)
    stations = ARC if corner else [-2.0, 2.0]

    def sweep(nm, prof, mat):
        return loft(nm, [[f(s, 0, d, z) for d, z in prof] for s in stations], mat)

    def mull(nm, s, half, z0=.80, z1=2.55):
        prof = {0: [(-.03, .04), (-.03, -.005), (-.015, -.02), (.015, -.02), (.03, -.005), (.03, .04)],
                -1: [(-.03, .04), (-.03, -.005), (-.015, -.02), (0, -.02), (0, .04)],   # half on the +a end of a run
                1: [(0, .04), (0, -.02), (.015, -.02), (.03, -.005), (.03, .04)]}[half]  # half on the -a end
        return loft(nm, [[f(s, a, d, z) for a, d in prof] for z in (z0, z1)], SS)

    p = [sweep("kick", KICK, DRED), sweep("band", BAND, DRED), sweep("rub", RUB, SS), sweep("kickplate", KICKPLATE, SS),
         sweep("sill", SILL, SS), sweep("header", HEADER, SS), sweep("cap", CAP, SS)]
    if corner:
        for k in range(4):  # flat glass facets, front (d=.02) to the closed back (d=.25)
            ring = lambda th: [f(th, 0, d, z) for d, z in ((.02, .80), (.25, .80), (.25, 2.52), (.02, 2.52))]
            p.append(loft("glass", [ring(FACETS[k]), ring(FACETS[k + 1])], PANE))
        p += [mull("mullion", th, 0) for th in FACETS[1:4]] + [mull("mullionEnd", FACETS[0], 1), mull("mullionEnd", FACETS[4], -1)]
    else:
        p.append(box("glass", (-2, .02, .80), (2, .25, 2.52), PANE))
        p += [mull("mullion", x, 0) for x in (-1, 0, 1)] + [mull("mullionEnd", -2, 1), mull("mullionEnd", 2, -1)]
    return join(name, p)


# ---------------------------------------------------------------- UVs (siding + shingle only; all other faces (0,0))
def set_uvs(o, fn):
    """fn(material_name, face, loop) -> (u, v) or None for (0,0). Writes the single UV layer 'UVMap'."""
    bm = bmesh.new()
    bm.from_mesh(o.data)
    for k in list(bm.loops.layers.uv.keys()):
        bm.loops.layers.uv.remove(bm.loops.layers.uv[k])
    uvl = bm.loops.layers.uv.new("UVMap")
    names = [m.name for m in o.data.materials]
    for f in bm.faces:
        n = names[f.material_index]
        for l in f.loops:
            l[uvl].uv = fn(n, f, l) or (0.0, 0.0)
    bm.to_mesh(o.data)
    bm.free()


def siding_uv(name):
    gable = name.startswith("SM_ET_Gable")
    zoff = 4.0 if "Upper" in name else 0.0

    def fn(n, f, l):
        if n != SIDE:
            return None
        x, z = l.vert.co.x, l.vert.co.z
        if gable:
            return x / 4, .97 + .02 * z / GABLE_APEX[name]
        return (x + 2) / 4, (z + zoff) / 8
    return fn


def shingle_uv(name):
    O, e, d, sx, th, fold = SHINGLE_INFO[name]
    t = (sx * math.cos(th), math.sin(th))
    nr = (-sx * math.sin(th), math.cos(th))
    ualong = 1 if name == "SM_ET_Portico" else 0  # roof ridge along X; portico roof ridge along Y

    def sn(l):
        h = fold(l.vert.co.y if ualong == 0 else l.vert.co.x)
        r = (h - O[0], l.vert.co.z - O[1])
        return r[0] * t[0] + r[1] * t[1], r[0] * nr[0] + r[1] * nr[1]

    def fn(n, f, l):
        if n != SHING:
            return None
        ss = [sn(k)[0] for k in f.loops]
        s, nn = sn(l)
        v = .5 * s / e
        if max(ss) - min(ss) < 1e-6:  # butt face of a course (lies along the surface normal): V just below i*.5
            v -= .02 * (1 - nn / d)
        return (l.vert.co.x if ualong == 0 else l.vert.co.y), v
    return fn


OUT.mkdir(parents=True, exist_ok=True)
PIECES = [wall_clapboard, wall_clapboard_upper, lambda: wall_window(False), lambda: wall_window(True), wall_door, door_leaf,
          lambda: corner(False), lambda: corner(True),
          cornice_4m, cornice_corner, roof_4m, roof_gable_end, gable_clapboard, portico,
          lambda: roof_4m(8.0, "SM_ET_Roof_Shingle_Span16_4m"), lambda: roof_gable_end(8.0, "SM_ET_Roof_Shingle_Span16_GableEnd_4m"),
          lambda: gable_clapboard(8.0), wall_block, wall_block_door, wall_block_utility, wall_rollup, block_pilaster, block_pilaster_parapet, parapet_band,
          lambda: diner(False), lambda: diner(True)]
pieces = [f() for f in PIECES]
for o in pieces:
    if o.name in SHINGLE_INFO:
        set_uvs(o, shingle_uv(o.name))
    elif o.name.startswith("SM_ET_Wall_Clapboard") or o.name.startswith("SM_ET_Gable"):
        set_uvs(o, siding_uv(o.name))
for i, o in enumerate(pieces):
    bb = bounds(o)
    print(o.name, "bounds x,y,z:", bb, "verts", len(o.data.vertices))
    if o.name.startswith("SM_ET_Wall") or o.name.startswith("SM_ET_Corner"):
        assert bb[2] == (0.0, 4.0), bb
    if o.name.startswith("SM_ET_Wall"):
        assert bb[0] == (-2.0, 2.0), bb
    exp_x = {"SM_ET_Cornice_4m": (-2.0, 2.0), "SM_ET_Roof_Shingle_4m": (-2.0, 2.0), "SM_ET_Roof_Shingle_GableEnd_4m": (-2.3, 2.0),
             "SM_ET_Gable_Clapboard_8m": (-4.0, 4.0), "SM_ET_Portico": (-2.26, 2.26), "SM_ET_Roof_Shingle_Span16_4m": (-2.0, 2.0),
             "SM_ET_Roof_Shingle_Span16_GableEnd_4m": (-2.3, 2.0), "SM_ET_Gable_Clapboard_16m": (-8.0, 8.0)}
    if o.name in exp_x:
        assert bb[0] == exp_x[o.name], bb
    if o.name.startswith("SM_ET_Roof_Shingle_Span16"):
        assert bb[1] == (-8.46, 8.46), bb
    if o.name == "SM_ET_Gable_Clapboard_16m":
        assert bb[2][0] == 0.0 and abs(bb[2][1] - 5.852) < .002 and bb[1] == (.02, .2), bb
    if o.name == "SM_ET_Portico":
        assert bb[2][1] < 4.9 and bb[1][0] == -2.6, bb
    if o.name == "SM_ET_Diner_Front_4m":
        assert bb[0] == (-2.0, 2.0) and bb[1] == (-.25, .25) and abs(bb[2][1] - 4.6) < .002, bb
    print(o.name, "open edges:", open_edges(o))
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    bpy.ops.export_scene.fbx(filepath=str(OUT / (o.name + ".fbx")), use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL",
                             axis_forward="-Z", axis_up="Y", bake_space_transform=True, mesh_smooth_type="FACE", add_leaf_bones=False, use_mesh_modifiers=True)
    o.location = (i * 6, 0, 0)  # display spacing only; exported at the origin above
BLEND.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(BLEND))
