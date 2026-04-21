using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace tsoa.core;

public class CompBiHeatPusher : ThingComp
{
    private const float TicksPerSecond = 60f;
    private const float CloseEnough = 0.01f;

    private string lowerTemp => Props.targetTemperatureRange.min.ToStringTemperature();
    private string upperTemp => Props.targetTemperatureRange.max.ToStringTemperature();

    protected virtual float HeatPerSecond => Props.heatPerSecond;

    public CompProperties_BiHeatPusher Props => (CompProperties_BiHeatPusher)props;

    public virtual bool ShouldPushHeatNow(out float temperature)
    {
        if (!parent.SpawnedOrAnyParentSpawned || !parent.IsHashIntervalTick(60))
        {
            temperature = 0;
            return false;
        }
        CompProperties_BiHeatPusher compProperties_HeatPusher = Props;
        Room room = parent.GetRoom();
        if (room == null || (room.UsesOutdoorTemperature && !Props.worksOutdoors))
        {
            temperature = 0;
            return false;
        }

        temperature = room.Temperature;
        if (Props.targetTemperatureRange.Includes(temperature))
        {
            return false;
        }

        return true;
    }

    public override void CompTick()
    {
        base.CompTick();

        if (!ShouldPushHeatNow(out float currentTemp))
        {
            return;
        }
        Map map = parent.Map;
        float targetTemp = GetDesiredTargetTemperature(currentTemp);

        float diff = targetTemp - currentTemp;
        if (Mathf.Abs(diff) < CloseEnough)
        {
            return;
        }

        float signedHeat = Mathf.Sign(diff) * HeatPerSecond;

        GenTemperature.PushHeat(parent.Position, map, signedHeat);
    }

    private float GetDesiredTargetTemperature(float currentTemp)
    {
        FloatRange range = Props.targetTemperatureRange;

        float distMin = Mathf.Abs(currentTemp - range.min);
        float distMax = Mathf.Abs(currentTemp - range.max);

        return distMin <= distMax ? range.min : range.max;
    }

    public override string CompInspectStringExtra()
    {
        return "TSOA_AnimaFlowerTempInspection".Translate(lowerTemp, upperTemp);
    }
}
