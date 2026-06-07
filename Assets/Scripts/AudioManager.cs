using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public struct AudioGroupData
{
    public string name;
    public AudioClip[] clips;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    
    [SerializeField] private List<AudioGroupData> audioGroupDataList;
    [SerializeField] private int poolSize = 10;

    private Queue<AudioPlayer> _pool = new Queue<AudioPlayer>();
    private List<AudioPlayer> _active = new List<AudioPlayer>();
    
    private Dictionary<string, long> _lastPlayTimestamp = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        for (int i = 0; i < poolSize; i++)
        {
            var go = new GameObject($"AudioPlayer_{i}");
            go.transform.SetParent(transform);

            var player = go.AddComponent<AudioPlayer>();
            go.SetActive(false);

            _pool.Enqueue(player);
        }
    }

    public void Play(string group, float volume = -1f, float interval = 50f)
    {
        var groupData = audioGroupDataList.Find(x => x.name == group);
        if (string.IsNullOrEmpty(groupData.name)) return;
        var clip = groupData.clips[Random.Range(0, groupData.clips.Length)];
        if (!clip) return;
        var timeStampNow = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (_lastPlayTimestamp.TryGetValue(groupData.name, out var lastTime) && 
            timeStampNow - lastTime < interval) return;

        if (volume < 0)
        {
            volume = Configs.GetVfxVolume(group);
        }
        
        var player = GetPlayer();
        player.Play(clip, volume, OnPlayerFinish);
        _lastPlayTimestamp[groupData.name] = timeStampNow;
    }

    private AudioPlayer GetPlayer()
    {
        AudioPlayer player;

        if (_pool.Count > 0)
        {
            player = _pool.Dequeue();
        }
        else
        {
            // 不够用就扩容（minimal但实用）
            var go = new GameObject("AudioPlayer_Extra");
            go.transform.SetParent(transform);
            player = go.AddComponent<AudioPlayer>();
        }

        _active.Add(player);
        return player;
    }

    private void OnPlayerFinish(AudioPlayer player)
    {
        _active.Remove(player);
        _pool.Enqueue(player);
    }

    public void PlayFootstep(bool isRunning)
    {
        Play("Walk", volume: 0.3f, interval: (isRunning ? 250f: 500f) / Time.timeScale);
    }
}