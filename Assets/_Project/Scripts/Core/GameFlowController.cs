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
        ShowResult(_loseText);
    }

    private void HandleBossDied()
    {
        ShowResult(_winText);
    }

    private void ShowResult(GameObject text)
    {
        if (_isOver == true)
            return;
        _isOver = true;
        _resultGroup.gameObject.SetActive(true);
        text.SetActive(true);
    }

    private void Update()
    {
        if (_isOver == true)
        {
            _resultGroup.alpha = Mathf.MoveTowards(_resultGroup.alpha, 1, _fadeSpeed * Time.deltaTime);
        }

        if (_isOver == true && Keyboard.current.rKey.wasPressedThisFrame && _resultGroup.alpha >= 1f)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}

