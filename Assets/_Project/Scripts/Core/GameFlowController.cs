using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameFlowController : MonoBehaviour
{
    [SerializeField] private HealthComponent _playerHealth;
    [SerializeField] private HealthComponent _bossHealth;
    [SerializeField] private CanvasGroup _resultGroup;
    [SerializeField] private GameObject _winText;
    [SerializeField] private GameObject _loseText;
    [SerializeField] private float _fadeSpeed = 1f;
    [SerializeField] private AudioClip _winSfx;
    [SerializeField] private AudioClip _loseSfx;
    [SerializeField] private AudioSource _bgm;
    [SerializeField] private float _bgmFadeSpeed = 0.4f;

    private bool _isOver;

    private void OnEnable()
    {
        _playerHealth.OnDied += HandlePlayerDied;
        _bossHealth.OnDied += HandleBossDied;
    }

    private void OnDisable()
    {
        _playerHealth.OnDied -= HandlePlayerDied;
        _bossHealth.OnDied -= HandleBossDied;
    }

    private void HandlePlayerDied()
    {
        ShowResult(_loseText, _loseSfx);
        PlayerPrefs.Save();
    }

    private void HandleBossDied()
    {
        ShowResult(_winText, _winSfx);
        PlayerPrefs.Save();
    }

    private void ShowResult(GameObject text, AudioClip sfx)
    {
        if (_isOver == true)
            return;
        _isOver = true;
        _resultGroup.gameObject.SetActive(true);
        text.SetActive(true);
        SfxPlayer.Instance?.Play(new[] { sfx });
    }

    private void Update()
    {
        if (_isOver == true)
        {
            _resultGroup.alpha = Mathf.MoveTowards(_resultGroup.alpha, 1, _fadeSpeed * Time.deltaTime);
            if (_bgm != null)
                _bgm.volume = Mathf.MoveTowards(_bgm.volume, 0, _bgmFadeSpeed * Time.deltaTime);
        }

        if (_isOver == true && Keyboard.current.rKey.wasPressedThisFrame && _resultGroup.alpha >= 1f)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}

