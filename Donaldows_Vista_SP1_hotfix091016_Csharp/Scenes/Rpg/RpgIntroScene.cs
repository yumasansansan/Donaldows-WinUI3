using System;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Audio;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Microsoft.Graphics.Canvas;
using Windows.Foundation;
using Microsoft.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Rpg
{
    // Ports *rpg's intro reveal (the punishment battle reached after typing
    // FORMAT). Resets AppState.Deldona, matching the original's `deldona=0`
    // at entry — the flag has done its job of routing here and shouldn't
    // still be set the next time *bios runs.
    public sealed class RpgIntroScene : IScene
    {
        // The original holds a black screen for `wait 300` (three seconds)
        // before the reveal even starts.
        private static readonly TimeSpan BlackHold = TimeSpan.FromMilliseconds(3000);
        private static readonly TimeSpan RevealDuration = TimeSpan.FromMilliseconds(1500);
        private static readonly TimeSpan DoneAt = BlackHold + RevealDuration;

        private SceneContext _context = null!;
        private TimeSpan _elapsed;
        private bool _revealSoundPlayed;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _elapsed = TimeSpan.Zero;
            _revealSoundPlayed = false;
            _context.AppState.Deldona = false;
            _context.CloseIntercept = SceneId.RpgIntro; // *rpg does `onexit goto *rpg`
            _context.Sound.StopAll();
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize)
        {
            session.Clear(Colors.Black);

            if (_elapsed < BlackHold)
            {
                return;
            }

            var progress = Math.Clamp((float)((_elapsed - BlackHold) / RevealDuration), 0f, 1f);
            var height = 270 * progress;
            var bitmap = _context.Buffers.GetBitmap(BufferId.MascotSprite);
            session.DrawImage(bitmap, new Rect(229, 300 - height, 182, height), new Rect(0, 0, 182, height));
        }

        public SceneTransition? Update(TimeSpan delta)
        {
            _elapsed += delta;

            if (!_revealSoundPlayed && _elapsed >= BlackHold)
            {
                _revealSoundPlayed = true;
                _context.Sound.PlayEffect(SoundId.U);
            }

            if (_elapsed < DoneAt)
            {
                return null;
            }

            _context.Sound.PlayEffect(SoundId.Donarudodes);
            _context.Sound.PlayBgm(SoundId.GameBgm2);
            return new SceneTransition(SceneId.RpgBattle, new RpgBattleState());
        }
    }
}
