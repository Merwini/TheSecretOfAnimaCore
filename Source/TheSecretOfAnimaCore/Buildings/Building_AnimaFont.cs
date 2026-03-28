using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
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

    private bool loadNow = false;
    public bool ShouldLoad
    {
        get
        {
            return loadNow && !hasBegun;
        }
    }

    private bool hasBegun = false;
    public bool HasBegun => hasBegun;

    public ThingOwner GetDirectlyHeldThings() => innerContainer;
    public void GetChildHolders(List<IThingHolder> outChildren) { }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }

        if (!loadNow && !hasBegun)
        {
            Command_Action loadGizmo = new Command_Action()
            {
                defaultLabel = "TSOA_FontLoadSapLabel".Translate(),
                defaultDesc = "TSOA_FontLoadSapDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("TSOA/Things/Item/Resource/AnimaSap"),
                action = () =>
                {
                    ToggleLoadNow();
                }
            };
            yield return loadGizmo;
        }

        if (!innerContainer.NullOrEmpty() && !HasBegun)
        {
            Command_Action beginCrystallizationGizmo = new Command_Action()
            {
                defaultLabel = "TSOA_FontBeginLabel".Translate(),
                defaultDesc = "TSOA_FontBeginDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("TSOA/Things/Item/Resource/AnimaAmber/AnimaAmber_a"),
                action = () =>
                {
                    Begin();
                }
            };
            yield return beginCrystallizationGizmo;
        }

        if (HasBegun && AmberAmount > 0)
        {
            Command_Action unloadAmberGizmo = new Command_Action()
            {
                defaultLabel = "TSOA_FontUnloadAmberLabel".Translate(),
                defaultDesc = "TSOA_FontUnloadAmberDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("TSOA/Things/Item/Resource/AnimaAmber/AnimaAmber_c"),
                action = () =>
                {
                    ToggleEmptyNow();
                }
            };
            yield return unloadAmberGizmo;
        }
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

    public void ToggleLoadNow()
    {
        if (!loadNow && !HasBegun)
        {
            loadNow = true;
        }
        UpdateDesignation();
    }

    private void Begin()
    {
        hasBegun = true;
        loadNow = false;
        UpdateDesignation();
    }

    private void UpdateDesignation()
    {
        if (!Spawned) return;

        Designation loadDesignation = Map.designationManager.DesignationOn(this, TSOA_DefOf.TSOA_LoadSapNow);
        if (loadNow)
        {
            if (loadDesignation == null)
                Map.designationManager.AddDesignation(new Designation(this, TSOA_DefOf.TSOA_LoadSapNow));
        }
        else
        {
            if (loadDesignation != null)
                loadDesignation.Delete();
        }

        Designation emptyDesignation = Map.designationManager.DesignationOn(this, TSOA_DefOf.TSOA_EmptyNow);
        if (emptyNow)
        {
            if (emptyDesignation == null)
                Map.designationManager.AddDesignation(new Designation(this, TSOA_DefOf.TSOA_EmptyNow));
        }
        else
        {
            if (emptyDesignation != null)
                emptyDesignation.Delete();
        }
    }

    public override void ExposeData()
    {
        Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        Scribe_Values.Look(ref amberAmount, "amberAmount");
        Scribe_Values.Look(ref emptyNow, "emptyNow");
        Scribe_Values.Look(ref loadNow, "loadNow");
        Scribe_Values.Look(ref hasBegun, "hasBegun");

        base.ExposeData();
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        UpdateDesignation();
    }
}
