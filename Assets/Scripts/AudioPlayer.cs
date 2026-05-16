using System;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioPlayer : MonoBehaviour
{
    private AudioSource _source;
    private Action<AudioPlayer> _onFinish;

    private void Awake()
    {
        _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;
    }

    public void Play(AudioClip clip, float volume, Action<AudioPlayer> onFinish)
    {
        gameObject.SetActive(true);

        _onFinish = onFinish;

        _source.clip = clip;
        _source.volume = volume;
        _source.Play();

        StartCoroutine(CoPlay());
    }

    private System.Collections.IEnumerator CoPlay()
    {
        yield return new WaitWhile(() => _source.isPlaying);

        gameObject.SetActive(false);
        _onFinish?.Invoke(this);
    }
}