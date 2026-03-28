using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace tsoa.core;

public class Building_AnimaFont : Building, IThingHolder
{
    public ThingOwner innerContainer;

    private int amberAmount; // not stored as an actual Thing, don't want it to all drop if destroyed
    public int AmberAmount
    {
        get
        {
            return amberAmount;
        }
        set
        {
            amberAmount = value; 
        }
    }

    private bool emptyNow = false;
    public bool ShouldEmpty
    {
        get
        {
            // no amber to empty
            if (amberAmount == 0)
                return false;

            // no sap left, remove amber. I think it's better to do this automatically, in case player doesn't notice that the last sap has been converted
            if (innerContainer[0] == null /*|| innerContainer[0].stackCount == 0*/) // can stack count be 0 without it being null? TODO test
                return true;

            // manually designated
            if (emptyNow)
                return true;

            return false;
        }
    }

    public ThingOwner GetDirectlyHeldThings() => innerContainer;
    public void GetChildHolders(List<IThingHolder> outChildren) { }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        // TODO gizmo to load sap
        // TODO gizmo to unload amber
        throw new NotImplementedException();
    }

    public override string GetInspectString()
    {
        // TODO show sap / amber
        throw new NotImplementedException();
    }

    public void ToggleEmptyNow()
    {
        if (!emptyNow && AmberAmount > 0)
        {
            emptyNow = true;
        }
        else
        {
            emptyNow = false;
        }
        UpdateDesignation();
    }

    private void UpdateDesignation()
    {
        if (!Spawned) return;

        Designation designation = Map.designationManager.DesignationOn(this, TSOA_DefOf.TSOA_EmptyNow);

        if (emptyNow)
        {
            if (designation == null)
                Map.designationManager.AddDesignation(new Designation(this, TSOA_DefOf.TSOA_EmptyNow));
        }
        else
        {
            if (designation != null)
                designation.Delete();
        }
    }

    public override void ExposeData()
    {
        Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        Scribe_Values.Look(ref amberAmount, "amberAmount");
        Scribe_Values.Look(ref emptyNow, "emptyNow");

        base.ExposeData();
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        UpdateDesignation();
    }
}
