using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class IntroSequence : MonoBehaviour
{
    [SerializeField] private CanvasGroup _prologueGroup;
    [SerializeField] private CanvasGroup _lineGroup;
    [SerializeField] private CanvasGroup _nameCardGroup;
    [SerializeField] private CanvasGroup _controlsGroup;
    [SerializeField] private TMP_Text _lineText;
    [SerializeField] private string[] _lines;
    [SerializeField] private CinemachineCamera _introCam;
    [SerializeField] private CinemachineCamera _playerCam;
    [SerializeField] private CinemachineBrain _brain;
    [SerializeField] private BossStateMachine _boss;
    [SerializeField] private AudioSource _bgm;
    [SerializeField] private AudioClip _revealSfx;

    private static bool _hasPlayed;

    private bool _skip;

    private IEnumerator Start()
    {
        SetGroup(_controlsGroup, false);

        if (_hasPlayed)
        {
            StartBattle();
            yield break;
        }

        Time.timeScale = 0f;
        _brain.IgnoreTimeScale = true;
        SetGroup(_prologueGroup, true);
        _introCam.gameObject.SetActive(true);

        foreach (string line in _lines)
        {
            if (_skip) break;

            _lineText.text = line;
            yield return StartCoroutine(Fade(_lineGroup, 1f, 1.2f));
            
            float t = 0f;
            while (t < 2.5f && !_skip)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return StartCoroutine(Fade(_lineGroup, 0, 0.8f));
        }

        yield return StartCoroutine(Fade(_prologueGroup, 0f, 1.5f));
        yield return new WaitForSecondsRealtime(0.8f);
        _boss.SetEyeGlow(true);
        SfxPlayer.Instance?.Play(new[] { _revealSfx });
        yield return new WaitForSecondsRealtime(0.8f);

        yield return StartCoroutine(Fade(_nameCardGroup, 1f, 1f));
        yield return new WaitForSecondsRealtime(2.5f);
        yield return StartCoroutine(Fade(_nameCardGroup, 0f, 0.8f));

        _boss.SetEyeGlow(false);
        _playerCam.BlendHint &= ~CinemachineCore.BlendHints.InheritPosition;
        _introCam.gameObject.SetActive(false);
        yield return null;
        _playerCam.BlendHint |= CinemachineCore.BlendHints.InheritPosition;

        SetGroup(_controlsGroup, true);
        yield return new WaitUntil(() => Keyboard.current.spaceKey.wasPressedThisFrame);
        SetGroup(_controlsGroup, false);

        StartBattle();
    }

    private void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            _skip = true;
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

    private void SetGroup(CanvasGroup g, bool visible)
    {
        g.alpha = visible ? 1f : 0f;
        g.interactable = visible;
        g.blocksRaycasts = visible;
    }

    private void StartBattle()
    {
        _hasPlayed = true;
        Time.timeScale = 1f;
        _brain.IgnoreTimeScale = false;
        _bgm.Play();
    }
}
