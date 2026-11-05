using System.Collections.Generic;
using UnityEngine;
using WhoBrokeIt.Props;
using WhoBrokeIt.Vehicles;
using WhoBrokeIt.World;

/// <summary>Builds the skateboard and the bicycle (original models made of primitives; both face +Z).</summary>
public static partial class ProjectSetup
{
    /// <summary>A round tube from a to b (local space of the parent).</summary>
    static GameObject Tube(string name, Vector3 a, Vector3 b, float radius, Material mat, Transform parent)
    {
        var go = Prim(PrimitiveType.Cylinder, name, (a + b) * 0.5f, new Vector3(radius * 2f, (b - a).magnitude * 0.5f, radius * 2f), mat, parent, false);
        go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
        return go;
    }

    static void BuildSkateboard(Vector3 position, float yaw)
    {
        var deckMat = MatPro("SkateDeck", new Color(0.10f, 0.62f, 0.66f), 0.55f, 0.05f);
        var stripe = MatPro("SkateStripe", new Color(1f, 0.78f, 0.12f), 0.5f, 0f);
        var grip = MatPro("SkateGrip", new Color(0.07f, 0.07f, 0.08f), 0.1f, 0f);
        var steel = MatPro("FkSteel", new Color(0.72f, 0.74f, 0.78f), 0.65f, 0.85f);
        var tire = MatPro("SkateWheel", new Color(0.95f, 0.93f, 0.85f), 0.5f, 0f);
        var rim = MatPro("SkateHub", new Color(0.86f, 0.30f, 0.20f), 0.6f, 0.2f);

        var hull = new GameObject("Skateboard");
        hull.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        var ht = hull.transform;
        var box = hull.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.12f, 0f);
        box.size = new Vector3(0.27f, 0.08f, 0.92f);

        // the deck leans into turns: everything of the deck hangs under a tilt pivot at wheel-axle height
        var tilt = new GameObject("Tilt").transform;
        tilt.SetParent(ht, false);
        tilt.localPosition = new Vector3(0f, 0.07f, 0f);
        Prim(PrimitiveType.Cube, "Deck", new Vector3(0f, 0.05f, 0f), new Vector3(0.26f, 0.022f, 0.72f), deckMat, tilt, false);
        foreach (var sz in new[] { -1f, 1f })
        {
            Prim(PrimitiveType.Cube, "Kick", new Vector3(0f, 0.062f, 0.405f * sz), new Vector3(0.26f, 0.022f, 0.14f), deckMat, tilt, false).transform.localRotation = Quaternion.Euler(-18f * sz, 0f, 0f);
            Prim(PrimitiveType.Cylinder, "Nose", new Vector3(0f, 0.062f, 0.455f * sz), new Vector3(0.26f, 0.011f, 0.26f), deckMat, tilt, false);
            Prim(PrimitiveType.Cube, "Stripe", new Vector3(0f, 0.037f, 0.3f * sz), new Vector3(0.262f, 0.004f, 0.06f), stripe, tilt, false);
            Prim(PrimitiveType.Cube, "Truck", new Vector3(0f, 0.012f, 0.27f * sz), new Vector3(0.20f, 0.025f, 0.045f), steel, tilt, false);
            Prim(PrimitiveType.Cube, "TruckBase", new Vector3(0f, 0.030f, 0.27f * sz), new Vector3(0.09f, 0.012f, 0.09f), steel, tilt, false);
        }
        Prim(PrimitiveType.Cube, "GripTape", new Vector3(0f, 0.063f, 0f), new Vector3(0.245f, 0.004f, 0.70f), grip, tilt, false);

