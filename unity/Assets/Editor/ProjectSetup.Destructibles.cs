using UnityEngine;
using WhoBrokeIt.Props;

/// <summary>Wires the breakable props of the workshop scene (crates 1-3, barrel 0) and adds one electrical panel. Called from BuildWorkshopScene.</summary>
public static partial class ProjectSetup
{
    static void WireDestructibles()
    {
        foreach (var n in new[] { "Crate_1", "Crate_2", "Crate_3" })
        {
            var go = GameObject.Find(n);
            if (go != null) Destructibles.Make(go, MaterialKind.Wood, 30f);                  // keeps its mass and carry component
        }
        var barrel = GameObject.Find("Barrel_0");
        if (barrel != null) Destructibles.Make(barrel, MaterialKind.Metal, 60f);

        // an electrical panel on the workshop back wall with its own lamp
        var panel = Prim(PrimitiveType.Cube, "ElectricalPanel_0", new Vector3(3.2f, 1.3f, 13.65f), new Vector3(0.9f, 1.2f, 0.3f), Mat("PanelGrey", new Color(0.55f, 0.6f, 0.66f)));
        var ind = Prim(PrimitiveType.Sphere, "PanelIndicator", new Vector3(0.28f, 0.45f, -0.2f), Vector3.one * 0.18f, Mat("Red", new Color(0.85f, 0.2f, 0.2f)), panel.transform, false);
        var lampGo = new GameObject("PanelLamp");
        lampGo.transform.position = new Vector3(3.2f, 2.6f, 12.4f);
        var l = lampGo.AddComponent<Light>();
        l.type = LightType.Point; l.range = 6f; l.intensity = 3f; l.color = new Color(1f, 0.9f, 0.7f);
        Destructibles.MakePanel(panel, new[] { l }, ind.GetComponent<Renderer>());
    }
}
