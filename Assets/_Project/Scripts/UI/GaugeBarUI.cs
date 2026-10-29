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

    [SerializeField] private Image _trail;
    [SerializeField] private float _trailDelay = 0.5f;
    [SerializeField] private float _trailSpeed = 0.8f;

    private float _trailTimer;

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
        if (_trail != null) _trail.fillAmount = _gauge.Ratio;
        ApplyWidth(_gauge.Max);
    }

    private void Update()
    {
        _fill.fillAmount = Mathf.MoveTowards(_fill.fillAmount, _targetRatio, _smoothSpeed * Time.deltaTime);

        if (_trail == null) return;

        if (_trailTimer > 0f)
        {
            _trailTimer -= Time.deltaTime;
            return;
        }

        _trail.fillAmount = Mathf.MoveTowards(_trail.fillAmount, _fill.fillAmount, _trailSpeed * Time.deltaTime);

        if (_trail.fillAmount < _fill.fillAmount) _trail.fillAmount = _fill.fillAmount;
    }

    private void UpdateBar(float current, float max)
    {
        ApplyWidth(max);

        float newRatio = current / max;
        if (newRatio < _targetRatio) _trailTimer = _trailDelay;
        _targetRatio = newRatio;
    }

    private void ApplyWidth(float max)
    {
        if (!_growWithMax)
            return;

        float width = _baseWidth * max / _baseMax;
        _bar.sizeDelta = new Vector2(width, _bar.sizeDelta.y);
    }
}

