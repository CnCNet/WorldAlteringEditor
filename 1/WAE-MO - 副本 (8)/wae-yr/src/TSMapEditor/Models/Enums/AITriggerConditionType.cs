namespace TSMapEditor.Models
{
    public enum AITriggerConditionType
    {
        None = -1,
        EnemyOwns = 0,
        OwnerOwns = 1,
        EnemyOnYellowPower = 2,
        EnemyOnRedPower = 3,
        EnemyHasCredits = 4,

        // The following are RA2/YR only
        OwnerHasIronCurtainReady = 5,
        OwnerHasChronosphereReady = 6,
        NeutralHouseOwns = 7
    }
}
