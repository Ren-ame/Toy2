using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
using TMPro;

public class PauseMenu : MonoBehaviour
{
    private const string KeyX = "MouseSensX10";
    private const string KeyY = "MouseSensY10";

    private const float DefaultSensitivity = 5f;
    private const float BaseSensitivity = 5f;

    [SerializeField] private CanvasGroup _group;
    [SerializeField] private Slider _mouseXSlider;
    [SerializeField] private Slider _mouseYSlider;
    [SerializeField] private TMP_Text _valueXText;
    [SerializeField] private TMP_Text _valueYText;
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _titleButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private CinemachineInputAxisController _axisController;

    private float[] _baseGains;
    private float _sensX, _sensY;
    private bool _isOpen;

    void Start()
    {
        var controllers = _axisController.Controllers;
        _baseGains = new float[controllers.Count];
        for (int i = 0; i < controllers.Count; i++)
            _baseGains[i] = controllers[i].Input.Gain;

        _sensX = PlayerPrefs.GetFloat(KeyX, DefaultSensitivity);
        _sensY = PlayerPrefs.GetFloat(KeyY, DefaultSensitivity);

        _mouseXSlider.value = _sensX * 10f;
        _valueXText.text = _sensX.ToString("0.0"); 
        _mouseYSlider.value = _sensY * 10f;
        _valueYText.text = _sensY.ToString("0.0");

        ApplySensitivity();

        _mouseXSlider.onValueChanged.AddListener(OnSensitivityXChanged);
        _mouseYSlider.onValueChanged.AddListener(OnSensitivityYChanged);
        _resumeButton.onClick.AddListener(Close);
        _titleButton.onClick.AddListener(ToTitle);
        _quitButton.onClick.AddListener(Quit);

        SetGroup(false);
    }

    void Update()
    {
        if (!Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        if (_isOpen)
            Close();
        else if (Time.timeScale == 0f)
            return;
        else
            Open();
    }

    private void ApplySensitivity()
    {
        var controllers = _axisController.Controllers;
        for (int i = 0; i < controllers.Count; i++)
        {
            float multiplier = 1f;

            if (controllers[i].Name == "Look Orbit X")
                multiplier = _sensX / BaseSensitivity;
            else if (controllers[i].Name == "Look Orbit Y")
                multiplier = _sensY / BaseSensitivity;

            controllers[i].Input.Gain = _baseGains[i] * multiplier;
        }
    }

    private void OnSensitivityXChanged(float v)
    {
        _sensX = v / 10f;
        PlayerPrefs.SetFloat(KeyX, _sensX);
        _valueXText.text = _sensX.ToString("0.0");
        ApplySensitivity();
    }

    private void OnSensitivityYChanged(float v)
    {
        _sensY = v / 10f;
        PlayerPrefs.SetFloat(KeyY, _sensY);
        _valueYText.text = _sensY.ToString("0.0");
        ApplySensitivity();
    }

    private void Open()
    {
        SetGroup(true);
        _isOpen = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Close()
    {
        SetGroup(false);
        _isOpen = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        PlayerPrefs.Save();
    }

    private void SetGroup(bool visible)
    {
        _group.alpha = visible ? 1f : 0f;
        _group.interactable = visible;
        _group.blocksRaycasts = visible;
    }

    private void ToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    private void Quit()
    {
        Application.Quit();
    }
}
