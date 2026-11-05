"""Who Broke It? Greenfield Meydani varliklari (prosedurel, Blender 4.5 ekransiz). generate_kit.py tarafindan iceri alinir.

Konvansiyon: Blender X = genislik, Y = derinlik (on cephe -Y), Z = yukseklik. 1 birim = 1 m. Yalnizca palet malzemeleri.
Olusturulan her varlik build() ile tabani z=0'a, XY sinir kutusu merkezine oturur. Binalarda on cephe -Y'dir.
"""
import math

import bmesh
from mathutils import Euler, Vector

import kit_lib as K
from kit_registry import asset

CREAM = "M_plaster_cream"


def rz(x, y, ang):
    c, s = math.cos(math.radians(ang)), math.sin(math.radians(ang))
    return (x * c - y * s, x * s + y * c)


def bricks(a, x0, x1, y, z0, z1, th, m1, m2, bw=0.5, bh=0.2, skip=None):
    rows = max(1, int(round((z1 - z0) / bh)))
    rh = (z1 - z0) / rows
    n = 0
    for r in range(rows):
        x = x0 - (bw / 2 if r % 2 else 0)
        k = 0
        while x < x1 - 1e-3:
            xa, xb = max(x, x0), min(x + bw, x1)
            if xb - xa > 0.08 and not (skip and skip(r, k, rows)):
                a.box(((xa + xb) / 2, y, z0 + (r + 0.5) * rh), (xb - xa - 0.02, th, rh - 0.02), m1 if (k + r) % 5 else m2, bevel=0)
                n += 1
            x += bw
            k += 1
    return n


def mortar_lines(a, x0, x1, y, z0, z1, mat, every=0.4, th=0.02):
    z = z0 + every
    while z < z1 - 0.05:
        a.box(((x0 + x1) / 2, y, z), (x1 - x0, th, 0.025), mat, bevel=0)
        z += every


def window(a, cx, y, zc, w, h, frame, glass="M_glass", depth=0.12, mullions=True):
    """On cepheye yapisik pencere: cerceve + cam (cx, y on yuzey, zc merkez)."""
    t = 0.09
    a.box((cx, y, zc), (w - 0.02, 0.04, h - 0.02), glass, bevel=0)
    a.box((cx - w / 2 + t / 2, y - 0.02, zc), (t, depth, h), frame, bevel=0.008)
    a.box((cx + w / 2 - t / 2, y - 0.02, zc), (t, depth, h), frame, bevel=0.008)
    a.box((cx, y - 0.02, zc + h / 2 - t / 2), (w, depth, t), frame, bevel=0.008)
    a.box((cx, y - 0.02, zc - h / 2 + t / 2), (w, depth, t), frame, bevel=0.008)
    if mullions:
        a.box((cx, y - 0.03, zc), (0.05, depth * 0.8, h - t), frame, bevel=0.005)
        a.box((cx, y - 0.03, zc), (w - t, depth * 0.8, 0.05), frame, bevel=0.005)


def striped_awning(a, x0, x1, y_wall, z_top, drop, depth, m1, m2, n=8, valance=True):
    """On cephe tentesi: n serit (m1/m2 dönüşümlü), duvardan -Y'ye egimli."""
    w = (x1 - x0) / n
    th = math.atan2(drop, depth)
    L = math.hypot(depth, drop)
    for i in range(n):
        cx = x0 + (i + 0.5) * w
        a.box((cx, y_wall - depth / 2, z_top - drop / 2), (w - 0.01, L, 0.05), m1 if i % 2 == 0 else m2, bevel=0.004, rot=(math.degrees(th), 0, 0))
        if valance:
            a.box((cx, y_wall - depth - 0.02, z_top - drop - 0.12), (w - 0.01, 0.04, 0.26), m1 if i % 2 == 0 else m2, bevel=0.004)


# ======================================================================================== DUKKAN CEPHELERI
def shopfront(a, v):
    """8 m genis, 5 m derin, ~6.9 m yuksek, iki katli dukkan. v: a (firin) / b (hirdavat) / c (manav)."""
    cfg = {
        "a": dict(wall="M_plaster_pink", aw1="M_canvas", aw2="M_paint_red", extra="M_foliage"),
        "b": dict(wall="M_plaster_teal", aw1="M_canvas", aw2="M_paint_blue", extra="M_metal_steel"),
        "c": dict(wall="M_plaster_sand", aw1="M_canvas", aw2="M_paint_green", extra="M_paint_orange"),
    }[v]
    W, D = 8.0, 5.0
    yf = -D / 2 + 0.15                    # on duvar ekseni
    wall, trim, wood, roof = cfg["wall"], CREAM, "M_wood_dark", "M_roof_tile"
    Z1, Z2 = 3.2, 5.6                     # zemin kat tavani, saçak
    # ---- on duvar acikliklari (zemin kat)
    op = []
    if v == "a":
        op = [(0.5, 3.5, 0.9, 2.5), (-3.3, -2.3, 0.0, 2.35)]
    elif v == "b":
        op = [(-0.8, 0.8, 0.0, 2.6), (-3.4, -1.6, 1.0, 2.5), (1.6, 3.4, 1.0, 2.5)]
    else:
        op = [(-3.4, 3.4, 0.9, 2.7)]
    a.wall_open(-W / 2, W / 2, yf, 0.3, 0.0, Z1, wall, op)
    a.solid_box(-W / 2, W / 2, yf - 0.15, yf + 0.15, Z1, Z2, wall)                          # ust kat on duvari
    # ---- govde / ic
    if v == "b":
        a.solid_box(-W / 2, -W / 2 + 0.3, yf, D / 2, 0.0, Z1, wall)                          # yan duvarlar
        a.solid_box(W / 2 - 0.3, W / 2, yf, D / 2, 0.0, Z1, wall)
        a.solid_box(-W / 2, W / 2, 0.8, D / 2, 0.0, Z1, wall)                                # arka blok (ic 3 m derin)
        a.solid_box(-W / 2, W / 2, yf, D / 2, Z1 - 0.15, Z1 + 0.05, wood)                    # ara tavan (ahsap)
        a.solid_box(-W / 2, W / 2, yf, D / 2, 0.0, 0.1, wood)                                # zemin
        a.solid_box(-W / 2, W / 2, yf + 0.15, D / 2, Z1 + 0.05, Z2, wall)                    # ust kat blogu
    elif v == "c":
        a.solid_box(-W / 2, W / 2, -1.5, D / 2, 0.0, Z2, wall)                               # govde (on tarafta 1 m'lik aciklik)
        a.solid_box(-W / 2, W / 2, yf, -1.5, 0.0, 0.9, wall)
        a.solid_box(-W / 2, W / 2, yf, -1.5, 2.7, Z2, wall)
        a.solid_box(-3.6, 3.6, -1.52, -1.2, 0.9, 2.7, wood)                                  # arka raf duvari
    else:
        a.solid_box(-W / 2, W / 2, yf + 0.15, D / 2, 0.0, Z2, wall)
    # ---- on yuz detaylari
    yfr = yf - 0.18                       # on yuz duzlemi
    for sx in (-1, 1):
        a.box((sx * (W / 2 - 0.12), yfr + 0.04, Z2 / 2), (0.28, 0.2, Z2), trim, bevel=0.015)  # koseler (pilastr)
    a.box((0, yfr + 0.04, 0.22), (W, 0.2, 0.44), "M_wood_dark" if v != "a" else trim, bevel=0.012)   # kaide
    a.box((0, yfr + 0.05, Z1 + 0.05), (W + 0.1, 0.24, 0.16), trim, bevel=0.012)             # kat kornisi
    # ---- zemin kat icerik
    if v == "a":
        window(a, 2.0, yfr + 0.1, 1.7, 3.0, 1.6, wood, mullions=True)
        a.box((2.0, yfr - 0.15, 0.9), (3.1, 0.45, 0.1), wood, bevel=0.01)                    # vitrin tezgahi
        a.box((-2.8, yfr + 0.1, 1.17), (0.98, 0.1, 2.34), wood, bevel=0.012)                 # kapi
        a.box((-2.8, yfr + 0.03, 1.7), (0.6, 0.04, 0.8), "M_glass", bevel=0)
        a.box((-2.8, yfr - 0.04, 2.45), (1.2, 0.1, 0.16), trim, bevel=0.01)                  # kapi ustlugu
        a.box((-2.8, yfr - 0.35, 0.08), (1.4, 0.55, 0.16), trim, bevel=0.01)                 # basamak
    elif v == "b":
        for cx in (-2.5, 2.5):
            window(a, cx, yfr + 0.1, 1.75, 1.8, 1.5, wood)
            a.box((cx, yfr - 0.12, 1.0), (1.9, 0.36, 0.08), wood, bevel=0.01)
        a.box((0, yfr + 0.02, 2.75), (1.9, 0.12, 0.2), trim, bevel=0.01)                     # kapi ustlugu
        for sx in (-1, 1):
            a.box((sx * 0.86, yfr + 0.03, 1.3), (0.1, 0.16, 2.6), wood, bevel=0.01)          # kapi kasasi
        a.box((0, yfr - 0.4, 0.07), (2.0, 0.8, 0.14), trim, bevel=0.01)                      # esik/basamak
    else:
        a.box((0, yfr - 0.2, 0.9), (6.8, 0.55, 0.12), wood, bevel=0.01)                      # tezgah ustu
        a.box((0, yfr - 0.2, 0.45), (6.6, 0.45, 0.9), "M_wood_dark", bevel=0.01)
        for i in range(8):                                                                   # raf uzerinde meyve sandiklari
            xx = -3.0 + i * 0.86
            a.box((xx, yfr - 0.18, 1.02), (0.7, 0.4, 0.12), wood, bevel=0.008)
            for j in range(3):
                a.ball((xx - 0.2 + j * 0.2, yfr - 0.18 + (j % 2) * 0.06, 1.16), 0.085, cfg["extra"], sub=1)
        for sx in (-1, 1):
            a.box((sx * 3.5, yfr + 0.02, 1.8), (0.14, 0.2, 1.8), wood, bevel=0.01)            # acik cephe direkleri
        a.box((0, yfr + 0.02, 2.8), (7.2, 0.2, 0.2), wood, bevel=0.01)
    # ---- tente
    if v == "a":
        striped_awning(a, -3.8, 3.8, yfr, 3.15, 0.55, 1.2, cfg["aw1"], cfg["aw2"], n=8)
    elif v == "b":
        striped_awning(a, -3.8, 3.8, yfr, 3.1, 0.5, 1.0, cfg["aw1"], cfg["aw2"], n=8)
    else:
        striped_awning(a, -3.8, 3.8, yfr, 3.2, 0.6, 1.5, cfg["aw1"], cfg["aw2"], n=10)
    # ---- tabela (zemin kat ile ust kat arasi)
    sy = yfr - 0.05
    a.box((0, sy, 3.78), (4.6, 0.16, 0.62), wood, bevel=0.012)
    rng = a.rng
    for i in range(7):
        a.box((-1.9 + i * 0.63, sy - 0.1, 3.78), (0.36, 0.05, 0.3 + rng.random() * 0.1), CREAM, bevel=0.005)
    if v == "b":
        a.box((3.55, sy - 0.5, 3.4), (0.06, 0.8, 0.06), cfg["extra"], bevel=0)             # askili cekic tabelasi
        a.box((3.55, sy - 0.9, 3.1), (0.05, 0.5, 0.6), cfg["extra"], bevel=0.005)
        a.box((3.55, sy - 0.9, 3.45), (0.2, 0.4, 0.12), cfg["extra"], bevel=0.005)
    # ---- ust kat pencereleri (kepenk + cicek sandigi)
    for cx in (-2.6, 0.0, 2.6):
        window(a, cx, yfr + 0.1, 4.55, 1.2, 1.5, wood)
        for sx in (-1, 1):
            a.box((cx + sx * 0.84, yfr + 0.02, 4.55), (0.4, 0.06, 1.5), cfg["aw2"], bevel=0.008)      # kepenk
        a.box((cx, yfr - 0.2, 3.72), (1.3, 0.3, 0.24), wood, bevel=0.01)                              # cicek sandigi
        if v == "a":
            for k in range(4):
                a.ball((cx - 0.45 + k * 0.3, yfr - 0.22, 3.92), 0.15, "M_foliage", sub=1)
    # ---- cati + baca
    a.box((0, 0, Z2 + 0.08), (W + 0.5, D + 0.5, 0.16), trim, bevel=0.012)
    a.prism(0, 0, Z2 + 0.16, W + 0.5, D + 0.7, 1.25, roof)
    cxh = 2.6 if v != "b" else -2.6
    a.box((cxh, 1.1, Z2 + 1.0), (0.7, 0.7, 1.5), trim, bevel=0.01)
    a.box((cxh, 1.1, Z2 + 1.78), (0.9, 0.9, 0.12), trim, bevel=0.01)
    # ---- colliderlar
    if v == "b":
        a.col((-2.4, yf, 1.6), (3.2, 0.3, 3.2))
        a.col((2.4, yf, 1.6), (3.2, 0.3, 3.2))
        a.col((0, yf, 2.6 + 1.5), (1.6, 0.3, Z2 - 2.6 + 0.5))
        a.col((0, yf, 4.4), (W, 0.3, 2.4))
        a.col((-W / 2 + 0.15, 0.2, 1.6), (0.3, D - 0.3, 3.2))
        a.col((W / 2 - 0.15, 0.2, 1.6), (0.3, D - 0.3, 3.2))
        a.col((0, 1.65, 1.6), (W, 1.7, 3.2))
        a.col((0, 0.2, Z1 + 1.2), (W, D - 0.3, Z2 - Z1))
    else:
        a.col((0, 0.1, Z2 / 2), (W, D - 0.2, Z2))


