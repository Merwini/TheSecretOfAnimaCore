using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;

namespace tsoa.core;

public class WorldObjectCompProperties_FontOfAnima : WorldObjectCompProperties
{
    public float timeoutDays = 10;

    public WorldObjectCompProperties_FontOfAnima()
    {
        compClass = typeof(FontOfAnimaComp);
    }
}
