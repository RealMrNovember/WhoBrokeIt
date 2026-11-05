"""Who Broke It? modüler kit üreticisi (prosedürel, Blender 4.5 ekransız).

  blender -b -P generate_kit.py -- --out <klasor> [--only ad1,ad2] [--selftest]

Çıktı: <klasor>/<ad>.glb (Y yukarı, 1 birim = 1 m, taban merkezi orijin) + WBI_Palette.png + manifest.json.
--selftest: doğrulayıcıyı sınamak için BİLEREK bozuk varlıklar üretir (<klasor>/selftest/bad_*.glb).
Yalnızca palet malzemeleri (style.json). Dış paket/indirme yok.
"""
import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import bmesh  # noqa: E402
import bpy  # noqa: E402
from mathutils import Euler, Vector  # noqa: E402

import kit_lib as K  # noqa: E402
from kit_lib import Asset  # noqa: E402

from kit_registry import REG, asset, meta_for  # noqa: E402


OPEN_H = 2.92      # duvar govdesi ust kotu (ustunde 0.08 sut)
PLINTH = 0.5


def wall_frame(a, x0, x1, z0=0.0, z1=OPEN_H, plinth=True):
    """Bir duvar parcasi (x0..x1, z0..z1): alt kisim beton kaide, ustu siva."""
    w = x1 - x0
    cx = (x0 + x1) / 2
    zb = max(z0, PLINTH)
    if plinth and z0 < PLINTH:
        ph = min(PLINTH, z1) - z0
        a.box((cx, 0, z0 + ph / 2), (w, 0.34, ph), "M_concrete", bevel=0.02)
    if z1 > zb:
        a.box((cx, 0, (zb + z1) / 2), (w, 0.30, z1 - zb), "M_plaster_cream", bevel=0.02)


def wall_cap(a):
    a.box((0, 0, 2.96), (3.04, 0.36, 0.08), "M_concrete", bevel=0.015)


@asset("bld_wall_plain_a", 11)
def wall_plain(a):
    wall_frame(a, -1.5, 1.5)
    wall_cap(a)
    a.col((0, 0, 1.5), (3.0, 0.3, 3.0))


@asset("bld_wall_door_a", 12)
def wall_door(a):
    wall_frame(a, -1.5, -0.5)
    wall_frame(a, 0.5, 1.5)
    a.box((0, 0, 2.56), (1.0, 0.30, 0.72), "M_plaster_cream", bevel=0.02)       # lintel (acikligin ustu, 2.2..2.92)
    wall_cap(a)
    for sx in (-1, 1):                                                         # kasa
        a.box((sx * 0.46, 0, 1.06), (0.08, 0.34, 2.12), "M_wood_dark", bevel=0.01)
    a.box((0, 0, 2.16), (1.0, 0.34, 0.08), "M_wood_dark", bevel=0.01)
    a.col((-1.0, 0, 1.5), (1.0, 0.3, 3.0))
    a.col((1.0, 0, 1.5), (1.0, 0.3, 3.0))
    a.col((0, 0, 2.6), (1.0, 0.3, 0.8))


@asset("bld_wall_window_a", 13)
def wall_window(a):
    wall_frame(a, -1.5, -0.6)
    wall_frame(a, 0.6, 1.5)
    wall_frame(a, -0.6, 0.6, 0.0, 0.9)                                          # pencere alti
    a.box((0, 0, 2.51), (1.2, 0.30, 0.82), "M_plaster_cream", bevel=0.02)       # pencere ustu (2.1..2.92)
    wall_cap(a)
    a.box((0, -0.02, 0.93), (1.36, 0.34, 0.06), "M_concrete", bevel=0.015)       # denizlik
    for sx in (-1, 1):
        a.box((sx * 0.56, 0, 1.49), (0.08, 0.34, 1.06), "M_wood_pale", bevel=0.01)
    a.box((0, 0, 2.06), (1.2, 0.34, 0.08), "M_wood_pale", bevel=0.01)
    a.col((-1.05, 0, 1.5), (0.9, 0.3, 3.0))
    a.col((1.05, 0, 1.5), (0.9, 0.3, 3.0))
    a.col((0, 0, 0.45), (1.2, 0.3, 0.9))
    a.col((0, 0, 2.55), (1.2, 0.3, 0.9))