@asset("bld_shopfront_a", 301)
def shop_a(a):
    shopfront(a, "a")


@asset("bld_shopfront_b", 302)
def shop_b(a):
    shopfront(a, "b")


@asset("bld_shopfront_c", 303)
def shop_c(a):
    shopfront(a, "c")


# ======================================================================================== ATOLYE / DEPO / BARAKA
@asset("bld_workshop_a", 311)
def workshop_a(a):
    W, D, H = 14.0, 10.0, 4.4
    th = 0.3
    brick, conc, steel, dark, glass, yel, wood, lamp = "M_brick", "M_concrete", "M_metal_steel", "M_metal_dark", "M_glass", "M_hazard_yellow", "M_wood_dark", "M_light_warm"
    a.solid_box(-W / 2, W / 2, -D / 2, D / 2, 0.0, 0.1, conc)                                       # zemin plakasi
    for (x0, x1, y0, y1) in ((-W / 2, W / 2, D / 2 - th, D / 2), (-W / 2, -W / 2 + th, -D / 2, D / 2), (W / 2 - th, W / 2, -D / 2, D / 2)):
        a.solid_box(x0, x1, y0, y1, 0.1, H, brick)                                                   # 3 duvar
    # tugla derzleri (dis yuz)
    mortar_lines(a, -W / 2, W / 2, D / 2 + 0.005, 0.1, H, conc, every=0.4)
    for sx in (-1, 1):
        for z in [0.4 + 0.4 * i for i in range(10)]:
            a.box((sx * (W / 2 + 0.005), 0, z), (0.02, D, 0.025), conc, bevel=0)
    a.box((0, D / 2 + 0.03, H + 0.1), (W + 0.3, 0.4, 0.22), conc, bevel=0.01)                      # korniş
    for sx in (-1, 1):
        a.box((sx * (W / 2 + 0.03), 0, H + 0.1), (0.4, D + 0.3, 0.22), conc, bevel=0.01)
    # on acikliga kolonlar + kiris
    for sx in (-1, 1):
        a.solid_box(sx * (W / 2 - 0.35) - 0.35, sx * (W / 2 - 0.35) + 0.35, -D / 2 - 0.4, -D / 2 + 0.1, 0.1, H, dark, bevel=0.012)
        for z in (0.5, 1.0, 1.5):                                                                    # uyari bantlari
            a.box((sx * (W / 2 - 0.35), -D / 2 - 0.42, z), (0.74, 0.04, 0.22), yel, bevel=0)
    a.box((0, -D / 2 - 0.15, H - 0.3), (W, 0.5, 0.6), dark, bevel=0.012)                            # on kiris
    a.box((0, -D / 2 - 0.42, H - 0.3), (W - 0.9, 0.06, 0.5), wood, bevel=0.01)                      # tabela
    for i in range(9):
        a.box((-4.8 + i * 1.2, -D / 2 - 0.47, H - 0.3), (0.5, 0.05, 0.3 + 0.1 * (i % 3)), yel, bevel=0.005)
    # cati (hafif egimli) + oluk
    a.prism(0, 0, H + 0.21, W + 0.8, D + 0.8, 1.1, steel)
    for sy in (-1, 1):
        a.box((0, sy * (D / 2 + 0.38), H + 0.25), (W + 0.8, 0.12, 0.14), dark, bevel=0.008)
    for x in (-5, -2, 2, 5):
        a.cyl((x, 2.2, H + 1.15), 0.18, 0.18, 0.3, dark, seg=10)                                    # havalandirma
    a.cyl((4.5, D / 2 - 1.0, H + 1.2), 0.2, 0.2, 2.1, dark, seg=10)                                  # baca borusu
    # yan pencereler (cam seritleri)
    for sx in (-1, 1):
        for y in (-2.5, 0.5, 3.0):
            a.box((sx * (W / 2 - 0.15), y, 2.9), (0.06, 1.4, 0.9), glass, bevel=0)
            a.box((sx * (W / 2 - 0.12), y, 2.9), (0.1, 1.55, 1.05), dark, bevel=0.005)
    # ic aydinlatma
    for x in (-4, 0, 4):
        a.box((x, 0, H - 0.5), (0.04, 0.04, 0.5), dark, bevel=0)
        a.cyl((x, 0, H - 0.8), 0.28, 0.1, 0.2, dark, seg=10)
        a.cyl((x, 0, H - 0.95), 0.1, 0.1, 0.06, lamp, seg=8)
    a.col((0, 0, 0.05), (W, D, 0.1))
    a.col((0, D / 2 - th / 2, H / 2), (W, th, H))
    a.col((-W / 2 + th / 2, 0, H / 2), (th, D, H))
    a.col((W / 2 - th / 2, 0, H / 2), (th, D, H))
    a.col((0, -D / 2 - 0.15, H - 0.3), (W, 0.5, 0.6))
    a.col((0, 0, H + 0.6), (W + 0.6, D + 0.6, 1.2))


