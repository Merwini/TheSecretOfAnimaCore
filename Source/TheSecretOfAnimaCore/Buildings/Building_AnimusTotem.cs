using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using tsoa.core;

namespace tsoa.core;

public abstract class Building_AnimusTotem : Building
{
    internal GameComponent_TotemTracker gameComp;
    internal CompFacility_Grouped compFacility_Grouped;
    internal CompRefuelable compRefuelable;

    private bool Linked => compFacility_Grouped.LinkedBuildings.Any();

    public bool ShouldBeActive => Spawned && !Destroyed && compRefuelable?.HasFuel == true && Linked;

    // Decided to go with a CompRefuelable instead of taking directly from the tree
    //private CompSpawnSubplant compSP;
    //public CompSpawnSubplant CompSpawnSubplant
    //{
    //    get
    //    {
    //        if (compSP == null)
    //        {
    //            List<Thing> linkedThings = compGF.LinkedThings;
    //            if (linkedThings.NullOrEmpty())
    //                return null;

    //            for (int i = 0; i < linkedThings.Count; i++)
    //            {
    //                CompSpawnSubplant comp = linkedThings[i].TryGetComp<CompSpawnSubplant>();
    //                if (comp != null)
    //                {
    //                    linkedTree = linkedThings[i];
    //                    compSP = comp;
    //                    break;
    //                }
    //            }
    //        }
    //        return compSP;
    //    }
    //}

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        compFacility_Grouped = GetComp<CompFacility_Grouped>();
        compRefuelable = GetComp<CompRefuelable>();
        gameComp = Current.Game.GetComponent<GameComponent_TotemTracker>();

        base.SpawnSetup(map, respawningAfterLoad);
    }

    public override void Tick()
    {
        if (ShouldBeActive)
        {
            DoTotemEffect();
        }
        else
        {
            EndTotemEffect();
        }

        base.Tick();
    }

    public virtual void StartTotemEffect()
    {
    }

    public virtual void DoTotemEffect()
    {
    }

    internal void RestoreRegistration()
    {
        if (ShouldBeActive)
            RegisterTotem();
    }

    public virtual void RegisterTotem()
    {
    }

    public virtual void EndTotemEffect()
    {
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        EndTotemEffect();

        base.Destroy(mode);
    }
    
    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        EndTotemEffect();

        base.DeSpawn(mode);
    }
    
    // This might actually be redundant, should have DeSpawn called when Minified anyway
    public override void Notify_MinifiedThingAboutToBeDestroyed(DestroyMode mode)
    {
        EndTotemEffect();

        base.Notify_MinifiedThingAboutToBeDestroyed(mode);
    }
}
