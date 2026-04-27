using UnityEngine;

public class ArmRuntimeUI : ArmUI
{
   public void SetData(RuntimeMetrics metrics)
   {
      if (hand != metrics.handType) return;

      float handUsage = metrics.usageByZone.ContainsKey(MotionZone.Hand) ? metrics.usageByZone[MotionZone.Hand] : 0;
      float wristUsage = metrics.usageByZone.ContainsKey(MotionZone.Wrist) ? metrics.usageByZone[MotionZone.Wrist] : 0;
      float forearmUsage = metrics.usageByZone.ContainsKey(MotionZone.Forearm) ? metrics.usageByZone[MotionZone.Forearm] : 0;

      ApplyAll(handUsage, wristUsage, forearmUsage);
   }
}