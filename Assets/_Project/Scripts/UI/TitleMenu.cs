using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TitleMenu : MonoBehaviour
{
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private string _gameScene = "Arena";

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

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
