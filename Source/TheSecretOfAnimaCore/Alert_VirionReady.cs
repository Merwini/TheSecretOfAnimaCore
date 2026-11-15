using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace tsoa.core;

internal class Alert_VirionReady : Alert
{
    private List<Pawn> virionReadyResult = new List<Pawn>();

    private StringBuilder sb = new StringBuilder();

    private List<Pawn> ReadyPawns
    {
        get
        {
            virionReadyResult.Clear();
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                if (!maps[i].IsPlayerHome)
                {
                    continue;
                }

                foreach (Pawn item in maps[i].mapPawns.AllHumanlikeSpawned)
                {
                    Hediff_Virion hediff = item.health?.hediffSet?.GetFirstHediffOfDef(TSOA_HediffDefOf.TSOA_VirionHediff) as Hediff_Virion;
                    if (hediff == null)
                    {
                        continue;
                    }

                    if (hediff.IsFullyGestated)
                    {
                        virionReadyResult.Add(item);
                    }
                }
            }

            return virionReadyResult;
        }
    }

    public override string GetLabel()
    {
        if (virionReadyResult.Count == 1)
        {
            return "TSOA_VirionAlertLabel".Translate();
        }
        return "TSOA_VirionAlertLabelPlural".Translate(virionReadyResult.Count.ToStringCached());
    }

    public override TaggedString GetExplanation()
    {
        sb.Length = 0;
        foreach (Pawn item in virionReadyResult)
        {
            sb.AppendLine("  - " + item.NameShortColored.Resolve());
        }
        return "TSOA_VirionAlertDesc".Translate(sb.ToString().TrimEndNewlines());
    }

    public override AlertReport GetReport()
    {
        return AlertReport.CulpritsAre(ReadyPawns);
    }
}
