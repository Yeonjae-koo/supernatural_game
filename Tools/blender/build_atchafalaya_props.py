"""Builds the CH02 Atchafalaya prop kit (pipe run, pipe valve, cage lamp, tin roof gable), exports FBXs to
Assets/_Game/Art/Models/Atchafalaya/ and saves ArtSource/Atchafalaya/KIT_Atchafalaya_props.blend.

Run from the project root:
  /Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup --python Tools/blender/build_atchafalaya_props.py

Coordinates: Z up, FRONT = -Y (Unity +Z), width along X. Materials are matched by name in Unity.
"""
import math
from pathlib import Path

import bpy
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets/_Game/Art/Models/Atchafalaya"
BLEND = ROOT / "ArtSource/Atchafalaya/KIT_Atchafalaya_props.blend"

bpy.ops.wm.read_homefile(use_empty=True)
scene = bpy.context.scene
kit = bpy.data.collections.new("KIT")
scene.collection.children.link(kit)
bpy.context.view_layer.active_layer_collection = bpy.context.view_layer.layer_collection.children["KIT"]

MATS = {}
for _n, _c in (("CH02 Iron", (.18, .17, .16, 1)), ("Siding boards", (.55, .5, .42, 1)), ("Window glow", (1, .75, .35, 1))):
    MATS[_n] = bpy.data.materials.new(_n)
    MATS[_n].diffuse_color = _c
I, SID, GLOW = "CH02 Iron", "Siding boards", "Window glow"


def _finish(o, name, mat):
    o.name = name
    o.data.materials.append(MATS[mat])
    return o


def box(name, mn, mx, mat, bevel=0, xf=None):
    mn, mx = Vector(mn), Vector(mx)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(mn + mx) / 2)
    o = bpy.context.object
    o.scale = mx - mn
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        m = o.modifiers.new("Bevel", "BEVEL")
        m.width, m.segments = bevel, 1
    if xf is not None:
        o.matrix_world = xf @ o.matrix_world
    return _finish(o, name, mat)


def cyl(name, r, p0, p1, mat, verts=16):
    p0, p1 = Vector(p0), Vector(p1)
    d = p1 - p0
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=d.length)
    o = bpy.context.object
    o.rotation_euler = Vector((0, 0, 1)).rotation_difference(d.normalized()).to_euler()
    o.location = (p0 + p1) / 2
    return _finish(o, name, mat)


def torus(name, R, r, loc, rot, mat):
    bpy.ops.mesh.primitive_torus_add(major_radius=R, minor_radius=r, major_segments=24, minor_segments=8, location=loc, rotation=rot)
    return _finish(bpy.context.object, name, mat)


def sphere(name, r, loc, mat):
    bpy.ops.mesh.primitive_uv_sphere_add(radius=r, segments=16, ring_count=8, location=loc)
    return _finish(bpy.context.object, name, mat)


def join(name, parts):
    bpy.ops.object.select_all(action="DESELECT")
    for o in parts:
        o.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for o in parts:
        bpy.context.view_layer.objects.active = o
        for m in list(o.modifiers):
            bpy.ops.object.modifier_apply(modifier=m.name)
    bpy.ops.object.join()
    o = bpy.context.object
    o.name = o.data.name = name
    for c in list(o.users_collection):
        c.objects.unlink(o)
    kit.objects.link(o)
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    return o


def pipe_run():
    p = [cyl("main", .14, (-2, -.32, 0), (2, -.32, 0), I), cyl("small", .07, (-2, -.22, -.38), (2, -.22, -.38), I)]
    for x in (-1.95, 1.95):
        p.append(cyl("fl", .19, (x - .03, -.32, 0), (x + .03, -.32, 0), I))
        p.append(cyl("fl", .10, (x - .03, -.22, -.38), (x + .03, -.22, -.38), I))
    for bx in (-1, 1):
        p.append(box("plate", (bx - .08, -.04, -.6), (bx + .08, 0, .25), I))
        p.append(box("arm", (bx - .03, -.48, -.18), (bx + .03, 0, -.14), I))
        p.append(box("arm2", (bx - .03, -.32, -.48), (bx + .03, 0, -.45), I))
    return join("SM_AT_PipeRun_4m", p)


def pipe_valve():
    p = [cyl("pipe", .11, (0, -.3, 0), (0, -.3, 4.2), I), cyl("flange", .2, (0, -.3, 0), (0, -.3, .06), I),
         sphere("elbow", .13, (0, -.3, 4.2), I), cyl("stub", .11, (0, -.3, 4.2), (0, 0, 4.2), I),
         box("body", (-.18, -.48, 1.15), (.18, -.12, 1.45), I), cyl("bonnet", .05, (0, -.48, 1.3), (0, -.68, 1.3), I),
         torus("wheel", .22, .025, (0, -.7, 1.3), (math.pi / 2, 0, 0), I),
         box("spoke", (-.22, -.71, 1.29), (.22, -.69, 1.31), I), box("spoke", (-.01, -.71, 1.08), (.01, -.69, 1.52), I)]
    for z in (2.2, 3.5):
        p.append(box("strap", (-.16, -.42, z), (.16, 0, z + .06), I))
    return join("SM_AT_PipeValve", p)


