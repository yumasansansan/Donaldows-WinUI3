using System;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Audio;
using Microsoft.Graphics.Canvas;
using Windows.Foundation;
using Microsoft.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Messenger
{
    // Ports *offline. The original also does a gag where it teleports the OS
    // mouse cursor back to where it was ~200ms earlier over 100 frames while
    // holding the window in place — that's native cursor-position control this
    // port doesn't do elsewhere and isn't reproduced; the label toggle and
    // sound cues are kept.
    public sealed class MessengerOfflineScene : IScene
    {
        // wait 100 -> mmplay 25 -> wait 200 -> 100-frame slide -> mmplay 9.
        private static readonly TimeSpan MagicAt = TimeSpan.FromMilliseconds(1000);
        private static readonly TimeSpan ExitSoundAt = TimeSpan.FromMilliseconds(3000);
        private static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(3300);

        private SceneContext _context = null!;
        private MessengerState _state = null!;
        private TimeSpan _elapsed;
        private bool _exitSoundPlayed;
        private bool _magicPlayed;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _state = payload as MessengerState ?? new MessengerState();
            _elapsed = TimeSpan.Zero;
            _exitSoundPlayed = false;
            _magicPlayed = false;
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize)
        {
            DesktopBackdrop.Draw(session, _context);
            MessengerChrome.DrawChatWindow(session, _context, offlineLabel: true);
        }

        public SceneTransition? Update(TimeSpan delta)
        {
            _elapsed += delta;

            if (!_magicPlayed && _elapsed >= MagicAt)
            {
                _magicPlayed = true;
                _context.Sound.PlayEffect(SoundId.Magic);
            }

            if (!_exitSoundPlayed && _elapsed >= ExitSoundAt)
            {
                _exitSoundPlayed = true;
                _context.Sound.PlayEffect(SoundId.Fu);
            }

            return _elapsed >= Duration ? new SceneTransition(SceneId.MessengerChat, _state) : null;
        }
    }
}
