"""Who Broke It? Blender kit kutuphanesi (Blender 4.5, ekransiz).

Tek dogruluk kaynagi: spec/style.json (palet, butce, adlandirma, olcek). Bu modul:
  - palet malzemelerini adla uretir (M_*), palet atlasi (8x8) ve atlas PNG'si,
  - bmesh ile "tombul" (kenar pahli) kutu/silindir/dilim parcalari kurar, hafif elle yapilmis kusurlar ekler,
  - her yuze palet dilimi UV'si verir (UV 0..1 icinde, sifir alanli yuz yok),
  - tabani orijine oturtur (taban merkezi), COL_* carpisma kutulari ekler,
  - GLB'yi (Y yukari) disa aktarir.
Calistirma: bu dosya dogrudan calismaz; generate_kit.py icinden kullanilir.
"""
import json
import math
import os
import random

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

HERE = os.path.dirname(os.path.abspath(__file__))


def load_style(path=None):
    with open(path or os.path.join(HERE, "spec", "style.json"), encoding="utf-8") as f:
        return json.load(f)


STYLE = load_style()
PALETTE = STYLE["palette"]["materials"]
PAL_NAMES = list(PALETTE.keys())          # sira = atlas dilimi sirasi
ATLAS_GRID = 8                            # 8x8 dilim, 256 px atlas


