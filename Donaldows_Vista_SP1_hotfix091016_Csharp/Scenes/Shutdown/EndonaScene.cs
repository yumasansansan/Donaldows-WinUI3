using System;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Audio;
using Microsoft.Graphics.Canvas;
using Windows.Foundation;
using Microsoft.UI;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Shutdown
{
    // Ports *endona/*endona0: the real "confirmed quit" fade shared by both
    // the shutdown dialog's Yes buttons and the virus-nag dialog's
    // "もちろんさあ" button. The original waits `wait 200` (two seconds, since
    // HSP's wait unit is 10ms) between the two voice clips, then fades to
    // black over `repeat 100` of `wait 1` (one more second).
    public sealed class EndonaScene : IScene
    {
        private static readonly TimeSpan ShutdownSoundAt = TimeSpan.FromMilliseconds(2000);
        private static readonly TimeSpan FadeDuration = TimeSpan.FromMilliseconds(1000);
        private static readonly TimeSpan ExitAt = ShutdownSoundAt + FadeDuration;

        private SceneContext _context = null!;
        private TimeSpan _elapsed;
        private bool _secondSoundPlayed;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _elapsed = TimeSpan.Zero;
            _secondSoundPlayed = false;
            _context.Sound.PlayEffect(SoundId.Uresiina);
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize)
        {
            var fade = _elapsed <= ShutdownSoundAt
                ? 0f
                : Math.Clamp((float)((_elapsed - ShutdownSoundAt) / FadeDuration), 0f, 1f);
            session.Clear(Color.FromArgb((byte)(fade * 255), 0, 0, 0));
        }

        public SceneTransition? Update(TimeSpan delta)
        {
            _elapsed += delta;

            if (!_secondSoundPlayed && _elapsed >= ShutdownSoundAt)
            {
                _secondSoundPlayed = true;
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
