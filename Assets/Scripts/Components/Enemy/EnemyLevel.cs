namespace Components.Enemy
{
    // Уровень врага. Выставляется в инспекторе у конкретных наследников EnemyBase
    // и используется для выборки значений из BalanceConfig.
    public enum EnemyLevel
    {
        Level_1 = 0,
        Level_2 = 1,
        Level_3 = 2,
        Level_4 = 3,
        Level_5 = 4
    }
}