@asset("bld_warehouse_a", 321)
def warehouse_a(a):
    W, D = 24.0, 16.0
    FL = 1.1                                    # zemin/yukleme rampasi yuksekligi
    HW = 6.2                                    # duvar ust kotu
    th = 0.35
    sand, conc, steel, dark, glass, yel, rub, blue = "M_plaster_sand", "M_concrete", "M_metal_steel", "M_metal_dark", "M_glass", "M_hazard_yellow", "M_rubber", "M_paint_blue"
    yf = -D / 2
    # --- platform (bina tabani + onde 3 m yukleme platformu)
    a.solid_box(-W / 2, W / 2, yf - 3.0, D / 2, 0.0, FL, conc)
    a.solid_box(-W / 2 - 0.1, W / 2 + 0.1, yf - 3.1, yf - 2.9, FL - 0.12, FL + 0.03, dark, bevel=0.01)       # kenar profili
    # --- rampa (12 derece): sagda, on kenardan -Y'ye
    rl = FL / math.tan(math.radians(12))
    a.wedge(W / 2 - 4.0, W / 2 - 0.5, yf - 3.0 - rl, yf - 3.0, 0.0, 0.0, FL, conc)
    for sx in (W / 2 - 3.9, W / 2 - 0.9):                                                                      # rampa uyari seritleri
        a.hexa([(sx, yf - 3.0 - rl, 0.0), (sx + 0.4, yf - 3.0 - rl, 0.0), (sx + 0.4, yf - 3.0, FL), (sx, yf - 3.0, FL),
                (sx, yf - 3.0 - rl, 0.02), (sx + 0.4, yf - 3.0 - rl, 0.02), (sx + 0.4, yf - 3.0, FL + 0.02), (sx, yf - 3.0, FL + 0.02)], yel)
    # --- duvarlar
    a.wall_open(-W / 2, W / 2, yf + th / 2, th, FL, HW, sand, [(-2.0, 2.0, FL, FL + 4.0)])                   # on duvar + rulo kapi boslugu
    a.solid_box(-W / 2, W / 2, D / 2 - th, D / 2, FL, HW, sand)
    a.solid_box(-W / 2, -W / 2 + th, yf, D / 2, FL, HW, sand)
    a.solid_box(W / 2 - th, W / 2, yf, D / 2, FL, HW, sand)
    for x in range(-11, 12, 2):                                                                                # on dis panel seritleri
        a.box((x, yf - 0.04, (FL + HW) / 2 + 0.4), (0.12, 0.08, HW - FL - 0.9), steel, bevel=0)
    for sx in (-1, 1):
        for y in range(-7, 8, 2):
            a.box((sx * (W / 2 + 0.04), y, (FL + HW) / 2 + 0.4), (0.08, 0.12, HW - FL - 0.9), steel, bevel=0)
    a.box((0, yf - 0.06, FL + 0.6), (W, 0.12, 1.2), conc, bevel=0.01)                                          # on kaide bandi
    # --- rulo kapi cercevesi + ust ruloy
    for sx in (-1, 1):
        a.box((sx * 2.15, yf - 0.1, FL + 2.0), (0.3, 0.3, 4.0), dark, bevel=0.01)
        for k in range(5):
            a.box((sx * 2.15, yf - 0.27, FL + 0.4 + k * 0.8), (0.32, 0.04, 0.3), yel, bevel=0)
    a.box((0, yf - 0.1, FL + 4.15), (4.6, 0.34, 0.34), dark, bevel=0.01)
    for k in range(6):                                                                                         # yari acik kapi lamelleri
        a.box((0, yf + 0.05, FL + 3.55 + k * 0.1), (3.9, 0.06, 0.09), blue, bevel=0)
    a.box((0, yf - 0.36, FL + 4.65), (4.0, 0.08, 0.5), blue, bevel=0.01)
    # --- kuzey cephe kucuk yuk kapisi + tabela
    a.box((8.0, yf - 0.4, HW - 0.6), (6.0, 0.12, 0.8), dark, bevel=0.01)
    for i in range(6):
        a.box((5.75 + i * 0.9, yf - 0.47, HW - 0.6), (0.5, 0.04, 0.4), yel, bevel=0)
    # --- cati
    a.box((0, 0, HW + 0.1), (W + 0.8, D + 0.8, 0.2), conc, bevel=0.01)
    a.prism(0, 0, HW + 0.2, W + 1.2, D + 1.6, 1.5, steel)
    for sy in (-1, 1):
        a.box((0, sy * (D / 2 + 0.7), HW + 0.28), (W + 1.2, 0.14, 0.16), dark, bevel=0.008)
    for x in (-8, -3, 3, 8):
        a.cyl((x, 3, HW + 1.6), 0.28, 0.28, 0.45, dark, seg=10)
    # --- yan yuksek camlar
    for sx in (-1, 1):
        for y in (-6.0, -2.0, 2.0, 6.0):
            a.box((sx * (W / 2 - 0.18), y, HW - 0.7), (0.08, 2.4, 0.8), glass, bevel=0)
            a.box((sx * (W / 2 - 0.14), y, HW - 0.7), (0.12, 2.55, 0.95), dark, bevel=0.005)
    # --- kasalar (ic): kolon + kiris
    for x in (-8, 0, 8):
        for y in (-3.0, 3.0):
            a.box((x, y, (FL + HW) / 2), (0.4, 0.4, HW - FL), dark, bevel=0.01)
    a.box((0, -3.0, HW - 0.3), (W - 0.6, 0.3, 0.3), dark, bevel=0.01)
    a.box((0, 3.0, HW - 0.3), (W - 0.6, 0.3, 0.3), dark, bevel=0.01)
    # --- platform aksesuarlari: tamponlar, babalar
    for x in (-9, -5, -1.5, 1.5, 5, 9):
        a.box((x, yf - 3.0 - 0.08, FL - 0.3), (0.5, 0.16, 0.5), rub, bevel=0.01)
    for x in (-10, -6, 6, 10):
        a.cyl((x, yf - 2.7, FL + 0.3), 0.12, 0.12, 0.6, yel, seg=8)
    a.col((0, (yf - 3.0 + D / 2) / 2, FL / 2), (W, D + 3.0, FL))
    a.col_rot((W / 2 - 2.25, yf - 3.0 - rl / 2, FL / 2 - 0.12), (3.5, rl / math.cos(math.radians(12)), 0.3), (12, 0, 0))
    a.col((-W / 2 + th / 2, 0, (FL + HW) / 2), (th, D, HW - FL))
    a.col((W / 2 - th / 2, 0, (FL + HW) / 2), (th, D, HW - FL))
    a.col((0, D / 2 - th / 2, (FL + HW) / 2), (W, th, HW - FL))
    a.col((-7.0, yf + th / 2, (FL + HW) / 2), (10.0, th, HW - FL))
    a.col((7.0, yf + th / 2, (FL + HW) / 2), (10.0, th, HW - FL))
    a.col((0, yf + th / 2, FL + 4.0 + (HW - FL - 4.0) / 2), (4.0, th, HW - FL - 4.0))
    a.col((0, 0, HW + 0.8), (W + 1.0, D + 1.0, 1.4))


@asset("bld_halfwall_a", 331)
def halfwall_a(a):
    W, H, T = 4.0, 1.8, 0.3
    a.solid_box(-W / 2 + 0.02, W / 2 - 0.02, -0.12, 0.12, 0.02, H - 0.1, "M_concrete")                       # harc cekirdegi
    rng = a.rng

    def ragged(r, k, rows):
        return r >= rows - 2 and rng.random() < (0.55 if r == rows - 1 else 0.22)

    for y in (-0.1, 0.1):
        bricks(a, -W / 2, W / 2, y, 0.0, H, 0.1, "M_brick", "M_roof_tile", bw=0.5, bh=0.2, skip=ragged)
    a.col((0, 0, H / 2), (W, T, H))


@asset("bld_halfwall_a_broken", 332)
def halfwall_broken(a):
    a.chunk_mode = True
    W, H = 4.0, 1.8
    rng = a.rng
    cols_n, rows_n = 4, 3
    cw, rh = W / cols_n, H / rows_n
    idx = 0
    for ci in range(cols_n):
        for ri in range(rows_n):
            idx += 1
            if idx > 1:
                a.new_part("bld_halfwall_a_chunk_%02d" % idx, pivot=True)
            else:
                a.cur.name = "bld_halfwall_a_chunk_01"
                a.cur.pivot = True
            x0, x1 = -W / 2 + ci * cw, -W / 2 + (ci + 1) * cw
            z0, z1 = ri * rh, (ri + 1) * rh
            a.solid_box(x0 + 0.02, x1 - 0.02, -0.12, 0.12, z0 + 0.02, z1 - 0.02, "M_concrete")
            for y in (-0.1, 0.1):
                bricks(a, x0, x1, y, z0, z1, 0.1, "M_brick", "M_roof_tile", bw=0.5, bh=0.2)
            bm = a.cur.bm
            ctr = sum((v.co for v in bm.verts), Vector()) / len(bm.verts)
            fall = 0.5 + 0.35 * (rows_n - 1 - ri)
            sh = Vector((rng.uniform(-0.35, 0.35) + (ci - 1.5) * 0.12, -fall * (0.6 + rng.random() * 0.5), -z0 + 0.02 + rng.random() * 0.05))
            bmesh.ops.translate(bm, verts=bm.verts, vec=sh)
            bmesh.ops.rotate(bm, verts=bm.verts, cent=ctr + sh, matrix=Euler((math.radians(rng.uniform(-60, 60)), math.radians(rng.uniform(-35, 35)),
                                                                                math.radians(rng.uniform(-55, 55))), "XYZ").to_matrix())


@asset("bld_shed_a", 341)
def shed_a(a):
    W, D, H = 4.0, 3.0, 2.5
    green, dark, wood, glass, conc, lamp, yel = "M_paint_green", "M_metal_dark", "M_wood_dark", "M_glass", "M_concrete", "M_light_warm", "M_hazard_yellow"
    a.solid_box(-W / 2, W / 2, -D / 2, D / 2, 0.0, 0.18, conc)                                               # temel
    a.solid_box(-W / 2 + 0.05, W / 2 - 0.05, -D / 2 + 0.05, D / 2 - 0.05, 0.18, H, green)
    for x in [-1.8 + 0.25 * i for i in range(15)]:                                                            # oluklu sac nervurleri (on/arka)
        a.box((x, -D / 2 + 0.03, 1.35), (0.09, 0.06, H - 0.35), dark, bevel=0)
        a.box((x, D / 2 - 0.03, 1.35), (0.09, 0.06, H - 0.35), dark, bevel=0)
    for sx in (-1, 1):
        for y in [-1.3 + 0.25 * i for i in range(11)]:
            a.box((sx * (W / 2 - 0.03), y, 1.35), (0.06, 0.09, H - 0.35), dark, bevel=0)
    a.prism(0, 0, H, W + 0.5, D + 0.6, 0.55, dark)
    # kapi + penceresi
    a.box((-1.0, -D / 2 - 0.02, 1.15), (0.95, 0.07, 1.95), wood, bevel=0.012)
    a.box((-1.0, -D / 2 - 0.08, 1.15), (1.15, 0.05, 2.1), dark, bevel=0.008)
    a.box((-0.65, -D / 2 - 0.08, 1.05), (0.07, 0.08, 0.22), "M_metal_steel" if False else yel, bevel=0.005)
    a.box((1.0, -D / 2 - 0.03, 1.5), (0.9, 0.05, 0.7), glass, bevel=0)
    a.box((1.0, -D / 2 - 0.06, 1.5), (1.05, 0.06, 0.85), dark, bevel=0.005)
    a.box((1.0, -D / 2 - 0.2, 1.0), (1.15, 0.3, 0.07), wood, bevel=0.008)                                      # pencere esigi
    # lamba + uyari levhasi
    a.box((-1.0, -D / 2 - 0.22, 2.35), (0.05, 0.4, 0.05), dark, bevel=0)
    a.cyl((-1.0, -D / 2 - 0.4, 2.25), 0.12, 0.12, 0.12, lamp, seg=8)
    a.box((1.0, -D / 2 - 0.05, 2.15), (0.8, 0.04, 0.3), yel, bevel=0.005)
    a.cyl((1.5, 0.0, H + 0.5), 0.1, 0.1, 0.35, dark, seg=8)
    a.col((0, 0, H / 2 + 0.05), (W, D, H))


# ======================================================================================== ANITLAR
def clock_face(a, ang):
    dist = 1.62
    cx, cy = rz(0, -dist, ang)
    a.cyl((cx, cy, 10.0), 1.22, 1.22, 0.14, "M_metal_dark", seg=20, rot=(90, 0, ang))
    cx2, cy2 = rz(0, -dist - 0.05, ang)
    a.cyl((cx2, cy2, 10.0), 1.06, 1.06, 0.08, CREAM, seg=20, rot=(90, 0, ang))
    for h in range(12):
        t = math.radians(h * 30)
        lx, ly = rz(math.sin(t) * 0.88, -dist - 0.12, ang)
        a.box((lx, ly, 10.0 + math.cos(t) * 0.88), (0.1, 0.04, 0.18 if h % 3 == 0 else 0.1), "M_wood_dark", bevel=0, rot=(0, -h * 30, ang))
    for (L, w, th) in ((0.55, 0.1, 40.0), (0.85, 0.06, 150.0)):                                               # akrep / yelkovan
        t = math.radians(th)
        lx, ly = rz(math.sin(t) * L / 2, -dist - 0.16, ang)
        a.box((lx, ly, 10.0 + math.cos(t) * L / 2), (w, 0.04, L), "M_metal_dark", bevel=0, rot=(0, -th, ang))
    px, py = rz(0, -dist - 0.17, ang)
    a.cyl((px, py, 10.0), 0.09, 0.09, 0.06, "M_metal_steel", seg=8, rot=(90, 0, ang))