def cage_lamp():
    bpy.ops.mesh.primitive_cone_add(vertices=12, radius1=.22, radius2=.06, depth=.2, location=(0, -.5, .05))
    shade = _finish(bpy.context.object, "shade", I)
    p = [box("plate", (-.1, -.04, -.15), (.1, 0, .15), I), box("arm", (-.025, -.5, .05), (.025, 0, .1), I), shade,
         sphere("bulb", .07, (0, -.5, -.1), GLOW), torus("ring", .1, .01, (0, -.5, -.12), (0, 0, 0), I),
         torus("ring", .1, .01, (0, -.5, -.24), (0, 0, 0), I)]
    for a in (45, 135, 225, 315):
        c, s = .1 * math.cos(math.radians(a)), -.5 + .1 * math.sin(math.radians(a))
        p.append(box("bar", (c - .008, s - .008, -.26), (c + .008, s + .008, -.04), I))
    return join("SM_AT_CageLamp", p)


def tin_roof():
    p = []
    for s in (-1, 1):
        L = math.hypot(2.3, 1.37)
        ang = math.atan2(-1.37, s * 2.3)  # rotation about X taking +Y along the slope (eave direction)
        if math.cos(ang) < 0:  # the sheet normal (+Z) must point up/out; flip the long axis instead
            ang += math.pi
        mid = Vector((0, s * 1.15, (1.2 - .17) / 2))  # midpoint of eave (y=s*2.3, z=-.17) and ridge (y=0, z=1.2)
        xf = Matrix.Translation(mid) @ Matrix.Rotation(ang, 4, "X")
        lo, hi = sorted((xf @ Vector((0, e * L / 2, 0)) for e in (-1, 1)), key=lambda v: v.z)
        normal = xf.to_3x3() @ Vector((0, 0, 1))
        assert normal.z > 0 and abs(hi.y) < 1e-4 and abs(hi.z - 1.2) < 1e-4 and abs(lo.y - s * 2.3) < 1e-4 and abs(lo.z + .17) < 1e-4, (s, normal, hi, lo)
        p.append(box("sheet", (-2.3, -L / 2, -.015), (2.3, L / 2, .015), I, xf=xf))
        for k in range(23):
            x = -2.2 + k * .2
            p.append(box("rib", (x - .02, -L / 2, .015), (x + .02, L / 2, .055), I, xf=xf))
    p.append(box("ridge", (-2.3, -.14, 1.15), (2.3, .14, 1.27), I))
    for x in (-2, 2):
        me = bpy.data.meshes.new("gable")
        me.from_pydata([(x + dx, y, z) for dx in (-.025, .025) for y, z in ((-2, 0), (2, 0), (0, 1.2))],
                       [], [(0, 1, 2), (3, 5, 4), (0, 3, 4, 1), (1, 4, 5, 2), (2, 5, 3, 0)])
        me.update()
        o = bpy.data.objects.new("gable", me)
        bpy.context.collection.objects.link(o)
        o.data.materials.append(MATS[SID])
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.select_all(action="DESELECT")
        o.select_set(True)
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.uv.cube_project(cube_size=2.0)
        bpy.ops.object.mode_set(mode="OBJECT")
        for l in o.data.uv_layers.active.data:  # rotate 90 deg about (.5,.5): (u,v) -> (1-v,u), as on SM_AT_Wall_Siding_4x4
            l.uv = (1 - l.uv.y, l.uv.x)
        p.append(o)
    return join("SM_AT_TinRoof_Gable", p)


def bounds(o):
    v = [o.matrix_world @ Vector(c) for c in o.bound_box]
    return tuple((round(min(c[i] for c in v), 3), round(max(c[i] for c in v), 3)) for i in range(3))


OUT.mkdir(parents=True, exist_ok=True)
pieces = [pipe_run(), pipe_valve(), cage_lamp(), tin_roof()]
for i, o in enumerate(pieces):
    print(o.name, "bounds x,y,z:", bounds(o), "verts", len(o.data.vertices))
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    bpy.ops.export_scene.fbx(filepath=str(OUT / (o.name + ".fbx")), use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL",
                             axis_forward="-Z", axis_up="Y", bake_space_transform=True, mesh_smooth_type="FACE", add_leaf_bones=False, use_mesh_modifiers=True)
    o.location = (i * 6, 0, 0)  # display spacing only; exported at the origin above
BLEND.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(BLEND))
