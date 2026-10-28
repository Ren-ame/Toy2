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

    public void Play(float duration, float scale = 0f)
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(StopRoutine(duration, scale));
    }

    private IEnumerator StopRoutine(float duration, float scale)
    {
        Time.timeScale = scale;
        yield return new WaitForSecondsRealtime(duration);
        if (Time.timeScale == scale) Time.timeScale = 1f;
    }
}
