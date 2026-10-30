using System.Collections;
using TMPro;
using UnityEngine;

public class BossSubtitles : MonoBehaviour
{
    [SerializeField] private CanvasGroup _group;
    [SerializeField] private TMP_Text _text;
    [SerializeField] private HealthComponent _bossHealth;
    [SerializeField] private PoiseComponent _bossPoise;
    [SerializeField] private HealthComponent _playerHealth;

    [SerializeField] private string _battleStartLine;
    [SerializeField] private string _halfHpLine;
    [SerializeField] private string _groggyLine;
    [SerializeField] private string _bossDeathLine;
    [SerializeField] private string _playerDeathLine;

    [SerializeField] private float _holdTime = 3f;
    private const float FadeTime = 0.3f;

    [SerializeField] private BossStateMachine _boss;

    private bool _halfShown, _groggyShown;
    private Coroutine _routine;

    private void OnEnable()
    {
        _bossHealth.OnChanged += HandleBossHealth;
        _bossPoise.OnBroken += HandlePoiseBroken;
        _bossHealth.OnDied += HandleBossDied;
        _playerHealth.OnDied += HandlePlayerDied;
        _boss.OnPlayerDetected += HandlePlayerDetected;
    }

    private void OnDisable()
    {
        _bossHealth.OnChanged -= HandleBossHealth;
        _bossPoise.OnBroken -= HandlePoiseBroken;
        _bossHealth.OnDied -= HandleBossDied;
        _playerHealth.OnDied -= HandlePlayerDied;
        _boss.OnPlayerDetected -= HandlePlayerDetected;
    }

    private void HandleBossHealth(float current, float max)
    {
        if (!_halfShown && current > 0 && current <= max * 0.5f)
        {
            _halfShown = true;
            Show(_halfHpLine);
        }
    }

    private void HandlePoiseBroken()
    {
        if (!_groggyShown)
        {
            _groggyShown = true;
            Show(_groggyLine);
        }
    }

    private void HandleBossDied()
    {
        Show(_bossDeathLine);
    }

    private void HandlePlayerDied()
    {
        Show(_playerDeathLine);
    }

    private void HandlePlayerDetected()
    {
        Show(_battleStartLine);
    }

    private void Show(string line)
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(ShowRoutine(line));
    }

    private IEnumerator ShowRoutine(string line)
    {
        _text.text = line;
        yield return Fade(_group, 1f, FadeTime);
        yield return new WaitForSecondsRealtime(_holdTime);
        yield return Fade(_group, 0f, FadeTime);
        _routine = null;
    }

    private IEnumerator Fade(CanvasGroup g, float to, float duration)
    {
        float speed = 1f / duration;
        while (g.alpha != to)
        {
            g.alpha = Mathf.MoveTowards(g.alpha, to, speed * Time.unscaledDeltaTime);
            yield return null;
        }
    }
}
