using System;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Audio;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Microsoft.Graphics.Canvas;
using Windows.Foundation;
using Microsoft.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Boot
{
    // Ports *blue. Every crash/game-over/nag-timeout flow in the original
    // funnels here before looping back into *bios.
    public sealed class BsodScene : IScene
    {
        private static readonly TimeSpan SecondSoundAt = TimeSpan.FromMilliseconds(900);
        private static readonly TimeSpan RebootAt = TimeSpan.FromMilliseconds(900 + 3250);

        private SceneContext _context = null!;
        private TimeSpan _elapsed;
        private bool _secondSoundPlayed;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _elapsed = TimeSpan.Zero;
            _secondSoundPlayed = false;
            _context.Sound.StopAll();
            _context.Sound.PlayEffect(SoundId.Dori);
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize)
        {
            session.Clear(Colors.Black);
            session.DrawImage(_context.Buffers.GetBitmap(BufferId.BsodImage), 0, 0);
        }

        public SceneTransition? Update(TimeSpan delta)
        {
            _elapsed += delta;

            if (!_secondSoundPlayed && _elapsed >= SecondSoundAt)
            {
                _secondSoundPlayed = true;
                _context.Sound.PlayEffect(SoundId.Yattyau);
            }

            return _elapsed >= RebootAt ? new SceneTransition(SceneId.BiosPost) : null;
        }
    }
}
