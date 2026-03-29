using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static HarmonyLib.Code;

namespace tsoa.core;

public class CompEmptyStateGraphicStuffed : ThingComp
{
    private CompProperties_EmptyStateGraphic Props => (CompProperties_EmptyStateGraphic)props;

    private Graphic cachedGraphic;

    public bool ParentIsEmpty
    {
        get
        {
            if (parent is IThingHolder thingHolder && thingHolder.GetDirectlyHeldThings().NullOrEmpty())
                return true;

            if (parent is IVirtualThingHolder virtualThingHolder && virtualThingHolder.IsEmpty)
                return true;

            CompPawnSpawnOnWakeup compPawnSpawnOnWakeup = parent.TryGetComp<CompPawnSpawnOnWakeup>();
            if (compPawnSpawnOnWakeup != null && !compPawnSpawnOnWakeup.CanSpawn)
                return true;

            return false;
        }
    }

    // Caching cuts the average time per call from 1.1 us to 0.58 us
    private Graphic EmptyGraphic
    {
        get
        {
            if (parent.Stuff == null)
                return Props.graphicData.Graphic;

            if (cachedGraphic == null)
            {
                cachedGraphic = Props.graphicData.GraphicColoredFor(parent);
            }
            return cachedGraphic;
        }
    }

    public override bool DontDrawParent()
    {
        if (ParentIsEmpty)
        {
            return !Props.alwaysDrawParent;
        }
        return false;
    }

    public override void PostDraw()
    {
        if (ParentIsEmpty && parent.def.drawerType != DrawerType.MapMeshOnly)
        {
            Graphic g = EmptyGraphic;
            Mesh mesh = g.MeshAt(parent.Rotation);
            Vector3 drawPos = parent.DrawPos;
            Graphics.DrawMesh(mesh, drawPos + Props.graphicData.drawOffset.RotatedBy(parent.Rotation), Quaternion.identity, g.MatAt(parent.Rotation), 0);
        }
    }

    public override void PostPrintOnto(SectionLayer layer)
    {
        if (ParentIsEmpty)
        {
            Props.graphicData.GraphicColoredFor(parent).Print(layer, parent, 0f);
        }
    }
}
