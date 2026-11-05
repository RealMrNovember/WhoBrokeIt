"""Who Broke It? kit varlik kayit defteri (generate_kit.py ve kit_greenfield.py ortak kullanir) + yikim/fizik meta verisi.

manifest.json'a her varlik icin yazilan alanlar (yikim ve fizik kodu bunlari okur):
  materialTag : wood | metal | concrete | glass | ceramic | plastic | canvas | stone | foliage
  massKg      : nominal kutle (kg) — oyun icinde kullanilirken Rigidbody.mass icin baslangic degeri
  massClass   : light (<=8 kg, elle tasinir) | medium (8-60 kg, iki kisi / el arabasi) | heavy (>60 kg, forklift)
  breakable   : kirilabilir mi (yikim sistemi bunu okur)
"""
import re

REG = {}
META_OVERRIDE = {}


def asset(name, seed, tag=None, kg=None):
    def deco(fn):
        REG[name] = (fn, seed)
        if tag or kg is not None:
            META_OVERRIDE[name] = (tag, kg)
        return fn
    return deco


# (regex, materialTag, massKg, breakable) — ilk eslesen kazanir; "_broken" son eki ayrica ele alinir
RULES = [
    (r"^prop_crate_a", "wood", 14, True),
    (r"^prop_crate_b", "wood", 4, True),
    (r"^prop_crate_c", "wood", 38, True),
    (r"^prop_barrel", "metal", 20, True),
    (r"^prop_pallet", "wood", 18, True),
    (r"^prop_planks", "wood", 9, True),
    (r"^prop_scaffold", "metal", 80, True),
    (r"^prop_stall", "wood", 60, True),
    (r"^prop_fusebox", "metal", 25, True),
    (r"^prop_toolbox", "metal", 6, False),
    (r"^prop_toolwall", "wood", 40, True),
    (r"^prop_workbench", "wood", 70, True),
    (r"^prop_table", "wood", 22, True),
    (r"^prop_shelf", "metal", 35, True),
    (r"^prop_rack", "metal", 120, False),
    (r"^prop_sawhorse", "wood", 10, True),
    (r"^prop_cone", "plastic", 1.5, False),
    (r"^prop_sack", "canvas", 6, False),
    (r"^prop_cementmixer", "metal", 90, False),
    (r"^prop_barrow", "metal", 15, False),
    (r"^prop_trolley", "metal", 28, False),
    (r"^prop_bench", "wood", 45, True),
    (r"^prop_bin", "metal", 8, False),
    (r"^prop_hydrant", "metal", 60, False),
    (r"^prop_lamp", "metal", 120, False),
    (r"^prop_sign", "metal", 25, False),
    (r"^prop_tree", "foliage", 400, False),
    (r"^prop_planter", "ceramic", 55, True),
    (r"^prop_fountain", "stone", 5000, False),
    (r"^prop_barrier", "metal", 15, False),
    (r"^prop_pipe", "metal", 40, False),
    (r"^prop_cart", "wood", 30, True),
    (r"^prop_clothesline", "metal", 12, False),
    (r"^prop_fence_post", "wood", 6, True),
    (r"^prop_awning", "canvas", 25, True),
    (r"^prop_", "wood", 10, False),
    (r"^fnc_", "wood", 30, True),
    (r"^bld_halfwall", "stone", 900, True),
    (r"^bld_door", "wood", 25, True),
    (r"^bld_window", "glass", 15, True),
    (r"^bld_wall", "concrete", 2000, False),
    (r"^bld_shed", "metal", 600, False),
    (r"^bld_house", "concrete", 20000, False),
    (r"^bld_", "concrete", 20000, False),
    (r"^env_", "stone", 0, False),
]


def mass_class(kg):
    return "light" if kg <= 8 else ("medium" if kg <= 60 else "heavy")


def meta_for(name):
    base = name[:-7] if name.endswith("_broken") else name
    tag, kg, br = "wood", 10, False
    for rx, t, k, b in RULES:
        if re.match(rx, base):
            tag, kg, br = t, k, b
            break
    if base in META_OVERRIDE:
        t, k = META_OVERRIDE[base]
        tag = t or tag
        kg = k if k is not None else kg
    return {"materialTag": tag, "massKg": kg, "massClass": mass_class(kg), "breakable": br, "isBrokenVariant": name.endswith("_broken")}