@asset("bld_door_a", 21)
def door_a(a):
    a.box((0, 0, 1.05), (0.92, 0.06, 2.10), "M_wood_warm", bevel=0.012)
    a.box((0, -0.036, 1.55), (0.60, 0.02, 0.80), "M_wood_dark", bevel=0.008)
    a.box((0, -0.036, 0.55), (0.60, 0.02, 0.80), "M_wood_dark", bevel=0.008)
    a.box((0.34, -0.06, 1.0), (0.05, 0.05, 0.14), "M_metal_steel", bevel=0.008)
    a.col((0, 0, 1.05), (0.92, 0.06, 2.1))


@asset("bld_door_b", 22)
def door_b(a):
    a.box((0, 0, 1.05), (0.92, 0.06, 2.10), "M_paint_blue", bevel=0.012)
    a.box((0, -0.035, 1.55), (0.56, 0.02, 0.72), "M_glass", bevel=0.0)
    a.box((0, -0.036, 0.5), (0.60, 0.02, 0.70), "M_paint_blue", bevel=0.008)
    a.box((0.34, -0.06, 1.0), (0.05, 0.05, 0.14), "M_metal_steel", bevel=0.008)
    a.col((0, 0, 1.05), (0.92, 0.06, 2.1))


@asset("bld_window_a", 23)
def window_a(a):
    a.box((-0.55, 0, 0.6), (0.10, 0.10, 1.2), "M_wood_pale", bevel=0.01)
    a.box((0.55, 0, 0.6), (0.10, 0.10, 1.2), "M_wood_pale", bevel=0.01)
    a.box((0, 0, 0.05), (1.0, 0.10, 0.10), "M_wood_pale", bevel=0.01)
    a.box((0, 0, 1.15), (1.0, 0.10, 0.10), "M_wood_pale", bevel=0.01)
    a.box((0, 0, 0.6), (0.06, 0.08, 1.0), "M_wood_pale", bevel=0.008)
    a.box((0, 0, 0.6), (1.0, 0.08, 0.06), "M_wood_pale", bevel=0.008)
    a.box((0, 0, 0.6), (1.0, 0.02, 1.0), "M_glass", bevel=0.0)


def fence_parts(a):
    for sx in (-1, 1):
        a.box((sx * 0.95, 0, 0.55), (0.10, 0.10, 1.10), "M_wood_dark", bevel=0.012)


@asset("fnc_panel_a", 31)
def fence_a(a):
    fence_parts(a)
    for z in (0.30, 0.80):
        a.box((0, 0.03, z), (1.8, 0.04, 0.08), "M_wood_dark", bevel=0.01)
    for i in range(10):
        h = 0.92 + a.jit(0.03)
        a.box((-0.81 + i * 0.18, -0.025, 0.1 + h / 2), (0.12, 0.03, h), "M_wood_warm", bevel=0.008, jitter_deg=1.0)
    a.col((0, 0, 0.55), (2.0, 0.1, 1.1))


