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

    private CanvasGroup _titleGroup;
    private CanvasGroup _controlsGroup;
    private Button _confirmButton;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        VolumeSettings.ApplyAll(_mixer);

        _startButton.onClick.AddListener(StartGame);
        _quitButton.onClick.AddListener(Quit);
    }

    private void StartGame()
    {
        SceneManager.LoadScene(_gameScene);
    }

    private void Quit()
    {
        Application.Quit();
    }
}
