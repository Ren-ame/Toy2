using UnityEngine;
using UnityEngine.UI;

public class GaugeBarUI : MonoBehaviour
{
    [SerializeField] private MonoBehaviour _source;
    [SerializeField] private Image _fill;
    [SerializeField] private float _smoothSpeed = 0.3f;
    
    [SerializeField] private bool _growWithMax;
    [SerializeField] private float _baseMax = 100f;
    [SerializeField] private RectTransform _bar;

    private float _baseWidth;

    private IGauge _gauge;
    private float _targetRatio;

    private void Awake()
    {
        _gauge = _source as IGauge;
        if (_growWithMax) _baseWidth = _bar.sizeDelta.x;
    }

    private void OnEnable()
    {
        _gauge.OnChanged += UpdateBar;
    }

    private void OnDisable()
    {
        _gauge.OnChanged -= UpdateBar;
    }

    private void Start()
    {
        _targetRatio = _gauge.Ratio;
        _fill.fillAmount = _gauge.Ratio;
        ApplyWidth(_gauge.Max);
    }

    private void Update()
    {
        _fill.fillAmount = Mathf.MoveTowards(_fill.fillAmount, _targetRatio, _smoothSpeed * Time.deltaTime);
    }

    private void UpdateBar(float current, float max)
    {
        ApplyWidth(max);
        _targetRatio = current / max;
    }

    private void ApplyWidth(float max)
    {
        if (!_growWithMax)
            return;

        float width = _baseWidth * max / _baseMax;
        _bar.sizeDelta = new Vector2(width, _bar.sizeDelta.y);
    }
}

