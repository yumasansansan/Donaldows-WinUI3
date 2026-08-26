using System;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Audio;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Microsoft.Graphics.Canvas;
using Windows.Foundation;
using Microsoft.UI;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Shutdown
{
    // Ports *logoff: the screen fades to black over the original's ~150-frame
    // `await 1` loop, the login chime plays, and control returns to the
    // desktop (which replays the morning greeting on arrival, as in *desktop).
    public sealed class LogoffScene : IScene
    {
        private static readonly TimeSpan FadeOut = TimeSpan.FromMilliseconds(1500);
        private static readonly TimeSpan Total = TimeSpan.FromMilliseconds(1800);

        private SceneContext _context = null!;
        private TimeSpan _elapsed;
        private bool _loginSoundPlayed;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _elapsed = TimeSpan.Zero;
            _loginSoundPlayed = false;
            _context.Sound.PlayEffect(SoundId.Logoff);
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize)
        {
            DesktopBackdrop.Draw(session, _context);

            var fade = Math.Clamp((float)(_elapsed / FadeOut), 0f, 1f);
            session.FillRectangle(0, 0, 640, 480, Color.FromArgb((byte)(fade * 255), 0, 0, 0));
        }

        public SceneTransition? Update(TimeSpan delta)
        {
            _elapsed += delta;

            if (!_loginSoundPlayed && _elapsed >= FadeOut)
            {
                _loginSoundPlayed = true;
                _context.Sound.PlayEffect(SoundId.Login);
            }

            return _elapsed >= Total ? new SceneTransition(SceneId.IdleDesktop) : null;
        }
    }
}
