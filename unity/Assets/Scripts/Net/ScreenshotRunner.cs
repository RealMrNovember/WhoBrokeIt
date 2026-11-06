using System;
using System.Collections;
using System.IO;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using WhoBrokeIt.Player;

namespace WhoBrokeIt.Net
{
    /// <summary>
    /// Development tool: "-shots &lt;folder&gt;" starts a local game, flies the camera to a list of viewpoints and writes PNG screenshots.
    /// Used on the build servers (virtual display) to look at the game without anybody running it.
    /// </summary>
    public class ScreenshotRunner : MonoBehaviour
    {
        IEnumerator MenuShot(string dir)
        {
            Directory.CreateDirectory(dir);
            yield return new WaitForSeconds(4f);
            foreach (var page in new[] { "main", "characters", "characters2", "characters3", "characters4", "characters5", "settings", "profile", "friends", "exit" })
            {
                if (NetworkMenu.Instance != null) NetworkMenu.Instance.DebugGo(page);
                yield return new WaitForSeconds(page.StartsWith("characters") ? 2.0f : 1.2f);
                yield return new WaitForEndOfFrame();
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(dir, "menu_" + page + ".jpg"), tex.EncodeToJPG(90));
                Destroy(tex);
                Debug.Log("[SHOT] menu " + page);
            }
            Application.Quit(0);
        }