# ---------------------------------------------------------------- renk / malzeme
def srgb_to_lin(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def hex_to_lin(h):
    h = h.lstrip("#")
    return tuple(srgb_to_lin(int(h[i:i + 2], 16) / 255.0) for i in (0, 2, 4))


def get_mat(name):
    """Palet malzemesi (yoksa olusturur). Yalnizca style.json'daki adlar."""
    m = bpy.data.materials.get(name)
    if m is not None:
        return m
    spec = PALETTE[name]
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    r, g, b = hex_to_lin(spec["color"])
    bsdf.inputs["Base Color"].default_value = (r, g, b, 1.0)
    bsdf.inputs["Roughness"].default_value = spec["roughness"]
    bsdf.inputs["Metallic"].default_value = spec["metallic"]
    if "alpha" in spec:
        bsdf.inputs["Alpha"].default_value = spec["alpha"]
        try:
            m.surface_render_method = "BLENDED"
        except Exception:
            pass
    if "emissive" in spec:
        bsdf.inputs["Emission Color"].default_value = (r, g, b, 1.0)
        bsdf.inputs["Emission Strength"].default_value = spec["emissive"]
    m.diffuse_color = (r, g, b, 1.0)
    return m


def write_palette_png(path, size=256):
    """Palet atlasi: 8x8 dilim, dilim sirasi = PAL_NAMES. (Unity'de tek paylasilan malzeme dokusu.)"""
    img = bpy.data.images.new("WBI_Palette", size, size, alpha=False)
    img.colorspace_settings.name = "sRGB"
    cell = size // ATLAS_GRID
    px = [0.0] * (size * size * 4)
    for y in range(size):
        for x in range(size):
            # Blender goruntu y=0 altta: UV (v=1-...) ile uyumlu olmasi icin satir ters cevrilir
            row = (size - 1 - y) // cell
            idx = row * ATLAS_GRID + (x // cell)
            if idx < len(PAL_NAMES):
                c = hex_to_lin(PALETTE[PAL_NAMES[idx]]["color"])
            else:
                c = (0.05, 0.05, 0.05)
            o = (y * size + x) * 4
            px[o:o + 4] = [c[0], c[1], c[2], 1.0]
    img.pixels.foreach_set(px)
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()
    bpy.data.images.remove(img)


# ---------------------------------------------------------------- parca / varlik
class Part:
    """Tek mesh nesnesi (bir bmesh + malzeme sirasi)."""

    def __init__(self, name):
        self.name = name
        self.bm = bmesh.new()
        self.mats = []
        self.loc = Vector((0, 0, 0))
        self.pivot = False       # True: nesne orijini parca merkezi olur (kirik parcalar, ayri kopan tahtalar)

    def mi(self, mat):
        if mat not in PALETTE:
            raise KeyError("palette yok: " + mat)
        if mat not in self.mats:
            self.mats.append(mat)
        return self.mats.index(mat)


class Asset:
    def __init__(self, name, seed=1):
        self.name = name
        self.rng = random.Random(seed)
        self.parts = [Part(name)]
        self.cur = self.parts[0]
        self.cols = []          # (merkez, boyut) -- normalizasyondan once koordinatlarda
        self.chunk_mode = False

    # -- parca yonetimi
    def new_part(self, name, pivot=False):
        p = Part(name)
        p.pivot = pivot
        self.parts.append(p)
        self.cur = p
        return p

    # -- yardimcilar
    def jit(self, amt):
        return self.rng.uniform(-amt, amt)

    def _finish_geom(self, verts, mat, bevel_edges_w):
        p = self.cur
        mi = p.mi(mat)
        faces = {f for v in verts for f in v.link_faces}
        for f in faces:
            f.material_index = mi
        if bevel_edges_w > 0:
            edges = {e for v in verts for e in v.link_edges}
            bmesh.ops.bevel(p.bm, geom=list(edges), offset=bevel_edges_w, offset_type="OFFSET",
                            segments=1, profile=0.5, affect="EDGES", clamp_overlap=True)

    def box(self, center, size, mat, bevel=0.015, rot=(0, 0, 0), jitter_deg=0.0):
        """Kutu. center/size metre (Blender: X genislik, Y derinlik, Z yukseklik). rot derece."""
        c = Vector(center) + Vector((self.jit(0.0015), self.jit(0.0015), self.jit(0.0015)))   # ayni koseyi paylasmayi onler
        r = Euler([math.radians(a + (self.jit(jitter_deg) if jitter_deg else 0.0)) for a in rot], "XYZ")
        m = Matrix.Translation(c) @ r.to_matrix().to_4x4() @ Matrix.Diagonal((size[0], size[1], size[2], 1.0))
        res = bmesh.ops.create_cube(self.cur.bm, size=1.0, matrix=m)
        bw = min(bevel, min(size) * 0.3)
        self._finish_geom(res["verts"], mat, bw)

    def cyl(self, center, r1, r2, h, mat, seg=16, bevel=0.0, rot=(0, 0, 0)):
        """Koni/silindir (kapaklar ucgen yelpazesi: n-gon yok)."""
        c = Vector(center) + Vector((self.jit(0.0015), self.jit(0.0015), self.jit(0.0015)))
        r = Euler([math.radians(a) for a in rot], "XYZ")
        m = Matrix.Translation(c) @ r.to_matrix().to_4x4()
        res = bmesh.ops.create_cone(self.cur.bm, cap_ends=True, cap_tris=True, segments=seg, radius1=r1, radius2=r2,
                                    depth=h, matrix=m)
        self._finish_geom(res["verts"], mat, min(bevel, h * 0.3, min(r1, r2) * 0.3) if bevel > 0 else 0)

    def sector(self, center, r, z0, z1, a0, a1, n, mat_out, mat_in):
        """Silindir dilimi (tam kapali kati): varil parcalari icin. Acilar derece, yaricap r, eksen = center'dan Z."""
        bm = self.cur.bm
        mo, mi_ = self.cur.mi(mat_out), self.cur.mi(mat_in)
        cx, cy, _ = center
        c0 = bm.verts.new((cx, cy, z0))
        c1 = bm.verts.new((cx, cy, z1))
        ring0, ring1 = [], []
        for i in range(n + 1):
            a = math.radians(a0 + (a1 - a0) * i / n)
            x, y = cx + math.cos(a) * r, cy + math.sin(a) * r
            ring0.append(bm.verts.new((x, y, z0)))
            ring1.append(bm.verts.new((x, y, z1)))
        faces = []
        for i in range(n):
            faces.append((bm.faces.new((c1, ring1[i], ring1[i + 1])), mi_))            # ust
            faces.append((bm.faces.new((c0, ring0[i + 1], ring0[i])), mi_))            # alt
            faces.append((bm.faces.new((ring0[i], ring0[i + 1], ring1[i + 1], ring1[i])), mo))   # dis
        faces.append((bm.faces.new((c0, ring0[0], ring1[0], c1)), mi_))                # radyal 1
        faces.append((bm.faces.new((c0, c1, ring1[n], ring0[n])), mi_))                # radyal 2
        for f, mi in faces:
            f.material_index = mi
        bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))


    # -- ek geometri (hepsi KAPALI kabuk; kose titresimi komsu kabuklarla kaynamayi onler)
    def _vtx(self, p):
        return self.cur.bm.verts.new((p[0] + self.jit(0.0015), p[1] + self.jit(0.0015), p[2] + self.jit(0.0015)))

    def _face(self, vs, mi):
        f = self.cur.bm.faces.new(vs)
        f.material_index = mi
        return f

    def hexa(self, pts, mat):
        """Alt 4 + ust 4 kose (alt: saat yonunun tersi, ust: ayni sira) -> kama, kesik piramit, egimli kutu."""
        mi = self.cur.mi(mat)
        v = [self._vtx(p) for p in pts]
        for q in ((0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)):
            self._face([v[i] for i in q], mi)

    def wedge(self, x0, x1, y0, y1, z0, h0, h1, mat):
        """Y boyunca yukseklik h0 (y0'da) -> h1 (y1'de) degisen rampa kutusu (en az 2 cm: dejenere yuz olmasin)."""
        h0, h1 = max(h0, 0.02), max(h1, 0.02)
        self.hexa([(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0),
                   (x0, y0, z0 + h0), (x1, y0, z0 + h0), (x1, y1, z0 + h1), (x0, y1, z0 + h1)], mat)

    def pyramid(self, cx, cy, z0, w, d, h, mat, apex=None):
        mi = self.cur.mi(mat)
        ax, ay = (apex if apex else (cx, cy))
        b = [self._vtx((cx - w / 2, cy - d / 2, z0)), self._vtx((cx + w / 2, cy - d / 2, z0)),
             self._vtx((cx + w / 2, cy + d / 2, z0)), self._vtx((cx - w / 2, cy + d / 2, z0))]
        t = self._vtx((ax, ay, z0 + h))
        self._face([b[0], b[3], b[2], b[1]], mi)
        for i in range(4):
            self._face([b[i], b[(i + 1) % 4], t], mi)

    def prism(self, cx, cy, z0, w, d, h, mat):
        """Ucgen kesitli cati: sirt X boyunca, genislik w (X), derinlik d (Y), yukseklik h."""
        mi = self.cur.mi(mat)
        x0, x1, y0, y1 = cx - w / 2, cx + w / 2, cy - d / 2, cy + d / 2
        ym = cy
        v = [self._vtx((x0, y0, z0)), self._vtx((x1, y0, z0)), self._vtx((x1, y1, z0)), self._vtx((x0, y1, z0)),
             self._vtx((x0, ym, z0 + h)), self._vtx((x1, ym, z0 + h))]
        self._face([v[0], v[3], v[2], v[1]], mi)
        self._face([v[0], v[1], v[5], v[4]], mi)
        self._face([v[3], v[4], v[5], v[2]], mi)
        self._face([v[0], v[4], v[3]], mi)
        self._face([v[1], v[2], v[5]], mi)

    def ball(self, center, r, mat, sub=2, scale=(1, 1, 1)):
        c = Vector(center) + Vector((self.jit(0.0015), self.jit(0.0015), self.jit(0.0015)))
        m = Matrix.Translation(c) @ Matrix.Diagonal((scale[0], scale[1], scale[2], 1.0))
        res = bmesh.ops.create_icosphere(self.cur.bm, subdivisions=sub, radius=r, matrix=m)
        mi = self.cur.mi(mat)
        for f in {f for v in res["verts"] for f in v.link_faces}:
            f.material_index = mi

    def frustum(self, c, rx0, ry0, rx1, ry1, h, mat, seg=16):
        """Eliptik kesik koni (kapali, kapaklar ucgen yelpazesi). Taban z=c.z, tepe z=c.z+h."""
        mi = self.cur.mi(mat)
        cx, cy, cz = c
        b = [self._vtx((cx + math.cos(2 * math.pi * i / seg) * rx0, cy + math.sin(2 * math.pi * i / seg) * ry0, cz)) for i in range(seg)]
        t = [self._vtx((cx + math.cos(2 * math.pi * i / seg) * rx1, cy + math.sin(2 * math.pi * i / seg) * ry1, cz + h)) for i in range(seg)]
        cb, ct = self._vtx((cx, cy, cz)), self._vtx((cx, cy, cz + h))
        for i in range(seg):
            j = (i + 1) % seg
            self._face([b[i], b[j], t[j], t[i]], mi)
            self._face([cb, b[j], b[i]], mi)
            self._face([ct, t[i], t[j]], mi)

    def annulus(self, c, r_in, r_out, z0, z1, mat, seg=48):
        """Halka (ortasi delik), kapali."""
        mi = self.cur.mi(mat)
        cx, cy = c

        def ring(r, z):
            return [self._vtx((cx + math.cos(2 * math.pi * i / seg) * r, cy + math.sin(2 * math.pi * i / seg) * r, z)) for i in range(seg)]
        ob, ot, ib, it = ring(r_out, z0), ring(r_out, z1), ring(r_in, z0), ring(r_in, z1)
        for i in range(seg):
            j = (i + 1) % seg
            self._face([ob[i], ob[j], ot[j], ot[i]], mi)
            self._face([ib[j], ib[i], it[i], it[j]], mi)
            self._face([ot[i], ot[j], it[j], it[i]], mi)
            self._face([ob[j], ob[i], ib[i], ib[j]], mi)

    def strut(self, p0, p1, t, mat, bevel=0.0):
        """p0 -> p1 arasinda kalinligi t olan cubuk/kiris (egik olabilir)."""
        a0, a1 = Vector(p0), Vector(p1)
        d = a1 - a0
        L = d.length
        q = Vector((0, 0, 1)).rotation_difference(d.normalized())
        e = q.to_euler("XYZ")
        self.box((a0 + a1) / 2, (t, t, L), mat, bevel=bevel, rot=(math.degrees(e.x), math.degrees(e.y), math.degrees(e.z)))

    def solid_box(self, x0, x1, y0, y1, z0, z1, mat, bevel=0.0):
        self.box(((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2), (x1 - x0, y1 - y0, z1 - z0), mat, bevel=bevel)

    def wall_open(self, x0, x1, y, th, z0, z1, mat, openings=(), bevel=0.0):
        """Duvar x0..x1 / z0..z1; openings = (xa, xb, za, zb) dikdortgen bosluklar (cakismaz)."""
        xs = sorted(openings, key=lambda o: o[0])
        cur = x0
        for (xa, xb, za, zb) in xs:
            if xa > cur + 1e-4:
                self.solid_box(cur, xa, y - th / 2, y + th / 2, z0, z1, mat, bevel)
            if za > z0 + 1e-4:
                self.solid_box(xa, xb, y - th / 2, y + th / 2, z0, za, mat, bevel)
            if zb < z1 - 1e-4:
                self.solid_box(xa, xb, y - th / 2, y + th / 2, zb, z1, mat, bevel)
            cur = xb
        if x1 > cur + 1e-4:
            self.solid_box(cur, x1, y - th / 2, y + th / 2, z0, z1, mat, bevel)

    def col_rot(self, center, size, rot):
        self.cols.append((Vector(center), Vector(size), tuple(rot)))

    def col_frustum(self, c, rx0, ry0, rx1, ry1, h, seg=12):
        self.cols.append(("frustum", Vector(c), rx0, ry0, rx1, ry1, h, seg))

    def col(self, center, size):
        self.cols.append((Vector(center), Vector(size), (0, 0, 0)))

    # -- bitirme
    def _transform_part(self, p, offset, pivot):
        """Parcayi offset kadar oteler; pivot=True ise nesne orijini parca merkezi olur."""
        bmesh.ops.translate(p.bm, verts=p.bm.verts, vec=offset)
        if pivot and p.bm.verts:
            ctr = sum((v.co for v in p.bm.verts), Vector()) / len(p.bm.verts)
            bmesh.ops.translate(p.bm, verts=p.bm.verts, vec=-ctr)
            p.loc = ctr
        else:
            p.loc = Vector((0, 0, 0))

    def build(self, pivot_chunks=False, center_xy=True):
        """Nesneleri olusturur ve sahneye baglar. Dondurur: nesne listesi (govde + COL_*)."""
        # genel sinir kutusu (dunya)
        mn = Vector((1e9, 1e9, 1e9))
        mx = Vector((-1e9, -1e9, -1e9))
        for p in self.parts:
            for v in p.bm.verts:
                for i in range(3):
                    mn[i] = min(mn[i], v.co[i])
                    mx[i] = max(mx[i], v.co[i])
        off = Vector((-(mn.x + mx.x) / 2 if center_xy else 0, -(mn.y + mx.y) / 2 if center_xy else 0, -mn.z))
        objs = []
        total_tris = 0
        for p in self.parts:
            bmesh.ops.recalc_face_normals(p.bm, faces=list(p.bm.faces))
            self._transform_part(p, off, pivot_chunks or p.pivot)
            swatch_uvs(p)
            total_tris += sum(len(f.verts) - 2 for f in p.bm.faces)
            me = bpy.data.meshes.new(p.name)
            p.bm.to_mesh(me)
            p.bm.free()
            for mname in p.mats:
                me.materials.append(get_mat(mname))
            for poly in me.polygons:
                poly.use_smooth = False
            ob = bpy.data.objects.new(p.name, me)
            ob.location = p.loc
            bpy.context.collection.objects.link(ob)
            objs.append(ob)
        for i, ent in enumerate(self.cols, 1):
            if ent[0] == "frustum":
                _, c, rx0, ry0, rx1, ry1, h, seg = ent
                objs.append(make_frustum_collider("COL_%s_%d" % (self.name, i), c + off, rx0, ry0, rx1, ry1, h, seg))
            else:
                c, s, rot = ent
                objs.append(make_collider("COL_%s_%d" % (self.name, i), c + off, s, rot))
        self.tris = total_tris
        return objs


def swatch_uvs(part):
    """Her yuze palet dilimi icinde kucuk, bozulmamis bir UV (yuz sekli korunur): 0..1 icinde, sifir alan yok."""
    bm = part.bm
    uv = bm.loops.layers.uv.verify()
    cell = 1.0 / ATLAS_GRID
    for f in bm.faces:
        name = part.mats[f.material_index]
        i = PAL_NAMES.index(name)
        cx = (i % ATLAS_GRID + 0.5) * cell
        cy = 1.0 - ((i // ATLAS_GRID) + 0.5) * cell
        n = f.normal
        e = f.verts[1].co - f.verts[0].co
        t = e - n * e.dot(n)
        if t.length < 1e-9:
            t = Vector((1, 0, 0))
        t.normalize()
        b = n.cross(t)
        c = f.calc_center_median()
        ext = 1e-6
        for l in f.loops:
            d = l.vert.co - c
            ext = max(ext, abs(d.dot(t)), abs(d.dot(b)))
        k = 0.35 * cell / ext
        for l in f.loops:
            d = l.vert.co - c
            l[uv].uv = (cx + d.dot(t) * k, cy + d.dot(b) * k)


def make_frustum_collider(name, center, rx0, ry0, rx1, ry1, h, seg):
    """Disbukey kesik koni collider (kapali, <=32 kose)."""
    bm = bmesh.new()
    cx, cy, cz = center
    b = [bm.verts.new((cx + math.cos(2 * math.pi * i / seg) * rx0, cy + math.sin(2 * math.pi * i / seg) * ry0, cz)) for i in range(seg)]
    t = [bm.verts.new((cx + math.cos(2 * math.pi * i / seg) * rx1, cy + math.sin(2 * math.pi * i / seg) * ry1, cz + h)) for i in range(seg)]
    for i in range(seg):
        j = (i + 1) % seg
        bm.faces.new((b[i], b[j], t[j], t[i]))
    bm.faces.new(list(reversed(b)))
    bm.faces.new(t)
    bmesh.ops.triangulate(bm, faces=list(bm.faces))
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    return ob


def make_collider(name, center, size, rot=(0, 0, 0)):
    bm = bmesh.new()
    r = Euler([math.radians(a) for a in rot], "XYZ").to_matrix().to_4x4()
    bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation(center) @ r @ Matrix.Diagonal((size[0], size[1], size[2], 1.0)))
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    return ob


# ---------------------------------------------------------------- disa aktarma
def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def export_glb(objs, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    kw = dict(filepath=path, export_format="GLB", use_selection=True, export_yup=True, export_apply=True,
              export_cameras=False, export_lights=False, export_image_format="NONE", export_materials="EXPORT")
    try:
        bpy.ops.export_scene.gltf(**kw)
    except TypeError:
        kw.pop("export_image_format")
        bpy.ops.export_scene.gltf(**kw)


def triangle_count(objs):
    n = 0
    for o in objs:
        if o.name.startswith("COL_") or o.type != "MESH":
            continue
        n += sum(len(p.vertices) - 2 for p in o.data.polygons)
    return n


def ngon_count(objs):
    n = 0
    for o in objs:
        if o.type == "MESH":
            n += sum(1 for p in o.data.polygons if len(p.vertices) > 4)
    return n
