"""Who Broke It? varlik doğrulayıcı (Blender 4.5, ekransız). GLB (veya .blend) dosyalarını style.json'a göre denetler.

  blender -b -P validate_asset.py -- a.glb b.glb ... [--report rapor.json] [--style style.json]

Kontroller (her biri PASS / WARN / FAIL):
  isim   : dosya adı kuralı (<kategori>_<ad>[_<varyant>], küçük harf), nesne adları, COL_ / _chunk_ adları, parça sayısı <= 8
  mesh   : manifold olmayan kenar, gevşek köşe/kenar, sıfır alanlı yüz, n-gon (yalnız .blend; glTF üçgenlenir)
  normal : ters yüzler (kapalı kabuk imzalı hacmi < 0), tutarsız sargı, uygulanmamış ölçek
  uv     : UV katmanı var, 0..1 içinde, sıfır alanlı UV yüzü yok
  malzeme: yalnız palet adları (M_*), slot sayısı bütçede, boş slot yok, renk paletle uyumlu (WARN)
  bütçe  : üçgen sayısı (COL_ hariç) kategori bütçesinde
  ölçek  : 1 birim = 1 m, taban z=0 (±2 cm), xy merkezde, kategori azami boyutu, bilinen varlıklar için beklenen boyutlar
Çıkış kodu: herhangi bir FAIL varsa 1.
"""
import json
import math
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
import bmesh  # noqa: E402
import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

CATS = ["char", "prop", "bld", "fnc", "veh", "fx", "env"]
NAME_RE = re.compile(r"^(%s)_[a-z0-9]+(_[a-z0-9]+)*$" % "|".join(CATS))

# kategori -> (azami en büyük boyut m); belirli adlar için beklenen boyutlar (x, y, z) ± tol (Blender eksenleri)
MAX_EXTENT = {"prop_small": 1.6, "prop_medium": 3.2, "furniture": 3.2, "module": 3.2, "wall": 3.2, "building": 40.0,
              "vehicle": 8.0, "character": 2.2, "default": 10.0}
EXPECT = [
    (r"^bld_wall_", {"x": (3.0, 0.1), "y": (0.3, 0.1), "z": (3.0, 0.1)}),
    (r"^bld_door_", {"x": (0.92, 0.1), "z": (2.1, 0.1)}),
    (r"^fnc_panel_a$", {"x": (2.0, 0.1), "z": (1.1, 0.1)}),
    (r"^prop_pallet_", {"x": (1.2, 0.08), "y": (0.8, 0.08), "z": (0.13, 0.05)}),
    (r"^prop_barrel_a$", {"z": (0.9, 0.06)}),
    (r"^prop_crate_a$", {"x": (0.8, 0.06), "y": (0.8, 0.06), "z": (0.8, 0.06)}),
]


def load_style(path=None):
    with open(path or os.path.join(HERE, "spec", "style.json"), encoding="utf-8") as f:
        return json.load(f)


