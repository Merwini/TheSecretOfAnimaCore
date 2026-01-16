using RimWorld;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Lifetime;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace tsoa.core
{
    public class HediffComp_StatExchanger : HediffComp
    {
        // Need comp more often than pawn, so saves some operations to store the comps instead of the pawns
        private HashSet<HediffComp_StatExchanger> linkedComps = new HashSet<HediffComp_StatExchanger>();
        //private float cachedDonation = 0;
        //private float cachedReceived = 0;
        private float cachedAmount;

        public IEnumerable<HediffComp_StatExchanger> LinkedComps => linkedComps.ToList();
        //public float Donation => cachedDonation;
        //public float Received => cachedReceived;
        public bool IsLinked => linkedComps.Count > 0;
        public bool IsMaster => Props.isMaster; // Master can be donor or recipient. Master is the one who controls the logic of linking and unlinking via gizmos.
        public bool IsDonor => Props.isDonor; // donor vs. recipient
        public StatDef AffectedStat => Props.affectedStat; 

        public HediffCompProperties_StatExchanger Props => (HediffCompProperties_StatExchanger)props;

        public float StatAdjustment
        {
            get
            {
                if (float.IsNaN(cachedAmount))
                {
                    Recache();
                }

                if (IsDonor)
                {
                    return -cachedAmount;
                }
                else
                {
                    return cachedAmount;
                }
            }
        }
        
        public override void CompExposeData()
        {
            // Can't save references to comps, need to save the Hediffs
            List<HediffWithComps> tempHediffs = new List<HediffWithComps>();
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                tempHediffs = linkedComps?.Select(c => c.parent).ToList();
            }

            Scribe_Collections.Look(ref tempHediffs, "linkedHediffs", LookMode.Reference);

            if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
            {
                linkedComps = new HashSet<HediffComp_StatExchanger>();
                if (tempHediffs != null)
                {
                    foreach (var h in tempHediffs)
                    {
                        if (h == null) continue;

                        var comp = h.TryGetComp<HediffComp_StatExchanger>();
                        if (comp != null)
                            linkedComps.Add(comp);
                    }
                }
            }

            Scribe_Values.Look(ref cachedAmount, "cachedAmount", 0);
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (parent.pawn.IsHashIntervalTick(600))
            {
                cachedAmount = float.NaN;
            }
        }

        public bool OtherPawnValid(Pawn pawn)
        {
            if (linkedComps.Count >= Props.maxLinks)
            {
                MessageAtCapacity(Pawn);
                return false;
            }

            HediffComp_StatExchanger comp = pawn.health?.hediffSet?.hediffs?.FirstOrDefault(h => Props.targetHediffs.Contains(h.def))?.TryGetComp<HediffComp_StatExchanger>();

            if (comp == null)
            {
                MessageNoComp();
                return false;
            }

            if (linkedComps.Contains(comp))
            {
                MessageAlreadyLinked();
                return false;
            }

            if (comp.linkedComps.Count >= comp.Props.maxLinks)
            {
                MessageAtCapacity(pawn);
                return false;
            }

            return true;
        }

        // Only Master can link other pawns, using targeting gizmo
        public void LinkOtherPawn(Pawn pawn)
        {
            HediffComp_StatExchanger comp = pawn.health?.hediffSet?.hediffs?.FirstOrDefault(h => Props.targetHediffs.Contains(h.def))?.TryGetComp<HediffComp_StatExchanger>();

            FormLink(comp);
        }

        // Only Master can unlink other pawns, no safety check needed since gizmo float menu will populate only with entries in linkedComps
        public void UnlinkOtherPawn(HediffComp_StatExchanger other)
        {
            BreakLink(other);
            Recache();
        }

        public void FormLink(HediffComp_StatExchanger other)
        {
            linkedComps.Add(other);
            Recache();

            if (IsMaster)
                other.FormLink(this);
        }

        public void BreakLink(HediffComp_StatExchanger other)
        {
            linkedComps.Remove(other);
            Recache();

            if (IsMaster)
                other.linkedComps.Remove(this);
        }

        // Only non-Master
        public void RequestBreakAllLinks()
        {
            var tempList = LinkedComps; // since foreach modifies linkedComps

            foreach (var master in tempList)
            {
                master.BreakLink(this);
                master.Recache();
            }
        }

        public void Recache()
        {
            ValidateLinksStillGood();

            if (IsDonor)
            {
                CacheDonationAmount();
            }
            else
            {
                CacheReceivedAmount();
            }
        }

        public override void Notify_SurgicallyRemoved(Pawn surgeon)
        {
            MessageSurgicalRemoval();
            NotifyLinkBreak();
            base.Notify_SurgicallyRemoved(surgeon);
        }

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            MessagePawnDied();
            NotifyLinkBreak();
            base.Notify_PawnDied(dinfo, culprit);
        }

        public void NotifyLinkBreak()
        {
            var tempList = LinkedComps;
            if (IsMaster)
            {
                foreach (var comp in tempList)
                {
                    BreakLink(comp);
                }
                Recache();
            }
            else
            {
                RequestBreakAllLinks();
            }
        }

        public void ValidateLinksStillGood()
        {
            var tempList = LinkedComps;
            foreach (var comp in tempList)
            {
                if (comp == null)
                {
                    linkedComps.Remove(comp);
                    continue;
                }

                if (comp.parent == null || comp.parent.pawn == null || comp.parent.pawn.Dead || comp.parent.pawn.Spawned == false)
                {
                    BreakLink(comp);
                    continue;
                }
            }
        }

        // Get pawn's stat, adjusting only for traits and genes.
        public void CacheDonationAmount()
        {
            float value = Props.affectedStat.defaultBaseValue;
            Pawn pawn = parent.pawn;

            if (pawn.def.statBases != null)
            {
                value = pawn.def.statBases.GetStatValueFromList(Props.affectedStat, value);
            }

            if (pawn.story?.traits != null)
            {
                foreach (Trait trait in pawn.story.traits.allTraits)
                {
                    TraitDegreeData data = trait.CurrentData;
                    if (data == null) continue;

                    if (data.statOffsets != null)
                    {
                        value += data.statOffsets.GetStatOffsetFromList(Props.affectedStat);
                    }

                    if (data.statFactors != null)
                    {
                        value *= data.statFactors.GetStatFactorFromList(Props.affectedStat);
                    }
                }
            }

            if (ModLister.BiotechInstalled && pawn.genes?.GenesListForReading != null)
            {
                foreach (Gene gene in pawn.genes.GenesListForReading)
                {
                    if (!gene.Active) continue;

                    GeneDef def = gene.def;

                    if (def.statOffsets != null)
                    {
                        value += def.statOffsets.GetStatOffsetFromList(Props.affectedStat);
                    }

                    if (def.statFactors != null)
                    {
                        value *= def.statFactors.GetStatFactorFromList(Props.affectedStat);
                    }
                }
            }

            cachedAmount = value * Props.donorLossPercent;
        }

        public void CacheReceivedAmount()
        {
            float newCache = 0;
            var tempList = LinkedComps;
            foreach (var comp in tempList)
            {
                newCache = Props.recipientGainFlat != 0f
                    ? newCache + Props.recipientGainFlat
                    : newCache + (comp.cachedAmount * Props.recipientGainPercent);
            }
            cachedAmount = newCache;
        }

        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            if (!IsMaster)
                yield break;

            Pawn pawn = parent.pawn;
            if (pawn == null || pawn.Faction != Faction.OfPlayer)
                yield break;

            // LINK
            yield return new Command_Target
            {
                defaultLabel = "TSOA_StatExchangeGizmoLinkLabel".Translate(),
                defaultDesc = "TSOA_StatExchangeGizmoLinkDescription".Translate(),
                //icon = TODO
                targetingParams = new TargetingParameters
                {
                    canTargetPawns = true,
                    canTargetSelf = false,
                    validator = t =>
                    {
                        Pawn p = t.Thing as Pawn;
                        return p != null
                               && p.Spawned
                               && p != pawn // redundant with canTargetSelf = false?
                               && p.health != null
                               && p.health.hediffSet != null;
                    }
                },
                action = t =>
                {
                    Pawn target = t.Thing as Pawn;
                    if (target == null) 
                        return;

                    if (!OtherPawnValid(target))
                        return;

                    pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(TSOA_DefOf.TSOA_StatExchangeLink, pawn, target));
                }
            };

            // UNLINK
            yield return new Command_Action
            {
                defaultLabel = "TSOA_StatExchangeGizmoUnlinkLabel".Translate(),
                defaultDesc = "TSOA_StatExchangeGizmoUnlinkDescription".Translate(),
                //icon = TODO,
                action = () =>
                {
                    List<FloatMenuOption> opts = new List<FloatMenuOption>();

                    if (linkedComps.NullOrEmpty())
                    {
                        opts.Add(new FloatMenuOption("TSOA_StatExchangeGizmoNoLinks".Translate(), null));
                    }
                    else
                    {
                        foreach (var comp in linkedComps)
                        {
                            Pawn otherPawn = comp?.parent?.pawn;
                            if (otherPawn == null) continue;

                            opts.Add(new FloatMenuOption(
                            otherPawn.LabelShortCap,
                            () =>
                            {
                                Job job = JobMaker.MakeJob(TSOA_DefOf.TSOA_StatExchangeUnlink, otherPawn);
                                Pawn.jobs.TryTakeOrderedJob(job);
                            }));
                        }
                    }

                    Find.WindowStack.Add(new FloatMenu(opts));
                }
            };
        }

        public override string CompLabelInBracketsExtra
        {
            get
            {
                if (Props.maxLinks == 1)
                {
                    return IsLinked ? "TSOA_StatExchangeHediffLabelExtraLinked".Translate() : "TSOA_StatExchangeHediffLabelExtraUnlinked".Translate();
                }
                else
                {
                    return $"TSOA_StatExchangeHediffLabelExtraLinkCount".Translate(linkedComps.Count);
                }
            }
        }

        public override string CompDescriptionExtra
        {
            get
            {
                string sign = "";
                string inactive = "";
                if (StatAdjustment > 0) { sign = "+"; }
                if (IsDonor && !IsLinked) { inactive = "TSOA_StatExchangeInactive".Translate(); }
                return "\n\n" + $"- {AffectedStat.LabelCap} {sign}{StatAdjustment * 100}%{inactive}";
            }
        }

        private void MessageNoComp()
        {
            Messages.Message("TSOA_StatExchangerNoComp".Translate(), MessageTypeDefOf.RejectInput);
        }

        private void MessageAlreadyLinked()
        {
            Messages.Message("TSOA_StatExchangerAlreadyLinked".Translate(this.Pawn.Name), MessageTypeDefOf.RejectInput);
        }

        private void MessageAtCapacity(Pawn pawn)
        {
            Messages.Message("TSOA_StatExchangerAtCapacity".Translate(pawn.Name), MessageTypeDefOf.RejectInput);
        }

        private void MessageSurgicalRemoval()
        {
            if (!PawnUtility.ShouldSendNotificationAbout(Pawn))
                return;

            if (IsMaster)
            {
                foreach (var comp in linkedComps)
                {
                    Messages.Message("TSOA_StatExchangerUnlinkSurgery".Translate(this.Pawn.Name, comp.Pawn.Name), MessageTypeDefOf.NeutralEvent);
                }
            }
            else
            {
                foreach (var comp in linkedComps)
                {
                    Messages.Message("TSOA_StatExchangerUnlinkSurgery".Translate(comp.Pawn.Name, this.Pawn.Name), MessageTypeDefOf.NeutralEvent);
                }
            }
        }

        private void MessagePawnDied()
        {
            if (!PawnUtility.ShouldSendNotificationAbout(Pawn))
                return;

            if (IsMaster)
            {
                foreach (var comp in linkedComps)
                {
                    Messages.Message("TSOA_StatExchangerUnlinkDeath".Translate(this.Pawn.Name, comp.Pawn.Name), MessageTypeDefOf.NeutralEvent);
                }
            }
            else
            {
                foreach (var comp in linkedComps)
                {
                    Messages.Message("TSOA_StatExchangerUnlinkDeath".Translate(comp.Pawn.Name, this.Pawn.Name), MessageTypeDefOf.NeutralEvent);
                }
            }
        }
    }
}
