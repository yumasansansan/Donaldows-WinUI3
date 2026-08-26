using System;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Microsoft.Graphics.Canvas;
using Windows.Foundation;
using Windows.System;
using Microsoft.UI;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Desktop
{
    // Ports *scr/*scred: on a magenta field, the mascot zooms outward from a
    // random point, each frame drawn over the last so the growth leaves a
    // tunnel of concentric copies; when it has grown past the screen a new
    // random origin is picked and it starts over. Any input exits.
    //
    // An earlier version of this port had a bouncing-sprite screensaver, which
    // is not what the original does at all.
    //
    // The original opens a real full-desktop borderless window for this; this
    // port's main window is a fixed, ordinary bordered 640x480 window
    // throughout (a fidelity choice made early on), so it plays in-canvas.
    public sealed class ScreensaverScene : IScene
    {
        private const int StepsPerCycle = 100;
        private static readonly TimeSpan CycleDuration = TimeSpan.FromMilliseconds(2500);

        private SceneContext _context = null!;
        private TimeSpan _cycleElapsed;
        private float _originX, _originY;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _context.CloseIntercept = SceneId.IdleDesktop; // *scr does `onexit goto *scred`
            PickOrigin();
        }

        private void PickOrigin()
        {
            _cycleElapsed = TimeSpan.Zero;
            _originX = Random.Shared.Next(0, 640) - 40;
            _originY = Random.Shared.Next(0, 480) - 40;
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize)
        {
            session.Clear(Color.FromArgb(255, 255, 0, 255));

            var mascot = _context.Buffers.GetBitmap(BufferId.MascotSprite);
            var step = (int)(_cycleElapsed / CycleDuration * StepsPerCycle);

            // Redrawing every size up to the current one reproduces the
            // original's within-cycle accumulation (it only clears between cycles).
            for (var i = 1; i <= step; i++)
            {
                var size = i * 40f;
                session.DrawImage(mascot, new Rect(_originX - i * 20f, _originY - i * 20f, size, size), mascot.Bounds);
            }
        }

        public SceneTransition? Update(TimeSpan delta)
        {
            _cycleElapsed += delta;
            if (_cycleElapsed >= CycleDuration)
            {
                PickOrigin();
            }

            return null;
        }

        public SceneTransition? OnPointerMoved(float x, float y) => new SceneTransition(SceneId.IdleDesktop);

        public SceneTransition? OnPointerPressed(float x, float y) => new SceneTransition(SceneId.IdleDesktop);

        public SceneTransition? OnKeyDown(VirtualKey key) => new SceneTransition(SceneId.IdleDesktop);
    }
}
