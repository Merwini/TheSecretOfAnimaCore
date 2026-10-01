using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace tsoa.core;

public class CompProperties_AffectedByFacilities_Grouped : CompProperties_AffectedByFacilities
{
    public List<FacilityLinkGroup> linkGroups = new List<FacilityLinkGroup>();

    //public List<ThingDef> linkableFacilities;

    public CompProperties_AffectedByFacilities_Grouped()
    {
        compClass = typeof(CompAffectedByFacilities_Grouped);
    }

    public FacilityLinkGroup GetLinkGroupForTag(string tag)
    {
        if (linkGroups == null)
            return null;

        for (int i = 0; i < linkGroups.Count; i++)
        {
            FacilityLinkGroup group = linkGroups[i];
            if (group.categoryTag == tag)
            {
                return group;
            }
        }
        return null;
    }

    public override void ResolveReferences(ThingDef parentDef)
    {
        if (!CompProperties_Facility_Grouped.dictionariesCached)
        {
            CompProperties_Facility_Grouped.CacheDictionaries();
        }

        linkableFacilities = new List<ThingDef>();

        foreach (FacilityLinkGroup group in linkGroups)
        {
            if (CompProperties_Facility_Grouped.cachedFacilities.TryGetValue(group.categoryTag, out List<ThingDef> facilities))
            {
                linkableFacilities.AddRange(facilities);
            }
        }
    }
}
