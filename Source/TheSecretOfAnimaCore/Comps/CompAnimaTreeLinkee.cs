using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace nuff.tsoa.core
{
    public class CompAnimaTreeLinkee : ThingComp
    {
        private Thing linkedTree;
        public virtual Thing LinkedTree
        {
            get
            {
                if (linkedTree == null)
                {
                    CompGroupedFacility compFac = parent.TryGetComp<CompGroupedFacility>();
                    if (compFac == null)
                    {
                        Log.Error($"CompAnimaTreeLinkee from {parent.Label} unable to get CompGroupedFacility");
                        return null;
                    }

                    Thing firstTree = compFac.LinkedThings.FirstOrDefault(t => t.HasComp<CompAnimaTreeEssence>());
                    if (firstTree == null)
                    {
                        Log.Error($"CompAnimaTreeLinkee from {parent.Label} linked to Thing without CompAnimaTreeEssence");
                        return null;
                    }
                    linkedTree = firstTree;
                }
                return linkedTree;
            }
        }

        CompAnimaTreeEssence compEssence;

        public virtual CompAnimaTreeEssence CompEssence
        {
            get
            {
                if (compEssence == null)
                {
                    CompAnimaTreeEssence comp = LinkedTree.TryGetComp<CompAnimaTreeEssence>();
                    if (comp == null)
                    {
                        Log.Error($"CompAnimaTreeLinkee from {parent.Label} linked to Tree without CompAnimaTreeEssence"); // I don't think this could happen without LinkedTree erroring first, just want to cover bases
                        return null;
                    }
                    compEssence = comp;
                }

                return compEssence;
            }
        }
    }
}
