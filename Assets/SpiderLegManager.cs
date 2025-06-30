using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiderLegManager : MonoBehaviour
{
    public SpiderLeg[] AllLegs;

    public void RunAnimation(int animIx)
    {
        foreach (var leg in AllLegs)
            leg.RunAnimation((animIx * AllLegs.Length) + leg.LegIx);
    }

    public void StopAnimation()
    {
        foreach (var leg in AllLegs)
            leg.StopAnimation();
    }
}