@asset("bld_clocktower_a", 351)
def clocktower_a(a):
    brick, cream, conc, wood, dark, roof, steel = "M_brick", CREAM, "M_concrete", "M_wood_dark", "M_metal_dark", "M_roof_tile", "M_metal_steel"
    a.solid_box(-1.75, 1.75, -1.75, 1.75, 0.0, 1.2, conc, bevel=0.04)                                          # kaide
    a.solid_box(-1.9, 1.9, -1.9, 1.9, 0.0, 0.3, conc, bevel=0.03)
    a.solid_box(-1.4, 1.4, -1.4, 1.4, 1.2, 8.6, brick)                                                          # govde
    for z in (3.0, 5.6, 8.0):
        a.box((0, 0, z), (3.0, 3.0, 0.18), cream, bevel=0.012)                                                  # kusak
    for sx in (-1, 1):
        for sy in (-1, 1):
            for i in range(7):
                a.box((sx * 1.42, sy * 1.42, 1.5 + i * 1.0), (0.3 if i % 2 else 0.42, 0.3 if i % 2 else 0.42, 0.45), cream, bevel=0.01)   # kose tuglalari
    for ang in (0, 90, 180, 270):                                                                               # dar pencereler
        for z in (4.2, 6.9):
            wx, wy = rz(0, -1.43, ang)
            a.box((wx, wy, z), (0.5, 0.1, 1.1) if ang in (0, 180) else (0.1, 0.5, 1.1), wood, bevel=0.01)
    a.solid_box(-1.7, 1.7, -1.7, 1.7, 8.6, 11.4, cream, bevel=0.02)                                             # saat odasi
    a.box((0, 0, 11.45), (3.9, 3.9, 0.2), conc, bevel=0.015)
    for ang in (0, 90, 180, 270):
        clock_face(a, ang)
    a.solid_box(-1.5, 1.5, -1.5, 1.5, 11.5, 12.5, wood)                                                         # çan odası (karanlık)
    for sx in (-1, 1):
        for sy in (-1, 1):
            a.solid_box(sx * 1.5 - 0.2 * sx - 0.2, sx * 1.5 - 0.2 * sx + 0.2, sy * 1.5 - 0.2 * sy - 0.2, sy * 1.5 - 0.2 * sy + 0.2, 11.5, 12.5, cream, bevel=0.01)
    a.cyl((0, 0, 11.95), 0.35, 0.18, 0.55, steel, seg=12)                                                       # çan
    a.box((0, 0, 12.55), (3.8, 3.8, 0.18), conc, bevel=0.015)
    a.pyramid(0, 0, 12.64, 3.7, 3.7, 1.4, roof)
    a.cyl((0, 0, 14.0 - 0.18), 0.04, 0.025, 0.36, steel, seg=6)
    a.ball((0, 0, 13.7), 0.1, steel, sub=1)
    a.col((0, 0, 6.5), (3.6, 3.6, 13.0))


@asset("prop_fountain_a", 361)
def fountain_a(a):
    conc, cream, water, steel, blue = "M_concrete", CREAM, "M_water", "M_metal_steel", "M_paint_blue"
    a.cyl((0, 0, 0.075), 3.0, 3.0, 0.15, conc, seg=24)                                                          # taban
    n = 24
    rr = 2.68
    seg = 2 * math.pi * rr / n
    for i in range(n):
        t = 360.0 * i / n
        x, y = math.cos(math.radians(t)) * rr, math.sin(math.radians(t)) * rr
        a.box((x, y, 0.33), (seg + 0.02, 0.34, 0.66), conc, bevel=0, rot=(0, 0, t + 90))
        a.box((x * 1.02, y * 1.02, 0.7), (seg + 0.04, 0.46, 0.09), cream, bevel=0, rot=(0, 0, t + 90))
        if i % 2 == 0:
            a.box((x * 1.06, y * 1.06, 0.3), (seg * 0.6, 0.05, 0.4), blue, bevel=0, rot=(0, 0, t + 90))        # cini seritleri
    a.cyl((0, 0, 0.34), 2.5, 2.5, 0.28, water, seg=24)                                                           # su
    a.frustum((0, 0, 0.15), 0.95, 0.95, 0.8, 0.8, 0.6, cream, seg=14)                                           # kaide 1
    a.frustum((0, 0, 0.75), 0.45, 0.45, 0.3, 0.3, 1.3, cream, seg=12)                                           # sutun
    a.frustum((0, 0, 1.95), 0.3, 0.3, 1.25, 1.25, 0.4, cream, seg=16)                                           # tas kase
    a.cyl((0, 0, 2.4), 1.15, 1.15, 0.06, water, seg=16)
    a.cyl((0, 0, 2.55), 0.25, 0.12, 0.3, cream, seg=10)
    a.ball((0, 0, 2.82), 0.18, steel, sub=1)
    for k in range(4):
        t = math.radians(45 + k * 90)
        a.box((math.cos(t) * 1.2, math.sin(t) * 1.2, 2.05), (0.12, 0.12, 0.14), steel, bevel=0.01, rot=(0, 0, 45 + k * 90))
    a.col_frustum((0, 0, 0), 3.0, 3.0, 3.0, 3.0, 0.7, seg=16)
    a.col_frustum((0, 0, 0.7), 0.45, 0.45, 0.35, 0.35, 2.1, seg=8)


@asset("bld_watertower_a", 371)
def watertower_a(a):
    steel, dark, blue, red = "M_metal_steel", "M_metal_dark", "M_paint_blue", "M_paint_red"
    for sx in (-1, 1):
        for sy in (-1, 1):
            a.strut((sx * 2.0, sy * 2.0, 0.0), (sx * 1.45, sy * 1.45, 7.0), 0.26, steel)                        # acili bacaklar
            a.box((sx * 2.0, sy * 2.0, 0.08), (0.7, 0.7, 0.16), dark, bevel=0.01)                              # tabanlar
    for z, k in ((2.4, 1.82), (4.8, 1.64)):                                                                     # halkalar
        for sy in (-1, 1):
            a.box((0, sy * k, z), (2 * k, 0.12, 0.12), dark, bevel=0)
        for sx in (-1, 1):
            a.box((sx * k, 0, z), (0.12, 2 * k, 0.12), dark, bevel=0)
    for (z0, z1, k0, k1) in ((0.1, 2.4, 2.0, 1.82), (2.4, 4.8, 1.82, 1.64), (4.8, 7.0, 1.64, 1.45)):             # capraz destekler
        for s in (-1, 1):
            a.strut((s * k0, -k0, z0), (-s * k1, -k1, z1), 0.1, dark)
            a.strut((s * k0, k0, z0), (-s * k1, k1, z1), 0.1, dark)
    a.box((0, 0, 7.05), (3.9, 3.9, 0.2), dark, bevel=0.01)                                                      # platform
    a.cyl((0, 0, 8.7), 2.5, 2.5, 3.3, blue, seg=24)                                                              # tank
    for z in (7.4, 8.7, 10.0):
        a.cyl((0, 0, z), 2.56, 2.56, 0.14, dark, seg=24)
    a.cyl((0, 0, 8.7), 2.52, 2.52, 0.55, red, seg=24)
    a.cyl((0, 0, 10.8), 2.6, 0.2, 1.0, steel, seg=24)                                                           # koni cati
    a.cyl((0, 0, 11.65), 0.12, 0.12, 0.7, dark, seg=8)
    a.cyl((0, -1.2, 3.5), 0.13, 0.13, 7.0, steel, seg=8)                                                         # boru
    for sx in (-1, 1):                                                                                           # merdiven
        a.box((sx * 0.25 + 1.8, -1.95, 3.7), (0.06, 0.06, 7.4), dark, bevel=0)
    for z in [0.5 + 0.42 * i for i in range(16)]:
        a.box((1.8, -1.95, z), (0.55, 0.05, 0.05), dark, bevel=0)
    for sx in (-1, 1):
        for sy in (-1, 1):
            a.col((sx * 1.72, sy * 1.72, 3.5), (0.5, 0.5, 7.0))
    a.col_frustum((0, 0, 7.0), 2.5, 2.5, 2.5, 2.5, 3.3, seg=12)


# ======================================================================================== SOKAK / AVLU PROPLARI
@asset("prop_lamp_a", 401)
def lamp_a(a):
    dark, steel, lamp = "M_metal_dark", "M_metal_steel", "M_light_warm"
    a.cyl((0, 0, 0.14), 0.3, 0.22, 0.28, dark, seg=12)
    a.cyl((0, 0, 0.32), 0.18, 0.18, 0.08, steel, seg=12)
    a.cyl((0, 0, 1.9), 0.09, 0.065, 3.2, dark, seg=12)
    for z in (0.7, 1.4):
        a.cyl((0, 0, z), 0.13, 0.13, 0.07, steel, seg=12)
    a.cyl((0, 0, 3.4), 0.12, 0.12, 0.12, steel, seg=12)
    for s in (-1, 1):
        a.strut((0, 0, 3.35), (s * 0.55, 0, 3.55), 0.07, dark)
        a.strut((s * 0.55, 0, 3.55), (s * 0.62, 0, 3.55), 0.07, dark)
        lx = s * 0.62
        a.box((lx, 0, 3.52), (0.3, 0.3, 0.04), dark, bevel=0.005)
        for ax in (-1, 1):
            for ay in (-1, 1):
                a.box((lx + ax * 0.13, ay * 0.13, 3.7), (0.025, 0.025, 0.3), dark, bevel=0)
        a.box((lx, 0, 3.7), (0.2, 0.2, 0.28), lamp, bevel=0.01)
        a.pyramid(lx, 0, 3.86, 0.4, 0.4, 0.14, dark)
        a.ball((lx, 0, 4.0), 0.04, steel, sub=1)
    a.col((0, 0, 1.9), (0.3, 0.3, 3.8))


