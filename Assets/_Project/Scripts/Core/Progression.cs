using UnityEngine;

public static class Progression
{
    private const string CurrencyKey = "Currency";
    private const string AtkLevelKey = "AtkLevel";
    private const string HpLevelKey = "HpLevel";
    private const string MaxStaminaLevelKey = "MaxStaminaLevel";
    private const string StRegenLevelKey = "StRegenLevel";

    private const float BaseAtk = 10f;
    private const float AtkPerLevel = 1f;
    private const float BaseHp = 100f;
    private const float HpPerLevel = 10f;
    private const float BaseStamina = 100f;
    private const float StaminaPerLevel = 10f;
    private const float BaseStRegen = 35f;
    private const float StRegenPerLevel = 3.5f;

    private const int BaseCost = 100;
    private const int CostStep = 50;
    public const int MaxLevel = 10;

    public static int Currency => PlayerPrefs.GetInt(CurrencyKey, 0);
    public static int AtkLevel => PlayerPrefs.GetInt(AtkLevelKey, 0);
    public static int HpLevel => PlayerPrefs.GetInt(HpLevelKey, 0);
    public static int MaxStaminaLevel => PlayerPrefs.GetInt(MaxStaminaLevelKey, 0);
    public static int StRegenLevel => PlayerPrefs.GetInt(StRegenLevelKey, 0);

    public static float Atk => BaseAtk + AtkLevel * AtkPerLevel;
    public static float MaxHp => BaseHp + HpLevel * HpPerLevel;
    public static float MaxStamina => BaseStamina + MaxStaminaLevel * StaminaPerLevel;
    public static float StaminaRegen => BaseStRegen + StRegenLevel * StRegenPerLevel;

    public static int GetCost(int level) => BaseCost + level * CostStep;

    public static void AddCurrency(int amount)
    {
        PlayerPrefs.SetInt(CurrencyKey, Currency + amount);
    }

    private static bool TryUpgrade(string levelKey)
    {
        int level = PlayerPrefs.GetInt(levelKey, 0);

        if (level >= MaxLevel)
            return false;

        int cost = GetCost(level);
        if (cost > Currency)
            return false;

        PlayerPrefs.SetInt(CurrencyKey, Currency - cost);
        PlayerPrefs.SetInt(levelKey, level + 1);
        PlayerPrefs.Save();
        return true;
    }

    public static bool TryUpgradeAtk() => TryUpgrade(AtkLevelKey);
    public static bool TryUpgradeHp() => TryUpgrade(HpLevelKey);
    public static bool TryUpgradeStamina() => TryUpgrade(MaxStaminaLevelKey);
    public static bool TryUpgradeStRegen() => TryUpgrade(StRegenLevelKey);
    
    private static int SpentOn(int level)
    {
        int sum = 0;
        for (int i = 0; i < level; i++)
            sum += GetCost(i);
        return sum;
    }

    public static int RefundAmount =>
        SpentOn(AtkLevel) + SpentOn(HpLevel) + SpentOn(MaxStaminaLevel) + SpentOn(StRegenLevel);

    public static void Respec()
    {
        int refund = RefundAmount;

        PlayerPrefs.SetInt(AtkLevelKey, 0);
        PlayerPrefs.SetInt(HpLevelKey, 0);
        PlayerPrefs.SetInt(MaxStaminaLevelKey, 0);
        PlayerPrefs.SetInt(StRegenLevelKey, 0);

        PlayerPrefs.SetInt(CurrencyKey, Currency + refund);
        PlayerPrefs.Save();
    }

    public static void ResetAll()
    {
        PlayerPrefs.SetInt(AtkLevelKey, 0);
        PlayerPrefs.SetInt(HpLevelKey, 0);
        PlayerPrefs.SetInt(MaxStaminaLevelKey, 0);
        PlayerPrefs.SetInt(StRegenLevelKey, 0);
        PlayerPrefs.SetInt(CurrencyKey, 0);

        PlayerPrefs.Save();
    }
}
