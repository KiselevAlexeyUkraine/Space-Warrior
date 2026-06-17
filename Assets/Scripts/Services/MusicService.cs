using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Services
{
    public class MusicService : MonoBehaviour
    {
        [SerializeField]
        private AudioSource _audioSource;

        [SerializeField]
        private List<AudioClip> _musicList;

        private int _index = -1;
        private Coroutine _playbackCoroutine;

        public bool IsPlaying => _audioSource.isPlaying;

        private void Awake()
        {
            foreach (var clip in _musicList)
            {
                if (!clip.preloadAudioData)
                    clip.LoadAudioData();
            }
        }

        private void Start()
        {
            if (_musicList.Count > 0)
            {
                _playbackCoroutine = StartCoroutine(PlaybackLoop());
            }
        }

        private IEnumerator PlaybackLoop()
        {
            while (true)
            {
                if (!_audioSource.isPlaying)
                {
                    yield return StartCoroutine(PlayNextCoroutine());
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        private IEnumerator PlayNextCoroutine()
        {
            if (_musicList.Count == 0) yield break;

            _index = (_index + 1) % _musicList.Count;

            AudioClip nextClip = _musicList[_index];

            if (nextClip.loadState != AudioDataLoadState.Loaded)
            {
                nextClip.LoadAudioData();
                yield return new WaitUntil(() => nextClip.loadState == AudioDataLoadState.Loaded);
            }

            _audioSource.clip = nextClip;
            _audioSource.Play();
        }

        public void PlayNext()
        {
            StartCoroutine(PlayNextCoroutine());
        }
    }
}