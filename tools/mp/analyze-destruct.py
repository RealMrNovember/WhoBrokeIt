#!/usr/bin/env python3
"""Yikim senaryosu analizi: A, B, C ve sunucu gunlukleri. Kullanim: analyze-destruct.py <klasor>"""
import re, sys, os

d = sys.argv[1]
def read(n):
    p = os.path.join(d, n)
    return open(p, errors="replace").read().splitlines() if os.path.exists(p) else []
A, B, C, S = read("A.log"), read("B.log"), read("C.log"), read("server.log")
res = []
def check(name, ok, detail=""):
    res.append(ok)
    print(("GECTI  " if ok else "KALDI  ") + name + (" - " + detail if detail else ""))

def samples(lines, who):
    out = []
    for l in lines:
        m = re.search(r"\[DS\] " + who + r" t=(\d+) (.*)", l)
        if not m: continue
        kv = dict(x.split("=", 1) for x in m.group(2).split() if "=" in x)
        out.append((int(m.group(1)), kv))
    return out
sa, sb, sc = samples(A, "A"), samples(B, "B"), samples(C, "C")
for n, l in (("A", A), ("B", B), ("C", C), ("sunucu", S)):
    check("gunluk var (" + n + ")", len(l) > 15, str(len(l)) + " satir")
# istisna taramasi
for n, l in (("A", A), ("B", B), ("C", C), ("sunucu", S)):
    bad = [x for x in l if re.search(r"NotServerException|NullReferenceException|InvalidOperationException|ArgumentException|Exception:", x)]
    check("istisna yok (" + n + ")", not bad, bad[0][:140] if bad else "")
# sunucu: kirilma ve onarim olaylari
dest = [l for l in S if "[DESTR]" in l and "Crate_1" in l]
check("sunucu: Crate_1 kirildi (->2)", any("->2 " in l for l in dest), "; ".join(l.split("[DESTR] ")[1][:60] for l in dest[:4]))
check("sunucu: Crate_1 onarildi (restored)", any("(restored)" in l for l in dest))
# A: kirdi ve onardi
check("A kirdi ('A BROKEN')", any("A BROKEN" in l for l in A))
check("A onardi ('A REPAIRED')", any("A REPAIRED" in l for l in A), next((l.split("[DS] ")[1] for l in A if "A REPAIRED" in l), ""))
# B ve A: durum 2 gordu, sonra 0; enkaz ve fx
def seq(s):
    out = []
    for _, kv in s:
        v = kv.get("st", "-")
        if v != "-" and (not out or out[-1] != v): out.append(v)
    return out
for n, s in (("A", sa), ("B", sb)):
    q = seq(s)
    check(n + " durum dizisi 0 -> ... -> 2 -> 0 (onarimdan sonra yeniden hasar serbest)", "2" in q and q[0] == "0" and "0" in q[q.index("2") + 1:], "dizi=" + ">".join(q))
    last = s[-1][1] if s else {}
    chunks = max((int(kv.get("chunks", 0)) for _, kv in s), default=0)
    check(n + " enkaz yerelde olustu (chunks>0)", chunks > 0, "chunks=" + str(chunks))
    check(n + " kirilma fx geldi (fxBreak>=1)", max((int(kv.get("fxBreak", 0)) for _, kv in s), default=0) >= 1)
    check(n + " onarim fx geldi (fxRepair>=1)", max((int(kv.get("fxRepair", 0)) for _, kv in s), default=0) >= 1)
# C: gec katilan
join = next((l for l in C if "[DS] C JOIN" in l), "")
m = re.search(r"state=(\d) chunks=(\d+) rendererOn=(\d) colliderOn=(\d)", join)
check("C gec katildi ve kirik durumu gordu (state=2)", bool(m) and m.group(1) == "2", join.split("[DS] ")[-1][:90])
check("C: kirik nesne gorunmuyor, carpisma kapali, gecmis enkaz tekrar OYNATILMADI", bool(m) and m.group(2) == "0" and m.group(3) == "0" and m.group(4) == "0", "chunks=%s renderer=%s collider=%s" % (m.groups()[1:] if m else "?",) if False else (("chunks=%s rendererOn=%s colliderOn=%s" % m.groups()[1:]) if m else "?"))
qc = seq(sc)
check("C onarimi gordu (2 -> 0)", "2" in qc and "0" in qc[qc.index("2") + 1:], "dizi=" + ">".join(qc))
# dunya nesne sayisi eslesmesi (A=B=C) ve elektrik panosu
def worlds(s): return {kv.get("world") for _, kv in s}
w = worlds(sa) | worlds(sb) | worlds(sc)
check("dunya nesne sayisi A=B=C", len(w) == 1, "world=" + ",".join(sorted(x for x in w if x)))
print("SONUC:", "GECTI" if all(res) else "KALDI", "(%d/%d)" % (sum(res), len(res)))
sys.exit(0 if all(res) else 1)
