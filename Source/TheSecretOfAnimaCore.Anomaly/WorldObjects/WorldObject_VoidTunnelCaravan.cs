using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace tsoa.core.anomaly;

public partial class WorldObject_VoidTunnelCaravan : WorldObject, IThingHolder
{
    private const float TravelSpeed = 0.000025f;
    private const float NoAmbush = -1f;
    private const int MassCheckInterval = 60;
    private static readonly Color PlayerCaravanColor = new(1f, 0.863f, 0.33f);
    // Publicizer exposes the event's backing field under the same name.
    private static readonly EventInfo ContentsChangedEvent = typeof(ThingOwner).GetEvent("OnContentsChanged");

    private PlanetTile destinationTile = PlanetTile.Invalid;
    private Building_VoidGateEntrance entrance;
    private MapParent entranceMapParent;
    private WorldObject_VoidTunnelExit tunnelExit;
    private bool outgoing;
    private string caravanName;
    private float traveledPct;
    private float ambushAt = NoAmbush;

    private ActiveTransporterInfo cargo;
    private List<Pawn> retainedPawns = new();
    private Material material;
    private int lastMassCheckTick = -MassCheckInterval;
    private bool overCapacity;
    private int nextArrivalAttemptTick;

    public WorldObject_VoidTunnelCaravan()
    {
        cargo = new ActiveTransporterInfo(this) { savePawnsWithReferenceMode = true };
    }

    public virtual float AmbushChancePercent
    {
        get
        {
            if (entrance?.ForceAmbush == true)
                return 100f;

            float quality = entrance?.RitualQuality ?? 1f;
            return quality > 0f ? Mathf.Min(100f, 20f / quality) : 100f;
        }
    }

    public override string Label => caravanName ?? base.Label;
    public override Color ExpandingIconColor => Faction?.Color ?? Color.white;
    public override bool ExpandingIconFlipHorizontal => GenWorldUI.WorldToUIPosition(Start).x > GenWorldUI.WorldToUIPosition(End).x;
    public override Material Material => material ??= MaterialPool.MatFrom(
        texPath: def.texture, color: Faction?.IsPlayer == true ? PlayerCaravanColor : ExpandingIconColor,
        shader: ShaderDatabase.WorldOverlayTransparentLit, renderQueue: 3600);

    private ThingOwner Contents => cargo.innerContainer;
    internal IEnumerable<Pawn> LivingPassengers => Contents.OfType<Pawn>().Where(IsAlive);
    // Tile stays at the departure point as the icon moves along the tunnel.
    private Vector3 Start => Find.WorldGrid.GetTileCenter(Tile);
    private Vector3 End => destinationTile.Valid ? Find.WorldGrid.GetTileCenter(destinationTile) : Start;
    public override Vector3 DrawPos => Vector3.Slerp(Start, End, traveledPct);

    public ThingOwner GetDirectlyHeldThings() => null;
    public void GetChildHolders(List<IThingHolder> outChildren) => outChildren.Add(cargo);

    public override void SpawnSetup()
    {
        base.SpawnSetup();
        ContentsChangedEvent.AddEventHandler(Contents, (Action)MarkColonistBarDirty);
        MarkColonistBarDirty();
    }

    public override void PostRemove()
    {
        ContentsChangedEvent.RemoveEventHandler(Contents, (Action)MarkColonistBarDirty);
        base.PostRemove();
        MarkColonistBarDirty();
    }

    private static void MarkColonistBarDirty() => (Find.UIRoot as UIRoot_Play)?.mapUI?.colonistBar?.MarkColonistsDirty();

    internal static bool IsOverCapacity(ThingOwner contents)
    {
        List<Pawn> pawns = contents.OfType<Pawn>().Where(IsAlive).ToList();
        return CollectionsMassCalculator.MassUsage(contents, IgnorePawnsInventoryMode.DontIgnore, includePawnsMass: false)
            > CollectionsMassCalculator.Capacity(pawns);
    }

