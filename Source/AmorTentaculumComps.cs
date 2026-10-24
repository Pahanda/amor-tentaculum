using RimWorld;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AmorTentaculum
{
    public class HediffCompProperties_MultipliedSeverityPerDay : HediffCompProperties_SeverityPerDay
    {


        public required float baseSeverityPerDay;
        public required string modSettingMultiplier;
        private float settingMultiplier;

        public HediffCompProperties_MultipliedSeverityPerDay()
        {
            compClass = typeof(HediffComp_MultipliedSeverityByDay);
        }

        public new float CalculateSeverityPerDay()
        {
            switch (modSettingMultiplier)
            {
                case "hostIncubationSpeedFactor":
                    settingMultiplier = AmorTentaculumModSettings.hostIncubationSpeedFactor;
                    break;
                case "geneticRewriteSpeedFactor":
                    settingMultiplier = AmorTentaculumModSettings.geneticRewriteSpeedFactor;
                    break;
                case "hostEggProductionSpeedFactor":
                    settingMultiplier = AmorTentaculumModSettings.hostEggProductionSpeedFactor;
                    break;
                case "hostResourceProductionSpeedFactor":
                    settingMultiplier = AmorTentaculumModSettings.hostResourceProductionSpeedFactor;
                    break;
                case "tentacleEggProductionSpeedFactor":
                    settingMultiplier = AmorTentaculumModSettings.tentacleEggProductionSpeedFactor;
                    break;
                case "tentacleResourceProductionSpeedFactor":
                    settingMultiplier = AmorTentaculumModSettings.tentacleResourceProductionSpeedFactor;
                    break;
                default:
                    settingMultiplier = 1f;
                    break;
            }

            severityPerDay = baseSeverityPerDay * settingMultiplier;
            return severityPerDay;
        }

        public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
                yield return error;

            if (modSettingMultiplier == null)
                yield return "multplier value needs to be set.";

            if (baseSeverityPerDay == 0f)
                yield return "base severity value cannot be 0.";
        }
    }

    public class HediffComp_MultipliedSeverityByDay : HediffComp_SeverityPerDay
    {
        public required float baseSeverityPerDay;
        public required string modSettingMultiplier;
        // private float settingMultiplier;
        private HediffCompProperties_MultipliedSeverityPerDay Props => (HediffCompProperties_MultipliedSeverityPerDay)props;

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            severityPerDay = Props.CalculateSeverityPerDay();
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref severityPerDay, "severityPerDay", 0f);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && severityPerDay == 0f && Props.severityPerDay != 0f && Props.severityPerDayRange == FloatRange.Zero)
            {
                severityPerDay = Props.CalculateSeverityPerDay();
                Log.Warning("Hediff " + parent.Label + " had severityPerDay not matching parent.");
            }
        }

        public override float SeverityChangePerDay()
        {
            /*
            if (base.Pawn.ageTracker.AgeBiologicalYearsFloat < Props.minAge)
            {
                return 0f;
            }
            */

            Log.Message("[<color='#CB78E3'>Amor Tentaculum</color>] Calculating hediff severity...");

            float num = severityPerDay * (parent.CurStage?.severityGainFactor ?? 1f);
            if (ModsConfig.BiotechActive && MechanitorUtility.IsMechanitor(base.Pawn))
            {
                num *= Props.mechanitorFactor;
            }

            return num;
        }

    }


}