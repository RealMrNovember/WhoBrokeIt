using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using WhoBrokeIt.Audio;
using WhoBrokeIt.Player;

namespace WhoBrokeIt.Net
{
    /// <summary>
    /// Main menu: quick play, play with friends (room code), single player, character selection, settings, profile, credits, exit.
    /// A live 3D lobby (a stylised yard at golden hour with your character on a podium) is rendered behind the interface by a private camera;
    /// the interface itself comes from UiKit (palette, rounded buttons with hover/pressed/focus states, sounds, keyboard and mouse navigation).
    /// </summary>
    public class NetworkMenu : MonoBehaviour
    {
        public ushort lanPort = 7777;
        public static NetworkMenu Instance;

        enum Page { Main, Friends, Characters, Settings, Profile, Credits, Advanced }
        Page page = Page.Main;
        float pageT = 1f;
        bool confirmExit;
        string code = "";
        string address = "127.0.0.1";
        string status = "";
        float statusAt;
        bool busy;
        int opId;
        float connectingSince = -1f;
        string myIps = "";
        GUIStyle field;

        // character selection: charIndex is the one being looked at, savedIndex the confirmed one
        int charIndex, savedIndex;
        float dragYaw, idleSince;
        bool dragging;
        int toastChar = -1;
        float toastAt = -10f;

        // ---------- 3D lobby ----------
        Transform stage, props, podium, heroRoot, buddyRoot;
        Camera cam;
        RenderTexture rt;
        Light stageLight, prevSun;
        GameObject hero;
        readonly GameObject[] buddies = new GameObject[2];
        int heroIdx = -1, buddyFor = -1;
        float heroX = 1.9f, heroScale = 1f, popT = 1f, camBlend;
        Material baseMat;
        readonly List<Transform> floaters = new List<Transform>();
        readonly List<Vector3> floaterBase = new List<Vector3>();
        readonly List<float> floaterSeed = new List<float>();
        class Particle { public Transform t; public Vector3 v; public float life; }
        readonly List<Particle> burst = new List<Particle>();
        RenderTexture[] thumbs;
        bool visible;

        void Start()
        {
            var args = System.Environment.GetCommandLineArgs();
            if (System.Array.IndexOf(args, "-server") >= 0 || System.Array.IndexOf(args, "-smoke") >= 0 || System.Array.IndexOf(args, "-shots") >= 0 || System.Array.IndexOf(args, "-gfshots") >= 0) { enabled = false; return; }
            Instance = this;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            myIps = LocalAddresses();
            savedIndex = charIndex = CharacterCatalog.SavedChoice();
            var nm = NetworkManager.Singleton;
            if (nm != null) nm.OnClientDisconnectCallback += OnDisconnected;
        }

        void OnDestroy()
        {
            if (NetworkManager.Singleton != null) NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnected;
            if (rt != null) rt.Release();
            if (thumbs != null) foreach (var t in thumbs) if (t != null) t.Release();
            if (Instance == this) Instance = null;
        }

        void OnDisconnected(ulong id)
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || id != nm.LocalClientId) return;
            Say(string.IsNullOrEmpty(nm.DisconnectReason) ? "Bağlantı kesildi." : nm.DisconnectReason);
            busy = false;
            connectingSince = -1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Say(string text) { status = text; statusAt = Time.unscaledTime; }

        static string LocalAddresses()
        {
            var sb = new System.Text.StringBuilder();
            try
            {
                foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                    if (ip.AddressFamily == AddressFamily.InterNetwork) sb.Append(ip).Append("   ");
            }
            catch { }
            return sb.ToString();
        }

        // ---------- actions ----------
        void Connect(string ip, int port)
        {
            var nm = NetworkManager.Singleton;
            ((UnityTransport)nm.NetworkConfig.NetworkTransport).SetConnectionData(ip, (ushort)port);
            ConnectionGate.PrepareClient();
            connectingSince = Time.time;
            status = "";
            PlayerPrefs.SetInt("wbi_games", PlayerPrefs.GetInt("wbi_games", 0) + 1);
            if (!nm.StartClient()) { Say("Bağlantı başlatılamadı."); connectingSince = -1f; }
        }

        void HostLocal(string listen)
        {
            var nm = NetworkManager.Singleton;
            ((UnityTransport)nm.NetworkConfig.NetworkTransport).SetConnectionData("127.0.0.1", lanPort, listen);
            status = "";
            PlayerPrefs.SetInt("wbi_games", PlayerPrefs.GetInt("wbi_games", 0) + 1);
            if (!nm.StartHost()) Say("Oda kurulamadı (port dolu olabilir).");
        }

        System.Collections.IEnumerator Online(System.Collections.IEnumerator call, OnlineApi.Room[] box, string waitText)
        {
            var my = ++opId;
            busy = true;
            status = waitText;
            yield return call;
            if (my != opId) yield break;           // cancelled meanwhile
            busy = false;
            var r = box[0];
            if (r == null || !string.IsNullOrEmpty(r.error)) { Say(r != null && !string.IsNullOrEmpty(r.error) ? r.error : "Bir hata oldu."); yield break; }
            status = "Bağlanılıyor...";
            Connect(r.address, r.port);
        }

        void RunQuick() { var box = new OnlineApi.Room[1]; StartCoroutine(Online(OnlineApi.QuickPlay(r => box[0] = r), box, "Oda aranıyor...")); }
        void RunCreate() { var box = new OnlineApi.Room[1]; StartCoroutine(Online(OnlineApi.CreateRoom(r => box[0] = r), box, "Özel oda hazırlanıyor...")); }
        void RunJoin() { var box = new OnlineApi.Room[1]; StartCoroutine(Online(OnlineApi.JoinRoom(code, r => box[0] = r), box, "Oda bulunuyor...")); }

        void CancelConnect()
        {
            opId++;
            busy = false;
            connectingSince = -1f;
            var nm = NetworkManager.Singleton;
            if (nm != null && nm.IsListening) nm.Shutdown();
            status = "";
        }

        void Go(Page p)
        {
            page = p;
            pageT = 0f;
            UiKit.Focus = 0;
            UiKit.ResetHover();
            status = "";
            if (p == Page.Characters) { charIndex = savedIndex; dragYaw = 0f; idleSince = Time.unscaledTime; UiKit.Focus = 3; }
        }

