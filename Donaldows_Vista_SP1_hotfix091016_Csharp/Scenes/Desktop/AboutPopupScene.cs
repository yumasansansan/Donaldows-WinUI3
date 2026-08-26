using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Microsoft.Graphics.Canvas;
using Microsoft.UI;
using Windows.Foundation;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Desktop
{
    // Ports *click (the taskbar clock hotspot)/*clock. The grow-in/shrink-out
    // window zoom animation isn't reproduced — the panel just appears/closes
    // instantly, consistent with how other popup dialogs in this port work.
    public sealed class AboutPopupScene : IScene
    {
        private static readonly Rect CloseButton = new(422, 122, 56, 18);

        private SceneContext _context = null!;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize)
        {
            DesktopBackdrop.Draw(session, _context);

            session.FillRectangle(180, 122, 220, 18, Color.FromArgb(255, 255, 100, 0));
            session.FillRectangle(422, 122, 56, 18, Color.FromArgb(255, 100, 100, 100));

            using var titleFormat = HspFont.Create();
            session.DrawText("About Donaldows V0.6", 180, 122, Color.FromArgb(255, 0, 20, 20), titleFormat);
            session.DrawText("CLOSE  [X]", 424, 122, Color.FromArgb(255, 0, 20, 20), titleFormat);

            session.DrawImage(_context.Buffers.GetBitmap(BufferId.DonaFrontFace), 380, 160);
            session.DrawImage(_context.Buffers.GetBitmap(BufferId.DonaFace), 200, 160);

            using var bodyFormat = HspFont.Create();
            // The original prints from pos 170,150 starting with a blank line,
            // and pads each line with leading spaces to centre it by hand.
            session.DrawText(
                "\n               Donaldows Vista sp1\n" +
                "                      (Version0.6)\n" +
                "                        作成：H,S\n\n\n" +
                "このドナルドウズはフリーウェアーです\n\n" +
                "・ドナルドに会いたくなったらいつでも\n" +
                "　会える夢のソフトです。",
                170, 150, Colors.White, bodyFormat);
        }

        public SceneTransition? OnPointerPressed(float x, float y)
        {
            if (CloseButton.Contains(new Point(x, y)))
            {
                return new SceneTransition(SceneId.IdleDesktop);
            }

            return null;
        }
    }
}
