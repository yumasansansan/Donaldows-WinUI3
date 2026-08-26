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
    // Ports *desktop/*bar/*de/*ham. The idle-frame counter (`c`, triggering
    // the screensaver at c=1000) was frame-rate-dependent in the original
    // (an uncapped loop) — ported here as a fixed real-time idle timeout
    // instead, reset on any pointer movement.
    public sealed class IdleDesktopScene : IScene
    {
        private const float StartButtonHotspotWidth = 20f;
        private const float TaskbarTop = 460f;
        private static readonly Rect ClockHotspot = new(474, 460, 166, 20);
        private static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(60);

        private SceneContext _context = null!;
        private TimeSpan _idleElapsed;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _idleElapsed = TimeSpan.Zero;
            _context.CloseIntercept = SceneId.VirusNag; // *de does `onexit goto *virus`

            // Ports *desktop's morning greeting, called out in the source's own
            // header comment as a feature of this build: between 5am and 11am
            // Donald says "やあ！おはよう！！" on arriving at the desktop.
            var hour = DateTime.Now.Hour;
            if (hour is > 4 and < 12)
            {
                _context.Sound.PlayEffect(SoundId.Welcome);
            }
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize)
        {
            session.DrawImage(_context.Buffers.GetBitmap(BufferId.DesktopBackground), new Rect(0, 0, 640, 480));

            session.FillRectangle(0, TaskbarTop, 640, 20, _context.Buffers.GetColor(BufferId.TaskbarBackdrop));
            session.DrawImage(_context.Buffers.GetBitmap(BufferId.TaskbarIcon), 0, TaskbarTop);

            using var startLabelFormat = new CanvasTextFormat { FontSize = 14 };
            session.DrawText("←スタートボタン", 20, TaskbarTop, Colors.White, startLabelFormat);

            using var clockFormat = new CanvasTextFormat { FontSize = 14, HorizontalAlignment = CanvasHorizontalAlignment.Right };
            var now = DateTime.Now;
            var dow = now.DayOfWeek.ToString().Substring(0, 3).ToUpperInvariant();
            var clockText = $"{now:yyyy/MM/dd}[{dow}]{now:HH:mm}";
            session.DrawText(clockText, new Rect(475, TaskbarTop + 1, 160, 18), Colors.DeepSkyBlue, clockFormat);
        }

        public SceneTransition? OnPointerPressed(float x, float y)
        {
            if (x < StartButtonHotspotWidth && y > TaskbarTop)
            {
                return new SceneTransition(SceneId.RooPopup);
            }

            if (ClockHotspot.Contains(new Point(x, y)))
            {
                return new SceneTransition(SceneId.AboutPopup);
            }

            _context.Sound.PlayEffect(SoundId.Kusy);
            return null;
        }

        public SceneTransition? OnPointerMoved(float x, float y)
        {
            _idleElapsed = TimeSpan.Zero;
            return null;
        }

        public SceneTransition? Update(TimeSpan delta)
        {
            _idleElapsed += delta;
            return _idleElapsed >= IdleTimeout ? new SceneTransition(SceneId.Screensaver) : null;
        }
    }
}
