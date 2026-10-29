using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UpgradeMenu : MonoBehaviour
{
    [System.Serializable]
    private class Row
    {
        public TMP_Text Value;
        public TMP_Text Cost;
        public Button Button; 
    }

    [SerializeField] private CanvasGroup _group;
    [SerializeField] private TMP_Text _currencyText;
    [SerializeField] private Row _atkRow;
    [SerializeField] private Row _hpRow;
    [SerializeField] private Row _staminaRow;
    [SerializeField] private Row _stRegenRow;
    [SerializeField] private Button _respecButton;
    [SerializeField] private TMP_Text _respecText;
    [SerializeField] private Button _closeButton;
    [SerializeField] private PlayerCombatTarget _player;

    private PlayerInputReader _input;

    public bool IsOpen { get; private set; }
    public event Action OnClosed;

    private void Awake()
    {
        _input = _player.GetComponent<PlayerInputReader>();
    }

    private void Start()
    {
        _atkRow.Button.onClick.AddListener(UpgradeAtk);
        _hpRow.Button.onClick.AddListener(UpgradeHp);
        _staminaRow.Button.onClick.AddListener(UpgradeStamina);
        _stRegenRow.Button.onClick.AddListener(UpgradeStRegen);
        _respecButton.onClick.AddListener(Respec);
        _closeButton.onClick.AddListener(Close);

        SetGroup(false);
    }

    private void LateUpdate()
    {
        if (IsOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    private void UpgradeAtk()
    {
        if (Progression.TryUpgradeAtk()) _player.ApplyProgression();
        Refresh();
    }

    private void UpgradeHp()
    {
        if (Progression.TryUpgradeHp()) _player.ApplyProgression();
        Refresh();
    }

    private void UpgradeStamina()
    {
        if (Progression.TryUpgradeStamina()) _player.ApplyProgression();
        Refresh();
    }

    private void UpgradeStRegen()
    {
        if (Progression.TryUpgradeStRegen()) _player.ApplyProgression();
        Refresh();
    }

    private void Respec()
    {
        Progression.Respec();
        _player.ApplyProgression();
        Refresh();
    }

    public void Open()
    {
        SetGroup(true);
        IsOpen = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        _input.SetGameplayEnabled(false);
        Refresh();
    }

    private void Close()
    {
        SetGroup(false);
        IsOpen = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _input.SetGameplayEnabled(true);
        OnClosed?.Invoke();
    }
    private void SetGroup(bool visible)
    {
        _group.alpha = visible ? 1f : 0f;
        _group.interactable = visible;
        _group.blocksRaycasts = visible;
    }
    private void Refresh()
    {
        _currencyText.text = Progression.Currency.ToString();

        SetRow(_atkRow, Progression.AtkLevel, Progression.Atk, Progression.AtkPerLevel);
        SetRow(_hpRow, Progression.HpLevel, Progression.MaxHp, Progression.HpPerLevel);
        SetRow(_staminaRow, Progression.MaxStaminaLevel, Progression.MaxStamina, Progression.StaminaPerLevel);
        SetRow(_stRegenRow, Progression.StRegenLevel, Progression.StaminaRegen, Progression.StRegenPerLevel);

        int refund = Progression.RefundAmount;
        _respecText.text = $"초기화 (+{refund})";
        _respecButton.interactable = refund > 0;
    }

    private void SetRow(Row row, int level, float current, float perLevel)
    {
        if (level >= Progression.MaxLevel)
        {
            row.Value.text = current.ToString("0.#");
            row.Cost.text = "MAX";
            row.Button.interactable = false;
            return;
        }

        int cost = Progression.GetCost(level);
        row.Value.text = $"{current:0.#} → {current + perLevel:0.#}";
        row.Cost.text = cost.ToString();
        row.Button.interactable = Progression.Currency >= cost;
    }
}