using System.Collections;
using UnityEngine;

public class HitStop : MonoBehaviour
{
    public static HitStop Instance { get; private set; }

    private Coroutine _routine;

    private void Awake()
    {
        Instance = this;
    }

    public void Play(float duration)
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(StopRoutine(duration));
    }

    private IEnumerator StopRoutine(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }
}
