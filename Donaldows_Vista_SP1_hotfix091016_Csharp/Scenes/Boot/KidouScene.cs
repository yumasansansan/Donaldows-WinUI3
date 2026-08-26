using System;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Audio;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Microsoft.Graphics.Canvas;
using Windows.Foundation;
using Microsoft.UI;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Boot
{
    // Ports *kidou, the post-install boot logo sequence.
    //
    // The original never clears between the blends — it repeatedly composites
    // the same picture at a rising alpha onto the persisted framebuffer, so the
    // image builds up to fully opaque even though no single pass exceeds ~40%.
    // Drawing one pass per frame here reproduces that; an earlier version drew
    // a single capped-alpha pass, which is why the logo stayed dim and then
    // appeared to darken.
    public sealed class KidouScene : IScene
    {
        private static readonly Rect FullScreen = new(0, 0, 640, 480);

        private static readonly TimeSpan BlackHold = TimeSpan.FromMilliseconds(1000);
        private static readonly TimeSpan NormalFade = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan Gap1 = TimeSpan.FromMilliseconds(100);
        private static readonly TimeSpan EyesFade = TimeSpan.FromMilliseconds(1000);
        private static readonly TimeSpan Gap2 = TimeSpan.FromMilliseconds(100);
        private static readonly TimeSpan BlackFade = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan PostHold = TimeSpan.FromMilliseconds(1000);
        private static readonly TimeSpan PreLogin = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan PostLogin = TimeSpan.FromMilliseconds(500);

        private static readonly TimeSpan NormalFadeAt = BlackHold;
        private static readonly TimeSpan EyesFadeAt = NormalFadeAt + NormalFade + Gap1;
        private static readonly TimeSpan BlackFadeAt = EyesFadeAt + EyesFade + Gap2;
        private static readonly TimeSpan ClearAt = BlackFadeAt + BlackFade + PostHold;
        private static readonly TimeSpan LoginAt = ClearAt + PreLogin;
        private static readonly TimeSpan DoneAt = LoginAt + PostLogin;

        private SceneContext _context = null!;
        private TimeSpan _elapsed;
        private bool _cleared;
        private bool _startPlayed;
        private bool _loginPlayed;
        private bool _finalCleared;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _elapsed = TimeSpan.Zero;
            _cleared = false;
            _startPlayed = false;
            _loginPlayed = false;
            _finalCleared = false;
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize)
        {
            // cls 4 once on entry; after that the framebuffer is left to build up.
            if (!_cleared)
            {
                session.Clear(Colors.Black);
                _cleared = true;
                return;
            }

            if (_elapsed >= ClearAt)
            {
                if (!_finalCleared)
                {
                    session.Clear(Colors.Black);
                    _finalCleared = true;
                }

                return;
            }

            // Each pass blends at roughly the alpha the original's loop counter
            // reaches at the same point, and the passes accumulate.
            if (_elapsed >= BlackFadeAt)
            {
                session.FillRectangle(FullScreen, Color.FromArgb(Alpha(_elapsed - BlackFadeAt, BlackFade), 0, 0, 0));
                return;
            }

            if (_elapsed >= EyesFadeAt)
            {
                var eyes = _context.Buffers.GetBitmap(BufferId.DonaldEyesShine);
                session.DrawImage(eyes, FullScreen, eyes.Bounds, Alpha(_elapsed - EyesFadeAt, EyesFade) / 255f);
                return;
            }

            if (_elapsed >= NormalFadeAt)
            {
                var normal = _context.Buffers.GetBitmap(BufferId.DonaldNormal);
                session.DrawImage(normal, FullScreen, normal.Bounds, Alpha(_elapsed - NormalFadeAt, NormalFade) / 255f);
            }
        }

        private static byte Alpha(TimeSpan into, TimeSpan over)
        {
            var t = Math.Clamp((float)(into / over), 0f, 1f);
            return (byte)Math.Clamp(t * 99f, 1f, 99f);
        }

        public SceneTransition? Update(TimeSpan delta)
        {
            _elapsed += delta;

            if (!_startPlayed && _elapsed >= NormalFadeAt)
            {
                _startPlayed = true;
                _context.Sound.PlayEffect(SoundId.Start);
            }

            if (!_loginPlayed && _elapsed >= LoginAt)
            {
                _loginPlayed = true;
                _context.Sound.PlayEffect(SoundId.Login);
            }

            return _elapsed >= DoneAt ? new SceneTransition(SceneId.IdleDesktop) : null;
        }
    }
}
