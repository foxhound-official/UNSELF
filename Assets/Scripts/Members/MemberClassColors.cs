using UnityEngine;

public static class MemberClassColors
{
    public static Color Get(MemberClass memberClass)
    {
        return memberClass switch
        {
            MemberClass.Social => new Color(0.30f, 0.70f, 1.00f),
            MemberClass.Technical => new Color(1.00f, 0.75f, 0.20f),
            MemberClass.Combat => new Color(1.00f, 0.30f, 0.25f),
            MemberClass.Agility => new Color(0.28f, 0.84f, 0.66f),
            _ => Color.white
        };
    }
}