        /// <summary>Screenshot tool: jumps to a page by name.</summary>
        public void DebugGo(string name)
        {
            confirmExit = false;
            switch (name)
            {
                case "characters": Go(Page.Characters); break;
                case "characters2": Go(Page.Characters); ChoosePreview(charIndex + 2); break;
                case "settings": Go(Page.Settings); break;
                case "profile": Go(Page.Profile); break;
                case "credits": Go(Page.Credits); break;
                case "friends": Go(Page.Friends); break;
                case "exit": Go(Page.Main); confirmExit = true; UiKit.Focus = 0; break;
                default: Go(Page.Main); break;
            }
        }

        static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------- character selection ----------
        void ChoosePreview(int index)
        {
            charIndex = (index + CharacterCatalog.Count) % CharacterCatalog.Count;
            dragYaw = 0f;
            idleSince = Time.unscaledTime;
            GameAudio.PlayUi(Sfx.UiClick);
        }

        void ConfirmCharacter()
        {
            savedIndex = charIndex;
            CharacterCatalog.Save(savedIndex);
            toastChar = savedIndex;
            toastAt = Time.unscaledTime;
            GameAudio.PlayUi(Sfx.Pickup);
            Go(Page.Main);
        }

        // ---------- 3D lobby construction ----------
        Material Mat(Color c)
        {
            if (baseMat == null) baseMat = Resources.Load<Material>("WBI/CharacterBase");
            var m = baseMat != null ? new Material(baseMat) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.color = c;
            return m;
        }

        GameObject Prim(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Color c, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            go.GetComponent<Renderer>().sharedMaterial = Mat(c);
            return go;
        }

        void Float(GameObject go, float seed) { floaters.Add(go.transform); floaterBase.Add(go.transform.localPosition); floaterSeed.Add(seed); }

