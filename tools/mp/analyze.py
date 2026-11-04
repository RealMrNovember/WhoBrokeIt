#!/usr/bin/env python3
"""Iki istemcili test cikti analizi: A, B ve sunucu gunlukleri. Kullanim: analyze.py <klasor>"""
import re, sys, os, math

d = sys.argv[1]
def read(n):
    p = os.path.join(d, n)
    return open(p, errors="replace").read().splitlines() if os.path.exists(p) else []

A, B, S = read("A.log"), read("B.log"), read("server.log")
res = []
def check(name, ok, detail=""):
    res.append(ok)
    print(("GECTI  " if ok else "KALDI  ") + name + (" - " + detail if detail else ""))
for _n, _l in (("A", A), ("B", B), ("sunucu", S)):
    check("gunluk var ve bos degil (" + _n + ")", len(_l) > 20, str(len(_l)) + " satir")

def samples(lines):
    out = []
    for l in lines:
        m = re.search(r"\[MP\] ([AB]) t=(\d+) (.*)", l)
        if not m: continue
        kv = dict(x.split("=", 1) for x in m.group(3).split() if "=" in x)
        out.append((int(m.group(2)), kv))
    return out
sa, sb = samples(A), samples(B)

# 1) hatalar (istisnalar): her uc gunlukte
exc = {n: [l for l in L if re.search(r"NotServerException|NullReferenceException|Exception:", l)] for n, L in (("A", A), ("B", B), ("sunucu", S))}
for n, v in exc.items():
    check(f"istisna yok ({n})", len(v) == 0, f"{len(v)} adet; ilki: {v[0][:140]}" if v else "")

# 2) dunya eslesmesi
def last(lines, pat):
    r = [l for l in lines if re.search(pat, l)]
    return r[-1] if r else ""
ra, rb = last(A, r"world ready"), last(B, r"world ready")
check("A dunya hazir (45+ nesne)", bool(ra), ra.split("[MP] ")[-1] if ra else "dunya hazir olmadi")
check("B dunya hazir (45+ nesne)", bool(rb), rb.split("[MP] ")[-1] if rb else "dunya hazir olmadi")
for n, r in (("A", ra), ("B", rb)):
    m = re.search(r"after ([\d.]+) s", r)
    if m: check(f"{n} 10 sn icinde hazir", float(m.group(1)) <= 10.0, m.group(1) + " sn")

srv = [(int(m.group(1)), int(m.group(2)), int(m.group(3))) for l in S for m in [re.search(r"\[MPSRV\] t=(\d+) clients=(\d+) objs=(\d+)", l)] if m]
if srv:
    t, c, o = srv[-1]
    check("sunucu iki istemci gordu", any(x[1] >= 2 for x in srv), f"son clients={c} objs={o}")
    if sa and sb:
        oa, ob = int(sa[-1][1].get("objs", 0)), int(sb[-1][1].get("objs", 0))
        # istemciler ayrilirken sayilar degisebilir: tam oturumdaki en yuksek degerleri karsilastir
        ma = max(int(s[1].get("objs", 0)) for s in sa); mb = max(int(s[1].get("objs", 0)) for s in sb); ms = max(x[2] for x in srv)
        check("nesne sayilari eslesiyor (A, B, sunucu)", ma == mb == ms, f"A={ma} B={mb} sunucu={ms}")
else:
    check("sunucu gunlugu", False, "[MPSRV] satiri yok")

# 3) arac ve nesne gorusu: A ve B ayni anda ayni seyi gormeli
def near(sx, t):
    best = None
    for s in sx:
        if best is None or abs(s[0] - t) < abs(best[0] - t): best = s
    return best if best and abs(best[0] - t) < 200 else None
def xy(v):
    try: return [float(x) for x in v.split(",")]
    except Exception: return None
maxd, n, dd = 0.0, 0, []
for t, kv in sa:
    o = near(sb, t)
    if not o or "fk" not in kv or "fk" not in o[1]: continue
    p, q = xy(kv["fk"]), xy(o[1]["fk"])
    if p and q: dd.append(math.hypot(p[0]-q[0], p[1]-q[1]))
if dd:
    check("forklift: A ve B ayni konumu goruyor (ort. fark < 1.5 m)", sum(dd)/len(dd) < 1.5, f"ort {sum(dd)/len(dd):.2f} m, en buyuk {max(dd):.2f} m, {len(dd)} orneklem")
    check("forklift: son konum A=B (< 0.5 m)", dd[-1] < 0.5, f"{dd[-1]:.2f} m")
else:
    check("forklift ornekleri", False, "karsilastirilacak orneklem yok")

cd = []
for t, kv in sa:
    o = near(sb, t)
    if not o or "crate" not in kv or "crate" not in o[1]: continue
    p = [float(x) for x in kv["crate"].split(",")]; q = [float(x) for x in o[1]["crate"].split(",")]
    cd.append(math.dist(p, q))
if cd:
    check("sandik: son konum A=B (< 0.5 m)", cd[-1] < 0.5, f"{cd[-1]:.2f} m, ort {sum(cd)/len(cd):.2f} m")

# 4) B'nin kendi karari ve A'nin sonucu
rbr = last(B, r"B RESULT"); print("B:", rbr.split("[MP] ")[-1] if rbr else "SONUC YOK")
check("B senaryo karari", any("B PASS" in l for l in B), "")
check("A senaryo tamamlandi", any("A DONE" in l for l in A) and not any("A FAIL" in l for l in A), last(A, r"A (DONE|FAIL)").split("[MP] ")[-1])
ex = [open(os.path.join(d, f)).read().strip() for f in os.listdir(d) if f.startswith("exit-")]
print("cikis kodlari:", ex)
print("SONUC:", "GECTI" if all(res) else "KALDI", f"({sum(res)}/{len(res)})")
sys.exit(0 if all(res) else 1)