        /// <summary>HUD tour (needs a real window, run under Xvfb): host, accept the first contract, shoot the screen as the player sees it from a few headings.</summary>
        IEnumerator RunHud(string dir)
        {
            Directory.CreateDirectory(dir);
            yield return null;
            var nm = NetworkManager.Singleton;
            ((UnityTransport)nm.NetworkConfig.NetworkTransport).SetConnectionData("127.0.0.1", 7777, "127.0.0.1");
            if (!nm.StartHost()) { Application.Quit(1); yield break; }
            var t = 0f;
            while (PlayerNet.Local == null && t < 20f) { t += Time.deltaTime; yield return null; }
            if (PlayerNet.Local == null) { Debug.Log("[SHOT] oyuncu yok"); Application.Quit(2); yield break; }
            yield return new WaitForSeconds(3f);
            var cd = FindFirstObjectByType<WhoBrokeIt.Contracts.ContractDirector>();
            if (cd != null) cd.ServerStart(0);
            yield return new WaitForSeconds(3f);
            var p = PlayerNet.Local; var cc = p.GetComponent<CharacterController>();
            PlayerNet.DebugTagSelf = true;
            var yaws = new[] { 0f, 90f, 180f, 270f };
            for (var i = 0; i < yaws.Length; i++)
            {
                cc.enabled = false; p.transform.rotation = Quaternion.Euler(0f, yaws[i], 0f); cc.enabled = true;
                yield return new WaitForSeconds(1.5f);
                yield return new WaitForEndOfFrame();
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(dir, "hud_" + i + ".jpg"), tex.EncodeToJPG(90));
                Destroy(tex);
                Debug.Log("[SHOT] hud " + i + " state=" + (cd != null ? cd.state.Value.ToString() : "?") + " aktif=" + (cd != null ? cd.activeId.Value.ToString() : "?"));
            }
            Application.Quit(0);
        }

        void Start()
        {
            var a = Environment.GetCommandLineArgs();
            var m = Array.IndexOf(a, "-menushot");
            if (m >= 0) { StartCoroutine(MenuShot(m + 1 < a.Length ? a[m + 1] : "shots_menu")); return; }
            var hs = Array.IndexOf(a, "-hudshot");
            if (hs >= 0) { StartCoroutine(RunHud(hs + 1 < a.Length ? a[hs + 1] : "shots_hud")); return; }
            var g = Array.IndexOf(a, "-gfshots");
            if (g >= 0) { StartCoroutine(RunGreenfield(g + 1 < a.Length ? a[g + 1] : "shots")); return; }
            var i = Array.IndexOf(a, "-shots");
            if (i < 0) return;
            StartCoroutine(Run(i + 1 < a.Length ? a[i + 1] : "shots"));
        }

        RenderTexture rt;

        IEnumerator Shot(string dir, string name, Vector3 pos, Vector3 lookAt)
        {
            var cam = Camera.main;
            if (rt == null)
            {
                // batch mode never presents to a window, so ScreenCapture returns a blank image: render into a texture instead
                rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;
            }
            cam.transform.position = pos;
            cam.transform.rotation = Quaternion.LookRotation(lookAt - pos, Vector3.up);
            yield return new WaitForSeconds(0.8f);
            yield return new WaitForEndOfFrame();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(dir, name + ".jpg"), tex.EncodeToJPG(88));
            Destroy(tex);
            Debug.Log("[SHOT] " + name);
        }

        /// <summary>Greenfield Square tour: a fixed set of viewpoints (world coordinates, origin = plaza centre).</summary>
        IEnumerator RunGreenfield(string dir)
        {
            Directory.CreateDirectory(dir);
            yield return null;
            var nm = NetworkManager.Singleton;
            ((UnityTransport)nm.NetworkConfig.NetworkTransport).SetConnectionData("127.0.0.1", 7777, "127.0.0.1");
            if (!nm.StartHost()) { Application.Quit(1); yield break; }
            var t = 0f;
            while (PlayerNet.Local == null && t < 20f) { t += Time.deltaTime; yield return null; }
            if (PlayerNet.Local == null) { Debug.Log("[SHOT] oyuncu yok"); Application.Quit(2); yield break; }
            yield return new WaitForSeconds(2f);
            if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.enabled = false;
            yield return Shot(dir, "g01_hava", new Vector3(-48f, 40f, -42f), new Vector3(4f, 0f, 4f));
            yield return Shot(dir, "g02_meydan", new Vector3(0f, 3.2f, -13f), new Vector3(0f, 2.2f, 4f));
            yield return Shot(dir, "g03_cesme_kule", new Vector3(-8f, 2.2f, -6f), new Vector3(0f, 4.5f, 8f));
            yield return Shot(dir, "g04_cadde", new Vector3(-30f, 2.6f, -14f), new Vector3(-12f, 3f, -30f));
            yield return Shot(dir, "g05_depo", new Vector3(-6f, 3f, 8f), new Vector3(-24f, 2.5f, 19f));
            yield return Shot(dir, "g06_atolye", new Vector3(4f, 2.6f, 14f), new Vector3(4f, 2f, 28f));
            yield return Shot(dir, "g07_insaat", new Vector3(20f, 4f, -2f), new Vector3(31f, 2f, 12f));
            yield return Shot(dir, "g08_tepe", new Vector3(17f, 4f, -4f), new Vector3(34f, 1f, -10f));
            yield return Shot(dir, "g09_su_kulesi", new Vector3(-16f, 5f, 18f), new Vector3(-33f, 8f, 36f));
            yield return Shot(dir, "g10_forklift", new Vector3(-4f, 2.2f, 17f), new Vector3(-8f, 1f, 21f));
            Debug.Log("[SHOT] bitti");
            Application.Quit(0);
        }

        IEnumerator Run(string dir)
        {
            Directory.CreateDirectory(dir);
            yield return null;
            var nm = NetworkManager.Singleton;
            ((UnityTransport)nm.NetworkConfig.NetworkTransport).SetConnectionData("127.0.0.1", 7777, "127.0.0.1");
            if (!nm.StartHost()) { Application.Quit(1); yield break; }
            var t = 0f;
            while (PlayerNet.Local == null && t < 20f) { t += Time.deltaTime; yield return null; }
            if (PlayerNet.Local == null) { Debug.Log("[SHOT] oyuncu yok"); Application.Quit(2); yield break; }
            yield return new WaitForSeconds(2f);

            var cam = Camera.main;
            if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.enabled = false;   // we place the camera ourselves
            var p = PlayerNet.Local;
            var cc = p.GetComponent<CharacterController>();

            yield return Shot(dir, "01_genel", new Vector3(-16f, 10f, -26f), new Vector3(3f, 1.5f, 3f));
            yield return Shot(dir, "02_avlu", new Vector3(2f, 2.4f, -13f), new Vector3(7f, 1.0f, -3f));
            yield return Shot(dir, "03_forklift", new Vector3(11.5f, 2.3f, -10.5f), new Vector3(8.5f, 0.9f, -7f));
            yield return Shot(dir, "04_forklift_on", new Vector3(5.2f, 1.7f, -9.5f), new Vector3(8.5f, 0.9f, -7f));
            yield return Shot(dir, "05_atolye", new Vector3(0f, 3.2f, 2.5f), new Vector3(0f, 1.6f, 12f));
            yield return Shot(dir, "06_kule", new Vector3(-32f, 7f, -6f), new Vector3(-17f, 9f, 16f));

            // every character, front view, in the middle of the yard
            for (var c = 0; c < CharacterCatalog.Count; c++)
            {
                cc.enabled = false;
                p.transform.position = new Vector3(-3f, 1.0f, -10f);
                p.transform.rotation = Quaternion.identity;
                cc.enabled = true;
                p.characterId.Value = c;
                yield return new WaitForSeconds(0.5f);
                yield return Shot(dir, "char_" + c + "_" + CharacterCatalog.All[c].key, new Vector3(-3f, 1.35f, -6.6f), new Vector3(-3f, 1.0f, -10f));
            }

            // frozen animation poses (CharacterAnimator.DebugPose): walk, jump, throw, carry
            foreach (var c in new[] { 0, 2, 3 })
            {
                cc.enabled = false;
                p.transform.position = new Vector3(-3f, 1.0f, -10f);
                p.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
                cc.enabled = true;
                p.characterId.Value = c;
                yield return new WaitForSeconds(0.5f);
                foreach (var pose in new[] { 1, 3, 5, 6, 7, 8, 9 })
                {
                    CharacterAnimator.DebugPose = pose;
                    CharacterAnimator.DebugPhase = Mathf.PI * 0.5f;
                    yield return Shot(dir, "pose_" + c + "_" + pose, new Vector3(-0.9f, 1.4f, -7.6f), new Vector3(-3f, 1.0f, -10f));
                }
                CharacterAnimator.DebugPose = 0;
            }

            // climbing onto the forklift and sitting on it
            p.characterId.Value = 0;
            yield return new WaitForSeconds(0.3f);
            var fk = FindFirstObjectByType<WhoBrokeIt.Vehicles.Forklift>();
            if (fk != null)
            {
                cc.enabled = false;
                p.transform.position = fk.transform.position + fk.transform.right * -2.2f + Vector3.up * 1.1f;
                p.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                cc.enabled = true;
                Physics.SyncTransforms();
                fk.RequestInteract();
                yield return new WaitForSeconds(0.85f);
                yield return Shot(dir, "08_forklift_climb", new Vector3(5.2f, 1.8f, -9.4f), new Vector3(8.2f, 1.1f, -7f));
                yield return new WaitForSeconds(1.2f);
                yield return Shot(dir, "09_forklift_sitting", new Vector3(5.6f, 1.7f, -9.0f), new Vector3(8.4f, 1.15f, -7.1f));
                yield return Shot(dir, "10_forklift_front", new Vector3(10.3f, 1.9f, -4.0f), new Vector3(8.5f, 1.0f, -7f));
            }

            // riding the skateboard and the bicycle (rider mounted, vehicle standing still)
            if (fk != null) { fk.RequestExit(); yield return new WaitForSeconds(1.0f); }
            foreach (var rv in FindObjectsByType<WhoBrokeIt.Vehicles.RideableVehicle>(FindObjectsSortMode.None))
            {
                cc.enabled = false;
                p.transform.position = rv.transform.position + rv.transform.right * 1.2f + Vector3.up * 1.1f;
                cc.enabled = true;
                Physics.SyncTransforms();
                rv.RequestInteract();
                yield return new WaitForSeconds(1.2f);
                var rp = rv.transform.position;
                yield return Shot(dir, "13_ride_" + rv.kind, rp + rv.transform.right * -2.4f + Vector3.up * 1.0f + rv.transform.forward * 0.4f, rp + Vector3.up * 0.75f);
                yield return Shot(dir, "14_ride_front_" + rv.kind, rp + rv.transform.forward * 2.6f + rv.transform.right * 0.9f + Vector3.up * 1.2f, rp + Vector3.up * 0.8f);
                rv.RequestExit();
                yield return new WaitForSeconds(0.8f);
            }

            // first person view
            if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.enabled = false;
            p.characterId.Value = 0;
            ThirdPersonCamera.FirstPersonPreferred = true;
            yield return Shot(dir, "07_birinci_sahis", p.transform.position + new Vector3(0f, 0.68f, 0.14f), p.transform.position + new Vector3(0f, 0.5f, 6f));
            ThirdPersonCamera.FirstPersonPreferred = false;
            Debug.Log("[SHOT] bitti");
            Application.Quit(0);
        }
    }
}
