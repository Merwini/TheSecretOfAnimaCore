using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace tsoa.core;

public class CompProperties_Facility_Grouped : CompProperties_Facility
{
    public static Dictionary<string, List<ThingDef>> cachedAffectees; // I know this should be on CompProperties_AffectedByGroupedFacilities, but this way I can initialize both caches with one method call

    public static Dictionary<string, List<ThingDef>> cachedFacilities;

    [Unsaved]
    public List<ThingDef> linkableThingDefs;

    public string categoryTag;

    public List<StatModifier> statOffsets;

    public Dictionary<StatDef, Dictionary<QualityCategory, float>> statOffsetsPerQuality;

    public int maxAffected = 1;

    public bool canPlaceWithoutLink = true;

    public CompProperties_Facility_Grouped()
    {
        compClass = typeof(CompFacility_Grouped);
    }

    public static void CacheDictionaries()
    {
        if (cachedAffectees == null)
        {
            cachedAffectees = new Dictionary<string, List<ThingDef>>();
        }
        
        if (cachedFacilities == null)
        {
            cachedFacilities = new Dictionary<string, List<ThingDef>>();
        }

        cachedAffectees.Clear();
        cachedFacilities.Clear();

        List<ThingDef> allDefsListForReading = DefDatabase<ThingDef>.AllDefsListForReading;
        for (int i = 0; i < allDefsListForReading.Count; i++)
        {
            ThingDef thingDef = allDefsListForReading[i];
            CompProperties_AffectedByGroupedFacilities compPropertiesAffected = thingDef.GetCompProperties<CompProperties_AffectedByGroupedFacilities>();
            if (compPropertiesAffected != null && compPropertiesAffected.linkGroups != null)
            {
                foreach (FacilityLinkGroup group in compPropertiesAffected.linkGroups)
                {
                    string tag = group.categoryTag;
                    if (!cachedAffectees.TryGetValue(tag, out List<ThingDef> list))
                    {
                        list = new List<ThingDef>();
                        cachedAffectees[tag] = list;
                    }

                    list.Add(allDefsListForReading[i]);
                }
            }

            CompProperties_GroupedFacility compPropertiesFacility = allDefsListForReading[i].GetCompProperties<CompProperties_GroupedFacility>();
            if (compPropertiesFacility != null)
            {
                string tag = compPropertiesFacility.categoryTag;
                if (!cachedFacilities.TryGetValue(tag, out List<ThingDef> list))
                {
                    list = new List<ThingDef>();
                    cachedFacilities[tag] = list;
                }
                list.Add(allDefsListForReading[i]);
            }
        }
    }

    public override void ResolveReferences(ThingDef parentDef)
    {
        linkableThingDefs = new List<ThingDef>();

        // Check dictionary for this CompProp's tag
        List<ThingDef> cachedList = cachedAffectees.TryGetValue(categoryTag);
        foreach (ThingDef def in cachedList ?? Enumerable.Empty<ThingDef>())
        {
            linkableThingDefs.Add(def);
        }
    }
}