@asset("prop_bench_a", 402)
def bench_a(a):
    wood, dark = "M_wood_warm", "M_metal_dark"
    for i in range(4):
        a.box((0, -0.2 + i * 0.13, 0.45), (1.8, 0.11, 0.045), wood, bevel=0.008, jitter_deg=0.4)
    for i in range(3):
        a.box((0, 0.27 - 0.03 * i, 0.63 + i * 0.15), (1.8, 0.04, 0.12), wood, bevel=0.008, rot=(-12, 0, 0))
    for sx in (-0.78, 0.78):
        a.box((sx, 0, 0.22), (0.07, 0.5, 0.44), dark, bevel=0.01)
        a.box((sx, 0.27, 0.65), (0.07, 0.06, 0.55), dark, bevel=0.01, rot=(-12, 0, 0))
        a.box((sx, -0.2, 0.62), (0.07, 0.4, 0.05), dark, bevel=0.008)
        a.box((sx, -0.2, 0.5), (0.07, 0.05, 0.25), dark, bevel=0.008)
        a.box((sx, 0, 0.03), (0.12, 0.55, 0.06), dark, bevel=0.008)
    a.col((0, 0, 0.45), (1.9, 0.6, 0.9))


def tree(a, trunk_h, lumps, lump_mat2=True):
    wood, g1, g2 = "M_wood_dark", "M_foliage", "M_grass"
    a.cyl((0, 0, 0.1), 0.4, 0.26, 0.2, wood, seg=8)
    a.cyl((0, 0, trunk_h / 2), 0.2, 0.13, trunk_h, wood, seg=8)
    for (x, y, z, r, mt) in lumps:
        a.ball((x, y, z), r, g1 if mt == 1 else g2, sub=2, scale=(1, 1, 0.9))
    a.col((0, 0, trunk_h / 2), (0.5, 0.5, trunk_h))


@asset("prop_tree_a", 411)
def tree_a(a):
    L = []
    for k in range(6):
        t = math.radians(k * 60)
        L.append((math.cos(t) * 0.85, math.sin(t) * 0.85, 3.0 + 0.25 * (k % 2), 0.9, 1 if k % 2 else 2))
    L += [(0, 0, 3.7, 1.2, 1), (0, 0, 2.7, 0.9, 2)]
    tree(a, 2.2, L)


@asset("prop_tree_b", 412)
def tree_b(a):
    L = []
    for k in range(4):
        t = math.radians(k * 90 + 45)
        L.append((math.cos(t) * 1.0, math.sin(t) * 1.0, 3.9 + 0.3 * (k % 2), 0.95, 2 if k % 2 else 1))
    for k in range(4):
        t = math.radians(k * 90)
        L.append((math.cos(t) * 0.6, math.sin(t) * 0.6, 4.9, 0.8, 1 if k % 2 else 2))
    L += [(0, 0, 4.4, 1.25, 1), (0, 0, 5.4, 0.7, 2)]
    tree(a, 3.1, L)


@asset("prop_planter_a", 421)
def planter_a(a):
    terra, soil, leaf, yel, red = "M_roof_tile", "M_wood_dark", "M_foliage", "M_paint_yellow", "M_paint_red"
    a.box((0, 0, 0.2), (1.0, 0.5, 0.4), terra, bevel=0.02)
    a.box((0, 0, 0.42), (1.08, 0.58, 0.06), terra, bevel=0.015)
    a.box((0, 0, 0.45), (0.9, 0.4, 0.05), soil, bevel=0)
    for i, x in enumerate((-0.32, 0.0, 0.32)):
        a.ball((x, 0, 0.62), 0.2 + 0.03 * (i % 2), leaf, sub=1, scale=(1, 1, 0.85))
    for i in range(7):
        a.ball((-0.38 + i * 0.127, -0.12 + 0.24 * (i % 2), 0.78), 0.05, yel if i % 2 else red, sub=1)
    a.col((0, 0, 0.3), (1.0, 0.5, 0.6))


@asset("prop_stall_a", 431)
def stall_a(a):
    wood, dark, can, red, org = "M_wood_warm", "M_wood_dark", "M_canvas", "M_paint_red", "M_paint_orange"
    for sx in (-1.15, 1.15):
        for sy, h in ((-0.7, 2.1), (0.7, 2.5)):
            a.box((sx, sy, h / 2), (0.08, 0.08, h), dark, bevel=0.008)
    a.box((0, -0.55, 0.45), (2.4, 0.7, 0.9), wood, bevel=0.01)
    for z in (0.2, 0.45, 0.7):
        a.box((0, -0.91, z), (2.38, 0.02, 0.03), dark, bevel=0)
    a.box((0, -0.55, 0.93), (2.5, 0.8, 0.07), dark, bevel=0.008)
    a.box((0, 0.65, 0.5), (2.2, 0.4, 0.04), dark, bevel=0.005)
    a.box((0, 0.65, 1.1), (2.2, 0.4, 0.04), dark, bevel=0.005)
    for i in range(6):
        x = -0.95 + i * 0.38
        a.box((x, -0.55, 1.0), (0.32, 0.4, 0.1), dark, bevel=0.006)
        for j in range(3):
            a.ball((x - 0.08 + 0.08 * j, -0.55 + 0.05 * (j % 2), 1.12), 0.06, org if i % 2 else red, sub=1)
    th = math.atan2(0.45, 1.5)
    for i in range(6):
        x = -1.25 + (i + 0.5) * 0.4167
        a.box((x, -0.0, 2.3), (0.41, 1.6, 0.04), can if i % 2 == 0 else red, bevel=0.004, rot=(math.degrees(th), 0, 0))
        a.box((x, -0.78, 2.0), (0.41, 0.04, 0.22), can if i % 2 == 0 else red, bevel=0.004)
    a.box((0, -0.95, 1.75), (1.6, 0.05, 0.35), dark, bevel=0.006)
    for i in range(5):
        a.box((-0.6 + i * 0.3, -0.98, 1.75), (0.16, 0.03, 0.2), "M_wood_warm", bevel=0)
    a.col((0, 0, 1.05), (2.5, 1.5, 2.1))


@asset("prop_stall_a_broken", 432)
def stall_broken(a):
    a.chunk_mode = True
    rng = a.rng
    wood, dark, can, red = "M_wood_warm", "M_wood_dark", "M_canvas", "M_paint_red"
    parts = [
        ("counter", lambda: a.box((0, 0, 0.3), (2.4, 0.7, 0.5), wood, bevel=0.01, rot=(5, 0, 8))),
        ("top", lambda: a.box((0.3, 0.2, 0.1), (2.5, 0.8, 0.07), dark, bevel=0.008, rot=(80, 6, 14))),
        ("post1", lambda: a.box((-0.9, 0.5, 0.1), (0.08, 0.08, 2.1), dark, bevel=0.008, rot=(84, 0, 20))),
        ("post2", lambda: a.box((0.9, -0.4, 0.1), (0.08, 0.08, 2.4), dark, bevel=0.008, rot=(-86, 4, -12))),
        ("awn1", lambda: [a.box((-0.6 + 0.4 * i, 0, 0.1), (0.41, 1.6, 0.04), can if i % 2 == 0 else red, bevel=0.004, rot=(8, 0, 4)) for i in range(3)]),
        ("awn2", lambda: [a.box((0.4 + 0.4 * i, 0.2, 0.1), (0.41, 1.6, 0.04), can if i % 2 == 0 else red, bevel=0.004, rot=(-6, 0, -9)) for i in range(3)]),
        ("sign", lambda: a.box((0, 0, 0.05), (1.6, 0.05, 0.35), dark, bevel=0.006, rot=(0, 0, 25))),
        ("shelf", lambda: a.box((0, 0, 0.04), (2.2, 0.4, 0.04), dark, bevel=0.005, rot=(2, 4, -30))),
    ]
    spots = [(0, -0.2), (-0.2, 0.7), (-0.8, 0.15), (0.8, 0.3), (0.1, -0.8), (-0.5, -0.7), (0.6, -0.7), (0.7, 0.7)]
    for i, (nm, fn) in enumerate(parts):
        if i == 0:
            a.cur.name = "prop_stall_a_chunk_01"
            a.cur.pivot = True
        else:
            a.new_part("prop_stall_a_chunk_%02d" % (i + 1), pivot=True)
        fn()
        bmesh.ops.translate(a.cur.bm, verts=a.cur.bm.verts, vec=(spots[i][0], spots[i][1], 0.0))


@asset("prop_bin_a", 441)
def bin_a(a):
    green, steel = "M_paint_green", "M_metal_steel"
    a.cyl((0, 0, 0.4), 0.3, 0.26, 0.8, green, seg=14)
    for z in (0.12, 0.45, 0.72):
        a.cyl((0, 0, z), 0.31, 0.31, 0.05, steel, seg=14)
    a.cyl((0, 0, 0.84), 0.33, 0.28, 0.1, green, seg=14)
    a.cyl((0, 0, 0.92), 0.08, 0.08, 0.06, steel, seg=8)
    a.box((0.3, 0, 0.55), (0.04, 0.12, 0.05), steel, bevel=0)
    a.col((0, 0, 0.45), (0.65, 0.65, 0.95))


@asset("prop_hydrant_a", 442)
def hydrant_a(a):
    red, steel = "M_paint_red", "M_metal_steel"
    a.cyl((0, 0, 0.07), 0.24, 0.2, 0.14, red, seg=12)
    a.cyl((0, 0, 0.42), 0.17, 0.15, 0.56, red, seg=12)
    a.cyl((0, 0, 0.72), 0.2, 0.2, 0.07, steel, seg=12)
    a.ball((0, 0, 0.78), 0.16, red, sub=2, scale=(1, 1, 0.8))
    a.cyl((0, 0, 0.97), 0.05, 0.05, 0.1, steel, seg=6)
    for s in (-1, 1):
        a.cyl((s * 0.24, 0, 0.5), 0.07, 0.07, 0.16, steel, seg=8, rot=(0, 90, 0))
    a.cyl((0, -0.22, 0.4), 0.09, 0.09, 0.12, steel, seg=8, rot=(90, 0, 0))
    a.col((0, 0, 0.5), (0.5, 0.5, 1.0))


@asset("prop_sign_a", 451)
def sign_a(a):
    dark, blue, yel, cream = "M_metal_dark", "M_paint_blue", "M_paint_yellow", CREAM
    a.cyl((0, 0, 1.5), 0.05, 0.05, 3.0, dark, seg=8)
    a.cyl((0, 0, 0.08), 0.18, 0.18, 0.16, dark, seg=10)
    a.ball((0, 0, 3.05), 0.07, dark, sub=1)
    a.box((0.52, 0, 2.7), (1.0, 0.05, 0.3), blue, bevel=0.01)
    a.box((1.06, 0, 2.7), (0.2, 0.05, 0.2), blue, bevel=0, rot=(0, 45, 0))
    a.box((-0.48, 0, 2.25), (0.9, 0.05, 0.3), yel, bevel=0.01)
    a.box((-1.0, 0, 2.25), (0.2, 0.05, 0.2), yel, bevel=0, rot=(0, 45, 0))
    for i in range(4):
        a.box((0.2 + i * 0.22, -0.035, 2.7), (0.12, 0.02, 0.1 + 0.04 * (i % 2)), cream, bevel=0)
        a.box((-0.7 + i * 0.22, -0.035, 2.25), (0.12, 0.02, 0.1 + 0.04 * (i % 2)), dark, bevel=0)
    a.col((0, 0, 1.5), (0.4, 0.3, 3.0))