    internal static bool TryDepart(ThingOwner source, Building_VoidGateEntrance entrance, WorldObject_VoidTunnelExit tunnelExit, Faction faction, bool outgoing, string caravanName = null)
    {
        if (!entrance.HasOpenTunnel(tunnelExit))
            return false;

        WorldObject_VoidTunnelCaravan voidCaravan = (WorldObject_VoidTunnelCaravan)WorldObjectMaker.MakeWorldObject(Anomaly_MiscDefOf.TSOA_VoidTunnelCaravan);
        voidCaravan.entrance = entrance;
        voidCaravan.entranceMapParent = entrance.Map.Parent;
        voidCaravan.tunnelExit = tunnelExit;
        voidCaravan.outgoing = outgoing;
        voidCaravan.caravanName = caravanName;
        voidCaravan.Tile = outgoing ? entrance.Map.Tile : tunnelExit.Tile;
        voidCaravan.destinationTile = outgoing ? tunnelExit.Tile : entrance.Map.Tile;
        voidCaravan.SetFaction(faction);

        source.TryTransferAllToContainer(voidCaravan.Contents, canMergeWithExistingStacks: false);
        if (source.Count != 0)
        {
            voidCaravan.Contents.TryTransferAllToContainer(source, canMergeWithExistingStacks: false);
            return false;
        }

        foreach (Pawn pawn in voidCaravan.Contents.OfType<Pawn>().Where(IsAlive))
            voidCaravan.RetainWorldPawn(pawn);

        voidCaravan.ambushAt = Rand.Chance(Mathf.Clamp01(voidCaravan.AmbushChancePercent / 100f)) ? Rand.Range(0.2f, 0.8f) : NoAmbush;
        Find.WorldObjects.Add(voidCaravan);
        return true;
    }

    public override void TickInterval(int delta)
    {
        base.TickInterval(delta);

        if (legacyAmbushMap != null)
        {
            MigrateLegacyAmbush();
            return;
        }

        if (traveledPct >= 1f)
        {
            TryArrive();
            return;
        }

        if (!Contents.OfType<Pawn>().Any(IsAlive))
        {
            Destroy();
            return;
        }

        if (ambushAt >= 0f && traveledPct >= ambushAt)
        {
            QueueAmbush();
            return;
        }

        if (MovementBlockReason() != null)
            return;

        float distance = GenMath.SphericalDistance(Start.normalized, End.normalized);
        float progress = distance > 0f ? traveledPct + TravelSpeed * delta / distance : 1f;
        if (ambushAt >= 0f && progress >= ambushAt)
        {
            traveledPct = ambushAt;
            QueueAmbush();
            return;
        }

        traveledPct = Mathf.Min(1f, progress);
        if (traveledPct >= 1f)
            TryArrive();
    }

    private string MovementBlockReason()
    {
        IEnumerable<Pawn> owners = Contents.OfType<Pawn>().Where(p => IsAlive(p) && CaravanUtility.IsOwner(p, Faction));
        if (owners.All(p => p.Downed))
            return "AllCaravanMembersDowned";
        if (owners.All(p => p.Downed || p.InMentalState))
            return "AllCaravanMembersMentalBreak";

        int ticks = Find.TickManager.TicksGame;
        if (ticks - lastMassCheckTick >= MassCheckInterval)
        {
            overCapacity = IsOverCapacity(Contents);
            lastMassCheckTick = ticks;
        }
        if (overCapacity)
            return "CaravanImmobilizedByMass";

        // Use the world grid's longitude formula with the current position along the tunnel.
        Vector3 position = DrawPos;
        float localHour = GenDate.HourFloat(GenTicks.TicksAbs, Mathf.Atan2(position.x, -position.z) * Mathf.Rad2Deg);
        return localHour < 6f || localHour > 22f ? "TSOA_VoidGateCaravanResting" : null;
    }

