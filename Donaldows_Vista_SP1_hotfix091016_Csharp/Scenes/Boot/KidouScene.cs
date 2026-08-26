using System;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Audio;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Microsoft.Graphics.Canvas;
using Windows.Foundation;
using Microsoft.UI;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Boot
{
    // Ports *kidou, the post-install boot logo sequence: a quick fade up to
    // Donald, a slow fade to the eyes-shining variant, a quick fade to black,
    // then a hold before the login chime hands off to the desktop.
    //
    // The original's fades top out around 40% alpha (its loop counters only
    // reach 98/99 out of 255), which is why the logo never becomes fully
    // opaque; that ceiling is preserved here.
    public sealed class KidouScene : IScene
    {
        private const float AlphaCeiling = 99f / 255f;
        private static readonly Rect FullScreen = new(0, 0, 640, 480);

        private static readonly TimeSpan BlackHold = TimeSpan.FromMilliseconds(1000);
        private static readonly TimeSpan NormalFade = TimeSpan.FromMilliseconds(50);
        private static readonly TimeSpan Gap1 = TimeSpan.FromMilliseconds(100);
        private static readonly TimeSpan EyesFade = TimeSpan.FromMilliseconds(1000);
        private static readonly TimeSpan Gap2 = TimeSpan.FromMilliseconds(100);
        private static readonly TimeSpan BlackFade = TimeSpan.FromMilliseconds(50);
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
        private bool _startPlayed;
        private bool _loginPlayed;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _elapsed = TimeSpan.Zero;
            _startPlayed = false;
            _loginPlayed = false;
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize)
        {
            session.Clear(Colors.Black);

            if (_elapsed < NormalFadeAt || _elapsed >= ClearAt)
            {
                return;
            }

            var normal = _context.Buffers.GetBitmap(BufferId.DonaldNormal);
            var eyes = _context.Buffers.GetBitmap(BufferId.DonaldEyesShine);

            if (_elapsed < EyesFadeAt)
            {
                var t = Math.Clamp((float)((_elapsed - NormalFadeAt) / NormalFade), 0f, 1f);
                session.DrawImage(normal, FullScreen, normal.Bounds, t * AlphaCeiling);
                return;
            }

            session.DrawImage(normal, FullScreen, normal.Bounds, AlphaCeiling);

            if (_elapsed < BlackFadeAt)
            {
                var t = Math.Clamp((float)((_elapsed - EyesFadeAt) / EyesFade), 0f, 1f);
                session.DrawImage(eyes, FullScreen, eyes.Bounds, t * AlphaCeiling);
                return;
            }

            session.DrawImage(eyes, FullScreen, eyes.Bounds, AlphaCeiling);

            var fade = Math.Clamp((float)((_elapsed - BlackFadeAt) / BlackFade), 0f, 1f);
            session.FillRectangle(FullScreen, Color.FromArgb((byte)(fade * AlphaCeiling * 255f), 0, 0, 0));
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
