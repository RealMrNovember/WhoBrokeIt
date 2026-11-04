#!/usr/bin/env python3
"""Who Broke It? eşleştirme servisi.

Sabit sayıda oyun sunucusu (slot) çalışır. Her sunucu 5 sn'de bir /server/heartbeat ile oyuncu sayısını bildirir.
- POST /quickplay      herkese açık odaya yerleştirir (yer varsa dolu olana, yoksa boş bir slot açar)
- POST /rooms          özel oda açar, oda kodu döner
- GET  /rooms/<KOD>    kodla odayı bulur
- GET  /health         durum özeti
Boşalan oda REST_AFTER sn sonra sıfırlanır (sunucu kapanır, Docker yenisini başlatır).
Yalnızca standart kütüphane kullanır.
"""
import json
import os
import random
import threading
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

ADDRESS = os.environ.get("PUBLIC_ADDRESS", "127.0.0.1")
SLOTS = int(os.environ.get("SLOTS", "6"))
BASE_PORT = int(os.environ.get("BASE_PORT", "7771"))
CAPACITY = int(os.environ.get("CAPACITY", "4"))
STALE_AFTER = 20          # sn: kalp atışı gelmezse sunucu ölü sayılır
RESET_AFTER = int(os.environ.get("RESET_AFTER", "120"))   # sn: boş oda sıfırlanır
CREATE_LIMIT = (6, 60)    # IP başına 60 sn'de en fazla 6 oda/hızlı oyun isteği

LOCK = threading.Lock()
SLOT = {}
for i in range(SLOTS):
    SLOT["s%d" % (i + 1)] = {"port": BASE_PORT + i, "last": 0.0, "players": 0, "version": "",
                              "assign": None, "empty_since": None, "cooldown": 0.0}
HITS = {}

ALPHA = "ABCDEFGHJKLMNPQRSTUVWXYZ"
DIGITS = "23456789"


def now():
    return time.time()


def alive(s):
    return now() - s["last"] < STALE_AFTER


def new_code():
    used = {s["assign"]["code"] for s in SLOT.values() if s["assign"]}
    while True:
        code = "".join(random.choice(ALPHA) for _ in range(4)) + "-" + "".join(random.choice(DIGITS) for _ in range(2))
        if code not in used:
            return code


def free_slot():
    for name, s in SLOT.items():
        if alive(s) and s["assign"] is None and s["players"] == 0 and now() >= s["cooldown"]:
            return name, s
    return None, None


def room_json(name, s):
    a = s["assign"]
    return {"code": a["code"] if a["type"] == "private" else "", "type": a["type"], "address": ADDRESS,
            "port": s["port"], "players": s["players"], "capacity": CAPACITY, "version": s["version"]}


def rate_ok(ip):
    t = now()
    window = [x for x in HITS.get(ip, []) if t - x < CREATE_LIMIT[1]]
    if len(window) >= CREATE_LIMIT[0]:
        HITS[ip] = window
        return False
    window.append(t)
    HITS[ip] = window
    return True


def normalize(code):
    c = "".join(ch for ch in code.upper() if ch.isalnum())
    return c[:4] + "-" + c[4:6] if len(c) >= 6 else c


