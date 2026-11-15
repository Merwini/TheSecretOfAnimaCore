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

    public static bool dictionariesCached = false;

    [Unsaved]
    public List<ThingDef> linkableThingDefs;

    public string categoryTag;

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
        else
        {
            cachedAffectees.Clear();
        }
        
        if (cachedFacilities == null)
        {
            cachedFacilities = new Dictionary<string, List<ThingDef>>();
        }
        else
        {
            cachedFacilities.Clear();
        }

        List<ThingDef> allDefsListForReading = DefDatabase<ThingDef>.AllDefsListForReading;
        for (int i = 0; i < allDefsListForReading.Count; i++)
        {
            ThingDef thingDef = allDefsListForReading[i];
            CompProperties_AffectedByFacilities_Grouped compPropertiesAffected = thingDef.GetCompProperties<CompProperties_AffectedByFacilities_Grouped>();
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

            CompProperties_Facility_Grouped compPropertiesFacility = allDefsListForReading[i].GetCompProperties<CompProperties_Facility_Grouped>();
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

        dictionariesCached = true;
    }

    public override void ResolveReferences(ThingDef parentDef)
    {
        if (!dictionariesCached)
        {
            CacheDictionaries();
        }

        linkableThingDefs = new List<ThingDef>();

        // Check dictionary for this CompProp's tag
        List<ThingDef> cachedList = cachedAffectees.TryGetValue(categoryTag);
        foreach (ThingDef def in cachedList ?? Enumerable.Empty<ThingDef>())
        {
            linkableThingDefs.Add(def);
        }
    }

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (string error in base.ConfigErrors(parentDef))
            yield return error;

        if (statOffsetsPerQuality == null)
            yield break;

        if (!parentDef.HasComp(typeof(CompQuality)))
            yield return "statOffsetsPerQuality requires a CompQuality.";

        foreach (var kvp in statOffsetsPerQuality)
        {
            if (kvp.Value == null)
            {
                yield return $"statOffsetsPerQuality has no quality values for {kvp.Key.defName}.";
                continue;
            }

            foreach (QualityCategory quality in Enum.GetValues(typeof(QualityCategory)))
            {
                if (!kvp.Value.ContainsKey(quality))
                    yield return $"statOffsetsPerQuality is missing {quality} for {kvp.Key.defName}.";
            }
        }
    }
}
