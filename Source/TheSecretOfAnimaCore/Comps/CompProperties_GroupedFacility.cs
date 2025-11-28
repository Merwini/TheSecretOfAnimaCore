using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace nuff.tsoa.core
{
    public class CompProperties_GroupedFacility : CompProperties  
    {
        public static Dictionary<string, List<ThingDef>> cachedAffectees; // I know this should be on CompProperties_AffectedByGroupedFacilities, but this way I can initialize both caches with one method call

        public static Dictionary<string, List<ThingDef>> cachedFacilities;

        [Unsaved(false)]
        public List<ThingDef> linkableThings;

        public string categoryTag;

        public List<StatModifier> statOffsets;

        public bool mustBePlacedAdjacent;

        public bool mustBePlacedAdjacentCardinalToBedHead;

        public bool mustBePlacedAdjacentCardinalToAndFacingBedHead;

        public bool mustBePlacedFacingThingLinear;

        public bool canLinkToMedBedsOnly;

        public float minDistance = 0f;

        public float maxDistance = 8f;

        public bool showMaxSimultaneous = true;

        public bool requiresLOS = true;

        public CompProperties_GroupedFacility()
        {
            compClass = typeof(CompGroupedFacility);
        }

        // TODO break cache on HotReloadDefs. Prefix PlayDataLoader.DoPlayLoad()?
        public static void CacheDictionaries()
        {
            if (cachedAffectees == null)
            {
                cachedAffectees = new Dictionary<string, List<ThingDef>>();

                List<ThingDef> allDefsListForReading = DefDatabase<ThingDef>.AllDefsListForReading;
                for (int i = 0; i < allDefsListForReading.Count; i++)
                {
                    CompProperties_AffectedByGroupedFacilities compProperties = allDefsListForReading[i].GetCompProperties<CompProperties_AffectedByGroupedFacilities>();
                    if (compProperties == null || compProperties.linkGroups == null)
                    {
                        continue;
                    }
                    foreach (FacilityLinkGroup group in compProperties.linkGroups)
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
            }

            if (cachedFacilities == null)
            {
                cachedFacilities = new Dictionary<string, List<ThingDef>>();

                List<ThingDef> allDefsListForReading = DefDatabase<ThingDef>.AllDefsListForReading;
                for (int i = 0; i < allDefsListForReading.Count; i++)
                {
                    CompProperties_GroupedFacility compProperties = allDefsListForReading[i].GetCompProperties<CompProperties_GroupedFacility>();
                    if (compProperties == null)
                    {
                        continue;
                    }
                    string tag = compProperties.categoryTag;
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
            base.ResolveReferences(parentDef); // Does nothing, but just in case someone Harmony patches it

            linkableThings = new List<ThingDef>();

            CacheDictionaries();

            // Check dictionary for this CompProp's tag
            List<ThingDef> cachedList = cachedAffectees.TryGetValue(categoryTag);
            foreach (ThingDef def in cachedList ?? Enumerable.Empty<ThingDef>())
            {
                linkableThings.Add(def);
            }
        }
    }
}
