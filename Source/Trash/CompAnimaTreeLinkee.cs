using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace tsoa.core
{
    public class CompAnimaTreeLinkee : ThingComp
    {
        private Thing linkedTree;
        public virtual Thing LinkedTree
        {
            get
            {
                if (linkedTree != null && (linkedTree.Destroyed || !linkedTree.Spawned))
                {
                    linkedTree = null;
                }

                if (linkedTree == null)
                {

                    CompGroupedFacility compFac = parent.TryGetComp<CompGroupedFacility>();
                    if (compFac == null)
                    {
                        return null;
                    }

                    linkedTree = compFac.LinkedThings.FirstOrDefault(t => t?.TryGetComp<CompAnimaTreeEssence>() != null);
                }
                return linkedTree;
            }
        }

        CompAnimaTreeEssence compEssence;

        public virtual CompAnimaTreeEssence CompEssence
        {
            get
            {
                if (compEssence != null && (compEssence.parent.Destroyed || !compEssence.parent.Spawned))
                {
                    compEssence = null;
                }

                if (compEssence == null)
                {
                    Thing tree = LinkedTree;
                    if (tree == null)
                        return null;

                    compEssence = LinkedTree.TryGetComp<CompAnimaTreeEssence>();
                }

                return compEssence;
            }
        }
    }
}
