using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace tsoa.core
{
    public class TheSecretOfAnimaMod : Mod
    {


        public TheSecretOfAnimaMod(ModContentPack content) : base(content)
        {
            
        }

        public override string SettingsCategory()
        {
            return "The Secret of Anima";
        }


        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard list = new Listing_Standard();
            list.SliderLabeled("Essence multiplier", TheSecretOfAnimaSettings.essenceMultiplier, 0.1f, 10f);

            base.DoSettingsWindowContents(inRect);
        }
    }
}