@asset("prop_sack_a", 461)
def sack_a(a):
    can, red, conc = "M_canvas", "M_paint_red", "M_concrete"
    a.box((0, 0, 0.12), (0.5, 0.32, 0.22), can, bevel=0.06)
    a.box((0, 0, 0.26), (0.44, 0.26, 0.08), can, bevel=0.03)
    for sx in (-1, 1):
        a.box((sx * 0.2, 0, 0.33), (0.1, 0.12, 0.08), can, bevel=0.02, rot=(0, sx * 18, 0))
    a.box((0, -0.168, 0.14), (0.3, 0.02, 0.14), red, bevel=0.004)
    a.box((0, -0.18, 0.14), (0.22, 0.02, 0.05), conc, bevel=0)
    a.col((0, 0, 0.16), (0.5, 0.32, 0.34))


@asset("prop_cementmixer_a", 471)
def cementmixer_a(a):
    org, dark, rub, steel = "M_paint_orange", "M_metal_dark", "M_rubber", "M_metal_steel"
    for sx in (-1, 1):
        a.strut((sx * 0.38, -0.2, 0.3), (sx * 0.2, 0.0, 0.95), 0.07, dark)
        a.strut((sx * 0.38, 0.4, 0.3), (sx * 0.2, 0.0, 0.95), 0.07, dark)
        a.cyl((sx * 0.5, 0.05, 0.3), 0.3, 0.3, 0.1, rub, seg=14, rot=(0, 90, 0))
    a.box((0, 0.05, 0.3), (1.0, 0.07, 0.07), dark, bevel=0)
    a.strut((0, 0.5, 0.3), (0, 1.0, 0.75), 0.05, dark)
    a.strut((0, 0.5, 0.3), (0, 1.0, 0.75), 0.05, dark)
    a.cyl((0.0, -0.1, 1.15), 0.48, 0.48, 0.55, org, seg=16, rot=(-45, 0, 0))
    a.cyl((0.0, -0.32, 1.42), 0.48, 0.18, 0.4, org, seg=16, rot=(-45, 0, 0))
    a.cyl((0.0, -0.4, 1.5), 0.36, 0.36, 0.06, steel, seg=14, rot=(-45, 0, 0))
    a.cyl((0.0, 0.1, 1.0), 0.5, 0.5, 0.06, steel, seg=16, rot=(-45, 0, 0))
    a.box((0.55, 0.0, 1.2), (0.3, 0.3, 0.35), "M_paint_orange", bevel=0.01)
    a.cyl((0, 0.55, 0.85), 0.1, 0.1, 0.05, dark, seg=8, rot=(-45, 0, 0))
    a.col((0, 0, 0.75), (1.1, 1.3, 1.5))


@asset("prop_barrow_a", 481)
def barrow_a(a):
    org, dark, rub = "M_paint_orange", "M_metal_dark", "M_rubber"
    a.hexa([(-0.28, 0.0, 0.38), (0.28, 0.0, 0.38), (0.28, 0.5, 0.38), (-0.28, 0.5, 0.38),
            (-0.4, -0.1, 0.62), (0.4, -0.1, 0.62), (0.4, 0.66, 0.62), (-0.4, 0.66, 0.62)], org)
    a.box((0, 0.58, 0.64), (0.82, 0.04, 0.04), dark, bevel=0)
    a.cyl((0, -0.45, 0.2), 0.2, 0.2, 0.09, rub, seg=14, rot=(0, 90, 0))
    a.cyl((0, -0.45, 0.2), 0.07, 0.07, 0.13, dark, seg=8, rot=(0, 90, 0))
    for sx in (-1, 1):
        a.strut((sx * 0.34, -0.1, 0.45), (sx * 0.12, -0.45, 0.2), 0.05, dark)
        a.strut((sx * 0.3, 0.3, 0.4), (sx * 0.34, 0.95, 0.3), 0.04, dark)
        a.strut((sx * 0.3, 0.5, 0.4), (sx * 0.28, 0.65, 0.0), 0.05, dark)
        a.box((sx * 0.3, 0.95, 0.3), (0.04, 0.2, 0.04), rub, bevel=0)
    a.col((0, 0.2, 0.4), (0.8, 1.7, 0.8))


@asset("prop_trolley_a", 491)
def trolley_a(a):
    wood, dark, rub = "M_wood_pale", "M_metal_dark", "M_rubber"
    a.box((0, 0, 0.3), (1.1, 0.7, 0.05), wood, bevel=0.01)
    a.box((0, 0, 0.255), (1.0, 0.6, 0.04), dark, bevel=0.005)
    for sx in (-1, 1):
        for sy in (-1, 1):
            a.box((sx * 0.45, sy * 0.28, 0.2), (0.05, 0.05, 0.12), dark, bevel=0)
            a.cyl((sx * 0.45, sy * 0.28, 0.1), 0.1, 0.1, 0.05, rub, seg=10, rot=(0, 90, 0))
    for sy in (-1, 1):
        a.strut((-0.5, sy * 0.3, 0.32), (-0.58, sy * 0.3, 0.95), 0.035, dark)
    a.box((-0.58, 0, 0.95), (0.04, 0.66, 0.04), dark, bevel=0)
    a.box((-0.58, 0, 0.62), (0.04, 0.6, 0.04), dark, bevel=0)
    a.col((0, 0, 0.35), (1.15, 0.75, 0.7))


@asset("prop_planks_a", 501)
def planks_a(a):
    tones = ["M_wood_warm", "M_wood_pale", "M_wood_warm"]
    k = 0
    for layer in range(2):
        for i in range(3):
            a.box((-0.15 + i * 0.15, 0, 0.025 + layer * 0.05 + 0.002 * i), (0.14, 1.2, 0.045), tones[(i + layer) % 3], bevel=0.004, jitter_deg=0.5)
            k += 1
    a.box((0, 0, 0.115), (0.5, 0.06, 0.012), "M_wood_warm", bevel=0)
    a.col((0, 0, 0.06), (0.46, 1.2, 0.12))