def srgb_to_lin(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def hex_to_lin(h):
    h = h.lstrip("#")
    return tuple(srgb_to_lin(int(h[i:i + 2], 16) / 255.0) for i in (0, 2, 4))


class Report:
    def __init__(self, name):
        self.name = name
        self.checks = []

    def add(self, group, status, detail=""):
        self.checks.append({"group": group, "status": status, "detail": detail})

    @property
    def failed(self):
        return any(c["status"] == "FAIL" for c in self.checks)


def category_of(stem, style):
    for key, spec in style["budgets"].items():
        if key.startswith("_"):
            continue
        for m in spec.get("match", []):
            if stem.startswith(m):
                return key, spec["maxTris"]
    d = style["budgets"]["default"]
    return "default", d["maxTris"]


def slot_limit(stem, style):
    lim = style["materialSlotsMax"]
    if stem.startswith("bld_building"):
        return lim["building"]
    if stem.startswith(("bld_", "fnc_")):
        return lim["module"]
    if stem.startswith("veh_"):
        return lim["vehicle"]
    if stem.startswith("char_"):
        return lim["character"]
    return lim["prop"]


def world_bbox(objs):
    mn = Vector((1e9, 1e9, 1e9))
    mx = Vector((-1e9, -1e9, -1e9))
    for o in objs:
        mw = o.matrix_world
        for v in o.data.vertices:
            w = mw @ v.co
            for i in range(3):
                mn[i] = min(mn[i], w[i])
                mx[i] = max(mx[i], w[i])
    return mn, mx


def islands(bm):
    seen = set()
    out = []
    for f in bm.faces:
        if f in seen:
            continue
        stack = [f]
        seen.add(f)
        comp = []
        while stack:
            cur = stack.pop()
            comp.append(cur)
            for e in cur.edges:
                for nf in e.link_faces:
                    if nf not in seen:
                        seen.add(nf)
                        stack.append(nf)
        out.append(comp)
    return out


def island_volume(faces):
    vol = 0.0
    for f in faces:
        vs = [l.vert.co for l in f.loops]
        for i in range(1, len(vs) - 1):
            vol += vs[0].dot(vs[i].cross(vs[i + 1])) / 6.0
    return vol


def analyze_mesh(o, is_glb):
    """Dondurur: dict(sayilar). Kaynak: kopya bmesh, glTF'nin boldugu koseler (duz golgeleme) kaynatilir."""
    bm = bmesh.new()
    bm.from_mesh(o.data)
    ngons = sum(1 for f in bm.faces if len(f.verts) > 4)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    nonman = sum(1 for e in bm.edges if not e.is_manifold)
    wire = sum(1 for e in bm.edges if e.is_wire)
    loose = sum(1 for v in bm.verts if not v.link_edges)
    zero = sum(1 for f in bm.faces if f.calc_area() < 1e-8)
    inconsistent = 0
    for e in bm.edges:
        if len(e.link_loops) == 2 and e.link_loops[0].vert == e.link_loops[1].vert:
            inconsistent += 1
    flipped = 0
    comps = islands(bm)
    for comp in comps:
        if island_volume(comp) < 0:
            flipped += 1
    bm.free()
    return dict(ngons=ngons, nonmanifold=nonman, wire=wire, loose=loose, zero_area=zero, inconsistent=inconsistent,
                flipped_islands=flipped, islands=len(comps))


def uv_stats(o):
    me = o.data
    layer = me.uv_layers.active
    if layer is None:
        return None
    out_of_range = 0
    zero = 0
    for p in me.polygons:
        pts = [layer.data[li].uv for li in p.loop_indices]
        for u in pts:
            if u[0] < -1e-4 or u[0] > 1 + 1e-4 or u[1] < -1e-4 or u[1] > 1 + 1e-4:
                out_of_range += 1
                break
        a = 0.0
        for i in range(len(pts)):
            x1, y1 = pts[i]
            x2, y2 = pts[(i + 1) % len(pts)]
            a += x1 * y2 - x2 * y1
        if abs(a) / 2 < 1e-9:
            zero += 1
    return dict(out_of_range=out_of_range, zero_area=zero)


def validate_scene(stem, is_glb, style, rep):
    pal = style["palette"]["materials"]
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    cols = [o for o in meshes if o.name.startswith("COL_")]
    body = [o for o in meshes if not o.name.startswith("COL_")]
    base = stem[:-7] if stem.endswith("_broken") else stem

    # ---- isim
    probs = []
    if not NAME_RE.match(stem):
        probs.append("dosya adi kurala uymuyor")
    if not body:
        probs.append("govde mesh'i yok")
    for o in body:
        if stem.endswith("_broken"):
            if not re.match(r"^%s_chunk_\d\d$" % re.escape(base), re.sub(r"\.\d+$", "", o.name)):
                probs.append("parca adi '%s' != %s_chunk_NN" % (o.name, base))
        elif re.sub(r"\.\d+$", "", o.name) != stem:
            probs.append("nesne adi '%s' != %s" % (o.name, stem))
    for o in cols:
        if not re.match(r"^COL_%s_\d+$" % re.escape(stem), re.sub(r"\.\d+$", "", o.name)):
            probs.append("collider adi '%s'" % o.name)
    if stem.endswith("_broken") and len(body) > 8:
        probs.append("%d parca (en fazla 8)" % len(body))
    rep.add("isim", "FAIL" if probs else "PASS", "; ".join(probs[:4]) or "%d govde, %d collider" % (len(body), len(cols)))
    if not body:
        return

    # ---- mesh / normal
    tot = dict(ngons=0, nonmanifold=0, wire=0, loose=0, zero_area=0, inconsistent=0, flipped_islands=0, islands=0)
    for o in body:
        s = analyze_mesh(o, is_glb)
        for k in tot:
            tot[k] += s[k]
    bad = [k for k in ("nonmanifold", "wire", "loose", "zero_area") if tot[k]]
    rep.add("mesh", "FAIL" if bad else "PASS",
            "kabuk=%d manifold-degil=%d gevsek-kenar=%d gevsek-kose=%d sifir-alan=%d" % (tot["islands"], tot["nonmanifold"], tot["wire"], tot["loose"], tot["zero_area"]))
    if is_glb:
        rep.add("ngon", "PASS", "n/a (glTF ucgenlenmis; uretimde generate_kit n-gon sayar)")
    else:
        rep.add("ngon", "FAIL" if tot["ngons"] else "PASS", "n-gon=%d" % tot["ngons"])
    nprob = []
    if tot["flipped_islands"]:
        nprob.append("%d kabuk ters (hacim<0)" % tot["flipped_islands"])
    if tot["inconsistent"]:
        nprob.append("%d kenarda tutarsiz sargi" % tot["inconsistent"])
    sc = [o.name for o in body if any(abs(c - 1.0) > 1e-4 for c in o.scale)]
    if sc:
        nprob.append("uygulanmamis olcek: " + ",".join(sc[:3]))
    rep.add("normal", "FAIL" if nprob else "PASS", "; ".join(nprob) or "dis yonlu, tutarli, olcek uygulanmis")

    # ---- uv
    uvp = []
    for o in body:
        st = uv_stats(o)
        if st is None:
            uvp.append("%s: UV katmani yok" % o.name)
        else:
            if st["out_of_range"]:
                uvp.append("%s: %d yuz 0..1 disinda" % (o.name, st["out_of_range"]))
            if st["zero_area"]:
                uvp.append("%s: %d sifir alanli UV yuzu" % (o.name, st["zero_area"]))
    rep.add("uv", "FAIL" if uvp else "PASS", "; ".join(uvp[:4]) or "UV var, 0..1, sifir alan yok")

    # ---- malzeme
    names = []
    mp = []
    warn = []
    for o in body:
        for i, slot in enumerate(o.material_slots):
            m = slot.material
            if m is None:
                mp.append("%s: bos slot %d" % (o.name, i))
                continue
            n = re.sub(r"\.\d+$", "", m.name)
            if n not in pal:
                mp.append("palet disi malzeme '%s'" % m.name)
            elif n not in names:
                names.append(n)
                bsdf = m.node_tree.nodes.get("Principled BSDF") if m.use_nodes and m.node_tree else None
                if bsdf is not None:
                    c = bsdf.inputs["Base Color"].default_value
                    want = hex_to_lin(pal[n]["color"])
                    if max(abs(c[0] - want[0]), abs(c[1] - want[1]), abs(c[2] - want[2])) > 0.02:
                        warn.append("%s rengi palette uymuyor" % n)
    lim = slot_limit(stem, style)
    if len(names) > lim:
        mp.append("%d malzeme > %d" % (len(names), lim))
    rep.add("malzeme", "FAIL" if mp else ("WARN" if warn else "PASS"), "; ".join((mp + warn)[:4]) or "%d palet malzemesi (limit %d)" % (len(names), lim))

    # ---- butce
    tris = 0
    for o in body:
        o.data.calc_loop_triangles()
        tris += len(o.data.loop_triangles)
    cat, budget = category_of(stem, style)
    rep.add("butce", "FAIL" if tris > budget else "PASS", "%d ucgen / %d (%s)" % (tris, budget, cat))

    # ---- olcek
    mn, mx = world_bbox(body)
    dim = mx - mn
    ctr = (mn + mx) / 2
    sp = []
    if abs(mn.z) > 0.02:
        sp.append("taban z=%.3f (0 olmali)" % mn.z)
    if abs(ctr.x) > 0.15 or abs(ctr.y) > 0.15:
        sp.append("xy merkezi (%.2f, %.2f)" % (ctr.x, ctr.y))
    mext = MAX_EXTENT.get(cat, 10.0)
    if max(dim) > mext:
        sp.append("en buyuk boyut %.2f > %.1f" % (max(dim), mext))
    if max(dim) < 0.15:
        sp.append("cok kucuk (%.2f m)" % max(dim))
    if not stem.endswith("_broken"):
        for rx, exp in EXPECT:
            if re.match(rx, stem):
                for ax, (val, tol) in exp.items():
                    d = dim["xyz".index(ax)]
                    if abs(d - val) > tol:
                        sp.append("%s=%.2f beklenen %.2f±%.2f" % (ax, d, val, tol))
    rep.add("olcek", "FAIL" if sp else "PASS", "; ".join(sp) or "boyut %.2f x %.2f x %.2f m, taban z=0" % (dim.x, dim.y, dim.z))
    rep.tris = tris
    rep.dim = (dim.x, dim.y, dim.z)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    files, report_path, style_path = [], None, None
    i = 0
    while i < len(argv):
        if argv[i] == "--report":
            report_path = argv[i + 1]; i += 2
        elif argv[i] == "--style":
            style_path = argv[i + 1]; i += 2
        else:
            files.append(argv[i]); i += 1
    style = load_style(style_path)
    reports = []
    for path in files:
        stem = os.path.splitext(os.path.basename(path))[0]
        rep = Report(stem)
        rep.path = path
        try:
            bpy.ops.wm.read_factory_settings(use_empty=True)
            is_glb = path.lower().endswith((".glb", ".gltf"))
            if is_glb:
                bpy.ops.import_scene.gltf(filepath=path)
            else:
                bpy.ops.wm.open_mainfile(filepath=path)
            validate_scene(stem, is_glb, style, rep)
        except Exception as ex:   # noqa: BLE001
            rep.add("calisma", "FAIL", "dogrulayici hatasi: %r" % ex)
        reports.append(rep)
        print("%s  %s" % ("FAIL" if rep.failed else "PASS", stem), "tris=%s" % getattr(rep, "tris", "?"))
        for c in rep.checks:
            if c["status"] != "PASS":
                print("     %-6s %-8s %s" % (c["status"], c["group"], c["detail"]))
    n_fail = sum(1 for r in reports if r.failed)
    print("[VALIDATE] %d dosya, %d gecti, %d kaldi" % (len(reports), len(reports) - n_fail, n_fail))
    if report_path:
        data = {"files": [{"name": r.name, "path": r.path, "passed": not r.failed, "tris": getattr(r, "tris", None),
                           "dim": getattr(r, "dim", None), "checks": r.checks} for r in reports],
                "failed": n_fail, "total": len(reports)}
        with open(report_path, "w", encoding="utf-8") as f:
            json.dump(data, f, ensure_ascii=False, indent=1)
    sys.stdout.flush()
    os._exit(1 if n_fail else 0)


main()