        void BuildStage()
        {
            stage = new GameObject("MenuLobbyStage").transform;
            stage.position = new Vector3(500f, 500f, 500f);
            props = new GameObject("Props").transform; props.SetParent(stage, false);
            heroRoot = new GameObject("HeroRoot").transform; heroRoot.SetParent(stage, false);
            buddyRoot = new GameObject("BuddyRoot").transform; buddyRoot.SetParent(stage, false);

            // sky: a big warm sun and layered hills and a little town skyline
            Prim(PrimitiveType.Cylinder, props, new Vector3(2.0f, 4.6f, 22f), new Vector3(9f, 0.1f, 9f), new Color(1f, 0.93f, 0.62f), new Vector3(90f, 0f, 0f));
            Prim(PrimitiveType.Cylinder, props, new Vector3(2.0f, 4.6f, 23f), new Vector3(13f, 0.1f, 13f), new Color(1f, 0.74f, 0.45f), new Vector3(90f, 0f, 0f));
            Prim(PrimitiveType.Sphere, props, new Vector3(-12f, -4f, 28f), new Vector3(34f, 12f, 8f), new Color(0.50f, 0.42f, 0.66f));
            Prim(PrimitiveType.Sphere, props, new Vector3(14f, -3.5f, 22f), new Vector3(30f, 9f, 8f), new Color(0.28f, 0.58f, 0.60f));
            var skyline = new[] { new Vector4(-10f, 2.4f, 1.8f, 0.80f), new Vector4(-7.6f, 3.4f, 2.0f, 0.55f), new Vector4(-5.2f, 2.0f, 1.8f, 0.90f), new Vector4(5.6f, 2.8f, 2.2f, 0.62f), new Vector4(8.2f, 3.8f, 2.0f, 0.78f), new Vector4(10.6f, 2.2f, 1.8f, 0.50f) };
            var cols = new[] { new Color(0.97f, 0.62f, 0.52f), new Color(0.98f, 0.80f, 0.45f), new Color(0.55f, 0.78f, 0.88f), new Color(0.90f, 0.55f, 0.62f), new Color(0.62f, 0.86f, 0.70f), new Color(0.98f, 0.72f, 0.40f) };
            for (var i = 0; i < skyline.Length; i++)
            {
                var b = skyline[i];
                Prim(PrimitiveType.Cube, props, new Vector3(b.x, b.y * 0.5f - 0.2f, 14f), new Vector3(b.z, b.y, 1.6f), cols[i] * (0.75f + 0.25f * b.w));
                Prim(PrimitiveType.Cube, props, new Vector3(b.x, b.y - 0.1f, 14f), new Vector3(b.z + 0.25f, 0.18f, 1.8f), new Color(0.35f, 0.26f, 0.30f));
                for (var wy = 0; wy < 3; wy++) Prim(PrimitiveType.Cube, props, new Vector3(b.x, 0.7f + wy * 0.75f, 13.15f), new Vector3(b.z * 0.5f, 0.32f, 0.05f), new Color(1f, 0.93f, 0.62f));
            }
            // ground: sandy yard, grass ring, podium
            Prim(PrimitiveType.Cylinder, props, new Vector3(0f, -0.3f, 4f), new Vector3(34f, 0.2f, 34f), new Color(0.42f, 0.72f, 0.38f));
            Prim(PrimitiveType.Cylinder, props, new Vector3(0f, -0.2f, 3f), new Vector3(24f, 0.2f, 24f), new Color(0.96f, 0.84f, 0.62f));
            podium = new GameObject("Podium").transform; podium.SetParent(stage, false);
            Prim(PrimitiveType.Cylinder, podium, new Vector3(0f, -0.04f, 0f), new Vector3(2.9f, 0.08f, 2.9f), UiKit.SunDeep);
            Prim(PrimitiveType.Cylinder, podium, new Vector3(0f, 0.04f, 0f), new Vector3(2.5f, 0.08f, 2.5f), UiKit.Slate);
            Prim(PrimitiveType.Cylinder, podium, new Vector3(0f, 0.11f, 0f), new Vector3(2.15f, 0.04f, 2.15f), new Color(0.40f, 0.46f, 0.74f));

            // floating workshop things behind the hero (crates, barrels, tool shapes) and confetti
            var wood = new Color(0.78f, 0.54f, 0.30f);
            for (var i = 0; i < 6; i++)
            {
                var pos = new Vector3(-6f + i * 2.5f + (i % 2) * 0.6f, 1.6f + (i % 3) * 1.1f, 5f + (i % 2) * 2f);
                GameObject g;
                if (i % 3 == 0) g = Prim(PrimitiveType.Cube, props, pos, Vector3.one * 0.9f, wood);
                else if (i % 3 == 1) g = Prim(PrimitiveType.Cylinder, props, pos, new Vector3(0.7f, 0.5f, 0.7f), UiKit.Coral);
                else g = Prim(PrimitiveType.Cube, props, pos, new Vector3(0.9f, 0.25f, 0.4f), UiKit.Sun);
                Float(g, i * 1.7f);
            }
            var palette = new[] { UiKit.Sun, UiKit.Sky, UiKit.Coral, UiKit.Mint, UiKit.Cream };
            for (var i = 0; i < 30; i++)
            {
                var g = Prim(i % 3 == 0 ? PrimitiveType.Sphere : PrimitiveType.Cube, props, new Vector3(Random.Range(-7f, 7f), Random.Range(0.3f, 6f), Random.Range(-3f, 10f)), Vector3.one * Random.Range(0.08f, 0.2f), palette[i % palette.Length]);
                Float(g, i * 0.9f);
            }

            // light: warm key from the front-left, cool rim from behind
            var lg = new GameObject("StageLight").transform; lg.SetParent(stage, false);
            lg.rotation = Quaternion.Euler(38f, -28f, 0f);
            stageLight = lg.gameObject.AddComponent<Light>();
            stageLight.type = LightType.Directional;
            stageLight.color = new Color(1f, 0.88f, 0.70f);
            stageLight.intensity = 0.8f;
            stageLight.shadows = LightShadows.Soft;
            stageLight.shadowStrength = 0.75f;
            var rim = new GameObject("Rim").transform; rim.SetParent(stage, false);
            rim.localPosition = new Vector3(2.5f, 3f, 3.5f);
            var rl = rim.gameObject.AddComponent<Light>();
            rl.type = LightType.Point; rl.color = new Color(0.55f, 0.78f, 1f); rl.intensity = 2.5f; rl.range = 14f;

            var camGo = new GameObject("MenuCamera");
            camGo.transform.SetParent(stage, false);
            cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 32f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.99f, 0.66f, 0.44f);
            cam.depth = -10f;
            cam.allowMSAA = false;
            cam.allowHDR = false;
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 80f;
            var data = camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            SetLayer(stage.gameObject);
            cam.cullingMask = 1 << 30;
            stageLight.cullingMask = 1 << 30;
            rl.cullingMask = 1 << 30;
        }

        static void SetLayer(GameObject root) { foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 30; }

        // render textures must use the same sample count as the URP asset, otherwise the pass fails and the lobby stays black
        static int Msaa()
        {
            return 1;   // the menu camera has MSAA switched off (cam.allowMSAA), so its targets are single-sample
        }

        void EnsureTarget()
        {
            if (rt != null && rt.width == Screen.width && rt.height == Screen.height && rt.antiAliasing == Msaa()) return;
            if (rt != null) { cam.targetTexture = null; rt.Release(); }
            rt = new RenderTexture(Mathf.Max(64, Screen.width), Mathf.Max(64, Screen.height), 24, RenderTextureFormat.ARGB32) { antiAliasing = Msaa() };
            cam.targetTexture = rt;
        }

        GameObject Spawn(int index, Transform root)
        {
            var m = CharacterCatalog.Build(index, root);
            SetLayer(m);
            return m;
        }

        void Burst(Vector3 at, Color c)
        {
            for (var i = 0; i < 18; i++)
            {
                var g = Prim(i % 2 == 0 ? PrimitiveType.Cube : PrimitiveType.Sphere, stage, at, Vector3.one * Random.Range(0.06f, 0.14f), i % 3 == 0 ? Color.white : c);
                g.layer = 30;
                burst.Add(new Particle { t = g.transform, v = Random.onUnitSphere * Random.Range(1.5f, 4f) + Vector3.up * 2f, life = 0.9f });
            }
        }

        void RefreshHero()
        {
            var want = page == Page.Characters ? charIndex : savedIndex;
            if (heroIdx == want && hero != null) return;
            var first = hero == null;
            if (hero != null) Destroy(hero);
            hero = Spawn(want, heroRoot);
            heroIdx = want;
            if (!first) { popT = 0f; Burst(heroRoot.position + new Vector3(0f, 1.0f, 0f), CharacterCatalog.All[want].accent); }
            // buddies are the next two characters in the list
            if (buddyFor == savedIndex && buddies[0] != null) return;
            buddyFor = savedIndex;
            for (var i = 0; i < 2; i++)
            {
                if (buddies[i] != null) Destroy(buddies[i]);
                buddies[i] = Spawn((savedIndex + 1 + i) % CharacterCatalog.Count, buddyRoot);
            }
        }

        void BuildThumbs()
        {
            if (thumbs != null) return;
            thumbs = new RenderTexture[CharacterCatalog.Count];
            var pPos = cam.transform.localPosition; var pRot = cam.transform.localRotation;
            var pFov = cam.fieldOfView; var pBg = cam.backgroundColor; var pTarget = cam.targetTexture;
            props.gameObject.SetActive(false); heroRoot.gameObject.SetActive(false); buddyRoot.gameObject.SetActive(false); podium.gameObject.SetActive(false);
            var room = new GameObject("ThumbRoom").transform; room.SetParent(stage, false); room.localPosition = new Vector3(0f, 0f, 60f);
            try
            {
                cam.fieldOfView = 26f;
                cam.transform.localPosition = new Vector3(0f, 1.05f, 60f - 5.2f);
                cam.transform.localRotation = Quaternion.LookRotation(new Vector3(0f, 0.95f, 60f) - cam.transform.localPosition, Vector3.up);
                for (var i = 0; i < thumbs.Length; i++)
                {
                    thumbs[i] = new RenderTexture(256, 320, 24, RenderTextureFormat.ARGB32) { antiAliasing = Msaa() };
                    var m = Spawn(i, room);
                    m.transform.localRotation = Quaternion.Euler(0f, 205f, 0f);
                    var a = CharacterCatalog.All[i].accent;
                    cam.backgroundColor = Color.Lerp(a, Color.white, 0.12f) * 0.75f;
                    cam.targetTexture = thumbs[i];
                    cam.Render();
                    DestroyImmediate(m);
                }
            }
            catch (System.Exception ex) { Debug.LogWarning("[WBI] karakter küçük resimleri çizilemedi: " + ex.Message); }
            Destroy(room.gameObject);
            props.gameObject.SetActive(true); heroRoot.gameObject.SetActive(true); buddyRoot.gameObject.SetActive(true); podium.gameObject.SetActive(true);
            cam.fieldOfView = pFov; cam.backgroundColor = pBg; cam.transform.localPosition = pPos; cam.transform.localRotation = pRot; cam.targetTexture = pTarget;
        }

        bool MenuVisible()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || InstallGuard.Blocked) return false;
            return !(nm.IsListening && (nm.IsHost || nm.IsConnectedClient || (nm.IsServer && !nm.IsClient)));
        }

        // ---------- per frame ----------
        void Update()
        {
            var nm = NetworkManager.Singleton;
            var vis = MenuVisible();
            if (vis != visible)
            {
                visible = vis;
                if (stage != null) stage.gameObject.SetActive(vis);
                if (cam != null) cam.enabled = vis;
                if (vis) { if (stageLight != null) { prevSun = RenderSettings.sun; RenderSettings.sun = stageLight; } }
                else if (stageLight != null && RenderSettings.sun == stageLight) RenderSettings.sun = prevSun;
                GameAudio.MenuMusic(vis);
            }
            if (!vis) return;
            if (stage == null) { BuildStage(); cam.enabled = true; prevSun = RenderSettings.sun; RenderSettings.sun = stageLight; GameAudio.MenuMusic(true); }
            EnsureTarget();
            RefreshHero();
            if (page == Page.Characters) BuildThumbs();

            // connection timeout
            if (nm != null && connectingSince > 0f && nm.IsConnectedClient) connectingSince = -1f;
            if (nm != null && connectingSince > 0f && Time.time - connectingSince > 15f)
            {
                nm.Shutdown();
                connectingSince = -1f;
                busy = false;
                Say("Sunucuya bağlanılamadı (zaman aşımı). Biraz sonra tekrar dene.");
            }
            if (!string.IsNullOrEmpty(status) && !busy && connectingSince < 0f && Time.unscaledTime - statusAt > 7f) status = "";

            pageT = Mathf.MoveTowards(pageT, 1f, Time.unscaledDeltaTime / 0.28f);
            UpdateInput(nm);
            UpdateStage();
        }

        bool typing;
        void UpdateInput(NetworkManager nm)
        {
            UiKit.KeyActivate = false;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            var connecting = busy || (nm != null && nm.IsListening && nm.IsClient && !nm.IsConnectedClient);
            if (kb.escapeKey.wasPressedThisFrame)
            {
                if (connecting) CancelConnect();
                else if (confirmExit) { confirmExit = false; UiKit.Focus = 0; }
                else if (page != Page.Main) { GameAudio.PlayUi(Sfx.UiBack); Go(Page.Main); }
                else { confirmExit = true; UiKit.Focus = 0; }
                return;
            }
            if (connecting || typing) return;
            var n = Mathf.Max(1, UiKit.Count);
            if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame) UiKit.Focus = (UiKit.Focus + 1) % n;
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) UiKit.Focus = (UiKit.Focus + n - 1) % n;
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) UiKit.KeyActivate = true;
            if (page == Page.Characters && !confirmExit)
            {
                if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) ChoosePreview(charIndex - 1);
                if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) ChoosePreview(charIndex + 1);
            }
        }

        void UpdateStage()
        {
            var dt = Time.unscaledDeltaTime;
            var chars = page == Page.Characters;
            var targetX = chars ? 0f : 1.9f;
            heroX = Mathf.Lerp(heroX, targetX, 1f - Mathf.Exp(-6f * dt));
            var targetScale = chars ? 1.08f : 1f;
            heroScale = Mathf.Lerp(heroScale, targetScale, 1f - Mathf.Exp(-6f * dt));
            popT = Mathf.Min(1f, popT + dt / 0.7f);
            var pop = 1f + Mathf.Sin(popT * Mathf.PI * 3f) * 0.16f * (1f - popT);
            heroRoot.localPosition = new Vector3(heroX, Mathf.Sin(Time.unscaledTime * 1.6f) * 0.03f, 0f);
            heroRoot.localScale = Vector3.one * heroScale * pop;
            podium.localPosition = new Vector3(heroX, 0f, 0f);
            podium.localScale = Vector3.one * heroScale;

            if (hero != null)
            {
                float yaw;
                if (chars)
                {
                    if (!dragging && Time.unscaledTime - idleSince > 2.5f) dragYaw += 24f * dt;
                    yaw = 180f + dragYaw;
                }
                else yaw = 180f + Mathf.Sin(Time.unscaledTime * 0.8f) * 22f - 18f;
                hero.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            }
            var bt = chars ? 0f : 1f;
            buddyRoot.localScale = Vector3.Lerp(buddyRoot.localScale, Vector3.one * bt, 1f - Mathf.Exp(-7f * dt));
            buddyRoot.gameObject.SetActive(buddyRoot.localScale.x > 0.02f);
            if (buddies[0] != null) { buddies[0].transform.localPosition = new Vector3(0.2f, Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.2f)) * 0.06f, 1.6f); buddies[0].transform.localRotation = Quaternion.Euler(0f, 180f + 24f + Mathf.Sin(Time.unscaledTime * 0.9f) * 14f, 0f); }
            if (buddies[1] != null) { buddies[1].transform.localPosition = new Vector3(3.55f, Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.0f + 1f)) * 0.06f, 1.3f); buddies[1].transform.localRotation = Quaternion.Euler(0f, 180f - 28f + Mathf.Sin(Time.unscaledTime * 0.7f) * 14f, 0f); }

            // camera framing per page
            camBlend = Mathf.Lerp(camBlend, chars ? 1f : 0f, 1f - Mathf.Exp(-5f * dt));
            var pos = Vector3.Lerp(new Vector3(0f, 1.55f, -7.6f), new Vector3(0f, 1.5f, -6.8f), camBlend);
            var look = Vector3.Lerp(new Vector3(0f, 1.05f, 0f), new Vector3(0f, 0.72f, 0f), camBlend);
            cam.transform.localPosition = pos;
            cam.transform.localRotation = Quaternion.LookRotation(look - pos, Vector3.up);

            if (Time.frameCount % 120 == 0 && System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-menushot") >= 0) DiagnoseLobby();
            var t = Time.unscaledTime;
            for (var i = 0; i < floaters.Count; i++)
            {
                var b = floaterBase[i]; var sd = floaterSeed[i];
                floaters[i].localPosition = b + new Vector3(Mathf.Sin(t * 0.4f + sd) * 0.3f, Mathf.Sin(t * 0.7f + sd) * 0.25f, 0f);
                floaters[i].localRotation = Quaternion.Euler(t * (12f + sd * 3f), t * (18f + sd * 2f), 0f);
            }
            for (var i = burst.Count - 1; i >= 0; i--)
            {
                var p = burst[i];
                p.life -= dt;
                if (p.life <= 0f) { Destroy(p.t.gameObject); burst.RemoveAt(i); continue; }
                p.v += Vector3.down * 9f * dt;
                p.t.position += p.v * dt;
                p.t.localScale *= 1f - 1.4f * dt;
            }
        }

        void DiagnoseLobby()
        {
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(1, 1, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(rt.width * 0.8f, rt.height * 0.5f, 1, 1), 0, 0);
            var c = tex.GetPixel(0, 0);
            RenderTexture.active = prev;
            Destroy(tex);
            Debug.Log("[WBI] lobby cam enabled=" + cam.isActiveAndEnabled + " stageActive=" + stage.gameObject.activeInHierarchy + " target=" + (cam.targetTexture != null) + " rt=" + rt.width + "x" + rt.height + " px=" + c + " hero=" + (hero != null) + " camPos=" + cam.transform.position + " cullMask=" + cam.cullingMask + " pipeline=" + (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null));
        }

        // ---------- UI ----------
        void Styles()
        {
            if (field != null) return;
            field = new GUIStyle(GUI.skin.textField) { fontSize = 34, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            field.normal.textColor = UiKit.Cream; field.focused.textColor = Color.white; field.hover.textColor = Color.white;
            field.normal.background = field.focused.background = field.hover.background = Texture2D.blackTexture;
            field.normal.background = field.hover.background = field.focused.background = null;
        }

        Texture2D logo;

        void OnGUI()
        {
            if (!visible || stage == null) return;
            Styles();
            var e = Event.current;
            var nm = NetworkManager.Singleton;
            var connecting = busy || (nm != null && nm.IsListening && nm.IsClient && !nm.IsConnectedClient);

            // 3D lobby fills the screen
            if (e.type == EventType.Repaint)
            {
                GUI.matrix = Matrix4x4.identity;
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), rt, ScaleMode.StretchToFill, false);
            }
            var s = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            var W = Screen.width / s; var H = Screen.height / s;

            UiKit.BeginPass();
            UiKit.Locked = confirmExit || connecting;
            var ease = UiKit.Ease(pageT);
            var prevColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, ease);
            var slide = (1f - ease) * 50f;

            typing = false;
            switch (page)
            {
                case Page.Main: DrawMain(W, H, slide); break;
                case Page.Friends: DrawFriends(W, H, slide); break;
                case Page.Characters: DrawCharacters(W, H, slide); break;
                case Page.Settings: DrawSettings(W, H, slide); break;
                case Page.Profile: DrawProfile(W, H, slide); break;
                case Page.Credits: DrawCredits(W, H, slide); break;
                default: DrawAdvanced(W, H, slide); break;
            }
            GUI.color = prevColor;
            UiKit.Locked = false;

            // transient messages
            if (!connecting && !confirmExit && !string.IsNullOrEmpty(status))
            {
                var r = new Rect(W / 2f - 420, H - 150, 840, 90);
                UiKit.Card(r, new Color(0.62f, 0.16f, 0.2f, 0.95f), 24f);
                UiKit.Text(new Rect(r.x + 20, r.y, r.width - 40, r.height), status, 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, false);
            }
            if (toastChar >= 0 && Time.unscaledTime - toastAt < 2.4f)
            {
                var a = Mathf.Clamp01(Mathf.Min((Time.unscaledTime - toastAt) * 6f, (2.4f - (Time.unscaledTime - toastAt)) * 3f));
                var gc = GUI.color; GUI.color = new Color(1, 1, 1, a);
                var r = new Rect(W / 2f - 330, 36, 660, 76);
                UiKit.Card(r, new Color(0.14f, 0.5f, 0.34f, 0.96f), 24f);
                UiKit.Text(r, "✓  " + CharacterCatalog.All[toastChar].name + " seçildi", 32, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                GUI.color = gc;
            }

            if (connecting) DrawConnecting(W, H);
            else if (confirmExit) DrawExitDialog(W, H);
            UiKit.EndPass();
        }

        // ---- left card shared by the list-like pages ----
        Rect LeftCard(float H, string title, string subtitle, float slide)
        {
            var card = new Rect(60 - slide, 30, 640, Mathf.Max(1020f, H - 60f));
            UiKit.Card(card, UiKit.Panel, 38f);
            if (logo == null) logo = Resources.Load<Texture2D>("Brand/logo");
            if (title == null)
            {
                if (logo != null) GUI.DrawTexture(new Rect(card.x + 30, card.y + 18, 580, 150), logo, ScaleMode.ScaleToFit, true);
                else UiKit.Text(new Rect(card.x + 30, card.y + 30, 580, 100), "Who Broke It?", 64, UiKit.Sun, TextAnchor.MiddleCenter, FontStyle.Bold);
                UiKit.Text(new Rect(card.x + 30, card.y + 166, 580, 40), "Birlikte kur, birlikte bozdur, birlikte çöz", 22, UiKit.Sky, TextAnchor.MiddleCenter, FontStyle.Bold, false);
            }
            else
            {
                UiKit.Text(new Rect(card.x + 40, card.y + 34, 560, 70), title, 52, UiKit.Sun, TextAnchor.MiddleLeft, FontStyle.Bold);
                if (!string.IsNullOrEmpty(subtitle)) UiKit.Text(new Rect(card.x + 40, card.y + 100, 560, 56), subtitle, 22, new Color(1, 1, 1, 0.75f), TextAnchor.UpperLeft, FontStyle.Normal, false);
            }
            UiKit.Text(new Rect(card.x + 30, card.yMax - 52, 580, 34), "Sürüm " + Application.version + "   ·   Esc geri   ·   ↑ ↓ Enter", 18, new Color(1, 1, 1, 0.45f), TextAnchor.MiddleCenter, FontStyle.Normal, false);
            return card;
        }

        void DrawMain(float W, float H, float slide)
        {
            var c = LeftCard(H, null, null, slide);
            var x = c.x + 30; var w = 580f; var y = c.y + 226; const float h = 76f; const float gap = 14f;
            if (UiKit.Button(new Rect(x, y, w, h), "Hızlı Oyun", UiKit.Kind.Primary)) RunQuick(); y += h + gap;
            if (UiKit.Button(new Rect(x, y, w, h), "Arkadaşlarla Oyna")) Go(Page.Friends); y += h + gap;
            if (UiKit.Button(new Rect(x, y, w, h), "Tek Kişilik")) HostLocal("127.0.0.1"); y += h + gap;
            if (UiKit.Button(new Rect(x, y, w, h), "Karakter Seçimi")) Go(Page.Characters); y += h + gap;
            if (UiKit.Button(new Rect(x, y, w, h), "Ayarlar")) Go(Page.Settings); y += h + gap;
            if (UiKit.Button(new Rect(x, y, w, h), "Oyuncu Profili")) Go(Page.Profile); y += h + gap;
            if (UiKit.Button(new Rect(x, y, w, h), "Emeği Geçenler")) Go(Page.Credits); y += h + gap;
            if (UiKit.Button(new Rect(x, y, w, h), "Oyundan Çık", UiKit.Kind.Danger)) { confirmExit = true; UiKit.Focus = 0; }

            // little name tag for the hero on the right
            var e = CharacterCatalog.All[savedIndex];
            var tag = new Rect(W - 560, H - 190, 470, 110);
            var gc = GUI.color;
            UiKit.Card(tag, new Color(UiKit.Ink.r, UiKit.Ink.g, UiKit.Ink.b, 0.78f), 26f);
            UiKit.Fill(new Rect(tag.x, tag.y, 14, tag.height), e.accent, 7f);
            UiKit.Text(new Rect(tag.x + 34, tag.y + 12, 420, 50), e.name, 38, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiKit.Text(new Rect(tag.x + 34, tag.y + 60, 420, 40), e.tag, 19, e.accent, TextAnchor.MiddleLeft, FontStyle.Bold, false);
            GUI.color = gc;
        }

        void DrawFriends(float W, float H, float slide)
        {
            var c = LeftCard(H, "Arkadaşlarla Oyna", "Özel oda aç ve kodu arkadaşlarına söyle, ya da onların kodunu yaz.", slide);
            var x = c.x + 30; var w = 580f; var y = c.y + 190;
            if (UiKit.Button(new Rect(x, y, w, 84), "Özel Oda Kur", UiKit.Kind.Primary)) RunCreate(); y += 92;
            UiKit.Text(new Rect(x, y, w, 56), "Oda kodu oyunun içinde ekranın sağ üstünde görünür.", 20, new Color(1, 1, 1, 0.7f), TextAnchor.UpperLeft, FontStyle.Normal, false); y += 80;
            UiKit.Text(new Rect(x, y, w, 36), "ARKADAŞININ ODA KODU", 20, UiKit.Sky, TextAnchor.MiddleLeft, FontStyle.Bold, false); y += 42;
            var fr = new Rect(x, y, w, 76);
            UiKit.Fill(fr, new Color(0f, 0f, 0f, 0.35f), 20f);
            UiKit.Outline(fr, new Color(1f, 1f, 1f, GUI.GetNameOfFocusedControl() == "code" ? 0.9f : 0.25f), 3f, 20f);
            GUI.SetNextControlName("code");
            code = GUI.TextField(fr, code, 10, field).ToUpperInvariant();
            if (GUI.GetNameOfFocusedControl() == "code")
            {
                typing = true;
                var ev = Event.current;
                if (ev.type == EventType.KeyDown && (ev.keyCode == KeyCode.Return || ev.keyCode == KeyCode.KeypadEnter) && code.Trim().Length >= 4) { ev.Use(); RunJoin(); }
            }
            y += 92;
            if (UiKit.Button(new Rect(x, y, w, 80), "Koda Katıl", UiKit.Kind.Normal, code.Trim().Length >= 4)) RunJoin(); y += 100;
            if (UiKit.Button(new Rect(x, y, w, 62), "Gelişmiş: IP ile bağlan / LAN", UiKit.Kind.Ghost, true, 22)) Go(Page.Advanced);
            if (UiKit.Button(new Rect(x, c.yMax - 150, w, 76), "← Geri", UiKit.Kind.Ghost)) Go(Page.Main);
        }

        void DrawAdvanced(float W, float H, float slide)
        {
            var c = LeftCard(H, "Gelişmiş", "Aynı ağdakilerle ya da VPN üzerinden doğrudan oynamak için.", slide);
            var x = c.x + 30; var w = 580f; var y = c.y + 190;
            if (UiKit.Button(new Rect(x, y, w, 80), "LAN Odası Kur", UiKit.Kind.Primary)) HostLocal("0.0.0.0"); y += 88;
            UiKit.Text(new Rect(x, y, w, 56), "Bu bilgisayar: " + myIps + "  ·  UDP " + lanPort, 20, new Color(1, 1, 1, 0.7f), TextAnchor.UpperLeft, FontStyle.Normal, false); y += 80;
            UiKit.Text(new Rect(x, y, w, 36), "KATILINACAK ADRES", 20, UiKit.Sky, TextAnchor.MiddleLeft, FontStyle.Bold, false); y += 42;
            var fr = new Rect(x, y, w, 76);
            UiKit.Fill(fr, new Color(0f, 0f, 0f, 0.35f), 20f);
            UiKit.Outline(fr, new Color(1f, 1f, 1f, GUI.GetNameOfFocusedControl() == "addr" ? 0.9f : 0.25f), 3f, 20f);
            GUI.SetNextControlName("addr");
            address = GUI.TextField(fr, address, 40, field);
            if (GUI.GetNameOfFocusedControl() == "addr") typing = true;
            y += 92;
            if (UiKit.Button(new Rect(x, y, w, 80), "Bu Adrese Katıl")) Connect(address.Trim(), lanPort);
            if (UiKit.Button(new Rect(x, c.yMax - 150, w, 76), "← Geri", UiKit.Kind.Ghost)) Go(Page.Friends);
        }

        void DrawSettings(float W, float H, float slide)
        {
            var c = LeftCard(H, "Ayarlar", "Değişiklikler anında uygulanır ve kaydedilir.", slide);
            var x = c.x + 30; var w = 580f; var y = c.y + 170;
            void Row(string name, float v, System.Action<float> set)
            {
                UiKit.Text(new Rect(x, y, 400, 36), name, 24, UiKit.Cream, TextAnchor.MiddleLeft, FontStyle.Bold, false);
                UiKit.Text(new Rect(x + 400, y, 180, 36), Mathf.RoundToInt(v * 100f) + "%", 24, UiKit.Sun, TextAnchor.MiddleRight, FontStyle.Bold, false);
                var nv = UiKit.Slider(new Rect(x, y + 38, w, 46), v, name);
                if (!Mathf.Approximately(nv, v)) set(nv);
                y += 98;
            }
            Row("Ana ses", GameAudio.Master, v => GameAudio.Master = v);
            Row("Müzik", GameAudio.Music, v => { GameAudio.Music = v; });
            Row("Efektler", GameAudio.Effects, v => GameAudio.Effects = v);
            y += 6;
            var fp = ThirdPersonCamera.FirstPersonPreferred;
            if (UiKit.Button(new Rect(x, y, w, 68), fp ? "Kamera: 1. Şahıs" : "Kamera: 3. Şahıs", UiKit.Kind.Normal, true, 26)) ThirdPersonCamera.FirstPersonPreferred = !fp; y += 80;
            var fs = Screen.fullScreenMode != FullScreenMode.Windowed;
            if (UiKit.Button(new Rect(x, y, w, 68), fs ? "Ekran: Tam Ekran" : "Ekran: Pencere", UiKit.Kind.Normal, true, 26))
            {
                if (fs) Screen.SetResolution(1600, 900, FullScreenMode.Windowed);
                else Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow);
            }
            y += 80;
            var q = GraphicsQuality.Level < 0 ? GraphicsQuality.Detect() : GraphicsQuality.Level;
            if (UiKit.Button(new Rect(x, y, w, 68), "Grafik Kalitesi: " + GraphicsQuality.Names[q], UiKit.Kind.Normal, true, 26)) GraphicsQuality.Cycle();
            UiKit.Text(new Rect(x, y + 74, w, 30), "Oyun içinde: V kamera · F3 FPS/ping · Esc menü", 18, new Color(1, 1, 1, 0.5f), TextAnchor.MiddleLeft, FontStyle.Normal, false);
            if (UiKit.Button(new Rect(x, c.yMax - 150, w, 76), "← Geri", UiKit.Kind.Ghost)) { PlayerPrefs.Save(); Go(Page.Main); }
        }

        void DrawProfile(float W, float H, float slide)
        {
            var c = LeftCard(H, "Oyuncu Profili", "Adın, karakterin ve oyun geçmişin bu bilgisayarda saklanır.", slide);
            var x = c.x + 30; var w = 580f; var y = c.y + 190;
            UiKit.Text(new Rect(x, y, w, 36), "OYUNCU ADI", 20, UiKit.Sky, TextAnchor.MiddleLeft, FontStyle.Bold, false); y += 42;
            var fr = new Rect(x, y, w, 76);
            UiKit.Fill(fr, new Color(0f, 0f, 0f, 0.35f), 20f);
            UiKit.Outline(fr, new Color(1f, 1f, 1f, GUI.GetNameOfFocusedControl() == "name" ? 0.9f : 0.25f), 3f, 20f);
            GUI.SetNextControlName("name");
            var cur = PlayerPrefs.GetString("wbi_name", "");
            var nn = GUI.TextField(fr, cur, 16, field);
            if (nn != cur) PlayerPrefs.SetString("wbi_name", nn);
            if (GUI.GetNameOfFocusedControl() == "name") typing = true;
            y += 100;
            var e = CharacterCatalog.All[savedIndex];
            var box = new Rect(x, y, w, 150);
            UiKit.Fill(box, new Color(1, 1, 1, 0.07f), 22f);
            UiKit.Fill(new Rect(box.x, box.y, 12, box.height), e.accent, 6f);
            UiKit.Text(new Rect(box.x + 32, box.y + 12, 500, 44), "Karakter: " + e.name, 30, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold, false);
            UiKit.Text(new Rect(box.x + 32, box.y + 60, 530, 80), "Oyun sayısı: " + PlayerPrefs.GetInt("wbi_games", 0) + "\n" + e.blurb, 22, new Color(1, 1, 1, 0.78f), TextAnchor.UpperLeft, FontStyle.Normal, false);
            y += 170;
            if (UiKit.Button(new Rect(x, y, w, 76), "Karakteri Değiştir")) Go(Page.Characters);
            if (UiKit.Button(new Rect(x, c.yMax - 150, w, 76), "← Geri", UiKit.Kind.Ghost)) { PlayerPrefs.Save(); Go(Page.Main); }
        }

        void DrawCredits(float W, float H, float slide)
        {
            var c = LeftCard(H, "Emeği Geçenler", null, slide);
            var x = c.x + 40; var w = 560f; var y = c.y + 130;
            void Block(string head, string body, float height)
            {
                UiKit.Text(new Rect(x, y, w, 32), head, 20, UiKit.Sky, TextAnchor.MiddleLeft, FontStyle.Bold, false);
                UiKit.Text(new Rect(x, y + 34, w, height), body, 22, UiKit.Cream, TextAnchor.UpperLeft, FontStyle.Normal, false);
                y += 34 + height + 16;
            }
            Block("YAPIM", "CiciByte", 34);
            Block("OYUN MOTORU VE ÇEVRİMİÇİ", "Unity 6 · Universal Render Pipeline\nNetcode for GameObjects · Unity Transport\nglTFast (Apache 2.0)", 100);
            Block("KARAKTER MODELLERİ", "Yapay zekâ destekli üretim: Stable Diffusion XL, rembg ve TRELLIS (MIT). Ekibin kendi modellerinin yerini alacak ilk sürüm.", 130);
            Block("SES VE MÜZİK", "Efektler ve menü müziği oyunun içinde kodla üretilir.", 70);
            Block("SÜRÜM", Application.version, 34);
            if (UiKit.Button(new Rect(c.x + 30, c.yMax - 150, 580, 76), "← Geri", UiKit.Kind.Ghost)) Go(Page.Main);
        }

        void DrawCharacters(float W, float H, float slide)
        {
            var e = CharacterCatalog.All[charIndex];
            // top bar
            UiKit.Text(new Rect(0, 34, W, 76), "KARAKTERİNİ SEÇ", 56, UiKit.Sun, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Text(new Rect(0, 104, W, 36), "Sürükleyerek döndür  ·  ← → ile değiştir  ·  Onayla'ya basınca kaydedilir ve herkes seni böyle görür", 20, new Color(1, 1, 1, 0.8f), TextAnchor.MiddleCenter, FontStyle.Normal);
            if (UiKit.Button(new Rect(60, 44, 240, 68), "← Geri", UiKit.Kind.Ghost, true, 26)) Go(Page.Main);
            // prev / next
            if (UiKit.Button(new Rect(W / 2f - 520, 360, 120, 120), "◀", UiKit.Kind.Normal, true, 44)) ChoosePreview(charIndex - 1);
            if (UiKit.Button(new Rect(W / 2f + 400, 360, 120, 120), "▶", UiKit.Kind.Normal, true, 44)) ChoosePreview(charIndex + 1);
            // confirm
            var already = charIndex == savedIndex;
            if (UiKit.Button(new Rect(W - 60 - 340, 44, 340, 68), already ? "✓ Seçili" : "Onayla", UiKit.Kind.Primary, true, 28)) ConfirmCharacter();

            // drag to rotate
            var ev = Event.current;
            var drag = new Rect(W / 2f - 330, 150, 660, 560);
            if (!UiKit.Locked)
            {
                if (ev.type == EventType.MouseDown && ev.button == 0 && drag.Contains(ev.mousePosition) && GUIUtility.hotControl == 0) { dragging = true; ev.Use(); }
                if (dragging && ev.type == EventType.MouseDrag) { dragYaw -= ev.delta.x * 0.7f; idleSince = Time.unscaledTime; ev.Use(); }
            }
            if (ev.type == EventType.MouseUp) dragging = false;

            // name plate
            var plate = new Rect(W / 2f - 380, 718, 760, 118);
            UiKit.Card(plate, new Color(UiKit.Ink.r, UiKit.Ink.g, UiKit.Ink.b, 0.8f), 28f);
            UiKit.Fill(new Rect(plate.x, plate.y, plate.width, 10), e.accent, 5f);
            UiKit.Text(new Rect(plate.x + 24, plate.y + 14, plate.width - 200, 54), e.name, 44, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiKit.Text(new Rect(plate.xMax - 200, plate.y + 22, 176, 40), e.tag, 22, e.accent, TextAnchor.MiddleRight, FontStyle.Bold, false);
            UiKit.Text(new Rect(plate.x + 24, plate.y + 68, plate.width - 48, 44), e.blurb, 23, new Color(1, 1, 1, 0.85f), TextAnchor.MiddleLeft, FontStyle.Normal, false);

            // card strip
            const float cw = 196f, ch = 214f, cg = 18f;
            var total = CharacterCatalog.Count * cw + (CharacterCatalog.Count - 1) * cg;
            var sx = (W - total) / 2f; var sy = 858f;
            for (var i = 0; i < CharacterCatalog.Count; i++)
            {
                var info = CharacterCatalog.All[i];
                var r = new Rect(sx + i * (cw + cg), sy, cw, ch);
                var sel = i == charIndex;
                var hov = !UiKit.Locked && r.Contains(ev.mousePosition);
                if (!UiKit.Locked && ev.type == EventType.MouseDown && ev.button == 0 && hov) { ChoosePreview(i); ev.Use(); }
                if (ev.type == EventType.Repaint)
                {
                    var lift = sel ? 14f : (hov ? 7f : 0f);
                    var rr = new Rect(r.x, r.y - lift, r.width, r.height);
                    UiKit.Card(rr, sel ? Color.Lerp(info.accent, Color.white, 0.15f) : new Color(UiKit.Ink.r, UiKit.Ink.g, UiKit.Ink.b, 0.85f), 24f);
                    if (thumbs != null && thumbs[i] != null)
                        GUI.DrawTexture(new Rect(rr.x + 10, rr.y + 10, rr.width - 20, rr.height - 68), thumbs[i], ScaleMode.ScaleToFit, true);
                    UiKit.Text(new Rect(rr.x, rr.yMax - 54, rr.width, 44), info.name, 22, sel ? UiKit.Ink : Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, false);
                    if (i == savedIndex)
                    {
                        UiKit.Fill(new Rect(rr.xMax - 44, rr.y + 8, 36, 36), UiKit.Mint, 18f);
                        UiKit.Text(new Rect(rr.xMax - 44, rr.y + 8, 36, 36), "✓", 24, UiKit.Ink, TextAnchor.MiddleCenter, FontStyle.Bold, false);
                    }
                    if (sel) UiKit.Outline(new Rect(rr.x - 4, rr.y - 4, rr.width + 8, rr.height + 8), Color.white, 4f, 28f);
                }
            }
        }

        void DrawConnecting(float W, float H)
        {
            UiKit.Fill(new Rect(0, 0, W, H), new Color(0.03f, 0.04f, 0.1f, 0.62f));
            var r = new Rect(W / 2f - 380, H / 2f - 190, 760, 380);
            UiKit.Card(r, new Color(0.1f, 0.12f, 0.26f, 0.97f), 36f);
            UiKit.Spinner(new Vector2(r.center.x, r.y + 100), 40f);
            UiKit.Text(new Rect(r.x + 30, r.y + 160, r.width - 60, 60), string.IsNullOrEmpty(status) ? "Bağlanıyor..." : status, 34, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Text(new Rect(r.x + 30, r.y + 214, r.width - 60, 36), "Lütfen bekle, bu birkaç saniye sürebilir.", 21, new Color(1, 1, 1, 0.65f), TextAnchor.MiddleCenter, FontStyle.Normal, false);
            if (UiKit.Button(new Rect(r.center.x - 160, r.yMax - 110, 320, 70), "İptal", UiKit.Kind.Ghost, true, 28)) CancelConnect();
        }

        void DrawExitDialog(float W, float H)
        {
            UiKit.Fill(new Rect(0, 0, W, H), new Color(0.03f, 0.04f, 0.1f, 0.66f));
            var r = new Rect(W / 2f - 380, H / 2f - 180, 760, 360);
            UiKit.Card(r, new Color(0.1f, 0.12f, 0.26f, 0.98f), 36f);
            UiKit.Text(new Rect(r.x + 30, r.y + 36, r.width - 60, 70), "Oyundan çıkılsın mı?", 46, UiKit.Sun, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Text(new Rect(r.x + 40, r.y + 112, r.width - 80, 70), "Kaydedilmemiş bir şey yok. İstediğin zaman geri dönebilirsin.", 22, new Color(1, 1, 1, 0.75f), TextAnchor.MiddleCenter, FontStyle.Normal, false);
            if (UiKit.Button(new Rect(r.x + 40, r.yMax - 120, 320, 78), "Vazgeç", UiKit.Kind.Normal, true, 30)) { confirmExit = false; UiKit.Focus = 0; }
            if (UiKit.Button(new Rect(r.xMax - 360, r.yMax - 120, 320, 78), "Evet, Çık", UiKit.Kind.Danger, true, 30)) QuitGame();
        }
    }
}