    private void TryArrive()
    {
        int ticks = Find.TickManager.TicksGame;
        if (ticks < nextArrivalAttemptTick)
            return;

        nextArrivalAttemptTick = ticks + 60;
        if (outgoing || entrance == null || !entrance.HasOpenTunnel(tunnelExit) || entrance.Map.Parent != entranceMapParent)
        {
            if (TryFormCaravan(out Caravan caravan))
            {
                if (caravan != null)
                    Messages.Message("TSOA_VoidGateCaravanEmerged".Translate(caravan.Label), caravan, MessageTypeDefOf.TaskCompletion);
                Destroy();
            }
            return;
        }

        if (Building_VoidGateEndpoint.DropContentsNear(Contents, entrance, out _))
        {
            Messages.Message("TSOA_VoidGateCaravanEmerged".Translate(Label), entrance, MessageTypeDefOf.TaskCompletion);
            Destroy();
        }
    }

    private bool TryFormCaravan(out Caravan caravan)
    {
        caravan = null;
        List<Pawn> pawns = Contents.OfType<Pawn>().Where(IsAlive).ToList();
        if (pawns.Count == 0)
            return true;

        // A normal caravan carries its cargo in pawn inventories.
        foreach (Thing item in Contents.Where(t => t is not Pawn).ToList())
        {
            Pawn carrier = CaravanInventoryUtility.FindPawnToMoveInventoryTo(item, pawns, null);
            ThingOwner inventory = carrier?.inventory?.innerContainer;
            if (inventory == null || !Contents.TryTransferToContainer(item, inventory))
                return false;
        }

        foreach (Pawn pawn in pawns)
            Contents.Remove(pawn);

        caravan = CaravanMaker.MakeCaravan(pawns, Faction, destinationTile, addToWorldPawnsIfNotAlready: true);
        if (caravanName != null)
            caravan.Name = caravanName;
        return true;
    }

    private static bool IsAlive(Pawn pawn) => pawn != null && !pawn.Destroyed && !pawn.Dead;

    private void RetainWorldPawn(Pawn pawn)
    {
        if (!pawn.IsWorldPawn())
            Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Decide);

        // The world pawn GC only recognizes vanilla caravans and transport pods.
        if (Find.WorldPawns.ForcefullyKeptPawns.Add(pawn) && !retainedPawns.Contains(pawn))
            retainedPawns.Add(pawn);
    }

    public override void Destroy()
    {
        foreach (Pawn pawn in retainedPawns)
            Find.WorldPawns.ForcefullyKeptPawns.Remove(pawn);

        base.Destroy();
    }

    public override string GetInspectString()
    {
        string status = traveledPct >= 1f ? "TSOA_VoidGateCaravanWaitingToUnload".Translate()
            : MovementBlockReason()?.Translate() ?? "TSOA_VoidGateCaravanTraveling".Translate(traveledPct.ToStringPercent());
        string inspect = base.GetInspectString();
        return inspect.NullOrEmpty() ? status : inspect + "\n" + status;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref cargo, "cargo");
        Scribe_Collections.Look(ref retainedPawns, "retainedPawns", LookMode.Reference);
        Scribe_Values.Look(ref destinationTile, "destinationTile", PlanetTile.Invalid);
        Scribe_References.Look(ref entrance, "entrance");
        Scribe_References.Look(ref entranceMapParent, "entranceMapParent");
        Scribe_References.Look(ref tunnelExit, "tunnelExit");
        Scribe_Values.Look(ref outgoing, "outgoing");
        Scribe_Values.Look(ref caravanName, "name");
        Scribe_Values.Look(ref traveledPct, "traveledPct");
        Scribe_Values.Look(ref ambushAt, "ambushAt", NoAmbush);
        Scribe_References.Look(ref legacyAmbushMap, "ambushMap");
        Scribe_Collections.Look(ref legacyAmbushParty, "ambushParty", LookMode.Reference);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            cargo.parent = this;
            retainedPawns ??= new();
        }
    }
}
