using System;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Audio;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI;
using Windows.Foundation;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Desktop
{
    // Ports *modoroo: the mascot slides back down out of view when the start
    // menu is dismissed without picking an item.
    public sealed class ModoRooScene : IScene
    {
        private static readonly TimeSpan Duration = TimeSpan.FromSeconds(0.6);

        private SceneContext _context = null!;
        private TimeSpan _elapsed;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _elapsed = TimeSpan.Zero;
            _context.Sound.PlayEffect(SoundId.Ur);
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize)
        {
            DesktopBackdrop.Draw(session, _context);

            var progress = Math.Clamp((float)(_elapsed / Duration), 0f, 1f);
            var y = 220f + progress * 240f; // slides from a=220 down to 460
            var height = Math.Max(0, 460f - y);
            if (height > 0)
            {
                session.DrawImage(_context.Buffers.GetBitmap(BufferId.MascotSprite), new Rect(0, y, 182, height), new Rect(0, 0, 182, height));
            }

            using var format = new CanvasTextFormat { FontSize = 14, HorizontalAlignment = CanvasHorizontalAlignment.Right };
            var now = DateTime.Now;
            var dow = now.DayOfWeek.ToString().Substring(0, 3).ToUpperInvariant();
            session.DrawText($"{now:yyyy/MM/dd}[{dow}]{now:HH:mm}", new Rect(475, 461, 160, 18), Colors.DeepSkyBlue, format);
        }

        public SceneTransition? Update(TimeSpan delta)
        {
            _elapsed += delta;
            return _elapsed >= Duration ? new SceneTransition(SceneId.IdleDesktop) : null;
        }
    }
}
