using System;
using UnityEngine;

public class MerchantNpc : MonoBehaviour
{
    [SerializeField] private GameObject _prompt;
    [SerializeField] private UpgradeMenu _menu;

    private PlayerInputReader _input;
    private Animator _animator;

    private const float TurnSpeed = 360f;
    private Quaternion _homeRotation;
    private bool _isTalking;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _homeRotation = transform.rotation;
    }

    private void Update()
    {
        Quaternion target = _homeRotation;

        if (_isTalking && _input != null)
        {
            Vector3 dir = _input.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                target = Quaternion.LookRotation(dir);
        }

        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, TurnSpeed * Time.unscaledDeltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        _input = other.GetComponent<PlayerInputReader>();
        _input.OnInteractPressed += Interact;
        _prompt.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        _input.OnInteractPressed -= Interact;
        _input = null;
        _prompt.SetActive(false);
        _animator.CrossFade("Idle", 0.2f);
        _isTalking = false;
    }

    private void Interact()
    {
        if (_menu.IsOpen) return;

        _animator.CrossFade("Talk", 0.2f);
        _isTalking = true;
        _prompt.SetActive(false);
        _menu.Open();
    }

    private void OnEnable()
    {
        _menu.OnClosed += HandleMenuClosed;
    }

    private void OnDisable()
    {
        if (_input != null)
            _input.OnInteractPressed -= Interact;

        _menu.OnClosed -= HandleMenuClosed;
    }

    private void HandleMenuClosed()
    {
        _animator.CrossFade("Idle", 0.2f);
        _isTalking = false;
        if (_input != null) _prompt.SetActive(true);
    }
}
