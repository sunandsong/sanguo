using System.Collections.Generic;
using UnityEngine;

namespace LiuBeiRPG.Game
{
    /// <summary>
    /// 音效：Sfx.Play("swing") 播 Resources/Audio/swing（wav / mp3 / ogg 都行）。缺文件就不响。
    /// 文件清单和 AI 生成提示词见 ArtSrc/音效提示词.md。
    /// </summary>
    public static class Sfx
    {
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        static AudioSource[] pool;
        static readonly Dictionary<string, float> lastAt = new Dictionary<string, float>();

        /// <summary>pitch 在 1±jitter 里随机，连续同一个声（脚步、金币）听着不机械。同一个声 0.03 秒内只响一次。</summary>
        public static void Play(string name, float volume = 1f, float jitter = 0f)
        {
            AudioClip clip;
            if (!clips.TryGetValue(name, out clip)) clips[name] = clip = Resources.Load<AudioClip>("Audio/" + name);
            if (clip == null) return;
            float last;
            if (lastAt.TryGetValue(name, out last) && Time.unscaledTime - last < 0.03f) return;
            lastAt[name] = Time.unscaledTime;
            if (pool == null)
            {
                var go = new GameObject("Sfx");
                Object.DontDestroyOnLoad(go);
                pool = new AudioSource[10];
                for (int i = 0; i < pool.Length; i++) { pool[i] = go.AddComponent<AudioSource>(); pool[i].playOnAwake = false; }
            }
            // 先用空着的声道；都在响就掐快放完的那个，别掐掉胜利号角这种长的（结算时金币、经验一连串短音会把 10 个声道转一遍）
            AudioSource s = null;
            float best = -1f;
            for (int i = 0; i < pool.Length; i++)
            {
                var p = pool[i];
                if (!p.isPlaying) { s = p; break; }
                float done = p.clip == null ? 1f : p.time / p.clip.length;
                if (done > best) { best = done; s = p; }
            }
            s.clip = clip;
            s.volume = volume;
            s.pitch = 1f + Random.Range(-jitter, jitter);
            s.Play();
        }

        static AudioSource music;
        static string playing;
        const float MusicVolume = 0.25f;

        /// <summary>背景音乐，每帧调，传这会儿该放哪首（Resources/Audio/bgm_xxx，null = 静音）。换曲：旧的 0.5 秒淡出，新的 1 秒淡入。</summary>
        public static void Music(string name)
        {
            if (music == null)
            {
                var go = new GameObject("Music");
                Object.DontDestroyOnLoad(go);
                music = go.AddComponent<AudioSource>();
                music.loop = true;
                music.playOnAwake = false;
                music.volume = 0f;
            }
            float dt = Time.unscaledDeltaTime;
            if (name == playing) { music.volume = Mathf.MoveTowards(music.volume, MusicVolume, dt * MusicVolume); return; }
            music.volume -= dt * MusicVolume * 2f;
            if (music.volume > 0f && music.isPlaying) return;
            playing = name;
            music.clip = name == null ? null : Resources.Load<AudioClip>("Audio/" + name);
            music.volume = 0f;
            if (music.clip != null) music.Play(); else music.Stop();
        }
    }
}
