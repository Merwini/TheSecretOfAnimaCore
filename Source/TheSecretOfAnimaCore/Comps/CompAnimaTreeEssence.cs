using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace nuff.tsoa.core
{
    public class CompAnimaTreeEssence : ThingComp
    {
        private int storedEssence;

        public int StoredEssence => storedEssence;

        CompProperties_AnimaTreeEssence Props => (CompProperties_AnimaTreeEssence)props;

        public void AddEssence(int amount)
        {
            storedEssence += amount;
            if (storedEssence > Props.maximumEssence)
            {
                storedEssence = Props.maximumEssence;
            }
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append($"Stored essence: {storedEssence} / {Props.maximumEssence}");

            return sb.ToString().TrimEnd();
        }
    }
}
