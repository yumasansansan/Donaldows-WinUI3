using System;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Audio;
using Microsoft.Graphics.Canvas;
using Windows.Foundation;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Shutdown
{
    // Ports *endona/*endona0: the real "confirmed quit" fade, shared by the
    // shutdown dialog's Yes buttons and the virus-nag dialog's もちろんさあ.
    //
    // The original leaves whatever is on screen alone for `wait 200` (two
    // seconds), then dims it to black by blending a black fill at 10/255 a
    // hundred times over — it never clears. Clearing here instead wiped the
    // frame to transparent, which is why the screen flashed white before going
    // black.
    public sealed class EndonaScene : IScene
    {
        private static readonly TimeSpan FadeStartsAt = TimeSpan.FromMilliseconds(2000);
        private static readonly TimeSpan FadeDuration = TimeSpan.FromMilliseconds(1000);
        private static readonly TimeSpan ExitAt = FadeStartsAt + FadeDuration;
        private const byte FadeStepAlpha = 10;

        private SceneContext _context = null!;
        private TimeSpan _elapsed;
        private bool _shutdownSoundPlayed;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _elapsed = TimeSpan.Zero;
            _shutdownSoundPlayed = false;
            _context.Sound.PlayEffect(SoundId.Uresiina);
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize)
        {
            // Before the fade begins the previous screen simply stays up, so
            // there is nothing to draw — the framebuffer already holds it.
            if (_elapsed < FadeStartsAt)
            {
                return;
            }

            session.FillRectangle(0, 0, 640, 480, Color.FromArgb(FadeStepAlpha, 0, 0, 0));
        }

        public SceneTransition? Update(TimeSpan delta)
        {
            _elapsed += delta;

            if (!_shutdownSoundPlayed && _elapsed >= FadeStartsAt)
            {
                _shutdownSoundPlayed = true;
                _context.Sound.PlayEffect(SoundId.Shutdown);
            }

            if (_elapsed >= ExitAt)
            {
                _context.RequestAppExit();
            }

            return null;
        }
    }
}
