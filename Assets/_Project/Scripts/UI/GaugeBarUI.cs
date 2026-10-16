using UnityEngine;
using UnityEngine.UI;

public class GaugeBarUI : MonoBehaviour
{
    [SerializeField] private MonoBehaviour _source;
    [SerializeField] private Image _fill;
    [SerializeField] private float _smoothSpeed = 0.3f;

    private IGauge _gauge;
    private float _targetRatio;

    private void Awake()
    {
        _gauge = _source as IGauge;
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
    }

    private void Update()
    {
        _fill.fillAmount = Mathf.MoveTowards(_fill.fillAmount, _targetRatio, _smoothSpeed * Time.deltaTime);
    }

    private void UpdateBar(float current, float max)
    {
        _targetRatio = current / max;
    }
}