        // four small wheels (colliders are spheres so the board rolls over edges)
        var spinList = new List<Transform>();
        var radii = new List<float>();
        var pts = new List<Vector3>();
        var idx = 0;
        foreach (var sx in new[] { -1f, 1f })
            foreach (var sz in new[] { -1f, 1f })
            {
                var pos = new Vector3(0.125f * sx, 0.05f, 0.27f * sz);
                WheelAssembly(ht, "Wheel" + idx++, pos, 0.05f, 0.045f, tire, rim, rim, out _, out var spin, 0.6f, false, false);
                spinList.Add(spin);
                radii.Add(0.05f);
                pts.Add(new Vector3(pos.x, 0f, pos.z));
                var sc = hull.AddComponent<SphereCollider>();
                sc.center = pos; sc.radius = 0.05f;
            }

        var body = hull.AddComponent<Rigidbody>();
        body.mass = 6f;
        body.centerOfMass = new Vector3(0f, 0.05f, 0f);
        Net(hull, true);
        hull.AddComponent<Resettable>();
        var rv = hull.AddComponent<RideableVehicle>();
        rv.kind = RideKind.Skateboard;
        rv.label = "Kaykay";
        rv.prompt = "Kaykaya bin";
        rv.maxSpeed = 9f; rv.accel = 5.5f; rv.steerMax = 26f; rv.wheelBase = 0.55f;
        rv.spinPivots = spinList.ToArray();
        rv.wheelRadius = radii.ToArray();
        rv.wheelPoints = pts.ToArray();
        rv.tiltPivot = tilt;
        rv.mountLocal = new Vector3(0f, 0.135f, 0f);
        rv.sitting = false;
        rv.visual = GatherVisual(hull);
    }

    static void BuildBicycle(Vector3 position, float yaw)
    {
        var paint = MatPro("BikePaint", new Color(0.86f, 0.17f, 0.12f), 0.7f, 0.3f);
        var dark = MatPro("FkDark", new Color(0.11f, 0.12f, 0.14f), 0.45f, 0.2f);
        var steel = MatPro("FkSteel", new Color(0.72f, 0.74f, 0.78f), 0.65f, 0.85f);
        var tire = MatPro("FkTire", new Color(0.045f, 0.045f, 0.05f), 0.12f, 0f);
        var rim = MatPro("BikeRim", new Color(0.82f, 0.84f, 0.88f), 0.8f, 0.9f);
        var saddleMat = MatPro("BikeSaddle", new Color(0.30f, 0.17f, 0.09f), 0.35f, 0f);
        var lampW = MatPro("FkLampWhite", new Color(1f, 0.96f, 0.82f), 0.9f, 0f, 2.2f);
        var lampR = MatPro("FkLampRed", new Color(1f, 0.08f, 0.06f), 0.9f, 0f, 1.8f);

        var hull = new GameObject("Bicycle");
        hull.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        var ht = hull.transform;
        var R = 0.34f;
        var frontAxle = new Vector3(0f, R, 0.52f);
        var rearAxle = new Vector3(0f, R, -0.52f);
        var bb = new Vector3(0f, 0.30f, 0.02f);
        var head = new Vector3(0f, 0.80f, 0.40f);
        var seatTop = new Vector3(0f, 0.86f, -0.20f);

        // colliders: a slim box for the frame + the two wheels
        var box = hull.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.58f, 0f);
        box.size = new Vector3(0.14f, 0.5f, 0.9f);
        foreach (var ax in new[] { frontAxle, rearAxle })
        {
            var sc = hull.AddComponent<SphereCollider>();
            sc.center = ax; sc.radius = R;
        }

        // frame
        const float tr = 0.02f;
        Tube("DownTube", bb, head, tr * 1.2f, paint, ht);
        Tube("TopTube", seatTop + new Vector3(0f, -0.02f, 0.02f), head + new Vector3(0f, -0.04f, 0f), tr, paint, ht);
        Tube("SeatTube", bb, seatTop, tr, paint, ht);
        Tube("SeatStayL", seatTop + new Vector3(0f, -0.04f, 0f), rearAxle + new Vector3(-0.06f, 0f, 0f), tr * 0.7f, paint, ht);
        Tube("SeatStayR", seatTop + new Vector3(0f, -0.04f, 0f), rearAxle + new Vector3(0.06f, 0f, 0f), tr * 0.7f, paint, ht);
        Tube("ChainStayL", bb, rearAxle + new Vector3(-0.06f, 0f, 0f), tr * 0.7f, paint, ht);
        Tube("ChainStayR", bb, rearAxle + new Vector3(0.06f, 0f, 0f), tr * 0.7f, paint, ht);
        Tube("HeadTube", head + new Vector3(0f, -0.09f, 0f), head + new Vector3(0f, 0.06f, 0.015f), tr * 1.5f, paint, ht);
        Tube("SeatPost", seatTop, seatTop + new Vector3(0f, 0.07f, -0.01f), tr * 0.8f, steel, ht);
        Prim(PrimitiveType.Cube, "Saddle", seatTop + new Vector3(0f, 0.10f, -0.015f), new Vector3(0.15f, 0.04f, 0.27f), saddleMat, ht, false);
        Prim(PrimitiveType.Cube, "SaddleNose", seatTop + new Vector3(0f, 0.095f, 0.14f), new Vector3(0.07f, 0.035f, 0.12f), saddleMat, ht, false);
        Prim(PrimitiveType.Cube, "RearLight", new Vector3(0f, 0.62f, -0.50f), new Vector3(0.05f, 0.05f, 0.025f), lampR, ht, false);
        Prim(PrimitiveType.Cube, "RearFender", new Vector3(0f, R * 2f + 0.03f, -0.52f), new Vector3(0.07f, 0.012f, 0.40f), dark, ht, false);

        // steering: fork + front wheel + handlebar turn together around the head tube
        var forkPivot = new GameObject("ForkPivot").transform;
        forkPivot.SetParent(ht, false);
        forkPivot.localPosition = head;
        var axleLocal = frontAxle - head;
        Tube("ForkL", new Vector3(-0.055f, 0.04f, 0.02f), axleLocal + new Vector3(-0.055f, 0f, 0f), tr * 0.8f, steel, forkPivot);
        Tube("ForkR", new Vector3(0.055f, 0.04f, 0.02f), axleLocal + new Vector3(0.055f, 0f, 0f), tr * 0.8f, steel, forkPivot);
        Prim(PrimitiveType.Cube, "ForkCrown", new Vector3(0f, 0.04f, 0.02f), new Vector3(0.13f, 0.03f, 0.04f), steel, forkPivot, false);
        Tube("Stem", new Vector3(0f, 0.04f, 0f), new Vector3(0f, 0.15f, -0.03f), tr, dark, forkPivot);
        Prim(PrimitiveType.Cylinder, "Handlebar", new Vector3(0f, 0.16f, -0.04f), new Vector3(0.03f, 0.29f, 0.03f), dark, forkPivot, false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        foreach (var sx in new[] { -1f, 1f })
        {
            Prim(PrimitiveType.Cylinder, "Grip", new Vector3(0.30f * sx, 0.16f, -0.04f), new Vector3(0.04f, 0.055f, 0.04f), tire, forkPivot, false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Tube("Lever", new Vector3(0.26f * sx, 0.165f, -0.01f), new Vector3(0.26f * sx, 0.155f, 0.07f), 0.008f, steel, forkPivot);
        }
        Prim(PrimitiveType.Cube, "Headlight", new Vector3(0f, 0.12f, 0.06f), new Vector3(0.07f, 0.06f, 0.04f), lampW, forkPivot, false);
        Prim(PrimitiveType.Sphere, "Bell", new Vector3(-0.12f, 0.19f, -0.04f), Vector3.one * 0.04f, steel, forkPivot, false);

        // wheels (thin tyres)
        var spinList = new List<Transform>();
        WheelAssembly(forkPivot, "WheelF", axleLocal, R, 0.05f, tire, rim, steel, out _, out var spinF, 0.86f, false, false);
        WheelAssembly(ht, "WheelR", rearAxle, R, 0.05f, tire, rim, steel, out _, out var spinR, 0.86f, false, false);
        spinList.Add(spinF); spinList.Add(spinR);

        // crank with pedals and chain ring
        var crank = new GameObject("Crank").transform;
        crank.SetParent(ht, false);
        crank.localPosition = bb;
        Prim(PrimitiveType.Cylinder, "ChainRing", new Vector3(0.055f, 0f, 0f), new Vector3(0.20f, 0.006f, 0.20f), steel, crank, false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        Prim(PrimitiveType.Cube, "ArmA", new Vector3(0.095f, 0.085f, 0f), new Vector3(0.022f, 0.18f, 0.03f), steel, crank, false);
        Prim(PrimitiveType.Cube, "PedalA", new Vector3(0.14f, 0.17f, 0f), new Vector3(0.10f, 0.02f, 0.06f), dark, crank, false);
        Prim(PrimitiveType.Cube, "ArmB", new Vector3(-0.095f, -0.085f, 0f), new Vector3(0.022f, 0.18f, 0.03f), steel, crank, false);
        Prim(PrimitiveType.Cube, "PedalB", new Vector3(-0.14f, -0.17f, 0f), new Vector3(0.10f, 0.02f, 0.06f), dark, crank, false);
        Prim(PrimitiveType.Cylinder, "BottomBracket", Vector3.zero, new Vector3(0.05f, 0.1f, 0.05f), steel, crank, false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

        var body = hull.AddComponent<Rigidbody>();
        body.mass = 14f;
        body.centerOfMass = new Vector3(0f, 0.4f, 0f);
        Net(hull, true);
        hull.AddComponent<Resettable>();
        var rv = hull.AddComponent<RideableVehicle>();
        rv.kind = RideKind.Bicycle;
        rv.label = "Bisiklet";
        rv.prompt = "Bisiklete bin";
        rv.maxSpeed = 7f; rv.accel = 3.2f; rv.brake = 8f; rv.steerMax = 32f; rv.wheelBase = 1.04f;
        rv.steerPivots = new[] { forkPivot };
        rv.spinPivots = spinList.ToArray();
        rv.wheelRadius = new[] { R, R };
        rv.wheelPoints = new[] { new Vector3(0f, 0f, 0.52f), new Vector3(0f, 0f, -0.52f) };
        rv.crank = crank;
        rv.mountLocal = new Vector3(0f, 0.97f, -0.20f);
        rv.sitting = true;
        rv.visual = GatherVisual(hull);
    }
}

public static partial class ProjectSetup
{
    /// <summary>Moves every child of the hull under a "Visual" child: the driver's machine shows that root at the predicted pose while the physics hull stays on the replicated pose.</summary>
    static Transform GatherVisual(GameObject hull)
    {
        var v = new GameObject("Visual").transform;
        v.SetParent(hull.transform, false);
        var kids = new List<Transform>();
        foreach (Transform c in hull.transform) if (c != v) kids.Add(c);
        foreach (var c in kids) c.SetParent(v, true);
        return v;
    }
}

public static partial class ProjectSetup
{
    /// <summary>Hand cart: a flat bed with low rails, two wheels and a handle. The player holds the handle from behind and pushes (RideKind.Cart); Space loads the light props next to it.</summary>
    static void BuildHandCart(Vector3 position, float yaw)
    {
        var bedMat = MatPro("CartBed", new Color(0.92f, 0.52f, 0.12f), 0.45f, 0.1f);
        var steel = MatPro("FkSteel", new Color(0.72f, 0.74f, 0.78f), 0.65f, 0.85f);
        var tire = MatPro("FkTire", new Color(0.045f, 0.045f, 0.05f), 0.12f, 0f);
        var rim = MatPro("BikeRim", new Color(0.82f, 0.84f, 0.88f), 0.8f, 0.9f);
        var grip = MatPro("SkateGrip", new Color(0.07f, 0.07f, 0.08f), 0.1f, 0f);

        var hull = new GameObject("HandCart");
        hull.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        var ht = hull.transform;

        // bed + rails (colliders: one flat box and three low rails; the rear stays open for the handle)
        var bed = hull.AddComponent<BoxCollider>();
        bed.center = new Vector3(0f, 0.36f, 0.15f); bed.size = new Vector3(0.9f, 0.08f, 1.3f);
        Prim(PrimitiveType.Cube, "Bed", new Vector3(0f, 0.36f, 0.15f), new Vector3(0.9f, 0.06f, 1.3f), bedMat, ht, false);
        foreach (var sx in new[] { -1f, 1f })
        {
            Prim(PrimitiveType.Cube, "Rail", new Vector3(0.435f * sx, 0.45f, 0.15f), new Vector3(0.03f, 0.14f, 1.3f), bedMat, ht, false);
            var rc = hull.AddComponent<BoxCollider>(); rc.center = new Vector3(0.435f * sx, 0.47f, 0.15f); rc.size = new Vector3(0.04f, 0.14f, 1.3f);
        }
        Prim(PrimitiveType.Cube, "FrontRail", new Vector3(0f, 0.45f, 0.785f), new Vector3(0.9f, 0.14f, 0.03f), bedMat, ht, false);
        var fc = hull.AddComponent<BoxCollider>(); fc.center = new Vector3(0f, 0.47f, 0.785f); fc.size = new Vector3(0.9f, 0.14f, 0.04f);

        // handle
        foreach (var sx in new[] { -1f, 1f })
            Tube("HandleBar", new Vector3(0.36f * sx, 0.40f, -0.45f), new Vector3(0.34f * sx, 0.98f, -0.95f), 0.022f, steel, ht);
        Tube("HandleGrip", new Vector3(-0.38f, 0.98f, -0.95f), new Vector3(0.38f, 0.98f, -0.95f), 0.03f, grip, ht);
        Prim(PrimitiveType.Cube, "FrontFoot", new Vector3(0f, 0.2f, 0.62f), new Vector3(0.06f, 0.34f, 0.06f), steel, ht, false);

        // two big wheels at the rear third + a caster foot in front (sphere colliders so it rolls over edges)
        var spinList = new System.Collections.Generic.List<Transform>();
        var radii = new System.Collections.Generic.List<float>();
        var pts = new System.Collections.Generic.List<Vector3>();
        var idx = 0;
        foreach (var sx in new[] { -1f, 1f })
        {
            var pos = new Vector3(0.50f * sx, 0.2f, -0.12f);
            WheelAssembly(ht, "Wheel" + idx++, pos, 0.2f, 0.07f, tire, rim, steel, out _, out var spin, 0.6f, false, false);
            spinList.Add(spin); radii.Add(0.2f);
            pts.Add(new Vector3(pos.x, 0f, pos.z));
            var sc = hull.AddComponent<SphereCollider>(); sc.center = pos; sc.radius = 0.2f;
        }
        pts.Add(new Vector3(0f, 0f, 0.62f));
        var foot = hull.AddComponent<SphereCollider>(); foot.center = new Vector3(0f, 0.05f, 0.62f); foot.radius = 0.05f;

        var body = hull.AddComponent<Rigidbody>();
        body.mass = 18f;
        body.centerOfMass = new Vector3(0f, 0.25f, 0.1f);
        Net(hull, true);
        hull.AddComponent<Resettable>();
        var rv = hull.AddComponent<RideableVehicle>();
        rv.kind = RideKind.Cart;
        rv.label = "El Arabası";
        rv.prompt = "El arabasını tut";
        rv.maxSpeed = 3.6f; rv.accel = 3.2f; rv.brake = 9f; rv.steerMax = 30f; rv.wheelBase = 1.1f;
        rv.spinPivots = spinList.ToArray();
        rv.wheelRadius = radii.ToArray();
        rv.wheelPoints = pts.ToArray();
        rv.mountLocal = new Vector3(0f, 0f, -1.05f);
        rv.sitting = false;
        rv.bedY = 0.4f; rv.maxLoadKg = 70f;
        rv.visual = GatherVisual(hull);
    }
}

public static partial class ProjectSetup
{
    /// <summary>An ambient pedestrian (see Npc): a kinematic capsule on the ground, dressed as a character by every client.</summary>
    static void BuildNpc(string name, Vector3 position, Vector3 areaCenter, float rMin, float rMax)
    {
        var go = new GameObject(name);
        go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        var cap = go.AddComponent<CapsuleCollider>();
        cap.center = new Vector3(0f, 0.9f, 0f); cap.height = 1.8f; cap.radius = 0.32f;
        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true; rb.useGravity = false;
        Net(go, false);
        var npc = go.AddComponent<Npc>();
        npc.center = areaCenter; npc.radiusMin = rMin; npc.radiusMax = rMax;
    }

    static void BuildNpcs()
    {
        for (var i = 0; i < 6; i++)
        {
            var a = (i * 60f + 30f) * Mathf.Deg2Rad;   // offset 30 degrees: the planters stand at (+-4, 7)
            BuildNpc("Npc_Plaza" + i, new Vector3(Mathf.Cos(a) * 8f, 0.02f, Mathf.Sin(a) * 8f), Vector3.zero, 5.5f, 11f);
        }
        BuildNpc("Npc_Yard0", new Vector3(26f, 0.02f, 14f), new Vector3(27f, 0f, 14f), 1f, 4f);
        BuildNpc("Npc_Yard1", new Vector3(31f, 0.02f, 15f), new Vector3(27f, 0f, 14f), 1f, 4f);
        BuildNpc("Npc_Dock0", new Vector3(-8f, 0.02f, 13f), new Vector3(-8f, 0f, 14f), 1f, 4f);
    }
}

public static partial class ProjectSetup
{
    /// <summary>A vendor behind a stall on the pavement in front of a shop: stationary pedestrian with something to say.</summary>
    static void BuildVendor(string name, string title, string[] lines, Vector3 stallPos, float yaw)
    {
        var stall = Kit("prop_stall_a", stallPos, yaw);
        var back = Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0f, -1.3f);   // the stall collider is 1.5 m deep (0.82 m behind its centre)
        var go = new GameObject(name);
        go.transform.SetPositionAndRotation(new Vector3(stallPos.x + back.x, 0.02f, stallPos.z + back.z), Quaternion.Euler(0f, yaw, 0f));
        var cap = go.AddComponent<CapsuleCollider>();
        cap.center = new Vector3(0f, 0.9f, 0f); cap.height = 1.8f; cap.radius = 0.32f;
        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true; rb.useGravity = false;
        Net(go, false);
        var npc = go.AddComponent<Npc>();
        npc.stationary = true;
        var v = go.AddComponent<NpcVendor>();
        v.title = title; v.lines = lines;
    }

    static void BuildVendors()
    {
        BuildVendor("Vendor_Fruit", "Manav", new[] { "Taze elma var, bugün sabah geldi!", "Kasaları taşıyan olursa çay benden.", "Meydandaki iş ilanlarına baktın mı?" }, new Vector3(-24f, 0f, -27.9f), 0f);
        BuildVendor("Vendor_Tea", "Çaycı", new[] { "Sıcak çay, sıcak simit!", "Forklifti dikkatli kullan, geçen hafta bir kasa uçtu.", "Atölyede iş çok, uğra bir." }, new Vector3(-8f, 0f, -27.9f), 0f);
        BuildVendor("Vendor_Tools", "Aletçi", new[] { "Çekiç, tornavida, her şey burada.", "Kırık bir şey mi var? Çekiçle onarırsın, [3] tuşu.", "Sağlam alet iş bitirir." }, new Vector3(8f, 0f, -27.9f), 0f);
        BuildVendor("Vendor_Flowers", "Çiçekçi", new[] { "Meydana çiçek lazım, değil mi?", "Bu sokak sabah en güzel.", "Bisiklet kiralayan var mı, soran çok." }, new Vector3(24f, 0f, -27.9f), 0f);
    }
}