@asset("fnc_panel_a_broken", 32)
def fence_broken(a):
    a.chunk_mode = True
    a.cur.name = "fnc_panel_a_chunk_01"
    a.box((-0.95, 0, 0.55), (0.10, 0.10, 1.10), "M_wood_dark", bevel=0.012)                       # sol direk ayakta
    a.new_part("fnc_panel_a_chunk_02")
    a.box((0.9, 0.2, 0.28), (0.10, 0.10, 0.55), "M_wood_dark", bevel=0.012, rot=(0, 24, 0))      # sag direk kirik
    a.new_part("fnc_panel_a_chunk_03")
    a.box((-0.55, 0.12, 0.30), (0.9, 0.04, 0.08), "M_wood_dark", bevel=0.01, rot=(0, -14, 8))    # ust ray yarimlari
    a.new_part("fnc_panel_a_chunk_04")
    a.box((0.45, 0.28, 0.06), (0.9, 0.04, 0.08), "M_wood_dark", bevel=0.01, rot=(0, 6, -20))
    xs = [-0.81, -0.63, -0.45, -0.27, -0.09, 0.09, 0.27, 0.45, 0.63, 0.81]
    groups = [xs[0:3], xs[3:5], xs[5:8], xs[8:10]]
    for gi, g in enumerate(groups):
        a.new_part("fnc_panel_a_chunk_%02d" % (5 + gi))
        for j, x in enumerate(g):
            h = 0.9 - 0.18 * gi
            a.box((x + gi * 0.05, -0.12 - 0.18 * gi, 0.06 + 0.02 * j), (0.12, 0.03, h), "M_wood_warm", bevel=0.008,
                  rot=(70 - 8 * gi, 0, 6 * (j - 1)))


@asset("prop_table_a", 41)
def table_a(a):
    a.box((0, 0, 0.77), (1.4, 0.8, 0.06), "M_wood_warm", bevel=0.02)
    for sx in (-1, 1):
        for sy in (-1, 1):
            a.box((sx * 0.62, sy * 0.32, 0.37), (0.07, 0.07, 0.74), "M_wood_dark", bevel=0.01)
    a.box((0, -0.32, 0.69), (1.2, 0.04, 0.08), "M_wood_dark", bevel=0.01)
    a.box((0, 0.32, 0.69), (1.2, 0.04, 0.08), "M_wood_dark", bevel=0.01)
    a.col((0, 0, 0.4), (1.4, 0.8, 0.8))


@asset("prop_workbench_a", 42)
def workbench_a(a):
    a.box((0, 0, 0.91), (1.8, 0.7, 0.08), "M_wood_pale", bevel=0.02)
    for sx in (-1, 1):
        for sy in (-1, 1):
            a.box((sx * 0.82, sy * 0.28, 0.435), (0.08, 0.08, 0.87), "M_metal_dark", bevel=0.01)
    a.box((0, 0, 0.25), (1.6, 0.55, 0.04), "M_wood_pale", bevel=0.012)
    a.box((0, 0.3, 0.6), (1.7, 0.03, 0.5), "M_wood_pale", bevel=0.01)
    a.box((0.7, -0.26, 1.02), (0.18, 0.14, 0.14), "M_paint_red", bevel=0.015)
    a.box((0.7, -0.35, 1.03), (0.16, 0.04, 0.10), "M_paint_red", bevel=0.01)
    a.col((0, 0, 0.48), (1.8, 0.7, 0.96))


@asset("prop_shelf_a", 43)
def shelf_a(a):
    for sx in (-1, 1):
        for sy in (-1, 1):
            a.box((sx * 0.57, sy * 0.17, 1.0), (0.05, 0.05, 2.0), "M_metal_steel", bevel=0.008)
    for z in (0.1, 0.55, 1.0, 1.45, 1.95):
        a.box((0, 0, z), (1.2, 0.4, 0.04), "M_wood_warm", bevel=0.01)
    a.col((0, 0, 1.0), (1.2, 0.4, 2.0))


@asset("prop_pallet_a", 51)
def pallet_a(a):
    for x in (-0.54, 0.0, 0.54):
        a.box((x, 0, 0.055), (0.12, 0.8, 0.10), "M_wood_warm", bevel=0.01)
    for i in range(5):
        a.box((0, -0.34 + i * 0.17, 0.117), (1.2, 0.12, 0.022), "M_wood_pale", bevel=0.006, jitter_deg=0.6)
    for y in (-0.35, 0.0, 0.35):
        a.box((0, y, 0.011), (1.2, 0.10, 0.022), "M_wood_pale", bevel=0.006)
    a.col((0, 0, 0.06), (1.2, 0.8, 0.12))


def barrel_rings(a):
    for z in (0.12, 0.45, 0.78):
        a.cyl((0, 0, z), 0.305, 0.305, 0.05, "M_metal_dark", seg=16)


