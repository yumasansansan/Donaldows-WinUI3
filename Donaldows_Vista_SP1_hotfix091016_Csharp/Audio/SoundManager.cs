using System;
using System.Collections.Generic;
using System.IO;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Audio
{
    // Effects are fire-and-forget: one MediaPlayer per call, so overlapping
    // sounds mix the way HSP's mmplay slots do. BGM keeps a single looping
    // player, and StopAll is the mmstop equivalent.
    //
    // MediaEnded/MediaFailed are raised on WinRT thread-pool threads, so a
    // finished player must NOT be disposed from inside its own callback, and
    // must not be disposed concurrently with StopAll — doing either could throw
    // on a thread with no handler and fail the process fast. Finished players
    // are therefore parked in a retired list and disposed later from whichever
    // thread next calls in.
    public sealed class SoundManager
    {
        private readonly Dictionary<SoundId, Uri> _uris = new();
        private readonly HashSet<MediaPlayer> _activeEffects = new();
        private readonly List<MediaPlayer> _retiredEffects = new();
        private readonly object _effectsLock = new();
        private readonly MediaPlayer _bgmPlayer = new();
        private bool _shutdown;

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
            DisposeRetired();

            lock (_effectsLock)
            {
                if (_shutdown)
                {
                    return;
                }
            }

            var player = new MediaPlayer { Source = MediaSource.CreateFromUri(_uris[id]) };

            lock (_effectsLock)
            {
                _activeEffects.Add(player);
            }

            player.MediaEnded += OnEffectEnded;
            player.MediaFailed += OnEffectFailed;
            player.Play();
        }

        private void OnEffectEnded(MediaPlayer sender, object args) => RetireEffect(sender);

        private void OnEffectFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args) => RetireEffect(sender);

        private void RetireEffect(MediaPlayer player)
        {
            lock (_effectsLock)
            {
                // StopAll may have already taken this one; if so it owns it.
                if (!_activeEffects.Remove(player))
                {
                    return;
                }

                _retiredEffects.Add(player);
            }

            player.MediaEnded -= OnEffectEnded;
            player.MediaFailed -= OnEffectFailed;
        }

        private void DisposeRetired()
        {
            MediaPlayer[] retired;
            lock (_effectsLock)
            {
                if (_retiredEffects.Count == 0)
                {
                    return;
                }

                retired = _retiredEffects.ToArray();
                _retiredEffects.Clear();
            }

            foreach (var player in retired)
            {
                SafeDispose(player);
            }
        }

        public void PlayBgm(SoundId id)
        {
            lock (_effectsLock)
            {
                if (_shutdown)
                {
                    return;
                }
            }

            _bgmPlayer.Source = MediaSource.CreateFromUri(_uris[id]);
            _bgmPlayer.IsLoopingEnabled = true;
            _bgmPlayer.Play();
        }

        public void StopBgm()
        {
            try
            {
                _bgmPlayer.Pause();
                _bgmPlayer.Source = null;
            }
            catch (ObjectDisposedException)
            {
            }
        }

        public void StopAll()
        {
            StopBgm();

            MediaPlayer[] taken;
            lock (_effectsLock)
            {
                taken = new MediaPlayer[_activeEffects.Count];
                _activeEffects.CopyTo(taken);
                _activeEffects.Clear();
            }

            foreach (var player in taken)
            {
                player.MediaEnded -= OnEffectEnded;
                player.MediaFailed -= OnEffectFailed;
                SafeDispose(player);
            }

            DisposeRetired();
        }

        // Called once while the window is still alive, so no straggling
        // playback callback can touch anything afterwards.
        public void Shutdown()
        {
            lock (_effectsLock)
            {
                _shutdown = true;
            }

            StopAll();
            SafeDispose(_bgmPlayer);
        }

        private static void SafeDispose(MediaPlayer player)
        {
            try
            {
                player.Pause();
                player.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}