@asset("prop_planks_a_broken", 502)
def planks_broken(a):
    a.chunk_mode = True
    rng = a.rng
    for i in range(6):
        if i == 0:
            a.cur.name = "prop_planks_a_chunk_01"
            a.cur.pivot = True
        else:
            a.new_part("prop_planks_a_chunk_%02d" % (i + 1), pivot=True)
        ln = 0.45 + rng.random() * 0.45
        a.box((0, 0, 0.0), (0.14, ln, 0.045), "M_wood_warm" if i % 2 else "M_wood_pale", bevel=0.004)
        a.box((0, ln / 2 + 0.04, 0.0), (0.14, 0.1, 0.045), "M_wood_pale", bevel=0.004, rot=(0, 0, rng.uniform(-30, 30)))   # kirik uc parcasi
        bmesh.ops.translate(a.cur.bm, verts=a.cur.bm.verts, vec=((i % 3 - 1) * 0.35, (i // 3 - 0.5) * 0.7, 0))


def crate_gen(a, s, brace=False):
    k = s / 0.8
    a.box((0, 0, 0.4 * k), (0.76 * k, 0.76 * k, 0.76 * k), "M_wood_warm", bevel=0.02 * k)
    for sx in (-1, 1):
        for sy in (-1, 1):
            a.box((sx * 0.35 * k, sy * 0.35 * k, 0.4 * k), (0.10 * k, 0.10 * k, 0.80 * k), "M_wood_dark", bevel=0.015 * k)
    for z in (0.04, 0.76):
        for sy in (-1, 1):
            a.box((0, sy * 0.35 * k, z * k), (0.60 * k, 0.10 * k, 0.08 * k), "M_wood_dark", bevel=0.012 * k)
        for sx in (-1, 1):
            a.box((sx * 0.35 * k, 0, z * k), (0.10 * k, 0.60 * k, 0.08 * k), "M_wood_dark", bevel=0.012 * k)
    if brace:
        for sy in (-1, 1):
            a.strut((-0.3 * k, sy * 0.385 * k, 0.1 * k), (0.3 * k, sy * 0.385 * k, 0.7 * k), 0.07 * k, "M_wood_dark")
            a.strut((0.3 * k, sy * 0.385 * k, 0.1 * k), (-0.3 * k, sy * 0.385 * k, 0.7 * k), 0.07 * k, "M_wood_dark")
    a.col((0, 0, 0.4 * k), (0.8 * k, 0.8 * k, 0.8 * k))


def crate_broken_gen(a, name, s):
    k = s / 0.8
    a.chunk_mode = True
    a.cur.name = name + "_chunk_01"
    panels = [((-0.45, 0.0, 0.30), (0.05, 0.76, 0.62), (0, -35, 0)), ((0.45, 0.1, 0.12), (0.05, 0.76, 0.62), (0, 70, 12)),
              ((0.0, 0.45, 0.28), (0.76, 0.05, 0.62), (30, 0, 0)), ((0.0, -0.45, 0.10), (0.76, 0.05, 0.62), (-72, 0, 10))]
    for i, (c, sz, r) in enumerate(panels):
        if i:
            a.new_part("%s_chunk_%02d" % (name, i + 1))
        a.box((c[0] * k, c[1] * k, c[2] * k), (sz[0] * k, sz[1] * k, sz[2] * k), "M_wood_warm", bevel=0.012 * k, rot=r)
    a.new_part("%s_chunk_05" % name)
    for sx, sy in ((-1, -1), (1, 1)):
        a.box((sx * 0.30 * k, sy * 0.30 * k, 0.05 * k), (0.10 * k, 0.10 * k, 0.80 * k), "M_wood_dark", bevel=0.012 * k, rot=(80, 8 * sx, 20 * sy))
    a.new_part("%s_chunk_06" % name)
    for sx, sy in ((1, -1), (-1, 1)):
        a.box((sx * 0.36 * k, sy * 0.40 * k, 0.06 * k), (0.10 * k, 0.10 * k, 0.60 * k), "M_wood_dark", bevel=0.012 * k, rot=(-76, 6 * sx, 40 * sy))
    a.new_part("%s_chunk_07" % name)
    a.box((0.1 * k, 0.05 * k, 0.05 * k), (0.76 * k, 0.76 * k, 0.05 * k), "M_wood_warm", bevel=0.012 * k, rot=(8, 14, 20))
    a.new_part("%s_chunk_08" % name)
    a.box((-0.15 * k, -0.2 * k, 0.025 * k), (0.60 * k, 0.10 * k, 0.08 * k), "M_wood_dark", bevel=0.01 * k, rot=(0, 0, 33))


@asset("prop_crate_b", 511)
def crate_b(a):
    crate_gen(a, 0.5)


@asset("prop_crate_b_broken", 512)
def crate_b_broken(a):
    crate_broken_gen(a, "prop_crate_b", 0.5)


@asset("prop_crate_c", 513)
def crate_c(a):
    crate_gen(a, 1.2, brace=True)


@asset("prop_crate_c_broken", 514)
def crate_c_broken(a):
    crate_broken_gen(a, "prop_crate_c", 1.2)


@asset("prop_toolwall_a", 521)
def toolwall_a(a):
    wp, wd, steel, dark, red = "M_wood_pale", "M_wood_dark", "M_metal_steel", "M_metal_dark", "M_paint_red"
    a.box((0, 0.0, 1.25), (3.0, 0.08, 1.9), wp, bevel=0.01)
    for sx in (-1, 1):
        a.box((sx * 1.46, -0.03, 1.25), (0.1, 0.14, 2.0), wd, bevel=0.01)
        a.box((sx * 1.2, 0.0, 0.15), (0.12, 0.5, 0.3), dark, bevel=0.01)
    a.box((0, -0.03, 2.22), (3.0, 0.14, 0.1), wd, bevel=0.01)
    a.box((0, -0.03, 0.34), (3.0, 0.14, 0.08), wd, bevel=0.01)
    # kapi golge silüetleri + aletler
    # cekic
    a.box((-1.1, -0.05, 1.8), (0.34, 0.03, 0.5), wd, bevel=0)
    a.box((-1.1, -0.08, 1.7), (0.05, 0.04, 0.4), wp if False else wd, bevel=0)
    a.box((-1.1, -0.09, 1.65), (0.05, 0.04, 0.42), wp, bevel=0.004)
    a.box((-1.1, -0.09, 1.9), (0.22, 0.05, 0.09), steel, bevel=0.005)
    # ingiliz anahtari
    a.box((-0.5, -0.05, 1.7), (0.3, 0.03, 0.82), wd, bevel=0)
    a.box((-0.5, -0.09, 1.58), (0.06, 0.04, 0.6), steel, bevel=0.004)
    a.box((-0.5, -0.09, 1.95), (0.17, 0.04, 0.12), steel, bevel=0.004)
    a.box((-0.5, -0.09, 1.2), (0.09, 0.05, 0.08), steel, bevel=0.004)
    # testere
    a.box((0.35, -0.05, 1.7), (0.95, 0.03, 0.4), wd, bevel=0)
    a.box((0.28, -0.09, 1.66), (0.7, 0.03, 0.18), steel, bevel=0)
    a.box((0.8, -0.09, 1.7), (0.16, 0.05, 0.22), wp, bevel=0.01)
    # kiskac
    a.box((1.1, -0.05, 1.7), (0.3, 0.03, 0.7), wd, bevel=0)
    a.strut((1.07, -0.09, 1.3), (1.12, -0.09, 1.75), 0.05, red)
    a.strut((1.15, -0.09, 1.3), (1.1, -0.09, 1.75), 0.05, red)
    # tornavida + metre
    a.strut((0.0, -0.09, 1.2), (0.0, -0.09, 1.55), 0.05, red)
    a.strut((0.0, -0.09, 1.55), (0.0, -0.09, 1.8), 0.025, steel)
    a.cyl((0.6, -0.09, 1.1), 0.12, 0.12, 0.07, red, seg=12, rot=(90, 0, 0))
    # raf: boya kutulari
    a.box((0, -0.15, 0.66), (2.8, 0.34, 0.06), wd, bevel=0.01)
    for i, x in enumerate((-1.0, -0.6, 0.6, 1.0)):
        a.cyl((x, -0.15, 0.78), 0.1, 0.1, 0.18, red if i % 2 else steel, seg=10)
    a.box((0, -0.15, 0.72), (0.5, 0.2, 0.1), wd, bevel=0.01)
    a.col((0, 0, 1.2), (3.0, 0.3, 2.4))


@asset("prop_scaffold_a", 531)
def scaffold_a(a):
    steel, dark, wood = "M_metal_steel", "M_metal_dark", "M_wood_warm"
    for sx in (-1, 1):
        for sy in (-1, 1):
            a.box((sx * 1.45, sy * 1.45, 1.5), (0.08, 0.08, 3.0), steel, bevel=0.004)
            a.box((sx * 1.45, sy * 1.45, 0.04), (0.3, 0.3, 0.08), dark, bevel=0.006)
    for z in (0.35, 1.45, 2.95):
        for s in (-1, 1):
            a.box((0, s * 1.45, z), (2.9, 0.07, 0.07), steel, bevel=0.003)
            a.box((s * 1.45, 0, z), (0.07, 2.9, 0.07), steel, bevel=0.003)
    for s in (-1, 1):
        a.strut((-1.45, s * 1.5, 0.3), (1.45, s * 1.5, 1.45), 0.05, dark)
        a.strut((1.45, s * 1.5, 1.45), (-1.45, s * 1.5, 2.9), 0.05, dark)
    for z in (0.7, 1.05):
        a.box((0, -1.45, z + 1.4 - 0.2), (2.9, 0.05, 0.05), steel, bevel=0)
    # kopan tahtalar: 4 ayri dugum, orijinleri kendi merkezleri
    for i in range(4):
        a.new_part("prop_scaffold_a_plank_%02d" % (i + 1), pivot=True)
        a.box((-1.02 + i * 0.68, 0, 1.54), (0.64, 2.9, 0.05), wood, bevel=0.005, jitter_deg=0.3)
    for sx in (-1, 1):
        a.col((sx * 1.45, 0, 1.5), (0.14, 3.0, 3.0))
    a.col((0, 0, 1.54), (2.9, 2.9, 0.08))


@asset("prop_rack_a", 541)
def rack_a(a):
    blue, org, wood, dark = "M_paint_blue", "M_paint_orange", "M_wood_pale", "M_metal_dark"
    for sx in (-1.25, 1.25):
        for sy in (-0.5, 0.5):
            a.box((sx, sy, 2.0), (0.09, 0.09, 4.0), blue, bevel=0.006)
            a.box((sx, sy, 0.03), (0.2, 0.2, 0.06), dark, bevel=0.005)
    for z in (0.45, 1.85, 3.25):
        for sy in (-0.5, 0.5):
            a.box((0, sy + (-0.07 if sy < 0 else 0.07), z), (2.7, 0.07, 0.14), org, bevel=0.006)
        for i in range(4):
            a.box((-0.9 + i * 0.6, 0, z + 0.06), (0.54, 1.0, 0.03), wood, bevel=0.004)
    for sx in (-1.25, 1.25):
        a.strut((sx, 0.5, 0.2), (sx, -0.5, 3.8), 0.04, dark)
    a.col((0, 0, 2.0), (2.7, 1.1, 4.0))


# ======================================================================================== ZEMIN / ARAZI
@asset("env_curb_a", 601)
def curb_a(a):
    conc, sand = "M_concrete", "M_plaster_sand"
    a.box((0, -0.65, 0.085), (2.0, 0.25, 0.17), conc, bevel=0.015)
    for i in range(2):
        a.box((-0.5 + i * 1.0, 0.2, 0.06), (0.97, 1.45, 0.12), sand if i == 0 else conc, bevel=0.01)
    a.col((0, 0, 0.09), (2.0, 1.8, 0.18))


@asset("env_ramp_a", 611)
def ramp_a(a):
    conc, yel = "M_concrete", "M_hazard_yellow"
    L = 4.5
    rise = L * math.tan(math.radians(12))
    a.wedge(-2.0, 2.0, -L / 2, L / 2, 0.0, 0.0, rise, conc)
    for sx in (-2.0, 1.7):
        a.hexa([(sx, -L / 2, 0.0), (sx + 0.3, -L / 2, 0.0), (sx + 0.3, L / 2, rise), (sx, L / 2, rise),
                (sx, -L / 2, 0.025), (sx + 0.3, -L / 2, 0.025), (sx + 0.3, L / 2, rise + 0.025), (sx, L / 2, rise + 0.025)], yel)
    for i in range(5):
        y = -L / 2 + 0.5 + i * 0.85
        z = (y + L / 2) / L * rise
        a.hexa([(-1.0, y, z - 0.03), (1.0, y, z - 0.03), (1.0, y + 0.12, z + 0.12 * math.tan(math.radians(12)) - 0.03), (-1.0, y + 0.12, z + 0.12 * math.tan(math.radians(12)) - 0.03),
                (-1.0, y, z + 0.015), (1.0, y, z + 0.015), (1.0, y + 0.12, z + 0.12 * math.tan(math.radians(12)) + 0.015), (-1.0, y + 0.12, z + 0.12 * math.tan(math.radians(12)) + 0.015)], yel)
    a.col_rot((0, 0, rise / 2 - 0.12), (4.0, L / math.cos(math.radians(12)), 0.25), (12, 0, 0))
    a.col((0, L / 2 - 0.1, rise / 2), (4.0, 0.2, rise))


@asset("env_hill_a", 621)
def hill_a(a):
    grass, foliage, conc = "M_grass", "M_foliage", "M_concrete"
    tiers = 10
    for i in range(tiers):
        f0, f1 = i / tiers, (i + 1) / tiers
        rx0, rx1 = 7.0 * (1 - f0 * 0.82), 7.0 * (1 - f1 * 0.82)
        ry0, ry1 = 5.0 * (1 - f0 * 0.82), 5.0 * (1 - f1 * 0.82)
        a.frustum((0, 0, 2.5 * ((i + 0.0) / tiers)), rx0, ry0, rx1, ry1, 2.5 / tiers, grass if i % 2 == 0 else foliage, seg=20)
    for k in range(5):
        t = a.rng.uniform(0, 6.28)
        a.ball((math.cos(t) * 5.0, math.sin(t) * 3.4, 0.3), 0.45 + 0.15 * (k % 2), conc, sub=1, scale=(1.2, 1, 0.7))
    a.col_frustum((0, 0, 0), 7.0, 5.0, 1.26, 0.9, 2.5, seg=16)


@asset("env_cobble_disc_a", 631)
def cobble_a(a):
    stones = ["M_concrete", "M_plaster_sand", CREAM, "M_roof_tile"]
    R, r0 = 14.0, 3.2
    a.annulus((0, 0), r0, R, 0.0, 0.07, "M_asphalt", seg=48)
    rng = a.rng
    r = r0 + 0.45
    ring = 0
    while r < R - 0.3:
        step = 0.82
        n = max(6, int(2 * math.pi * r / 1.18))
        off = rng.random() * 6.28
        for i in range(n):
            t = off + 2 * math.pi * i / n
            L = 2 * math.pi * r / n - 0.1
            u = rng.random()
            m = stones[0] if u < 0.52 else (stones[1] if u < 0.84 else (stones[2] if u < 0.94 else stones[3]))
            a.box((math.cos(t) * r, math.sin(t) * r, 0.07 + 0.015), (0.7, L, 0.03 + rng.random() * 0.012), m, bevel=0,
                  rot=(0, 0, math.degrees(t)))
        r += step
        ring += 1
    for i in range(48):                                                              # dis kenar tasi: koyu cerceve
        t = 360.0 * i / 48
        a.box((math.cos(math.radians(t)) * (R - 0.15), math.sin(math.radians(t)) * (R - 0.15), 0.06), (0.3, 2 * math.pi * R / 48 + 0.04, 0.11), "M_concrete", bevel=0,
              rot=(0, 0, t))
    for i in range(16):
        t = 360.0 * i / 16 + 11.25
        c, s = math.cos(math.radians(t)), math.sin(math.radians(t))
        a.col_rot(((R + r0) / 2 * c, (R + r0) / 2 * s, 0.05), (R - r0, 2 * math.pi * R / 16 + 0.3, 0.1), (0, 0, t))


@asset("env_asphalt_strip_a", 641)
def asphalt_a(a):
    asph, conc, yel, cream, dark = "M_asphalt", "M_concrete", "M_hazard_yellow", CREAM, "M_metal_dark"
    a.solid_box(-5, 5, -4, 4, 0.0, 0.12, asph)
    for sy in (-1, 1):
        a.box((0, sy * 3.88, 0.1), (10.0, 0.24, 0.2), conc, bevel=0.008)
        a.box((0, sy * 3.45, 0.126), (10.0, 0.08, 0.012), cream, bevel=0)
    for i in range(6):
        a.box((-4.1 + i * 1.64, 0, 0.126), (1.0, 0.14, 0.012), yel, bevel=0)
    a.cyl((2.5, 1.5, 0.13), 0.38, 0.38, 0.03, dark, seg=14)
    a.cyl((2.5, 1.5, 0.15), 0.28, 0.28, 0.012, "M_concrete", seg=12)
    a.col((0, 0, 0.1), (10.0, 8.0, 0.2))


# ======================================================================================== P2: EK PROPLAR / EV
@asset("bld_house_a", 701)
def house_a(a):
    W, D = 6.0, 5.0
    yf = -D / 2 + 0.15
    wall, trim, wood, green, roof, glass, conc = "M_plaster_cream", "M_plaster_cream", "M_wood_dark", "M_paint_green", "M_roof_tile", "M_glass", "M_concrete"
    Z1, Z2 = 3.0, 5.2
    a.wall_open(-W / 2, W / 2, yf, 0.3, 0.0, Z1, wall, [(1.4, 2.4, 0.0, 2.2), (-2.4, -0.6, 0.95, 2.25)])
    a.solid_box(-W / 2, W / 2, yf - 0.15, yf + 0.15, Z1, Z2, wall)
    a.solid_box(-W / 2, W / 2, yf + 0.15, D / 2, 0.0, Z2, wall)
    yfr = yf - 0.18
    a.box((0, yfr + 0.05, 0.25), (W + 0.05, 0.2, 0.5), conc, bevel=0.012)
    a.box((0, yfr + 0.05, Z1 + 0.05), (W + 0.1, 0.22, 0.14), trim, bevel=0.012)
    window(a, -1.5, yfr + 0.1, 1.6, 1.8, 1.3, wood)
    a.box((-1.5, yfr - 0.12, 0.95), (1.9, 0.34, 0.08), wood, bevel=0.01)
    a.box((1.9, yfr + 0.1, 1.1), (0.98, 0.1, 2.2), green, bevel=0.012)                       # kapi
    a.box((1.9, yfr + 0.03, 1.65), (0.5, 0.04, 0.7), glass, bevel=0)
    a.box((1.9, yfr - 0.45, 0.07), (1.5, 0.9, 0.14), conc, bevel=0.01)                       # basamak
    a.box((1.9, yfr - 0.85, 2.55), (1.8, 1.0, 0.1), wood, bevel=0.01)                        # kapi saçagi
    for sx in (-1, 1):
        a.box((1.9 + sx * 0.8, yfr - 1.25, 1.3), (0.09, 0.09, 2.6), wood, bevel=0.008)
    for cx in (-1.8, 0.0, 1.8):
        window(a, cx, yfr + 0.1, 4.1, 1.1, 1.4, wood)
        for sx in (-1, 1):
            a.box((cx + sx * 0.78, yfr + 0.02, 4.1), (0.36, 0.06, 1.4), green, bevel=0.008)
    a.box((0, 0, Z2 + 0.08), (W + 0.4, D + 0.4, 0.16), trim, bevel=0.012)
    a.prism(0, 0, Z2 + 0.16, W + 0.4, D + 0.6, 1.2, roof)
    a.box((1.8, 1.0, Z2 + 1.0), (0.6, 0.6, 1.4), conc, bevel=0.01)
    a.col((0, 0.1, Z2 / 2), (W, D - 0.2, Z2))


@asset("prop_barrier_a", 711)
def barrier_a(a):
    dark, yel, rub = "M_metal_dark", "M_hazard_yellow", "M_rubber"
    for sx in (-0.85, 0.85):
        a.strut((sx, -0.25, 0.0), (sx, 0.0, 0.95), 0.06, dark)
        a.strut((sx, 0.25, 0.0), (sx, 0.0, 0.95), 0.06, dark)
    for i in range(8):
        a.box((-0.9 + i * 0.257, 0, 0.8), (0.25, 0.05, 0.16), yel if i % 2 == 0 else rub, bevel=0.004)
        a.box((-0.9 + i * 0.257, 0, 0.58), (0.25, 0.05, 0.16), rub if i % 2 == 0 else yel, bevel=0.004)
    a.col((0, 0, 0.5), (2.0, 0.6, 1.0))


@asset("prop_awning_a", 721)
def awning_a(a):
    dark, can, blue = "M_wood_dark", "M_canvas", "M_paint_blue"
    for sx in (-1.4, 1.4):
        a.box((sx, -0.7, 1.15), (0.08, 0.08, 2.3), dark, bevel=0.008)
        a.box((sx, 0.7, 1.45), (0.08, 0.08, 2.9), dark, bevel=0.008)
    striped_awning(a, -1.5, 1.5, 0.75, 2.9, 0.7, 1.45, can, blue, n=8)
    a.col((0, 0, 1.2), (3.0, 1.5, 2.4))


@asset("prop_pipe_a", 731)
def pipe_a(a):
    steel, dark, red = "M_metal_steel", "M_metal_dark", "M_paint_red"
    a.cyl((0, 0, 0.6), 0.16, 0.16, 3.0, steel, seg=12, rot=(0, 90, 0))
    for x in (-1.45, 0.0, 1.45):
        a.cyl((x, 0, 0.6), 0.22, 0.22, 0.07, dark, seg=12, rot=(0, 90, 0))
    for x in (-1.0, 1.0):
        a.box((x, 0, 0.25), (0.1, 0.4, 0.5), dark, bevel=0.006)
        a.box((x, 0, 0.45), (0.14, 0.44, 0.06), dark, bevel=0.006)
    a.cyl((0.4, 0, 0.9), 0.04, 0.04, 0.3, dark, seg=6)
    a.cyl((0.4, 0, 1.05), 0.14, 0.14, 0.03, red, seg=10)
    a.col((0, 0, 0.45), (3.0, 0.5, 0.9))


@asset("prop_cart_a", 741)
def cart_a(a):
    wood, dark, rub, org, red = "M_wood_warm", "M_wood_dark", "M_rubber", "M_paint_orange", "M_paint_red"
    a.box((0, 0, 0.55), (1.5, 0.8, 0.07), wood, bevel=0.01)
    for sy in (-0.37, 0.37):
        a.box((0, sy, 0.7), (1.5, 0.05, 0.25), dark, bevel=0.008)
    a.box((0.72, 0, 0.7), (0.05, 0.8, 0.25), dark, bevel=0.008)
    for sy in (-0.5, 0.5):
        a.cyl((0.0, sy, 0.35), 0.35, 0.35, 0.07, rub, seg=14, rot=(90, 0, 0))
        a.cyl((0.0, sy, 0.35), 0.07, 0.07, 0.1, dark, seg=8, rot=(90, 0, 0))
    for sy in (-0.3, 0.3):
        a.strut((-0.7, sy, 0.6), (-1.15, sy, 0.95), 0.05, dark)
    a.box((-1.15, 0, 0.95), (0.05, 0.7, 0.05), dark, bevel=0)
    for i in range(3):
        for j in range(3):
            a.ball((-0.4 + i * 0.4, -0.2 + j * 0.2, 0.74), 0.08, org if (i + j) % 2 else red, sub=1)
    a.col((0, 0, 0.6), (1.6, 0.9, 1.2))


@asset("prop_clothesline_a", 751)
def clothesline_a(a):
    dark, red, blue, yel, cream = "M_metal_dark", "M_paint_red", "M_paint_blue", "M_paint_yellow", CREAM
    for sx in (-1.6, 1.6):
        a.cyl((sx, 0, 1.1), 0.045, 0.045, 2.2, dark, seg=8)
        a.box((sx, 0, 0.04), (0.3, 0.3, 0.08), dark, bevel=0.005)
    a.box((0, 0, 1.95), (3.2, 0.02, 0.02), dark, bevel=0)
    for i, (m, w, h) in enumerate(((red, 0.4, 0.55), (blue, 0.35, 0.7), (yel, 0.45, 0.5), (cream, 0.3, 0.6))):
        a.box((-1.1 + i * 0.75, 0, 1.95 - h / 2 - 0.02), (w, 0.02, h), m, bevel=0.003, jitter_deg=2)
    a.col((-1.6, 0, 1.1), (0.2, 0.2, 2.2))
    a.col((1.6, 0, 1.1), (0.2, 0.2, 2.2))


@asset("prop_fence_post_a", 761)
def fence_post_a(a):
    a.box((0, 0, 0.6), (0.12, 0.12, 1.2), "M_wood_dark", bevel=0.012)
    a.pyramid(0, 0, 1.2, 0.16, 0.16, 0.08, "M_wood_dark")
    a.col((0, 0, 0.6), (0.14, 0.14, 1.2))