@asset("prop_barrel_a", 61)
def barrel_a(a):
    a.cyl((0, 0, 0.225), 0.27, 0.30, 0.45, "M_paint_red", seg=16)
    a.cyl((0, 0, 0.675), 0.30, 0.27, 0.45, "M_paint_red", seg=16)
    barrel_rings(a)
    a.cyl((0, 0, 0.905), 0.22, 0.22, 0.03, "M_metal_steel", seg=16)
    a.cyl((0.08, 0.05, 0.93), 0.035, 0.035, 0.03, "M_metal_steel", seg=8)
    a.col((0, 0, 0.45), (0.6, 0.6, 0.9))


@asset("prop_barrel_a_broken", 62)
def barrel_broken(a):
    a.chunk_mode = True
    a.cur.name = "prop_barrel_a_chunk_01"
    idx = 0
    for hz, (z0, z1) in enumerate(((0.0, 0.45), (0.45, 0.9))):
        for q in range(4):
            idx += 1
            if idx > 1:
                a.new_part("prop_barrel_a_chunk_%02d" % idx)
            a0 = q * 90.0 + 8
            a1 = a0 + 82
            mid = math.radians((a0 + a1) / 2)
            push = 0.10 + 0.14 * a.rng.random() + 0.05 * hz
            cx, cy = math.cos(mid) * push, math.sin(mid) * push
            a.sector((cx, cy, 0), 0.29, z0 + 0.0, z1, a0, a1, 6, "M_paint_red", "M_metal_dark")
            # parcayi hafif devir: her parca kendi merkezinde donsun (pivot_chunks ile merkeze oturur)
            bm = a.cur.bm
            ctr = sum((v.co for v in bm.verts), Vector()) / len(bm.verts)
            bmesh.ops.rotate(bm, verts=bm.verts, cent=ctr, matrix=Euler(
                (math.radians(a.rng.uniform(-22, 22)), math.radians(a.rng.uniform(-22, 22)), math.radians(a.rng.uniform(-40, 40))), "XYZ").to_matrix())


@asset("prop_crate_a", 71)
def crate_a(a):
    a.box((0, 0, 0.4), (0.76, 0.76, 0.76), "M_wood_warm", bevel=0.02)
    for sx in (-1, 1):
        for sy in (-1, 1):
            a.box((sx * 0.35, sy * 0.35, 0.4), (0.10, 0.10, 0.80), "M_wood_dark", bevel=0.015)
    for z in (0.04, 0.76):
        for sy in (-1, 1):
            a.box((0, sy * 0.35, z), (0.60, 0.10, 0.08), "M_wood_dark", bevel=0.012)
        for sx in (-1, 1):
            a.box((sx * 0.35, 0, z), (0.10, 0.60, 0.08), "M_wood_dark", bevel=0.012)
    a.col((0, 0, 0.4), (0.8, 0.8, 0.8))


@asset("prop_crate_a_broken", 72)
def crate_broken(a):
    a.chunk_mode = True
    a.cur.name = "prop_crate_a_chunk_01"
    panels = [((-0.45, 0.0, 0.30), (0.05, 0.76, 0.62), (0, -35, 0)), ((0.45, 0.1, 0.12), (0.05, 0.76, 0.62), (0, 70, 12)),
              ((0.0, 0.45, 0.28), (0.76, 0.05, 0.62), (30, 0, 0)), ((0.0, -0.45, 0.10), (0.76, 0.05, 0.62), (-72, 0, 10))]
    for i, (c, s, r) in enumerate(panels):
        if i:
            a.new_part("prop_crate_a_chunk_%02d" % (i + 1))
        a.box(c, s, "M_wood_warm", bevel=0.012, rot=r)
    a.new_part("prop_crate_a_chunk_05")
    for sx, sy in ((-1, -1), (1, 1)):
        a.box((sx * 0.30, sy * 0.30, 0.05), (0.10, 0.10, 0.80), "M_wood_dark", bevel=0.012, rot=(80, 8 * sx, 20 * sy))
    a.new_part("prop_crate_a_chunk_06")
    for sx, sy in ((1, -1), (-1, 1)):
        a.box((sx * 0.36, sy * 0.40, 0.06), (0.10, 0.10, 0.60), "M_wood_dark", bevel=0.012, rot=(-76, 6 * sx, 40 * sy))
    a.new_part("prop_crate_a_chunk_07")
    a.box((0.1, 0.05, 0.05), (0.76, 0.76, 0.05), "M_wood_warm", bevel=0.012, rot=(8, 14, 20))      # kapak/taban
    a.new_part("prop_crate_a_chunk_08")
    a.box((-0.15, -0.2, 0.025), (0.60, 0.10, 0.08), "M_wood_dark", bevel=0.01, rot=(0, 0, 33))


