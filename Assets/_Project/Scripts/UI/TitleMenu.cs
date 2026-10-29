using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

public class TitleMenu : MonoBehaviour
{
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private string _gameScene = "Arena";
    [SerializeField] private AudioMixer _mixer;

    [Header("기록 초기화")]
    [SerializeField] private Button _resetButton;       // 화면 구석의 [기록 초기화]
    [SerializeField] private CanvasGroup _confirmGroup; // 확인 창
    [SerializeField] private Button _confirmYesButton;
    [SerializeField] private Button _confirmNoButton;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        VolumeSettings.ApplyAll(_mixer);

        _startButton.onClick.AddListener(StartGame);
        _quitButton.onClick.AddListener(Quit);

        _resetButton.onClick.AddListener(OpenConfirm);
        _confirmYesButton.onClick.AddListener(ConfirmReset);
        _confirmNoButton.onClick.AddListener(CloseConfirm);
        SetConfirm(false);
    }

    private void StartGame()
    {
        SceneManager.LoadScene(_gameScene);
    }

    private void Quit()
    {
        Application.Quit();
    }

    // 기록 초기화: 되돌릴 수 없으니 확인 창을 한 번 거친다
    private void OpenConfirm() => SetConfirm(true);
    private void CloseConfirm() => SetConfirm(false);

    private void ConfirmReset()
    {
        Progression.ResetAll();   // 강화 레벨·재화만 0 (감도·볼륨 설정은 유지)
        SetConfirm(false);
    }

    private void SetConfirm(bool visible)
    {
        _confirmGroup.alpha = visible ? 1f : 0f;
        _confirmGroup.interactable = visible;
        _confirmGroup.blocksRaycasts = visible;
    }
}
