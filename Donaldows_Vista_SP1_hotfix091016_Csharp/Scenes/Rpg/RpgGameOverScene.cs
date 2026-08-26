using System;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Audio;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Microsoft.Graphics.Canvas;
using Microsoft.UI;
using Windows.Foundation;
using Windows.System;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Rpg
{
    // Ports *rpg_gameoveroo. Any key or click skips straight to the BSOD,
    // matching the original's onclick/onkey goto *blue re-arm — which is how
    // this is meant to end, since the fallback `wait 4900` is 49 seconds
    // (HSP's wait unit is 10ms), i.e. effectively "hold until the player acts".
    public sealed class RpgGameOverScene : IScene
    {
        private static readonly TimeSpan AutoAdvance = TimeSpan.FromMilliseconds(49000);

        private SceneContext _context = null!;
        private TimeSpan _elapsed;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _elapsed = TimeSpan.Zero;
            _context.Sound.StopAll();
            _context.Sound.PlayEffect(SoundId.Gameover);
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize)
        {
            session.Clear(Colors.Black);
            var name = string.IsNullOrEmpty(_context.Save.PlayerName) ? "君" : _context.Save.PlayerName;

            using var format = HspFont.Create();
            session.DrawText(
                $"げ～むお～ば～る～☆\n\n{name}は洗脳されてドナルドにされてしまいました。\n\nドナルド「君がドナルドだなんて嬉しいなあ～ついやっちゃうんDA☆」",
                new Rect(0, 0, 640, 300), Color.FromArgb(255, 200, 200, 200), format);
        }

        public SceneTransition? Update(TimeSpan delta)
        {
            _elapsed += delta;
            return _elapsed >= AutoAdvance ? new SceneTransition(SceneId.Bsod) : null;
        }

        public SceneTransition? OnKeyDown(VirtualKey key) => new SceneTransition(SceneId.Bsod);

        public SceneTransition? OnPointerPressed(float x, float y) => new SceneTransition(SceneId.Bsod);
    }
}
