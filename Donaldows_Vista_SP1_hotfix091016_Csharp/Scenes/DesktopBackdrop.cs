using System;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Microsoft.Graphics.Canvas;
using Microsoft.UI;
using Windows.Foundation;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes
{
    // Every screen reached from the desktop draws itself over the desktop
    // rather than over black: the original captures the composed desktop into
    // a scratch buffer at *bar (`gch=1 : gosub *gcop`) and restores it with
    // `pos 0,0 : gcopy 30,0,0,640,480` before drawing its own windows. This
    // reproduces that backdrop deterministically instead of capturing it.
    public static class DesktopBackdrop
    {
        public const float TaskbarTop = 460f;

        public static void Draw(CanvasDrawingSession session, SceneContext context)
        {
            session.Clear(Colors.Black);
            session.DrawImage(context.Buffers.GetBitmap(BufferId.DesktopBackground), new Rect(0, 0, 640, 480));
            session.FillRectangle(0, TaskbarTop, 640, 20, context.Buffers.GetColor(BufferId.TaskbarBackdrop));
            session.DrawImage(context.Buffers.GetBitmap(BufferId.TaskbarIcon), 0, TaskbarTop);

            using var format = HspFont.Create();
            session.DrawText("←スタートボタン", 20, TaskbarTop, Colors.White, format);

            var now = DateTime.Now;
            var dow = now.DayOfWeek.ToString()[..3].ToUpperInvariant();
            session.DrawText($"{now:yyyy/MM/dd}[{dow}]{now:HH:mm}", 475, TaskbarTop + 1, Colors.DeepSkyBlue, format);
        }
    }
}
