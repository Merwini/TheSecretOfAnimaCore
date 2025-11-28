using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace nuff.tsoa.core
{
    public class CompProperties_AffectedByGroupedFacilities : CompProperties
    {
        public List<FacilityLinkGroup> linkGroups = new List<FacilityLinkGroup>();

        public List<ThingDef> linkableFacilities;

        public CompProperties_AffectedByGroupedFacilities()
        {
            compClass = typeof(CompAffectedByGroupedFacilities);
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
            base.ResolveReferences(parentDef); // Does nothing, but just in case someone Harmony patches it

            linkableFacilities = new List<ThingDef>();

            CompProperties_GroupedFacility.CacheDictionaries();

            foreach (FacilityLinkGroup group in linkGroups)
            {
                if (CompProperties_GroupedFacility.cachedFacilities.TryGetValue(group.categoryTag, out List<ThingDef> facilities))
                {
                    linkableFacilities.AddRange(facilities);
                }
            }
        }
    }
}
