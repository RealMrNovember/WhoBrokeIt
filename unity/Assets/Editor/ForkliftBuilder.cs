using System.Collections.Generic;
using UnityEditor;
using Unity.Netcode;
using UnityEngine;
using WhoBrokeIt.Props;
using WhoBrokeIt.Vehicles;

/// <summary>Builds the modern forklift (all parts are primitives of real size under an empty, unit-scale root; faces +Z).</summary>
public static partial class ProjectSetup
{
    static Material MatPro(string name, Color color, float smooth, float metal, float emission = 0f)
    {
        System.IO.Directory.CreateDirectory("Assets/Materials");
        var path = "Assets/Materials/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.color = color;
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", smooth);
        mat.SetFloat("_Metallic", metal);
        if (emission > 0f)
        {
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            mat.SetColor("_EmissionColor", color * emission);
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Transform WheelAssembly(Transform hull, string name, Vector3 pos, float r, float width, Material tire, Material rim, Material bolt, out Transform steerPivot, out Transform spinPivot, float rimFrac = 0.625f, bool treads = true, bool lugs = true)
    {
        var root = new GameObject(name).transform;
        root.SetParent(hull, false);
        root.localPosition = pos;
        var spin = new GameObject("Spin").transform;
        spin.SetParent(root, false);
        var sx = Mathf.Sign(pos.x);
        // a cylinder's axis is local Y: rotate to lay it along X; scale = (diameter, half width, diameter)
        Prim(PrimitiveType.Cylinder, "Tire", Vector3.zero, new Vector3(2f * r, width * 0.5f, 2f * r), tire, spin, false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        Prim(PrimitiveType.Cylinder, "Rim", new Vector3(sx * 0.012f, 0f, 0f), new Vector3(r * 2f * rimFrac, width * 0.5f + 0.012f, r * 2f * rimFrac), rim, spin, false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        Prim(PrimitiveType.Cylinder, "Cap", new Vector3(sx * (width * 0.5f + 0.02f), 0f, 0f), new Vector3(r * 0.5f, 0.012f, r * 0.5f), bolt, spin, false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        for (var i = 0; i < 3; i++)       // three spokes across the rim
            Prim(PrimitiveType.Cube, "Spoke", new Vector3(sx * (width * 0.5f + 0.014f), 0f, 0f), new Vector3(0.018f, r * 2f * rimFrac * 0.95f, 0.07f), bolt, spin, false).transform.localRotation = Quaternion.Euler(i * 60f, 0f, 0f);
        for (var i = 0; lugs && i < 5; i++)       // lug nuts
        {
            var a = i * 72f * Mathf.Deg2Rad;
            Prim(PrimitiveType.Sphere, "Lug", new Vector3(sx * (width * 0.5f + 0.02f), Mathf.Sin(a) * r * 0.32f, Mathf.Cos(a) * r * 0.32f), Vector3.one * 0.035f, bolt, spin, false);
        }
        for (var i = 0; treads && i < 16; i++)      // tread blocks so the rolling is visible
        {
            var phi = i * 22.5f;
            var a = phi * Mathf.Deg2Rad;
            var b = Prim(PrimitiveType.Cube, "Tread", new Vector3(0f, Mathf.Sin(a) * r, Mathf.Cos(a) * r), new Vector3(width * 0.96f, 0.14f, 0.045f), tire, spin, false);
            b.transform.localRotation = Quaternion.Euler(-phi, 0f, 0f);
        }
        steerPivot = root;
        spinPivot = spin;
        return root;
    }

    static void BuildForklift(Vector3 position)
    {
        var paint = MatPro("FkPaint", new Color(0.98f, 0.70f, 0.04f), 0.7f, 0.15f);
        var dark = MatPro("FkDark", new Color(0.11f, 0.12f, 0.14f), 0.45f, 0.2f);
        var graphite = MatPro("FkGraphite", new Color(0.20f, 0.21f, 0.24f), 0.5f, 0.35f);
        var steel = MatPro("FkSteel", new Color(0.72f, 0.74f, 0.78f), 0.65f, 0.85f);
        var tire = MatPro("FkTire", new Color(0.045f, 0.045f, 0.05f), 0.12f, 0f);
        var rim = MatPro("FkRim", new Color(0.86f, 0.88f, 0.92f), 0.75f, 0.9f);
        var lampW = MatPro("FkLampWhite", new Color(1f, 0.96f, 0.82f), 0.9f, 0f, 2.2f);
        var lampR = MatPro("FkLampRed", new Color(1f, 0.08f, 0.06f), 0.9f, 0f, 1.8f);
        var beacon = MatPro("FkBeacon", new Color(1f, 0.55f, 0.04f), 0.9f, 0f, 2.0f);
        var screen = MatPro("FkScreen", new Color(0.2f, 0.75f, 1f), 0.9f, 0f, 1.6f);

        var hull = new GameObject("Forklift");
        hull.transform.position = position;
        var ht = hull.transform;

        // colliders: the hull box sits above the ground; four wheel spheres do the rolling
        var box = hull.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.72f, -0.02f);
        box.size = new Vector3(1.22f, 0.62f, 2.0f);
        var frontR = 0.34f; var rearR = 0.27f;
        var wheelDefs = new[]
        {
            new Vector3(-0.64f, frontR, 0.66f), new Vector3(0.64f, frontR, 0.66f),
            new Vector3(-0.58f, rearR, -0.80f), new Vector3(0.58f, rearR, -0.80f)
        };
        var radii = new[] { frontR, frontR, rearR, rearR };
        var points = new Vector3[4];
        for (var i = 0; i < 4; i++)
        {
            var sc = hull.AddComponent<SphereCollider>();
            sc.center = wheelDefs[i];
            sc.radius = radii[i];
            points[i] = new Vector3(wheelDefs[i].x, 0f, wheelDefs[i].z);
        }

        // ---- body ----
        Prim(PrimitiveType.Cube, "Body", new Vector3(0f, 0.58f, 0.08f), new Vector3(1.22f, 0.50f, 1.72f), paint, ht, false);
        Prim(PrimitiveType.Cube, "SideStripeL", new Vector3(-0.616f, 0.58f, 0.08f), new Vector3(0.02f, 0.10f, 1.5f), dark, ht, false);
        Prim(PrimitiveType.Cube, "SideStripeR", new Vector3(0.616f, 0.58f, 0.08f), new Vector3(0.02f, 0.10f, 1.5f), dark, ht, false);
        Prim(PrimitiveType.Cube, "Hood", new Vector3(0f, 0.86f, 0.58f), new Vector3(1.06f, 0.20f, 0.74f), graphite, ht, false).transform.localRotation = Quaternion.Euler(7f, 0f, 0f);
        Prim(PrimitiveType.Cube, "Dash", new Vector3(0f, 1.0f, 0.36f), new Vector3(0.86f, 0.20f, 0.14f), dark, ht, false).transform.localRotation = Quaternion.Euler(-20f, 0f, 0f);
        Prim(PrimitiveType.Cube, "DashScreen", new Vector3(0f, 1.045f, 0.30f), new Vector3(0.22f, 0.10f, 0.012f), screen, ht, false).transform.localRotation = Quaternion.Euler(-20f, 0f, 0f);
        Prim(PrimitiveType.Cube, "FloorPlate", new Vector3(0f, 0.84f, -0.02f), new Vector3(1.1f, 0.04f, 0.8f), graphite, ht, false);
        foreach (var sx in new[] { -1f, 1f })
        {
            Prim(PrimitiveType.Cube, "Fender", new Vector3(0.64f * sx, 0.80f, 0.66f), new Vector3(0.30f, 0.05f, 0.82f), paint, ht, false);
            Prim(PrimitiveType.Cube, "FenderLip", new Vector3(0.78f * sx, 0.72f, 0.66f), new Vector3(0.03f, 0.20f, 0.82f), paint, ht, false);
            Prim(PrimitiveType.Cube, "Step", new Vector3(0.70f * sx, 0.40f, -0.12f), new Vector3(0.30f, 0.05f, 0.52f), dark, ht, false);
            Prim(PrimitiveType.Cube, "StepGrip", new Vector3(0.70f * sx, 0.43f, -0.12f), new Vector3(0.26f, 0.015f, 0.46f), graphite, ht, false);
            Prim(PrimitiveType.Cube, "Headlight", new Vector3(0.44f * sx, 0.66f, 0.945f), new Vector3(0.22f, 0.09f, 0.05f), lampW, ht, false);
            Prim(PrimitiveType.Cube, "HeadlightBezel", new Vector3(0.44f * sx, 0.66f, 0.93f), new Vector3(0.26f, 0.13f, 0.04f), dark, ht, false);
            Prim(PrimitiveType.Cube, "TailLight", new Vector3(0.46f * sx, 0.84f, -1.265f), new Vector3(0.22f, 0.09f, 0.04f), lampR, ht, false);
        }
        Prim(PrimitiveType.Cube, "Grille", new Vector3(0f, 0.55f, 0.945f), new Vector3(0.5f, 0.2f, 0.03f), dark, ht, false);
        for (var i = 0; i < 4; i++) Prim(PrimitiveType.Cube, "GrilleBar", new Vector3(0f, 0.48f + i * 0.045f, 0.962f), new Vector3(0.46f, 0.014f, 0.01f), steel, ht, false);

        // counterweight (rounded top) with a hazard strip
        Prim(PrimitiveType.Cube, "Counterweight", new Vector3(0f, 0.62f, -0.96f), new Vector3(1.22f, 0.66f, 0.62f), graphite, ht, false);
        Prim(PrimitiveType.Cylinder, "CounterweightTop", new Vector3(0f, 0.95f, -0.96f), new Vector3(0.34f, 0.61f, 0.34f), graphite, ht, false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        for (var i = 0; i < 6; i++)
            Prim(PrimitiveType.Cube, "Hazard", new Vector3(-0.50f + i * 0.2f, 0.46f, -1.275f), new Vector3(0.1f, 0.08f, 0.012f), i % 2 == 0 ? paint : dark, ht, false);
        Prim(PrimitiveType.Cylinder, "Exhaust", new Vector3(0.40f, 1.38f, -1.05f), new Vector3(0.07f, 0.28f, 0.07f), dark, ht, false);

        // ---- cab ----
        Prim(PrimitiveType.Cube, "SeatBase", new Vector3(0f, 0.92f, -0.12f), new Vector3(0.48f, 0.12f, 0.48f), dark, ht, false);
        Prim(PrimitiveType.Cube, "SeatBack", new Vector3(0f, 1.22f, -0.38f), new Vector3(0.46f, 0.52f, 0.09f), dark, ht, false).transform.localRotation = Quaternion.Euler(-9f, 0f, 0f);
        Prim(PrimitiveType.Cube, "Headrest", new Vector3(0f, 1.56f, -0.41f), new Vector3(0.28f, 0.16f, 0.07f), dark, ht, false);
        foreach (var sx in new[] { -1f, 1f })
            Prim(PrimitiveType.Cube, "Armrest", new Vector3(0.27f * sx, 1.05f, -0.2f), new Vector3(0.05f, 0.04f, 0.3f), graphite, ht, false);

        var wheelPivot = new GameObject("SteeringPivot").transform;
        wheelPivot.SetParent(ht, false);
        wheelPivot.localPosition = new Vector3(0f, 1.14f, 0.26f);
        wheelPivot.localRotation = Quaternion.Euler(-35f, 0f, 0f);
        Prim(PrimitiveType.Cylinder, "SteeringRim", Vector3.zero, new Vector3(0.34f, 0.012f, 0.34f), dark, wheelPivot, false);
        Prim(PrimitiveType.Cylinder, "SteeringHub", Vector3.zero, new Vector3(0.09f, 0.03f, 0.09f), steel, wheelPivot, false);
        for (var i = 0; i < 2; i++) Prim(PrimitiveType.Cube, "SteeringSpoke", Vector3.zero, new Vector3(0.32f, 0.012f, 0.03f), dark, wheelPivot, false).transform.localRotation = Quaternion.Euler(0f, i * 90f, 0f);
        Prim(PrimitiveType.Cube, "SteeringKnob", new Vector3(0.13f, 0.03f, 0.0f), new Vector3(0.05f, 0.05f, 0.05f), steel, wheelPivot, false);
        Prim(PrimitiveType.Cylinder, "SteeringColumn", new Vector3(0f, 1.02f, 0.30f), new Vector3(0.06f, 0.14f, 0.06f), dark, ht, false).transform.localRotation = Quaternion.Euler(-35f, 0f, 0f);

        foreach (var sx in new[] { -1f, 1f })
        {
            Prim(PrimitiveType.Cube, "GuardPostFront", new Vector3(0.57f * sx, 1.5f, 0.44f), new Vector3(0.06f, 1.18f, 0.06f), dark, ht, false);
            Prim(PrimitiveType.Cube, "GuardPostRear", new Vector3(0.57f * sx, 1.5f, -0.54f), new Vector3(0.06f, 1.18f, 0.06f), dark, ht, false);
            Prim(PrimitiveType.Cylinder, "Handle", new Vector3(0.63f * sx, 1.15f, 0.42f), new Vector3(0.03f, 0.2f, 0.03f), steel, ht, false);
        }
        Prim(PrimitiveType.Cube, "GuardRoof", new Vector3(0f, 2.11f, -0.05f), new Vector3(1.28f, 0.05f, 1.26f), dark, ht, false);
        for (var i = 0; i < 6; i++) Prim(PrimitiveType.Cube, "RoofSlat", new Vector3(0f, 2.145f, -0.58f + i * 0.215f), new Vector3(1.28f, 0.025f, 0.05f), steel, ht, false);
        Prim(PrimitiveType.Cube, "WorkLight", new Vector3(0f, 2.06f, 0.60f), new Vector3(0.5f, 0.05f, 0.05f), lampW, ht, false);
        Prim(PrimitiveType.Cylinder, "BeaconBase", new Vector3(0f, 2.18f, -0.5f), new Vector3(0.13f, 0.03f, 0.13f), dark, ht, false);
        Prim(PrimitiveType.Sphere, "Beacon", new Vector3(0f, 2.26f, -0.5f), new Vector3(0.17f, 0.14f, 0.17f), beacon, ht, false);
        Prim(PrimitiveType.Cube, "MirrorArm", new Vector3(-0.64f, 1.78f, 0.44f), new Vector3(0.1f, 0.02f, 0.02f), dark, ht, false);
        Prim(PrimitiveType.Cube, "Mirror", new Vector3(-0.70f, 1.78f, 0.44f), new Vector3(0.02f, 0.11f, 0.15f), steel, ht, false);

        // ---- mast (static; the carriage with the forks slides in front of it) ----
        foreach (var sx in new[] { -1f, 1f })
        {
            Prim(PrimitiveType.Cube, "MastOuter", new Vector3(0.31f * sx, 1.04f, 1.06f), new Vector3(0.09f, 2.05f, 0.11f), dark, ht, false);
            Prim(PrimitiveType.Cube, "MastInner", new Vector3(0.19f * sx, 1.0f, 1.13f), new Vector3(0.05f, 1.9f, 0.07f), steel, ht, false);
            Prim(PrimitiveType.Cylinder, "LiftCylinder", new Vector3(0.10f * sx, 0.85f, 0.98f), new Vector3(0.07f, 0.8f, 0.07f), steel, ht, false);
            Prim(PrimitiveType.Cube, "Chain", new Vector3(0.24f * sx, 1.0f, 1.19f), new Vector3(0.012f, 1.8f, 0.012f), graphite, ht, false);
        }
        foreach (var y in new[] { 0.32f, 1.0f, 2.02f })
            Prim(PrimitiveType.Cube, "MastCross", new Vector3(0f, y, 1.06f), new Vector3(0.72f, 0.07f, 0.10f), dark, ht, false);
        Prim(PrimitiveType.Cube, "Pivot", new Vector3(0f, 0.42f, 0.98f), new Vector3(0.8f, 0.22f, 0.12f), graphite, ht, false);

        // ---- wheels ----
        var steerList = new List<Transform>();
        var spinList = new List<Transform>();
        var names = new[] { "WheelFL", "WheelFR", "WheelRL", "WheelRR" };
        for (var i = 0; i < 4; i++)
        {
            WheelAssembly(ht, names[i], wheelDefs[i], radii[i], i < 2 ? 0.24f : 0.20f, tire, rim, steel, out var sp, out var sn);
            if (i < 2) steerList.Add(sp);
            spinList.Add(sn);
        }

        var body = hull.AddComponent<Rigidbody>();
        body.mass = 700f;
        body.linearDamping = 0f;
        body.angularDamping = 0f;
        body.centerOfMass = new Vector3(0f, 0.45f, -0.1f);
        Net(hull, true);
        hull.AddComponent<Resettable>();

        // forks: kinematic body that follows the hull and carries loads by friction (own root, not a child of the hull)
        var forksGo = new GameObject("Forks");
        forksGo.transform.position = hull.transform.position;
        var ft = forksGo.transform;
        Prim(PrimitiveType.Cube, "CarriagePlate", new Vector3(0f, 0.42f, 1.22f), new Vector3(1.1f, 0.85f, 0.06f), dark, ft);
        for (var i = 0; i < 4; i++) Prim(PrimitiveType.Cube, "CarriageBar", new Vector3(0f, 0.12f + i * 0.24f, 1.26f), new Vector3(1.1f, 0.05f, 0.04f), steel, ft, false);
        for (var i = 0; i < 5; i++) Prim(PrimitiveType.Cube, "LoadBackrest", new Vector3(-0.44f + i * 0.22f, 1.1f, 1.19f), new Vector3(0.04f, 0.5f, 0.04f), steel, ft, false);
        Prim(PrimitiveType.Cube, "BackrestTop", new Vector3(0f, 1.34f, 1.19f), new Vector3(1.0f, 0.04f, 0.04f), steel, ft, false);
        foreach (var sx in new[] { -1f, 1f })
        {
            Prim(PrimitiveType.Cube, "Prong", new Vector3(0.32f * sx, 0.045f, 1.9f), new Vector3(0.14f, 0.07f, 1.3f), steel, ft);
            Prim(PrimitiveType.Cube, "ProngHeel", new Vector3(0.32f * sx, 0.20f, 1.28f), new Vector3(0.14f, 0.4f, 0.07f), steel, ft, false);
            Prim(PrimitiveType.Cube, "ProngTip", new Vector3(0.32f * sx, 0.03f, 2.58f), new Vector3(0.12f, 0.035f, 0.14f), steel, ft, false).transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
        }
        var forkRb = forksGo.AddComponent<Rigidbody>();
        forkRb.isKinematic = true;
        Net(forksGo, false);

        var fk = hull.AddComponent<Forklift>();
        fk.forks = forkRb;
        fk.forkOrigin = Vector3.zero;
        fk.forkMax = 1.6f;
        fk.forkMin = 0.04f;                // low enough to slide under a pallet, high enough not to scrape the ground
        fk.prompt = "Forklifte bin";
        fk.steerPivots = steerList.ToArray();
        fk.spinPivots = spinList.ToArray();
        fk.wheelRadius = radii;
        fk.wheelPoints = points;
        fk.steeringWheel = wheelPivot;
        fk.seatLocal = new Vector3(0f, 0.98f, -0.12f);
        fk.wheelBase = 1.46f;

        // visuals: everything under one root; the fork meshes are copied there (the fork body keeps only its colliders)
        var vis = GatherVisual(hull);
        var forkPivot = new GameObject("ForkVisual").transform;
        forkPivot.SetParent(vis, false);
        var forkParts = new List<Transform>();
        foreach (Transform c in forksGo.transform) forkParts.Add(c);
        foreach (var c in forkParts)
        {
            var twin = Object.Instantiate(c.gameObject, forkPivot, false);
            foreach (var col in twin.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
            var mr = c.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
        }
        fk.visual = vis;
        fk.forkVisual = forkPivot;
    }
}
