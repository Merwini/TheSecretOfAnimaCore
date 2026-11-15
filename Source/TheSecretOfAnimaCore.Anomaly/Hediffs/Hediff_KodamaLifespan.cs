// using RimWorld;
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;
// using Verse;
// using Verse.Sound;

// namespace tsoa.core.anomaly;

// public class Hediff_KodamaLifespan : Hediff
// {
//     private int ticksRemaining = -1;
//     private bool vanished;
//     private HediffDefExtension_KodamaLifetime Props =>
//             def.GetModExtension<HediffDefExtension_KodamaLifetime>();

//     public override void Tick()
//     {
//         base.Tick();

//         ticksRemaining--;

//         if (ticksRemaining <= 0)
//         {
//             Vanish();
//         }
//     }

//     public override void PostAdd(DamageInfo? dinfo)
//     {
//         base.PostAdd(dinfo);

//         if (ticksRemaining < 0)
//         {
//             ticksRemaining = Props.lifespanDays * GenDate.TicksPerDay;
//         }
//     }

//     public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
//     {
//         base.Notify_PawnDied(dinfo, culprit);

//         Vanish();
//     }

//     public override string LabelInBrackets
//     {
//         get
//         {
//             if (ticksRemaining <= 0)
//             {
//                 return null;
//             }

//             return ticksRemaining.ToStringTicksToPeriod();
//         }
//     }

//     public override void ExposeData()
//     {
//         base.ExposeData();

//         Scribe_Values.Look(ref ticksRemaining, "ticksRemaining", -1);
//         Scribe_Values.Look(ref vanished, "vanished", false);
//     }

//     private void Vanish()
//     {
//         if (vanished || pawn == null)
//         {
//             return;
//         }

//         vanished = true;

//         Map map = pawn.MapHeld;
//         IntVec3 pos = pawn.PositionHeld;

//         if (map != null)
//         {
//             Props?.vanishSound?.PlayOneShot(new TargetInfo(pos, map));

//             if (Props?.vanishEffecter != null)
//             {
//                 Effecter effecter = Props.vanishEffecter.SpawnMaintained(pos, map);
//                 effecter.Trigger(new TargetInfo(pos, map), TargetInfo.Invalid);
//             }

            
//             SpawnVanishFilth(map, pos);
//         }

//         if (pawn.Corpse != null && !pawn.Corpse.Destroyed)
//         {
//             pawn.Corpse.Destroy(DestroyMode.Vanish);
//         }

//         if (!pawn.Destroyed)
//         {
//             pawn.Destroy(DestroyMode.Vanish);
//         }
//     }

//     private void SpawnVanishFilth(Map map, IntVec3 pos)
//     {
//         if (Props?.vanishFilth == null)
//         {
//             return;
//         }

//         FilthMaker.TryMakeFilth(pos, map, Props.vanishFilth);

//         int extraFilthCount = Props.vanishFilthCountRange.RandomInRange;
//         for (int i = 0; i < extraFilthCount; i++)
//         {
//             IntVec3 cell = CellFinder.RandomClosewalkCellNear(pos, map, 2);

//             if (!cell.InBounds(map))
//             {
//                 continue;
//             }

//             FilthMaker.TryMakeFilth(cell, map, Props.vanishFilth);
//         }
//     }
// }

// public class HediffDefExtension_KodamaLifetime : DefModExtension
// {
//     public int lifespanDays;
//     public EffecterDef vanishEffecter;
//     public SoundDef vanishSound;
//     public ThingDef vanishFilth;
//     public IntRange vanishFilthCountRange;
// }
