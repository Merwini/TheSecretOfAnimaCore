using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace tsoa.core;

public class VirionExtension : DefModExtension
{
    public ThingDef producedItem;
    public int producedCount = 1;
    public ThingDef productStuff;
    public float gestationDays = 10;
    public bool alwaysKillsHost = true;

    public float tendQualityRequirement = 0.15f;

    public FloatRange virionActivityDaysRange = new FloatRange(3f, 7f);

    public FloatRange virionDamageDaysRange = new FloatRange(0.041f, 0.125f);

    public VirionSpawnEntry entitySpawn;

    // 1 adult metalhorror

    public static VirionSpawnEntry _metalhorrorDefault;
    public static VirionSpawnEntry MetalhorrorDefault
    {
        get
        {
            if (_metalhorrorDefault == null)
            {
                _metalhorrorDefault = new()
                {
                    kind = PawnKindDefOf.Metalhorror,
                    forcedLifeStageIndex = 2,
                    count = 1
                };
            }
            return _metalhorrorDefault;
        }
    }

    public override void ResolveReferences(Def parentDef)
    {
        base.ResolveReferences(parentDef);

        if (producedItem == null)
        {
            producedItem = ThingDefOf.WoodLog;
            Log.Error($"[TSOA] VirionExtension on {parentDef.defName} is missing producedItem. Defaulting to one WoodLog (this is funny to me).");
        }

        if (producedItem.MadeFromStuff && productStuff?.stuffProps?.CanMake(producedItem) != true)
        {
            productStuff = GenStuff.DefaultStuffFor(producedItem);
            Log.Error($"[TSOA] VirionExtension on {parentDef.defName} has missing or incompatible productStuff " + $"for {producedItem.defName}. Defaulting to {productStuff.defName}.");
        }

        if (gestationDays == 0)
        {
            gestationDays = 10;
            Log.Error($"[TSOA] VirionExtension had gestationDays 0, defaulting to 10 instead.");
        }
    }
}

public class VirionSpawnEntry
{
    public PawnKindDef kind;
    public int forcedLifeStageIndex = 1;
    public int count = 1;
}