@asset("prop_sawhorse_a", 81)
def sawhorse_a(a):
    a.box((0, 0, 0.77), (1.2, 0.08, 0.12), "M_wood_pale", bevel=0.01)
    for sx in (-1, 1):
        a.box((sx * 0.45, -0.12, 0.38), (0.07, 0.07, 0.80), "M_wood_pale", bevel=0.008, rot=(-18, 0, 0))
        a.box((sx * 0.45, 0.12, 0.38), (0.07, 0.07, 0.80), "M_wood_pale", bevel=0.008, rot=(18, 0, 0))
    for x in (-0.4, 0.0, 0.4):
        a.box((x, 0, 0.77), (0.15, 0.09, 0.125), "M_paint_yellow", bevel=0.008)
    a.col((0, 0, 0.4), (1.2, 0.5, 0.82))


@asset("prop_cone_a", 82)
def cone_a(a):
    a.box((0, 0, 0.02), (0.36, 0.36, 0.04), "M_rubber", bevel=0.01)
    a.cyl((0, 0, 0.32), 0.14, 0.03, 0.56, "M_paint_orange", seg=12)
    a.cyl((0, 0, 0.30), 0.105, 0.085, 0.10, "M_canvas", seg=12)
    a.col((0, 0, 0.3), (0.36, 0.36, 0.6))


@asset("prop_fusebox_a", 91)
def fusebox_a(a):
    a.box((0, 0, 0.35), (0.5, 0.2, 0.7), "M_metal_steel", bevel=0.015)
    a.box((0, -0.105, 0.35), (0.44, 0.02, 0.62), "M_paint_red", bevel=0.01)
    a.box((0.12, -0.13, 0.30), (0.05, 0.03, 0.20), "M_metal_steel", bevel=0.006)
    a.cyl((-0.13, -0.125, 0.55), 0.035, 0.035, 0.03, "M_light_warm", seg=12, rot=(90, 0, 0))
    a.col((0, 0, 0.35), (0.5, 0.2, 0.7))


@asset("prop_toolbox_a", 92)
def toolbox_a(a):
    a.box((0, 0, 0.11), (0.5, 0.22, 0.22), "M_paint_red", bevel=0.015)
    a.box((0, 0, 0.255), (0.52, 0.24, 0.07), "M_paint_red", bevel=0.012)
    for sx in (-1, 1):
        a.box((sx * 0.11, 0, 0.36), (0.02, 0.02, 0.14), "M_metal_steel", bevel=0.004)
    a.box((0, 0, 0.43), (0.24, 0.02, 0.02), "M_metal_steel", bevel=0.004)
    for sx in (-1, 1):
        a.box((sx * 0.18, -0.115, 0.22), (0.05, 0.02, 0.06), "M_metal_dark", bevel=0.006)
    a.col((0, 0, 0.23), (0.52, 0.24, 0.46))