class Handler(BaseHTTPRequestHandler):
    server_version = "wbi-mm"

    def log_message(self, fmt, *args):
        pass

    def reply(self, code, obj):
        body = json.dumps(obj).encode()
        self.send_response(code)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        self.wfile.write(body)

    def body(self):
        n = int(self.headers.get("Content-Length") or 0)
        if n <= 0 or n > 4096:
            return {}
        try:
            return json.loads(self.rfile.read(n))
        except Exception:
            return {}

    def client_ip(self):
        return self.headers.get("CF-Connecting-IP") or self.headers.get("X-Real-IP") or self.client_address[0]

    def do_POST(self):
        path = self.path.split("?")[0].rstrip("/")
        if path == "/server/heartbeat":
            return self.heartbeat()
        if path in ("/quickplay", "/rooms"):
            if not rate_ok(self.client_ip()):
                return self.reply(429, {"error": "Çok sık denedin. Bir dakika sonra tekrar dene."})
            return self.quickplay() if path == "/quickplay" else self.create_room()
        self.reply(404, {"error": "bulunamadı"})

    def do_GET(self):
        path = self.path.split("?")[0].rstrip("/")
        if path.startswith("/rooms/"):
            return self.join(path[len("/rooms/"):])
        if path == "/status":
            with LOCK:
                up = [s for s in SLOT.values() if alive(s)]
                return self.reply(200, {"servers": len(up), "online": sum(s["players"] for s in up),
                                        "rooms": sum(1 for s in up if s["assign"] and s["players"] > 0),
                                        "free": sum(1 for s in up if s["assign"] is None and s["players"] == 0)})
        if path == "/health":
            with LOCK:
                return self.reply(200, {"slots": {k: {"alive": alive(v), "players": v["players"], "assign": v["assign"] and v["assign"]["type"], "version": v["version"]} for k, v in SLOT.items()}})
        self.reply(404, {"error": "bulunamadı"})

    # ---- sunucudan ----
    def heartbeat(self):
        b = self.body()
        name = b.get("slot")
        with LOCK:
            s = SLOT.get(name)
            if s is None:
                return self.reply(404, {"error": "bilinmeyen slot"})
            s["last"] = now()
            s["players"] = int(b.get("players", 0))
            s["version"] = str(b.get("version", ""))
            reset = False
            if s["assign"]:
                if s["players"] == 0:
                    if s["empty_since"] is None:
                        s["empty_since"] = now()
                    elif now() - s["empty_since"] > RESET_AFTER:
                        reset = True
                else:
                    s["empty_since"] = None
            if reset:
                s["assign"] = None
                s["empty_since"] = None
                s["cooldown"] = now() + 25      # sunucu yeniden başlarken kimseyi buraya yönlendirme
                s["last"] = 0.0
            a = s["assign"]
            self.reply(200, {"code": a["code"] if a and a["type"] == "private" else "", "type": a["type"] if a else "free", "reset": reset})

    # ---- oyuncudan ----
    def quickplay(self):
        with LOCK:
            best = None
            for name, s in SLOT.items():
                a = s["assign"]
                if alive(s) and a and a["type"] == "public" and s["players"] < CAPACITY:
                    if best is None or s["players"] > SLOT[best]["players"]:
                        best = name
            if best is None:
                name, s = free_slot()
                if s is None:
                    return self.reply(503, {"error": "Şu an tüm sunucular dolu. Birkaç dakika sonra tekrar dene."})
                s["assign"] = {"type": "public", "code": "", "at": now()}
                s["empty_since"] = now()
                best = name
            self.reply(200, room_json(best, SLOT[best]))

    def create_room(self):
        with LOCK:
            name, s = free_slot()
            if s is None:
                return self.reply(503, {"error": "Şu an tüm sunucular dolu. Birkaç dakika sonra tekrar dene."})
            s["assign"] = {"type": "private", "code": new_code(), "at": now()}
            s["empty_since"] = now()
            self.reply(200, room_json(name, s))

    def join(self, raw):
        code = normalize(raw)
        with LOCK:
            for name, s in SLOT.items():
                a = s["assign"]
                if a and a["type"] == "private" and a["code"] == code:
                    if not alive(s):
                        return self.reply(503, {"error": "Oda şu an yanıt vermiyor. Biraz sonra tekrar dene."})
                    if s["players"] >= CAPACITY:
                        return self.reply(409, {"error": "Oda dolu (en fazla %d kişi)." % CAPACITY})
                    return self.reply(200, room_json(name, s))
        self.reply(404, {"error": "Bu kodla bir oda bulunamadı. Kodu kontrol et."})


if __name__ == "__main__":
    port = int(os.environ.get("PORT", "8100"))
    print("matchmaker: %d slot, adres %s, port %d" % (SLOTS, ADDRESS, port), flush=True)
    ThreadingHTTPServer(("0.0.0.0", port), Handler).serve_forever()
