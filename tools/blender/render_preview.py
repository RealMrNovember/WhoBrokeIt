"""Who Broke It? kit onizlemesi (Cycles, CPU, ekransiz): GLB'leri izgara duzeninde dizip tek PNG olarak cizer.
  blender -b -P render_preview.py -- <cikis.png> <glb...> [--width 1600 --height 900 --samples 24 --row 20]
COL_ carpisma nesneleri cizilmez."""
import math
import sys

import bpy
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
out = argv[0]
files, W, H, S, ROW = [], 1600, 900, 24, 20.0
i = 1
while i < len(argv):
    if argv[i] == "--width":
        W = int(argv[i + 1]); i += 2
    elif argv[i] == "--height":
        H = int(argv[i + 1]); i += 2
    elif argv[i] == "--samples":
        S = int(argv[i + 1]); i += 2
    elif argv[i] == "--row":
        ROW = float(argv[i + 1]); i += 2
    else:
        files.append(argv[i]); i += 1

bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
x = y = 0.0
rowh = 0.0
maxx = 0.0
GAP = 0.45
for f in sorted(files):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=f)
    new = [o for o in bpy.data.objects if o not in before]
    body = [o for o in new if o.type == "MESH" and not o.name.startswith("COL_")]
    for o in new:
        if o.name.startswith("COL_"):
            o.hide_render = True
            o.hide_viewport = True
    if not body:
        continue
    mn = Vector((1e9, 1e9, 1e9))
    mx = Vector((-1e9, -1e9, -1e9))
    for o in body:
        for v in o.data.vertices:
            w = o.matrix_world @ v.co
            for k in range(3):
                mn[k] = min(mn[k], w[k])
                mx[k] = max(mx[k], w[k])
    wd, dp = mx.x - mn.x, mx.y - mn.y
    if x + wd > ROW and x > 0:
        x = 0.0
        y -= rowh + GAP * 2
        rowh = 0.0
    off = Vector((x - mn.x, y - (mn.y + mx.y) / 2, 0))
    for o in new:
        if o.parent is None:
            o.location += off
    x += wd + GAP
    maxx = max(maxx, x)
    rowh = max(rowh, dp, 1.4)

bpy.ops.mesh.primitive_plane_add(size=1, location=(maxx / 2, y / 2 - 0.5, -0.001))
fl = bpy.context.object
fl.scale = (maxx + 8, abs(y) + 12, 1)
m = bpy.data.materials.new("floor")
m.use_nodes = True
m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.62, 0.50, 0.34, 1)
m.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.95
fl.data.materials.append(m)
sun = bpy.data.lights.new("sun", "SUN")
sun.energy = 4.5
sun.color = (1.0, 0.88, 0.70)
sun.angle = math.radians(6)
so = bpy.data.objects.new("sun", sun)
bpy.context.collection.objects.link(so)
so.rotation_euler = (math.radians(52), 0, math.radians(-35))
fill = bpy.data.lights.new("fill", "SUN")
fill.energy = 1.2
fill.color = (0.65, 0.8, 1.0)
fo = bpy.data.objects.new("fill", fill)
bpy.context.collection.objects.link(fo)
fo.rotation_euler = (math.radians(70), 0, math.radians(150))
wd_ = bpy.data.worlds.new("w")
wd_.use_nodes = True
bg = wd_.node_tree.nodes["Background"]
bg.inputs["Color"].default_value = (0.52, 0.72, 0.95, 1)
bg.inputs["Strength"].default_value = 1.0
sc.world = wd_
cam = bpy.data.cameras.new("cam")
cam.lens = 38
co = bpy.data.objects.new("cam", cam)
bpy.context.collection.objects.link(co)
ctr = Vector((maxx / 2, y / 2 - 0.5, 0.9))
span = max(maxx, abs(y) * 1.6 + 4)
co.location = ctr + Vector((0, -span * 0.95, span * 0.55))
co.rotation_euler = (ctr - co.location).to_track_quat("-Z", "Y").to_euler()
sc.camera = co
sc.render.engine = "CYCLES"
sc.cycles.device = "CPU"
sc.cycles.samples = S
sc.cycles.use_denoising = True
sc.render.resolution_x, sc.render.resolution_y = W, H
sc.render.filepath = out
sc.render.image_settings.file_format = "PNG"
try:
    sc.view_settings.view_transform = "Standard"
    sc.view_settings.look = "None"
except Exception:
    pass
bpy.ops.render.render(write_still=True)
print("[PREVIEW]", out)
