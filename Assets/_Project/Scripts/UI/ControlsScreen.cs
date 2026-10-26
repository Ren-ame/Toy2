using UnityEngine;
using UnityEngine.InputSystem;

public class ControlsScreen : MonoBehaviour
{
    private static bool _isShown;

    [SerializeField] private CanvasGroup _group;

    private void Start()
    {
        if (_isShown)
            SetGroup(false);
        else
        {
            SetGroup(true);
            Time.timeScale = 0;
        }
    }

    private void Update()
    {
        if (!_isShown && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            SetGroup(false);
            Time.timeScale = 1;
            _isShown = true;
        }
    }

    private void SetGroup(bool visible)
    {
        _group.alpha = visible ? 1f : 0f;
        _group.interactable = visible;
        _group.blocksRaycasts = visible;
    }
}
