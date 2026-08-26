using Microsoft.Graphics.Canvas;
using Microsoft.UI;
using Windows.Foundation;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Boot
{
    // Ports *setumei. The screen itself is a XAML overlay (it needs a real
    // IME-capable text box for the player's name), so this scene only clears
    // the canvas to black behind it — the original's `cls 4` — and asks the
    // window to reveal that overlay. The overlay's 起動 button drives the
    // transition onward into *power_sw.
    public sealed class SetumeiScene : IScene
    {
        private SceneContext _context = null!;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _context.ShowNameEntry();
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize) => session.Clear(Colors.Black);
    }
}