# ---------------------------------------------------------------- self-test (bilerek bozuk)
def build_selftest(out):
    """Dogrulayicinin gercekten yakaladigini kanitlamak icin bozuk varliklar."""
    d = os.path.join(out, "selftest")
    os.makedirs(d, exist_ok=True)
    made = []

    def make(name, fn):
        K.reset_scene()
        a = Asset(name, 5)
        fn(a)
        objs = a.build()
        bad = getattr(a, "post", None)
        if bad:
            bad(objs)
        K.export_glb(objs, os.path.join(d, name + ".glb"))
        made.append(name)

    def flipped(a):
        a.box((0, 0, 0.4), (0.76, 0.76, 0.76), "M_wood_warm")
        a.post = lambda objs: [flip(o) for o in objs if not o.name.startswith("COL_")]

    def flip(o):
        bm = bmesh.new()
        bm.from_mesh(o.data)
        bmesh.ops.reverse_faces(bm, faces=bm.faces)
        bm.to_mesh(o.data)
        bm.free()

    def overbudget(a):
        for i in range(30):
            a.box((0, 0, 0.4 + i * 0.001), (0.6, 0.6, 0.6), "M_wood_warm", bevel=0.05)

    def bigscale(a):
        a.box((0, 0, 1.5), (3.0, 3.0, 3.0), "M_wood_warm")

    def badmat(a):
        a.box((0, 0, 0.4), (0.4, 0.4, 0.4), "M_wood_warm")
        a.post = lambda objs: rename_mat(objs)

    def rename_mat(objs):
        for o in objs:
            if o.type == "MESH" and o.data.materials:
                m = bpy.data.materials.new("Material")
                o.data.materials[0] = m

    def floating(a):
        a.box((0, 0, 0.4), (0.4, 0.4, 0.4), "M_wood_warm")
        a.post = lambda objs: [setattr(o, "location", (0, 0, 0.5)) for o in objs]

    make("prop_bad_flipped", flipped)
    make("prop_crate_overbudget", overbudget)
    make("prop_crate_bigscale", bigscale)
    make("prop_bad_material", badmat)
    make("prop_bad_floating", floating)
    return made


def bbox_dims(objs):
    mn = Vector((1e9, 1e9, 1e9))
    mx = Vector((-1e9, -1e9, -1e9))
    for o in objs:
        if o.type != "MESH" or o.name.startswith("COL_"):
            continue
        for v in o.data.vertices:
            w = v.co + o.location
            for i in range(3):
                mn[i] = min(mn[i], w[i])
                mx[i] = max(mx[i], w[i])
    d = mx - mn
    return (d.x, d.y, d.z)


import kit_greenfield  # noqa: E402,F401  (Greenfield Meydani varliklari; kayit defterine eklenir)


# ---------------------------------------------------------------- calistir
def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    out = "kit_out"
    only = None
    selftest = False
    i = 0
    while i < len(argv):
        if argv[i] == "--out":
            out = argv[i + 1]; i += 2
        elif argv[i] == "--only":
            only = set(argv[i + 1].split(",")); i += 2
        elif argv[i] == "--selftest":
            selftest = True; i += 1
        else:
            i += 1
    os.makedirs(out, exist_ok=True)
    if selftest:
        made = build_selftest(out)
        print("[KIT] selftest varliklari:", ",".join(made))
        return
    manifest = []
    for name, (fn, seed) in REG.items():
        if only and name not in only:
            continue
        K.reset_scene()
        a = Asset(name, seed)
        fn(a)
        objs = a.build(pivot_chunks=a.chunk_mode)
        ng = K.ngon_count(objs)
        path = os.path.join(out, name + ".glb")
        K.export_glb(objs, path)
        tris = K.triangle_count(objs)
        dims = bbox_dims(objs)
        entry = {"name": name, "category": name.split("_")[0], "tris": tris, "ngons": ng, "objects": [o.name for o in objs],
                 "bytes": os.path.getsize(path), "dims": [round(d, 3) for d in dims]}
        entry.update(meta_for(name))
        manifest.append(entry)
        print("[KIT] %-24s tris=%-5d ngons=%d nesne=%d %d bayt" % (name, tris, ng, len(objs), os.path.getsize(path)))
    K.reset_scene()
    K.write_palette_png(os.path.join(out, "WBI_Palette.png"))
    with open(os.path.join(out, "manifest.json"), "w", encoding="utf-8") as f:
        json.dump({"assets": manifest}, f, ensure_ascii=False, indent=1)
    print("[KIT] tamam: %d varlik -> %s" % (len(manifest), out))


main()
