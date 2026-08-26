using System;
using System.Collections.Generic;
using System.IO;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Audio
{
    public sealed class SoundManager
    {
        private readonly Dictionary<SoundId, Uri> _uris = new();
        private readonly HashSet<MediaPlayer> _activeEffects = new();
        private readonly object _effectsLock = new();
        private readonly MediaPlayer _bgmPlayer = new();

        public SoundManager()
        {
            var soundDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Sound");
            foreach (var (id, fileName) in SoundCatalog.FileNames)
            {
                _uris[id] = new Uri(Path.Combine(soundDir, fileName));
            }
        }

        public void PlayEffect(SoundId id)
        {
            var player = new MediaPlayer { Source = MediaSource.CreateFromUri(_uris[id]) };

            lock (_effectsLock)
            {
                _activeEffects.Add(player);
            }

            player.MediaEnded += OnEffectEnded;
            player.MediaFailed += OnEffectFailed;
            player.Play();
        }

        private void OnEffectEnded(MediaPlayer sender, object args) => CleanUpEffect(sender);

        private void OnEffectFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args) => CleanUpEffect(sender);

        private void CleanUpEffect(MediaPlayer player)
        {
            lock (_effectsLock)
            {
                _activeEffects.Remove(player);
            }

            player.MediaEnded -= OnEffectEnded;
            player.MediaFailed -= OnEffectFailed;
            player.Dispose();
        }

        public void PlayBgm(SoundId id)
        {
            _bgmPlayer.Source = MediaSource.CreateFromUri(_uris[id]);
            _bgmPlayer.IsLoopingEnabled = true;
            _bgmPlayer.Play();
        }

        public void StopBgm()
        {
            _bgmPlayer.Pause();
            _bgmPlayer.Source = null;
        }

        public void StopAll()
        {
            StopBgm();

            lock (_effectsLock)
            {
                foreach (var player in _activeEffects)
                {
                    player.MediaEnded -= OnEffectEnded;
                    player.MediaFailed -= OnEffectFailed;
                    player.Pause();
                    player.Dispose();
                }

                _activeEffects.Clear();
            }
        }
    }
}
