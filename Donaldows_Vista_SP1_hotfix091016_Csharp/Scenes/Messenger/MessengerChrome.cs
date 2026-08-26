using System.Collections.Generic;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Microsoft.Graphics.Canvas;
using Microsoft.UI;
using Windows.Foundation;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Messenger
{
    // Shared drawing for the messenger's chat window, plus the scrolling
    // message log.
    //
    // The original prints with `mes`, which appends a line and advances the
    // cursor, so messages stack downward from y=90; once they reach the bottom
    // of the chat area it scrolls by blitting the region 18px upward
    // (`gcopy 0,200,108,350,280`) and printing the new line at y=342. Keeping
    // a bounded list of the most recent lines and drawing them top-down is
    // equivalent.
    public static class MessengerChrome
    {
        public const float LogX = 200f;
        public const float LogTop = 90f;
        public const float LogLineHeight = 18f;
        public const int MaxVisibleLines = 14;

        public static readonly Rect LogoffBox = new(98, 300, 81, 36);
        public static readonly Rect CloseBox = new(496, 62, 54, 18);

        public static void DrawChatWindow(
            CanvasDrawingSession session,
            SceneContext context,
            bool offlineLabel = false,
            float dx = 0,
            float dy = 0)
        {
            session.FillRectangle(80 + dx, 60 + dy, 480, 360, Color.FromArgb(255, 0, 20, 20));
            session.FillRectangle(98 + dx, 62 + dy, 360, 18, Color.FromArgb(255, 255, 50, 0));

            using var format = HspFont.Create();
            session.DrawText("ドナルドウズ　メッセンジャー 会話画面", 98 + dx, 62 + dy, Colors.White, format);

            session.FillRectangle(200 + dx, 90 + dy, 350, 320, Colors.White);
            session.FillRectangle(496 + dx, 62 + dy, 54, 18, Colors.White);
            session.FillRectangle(98 + dx, 300 + dy, 81, 36, Colors.White);

            session.DrawText(offlineLabel ? "ドナルドの\n発言を許可" : "ドナルドの\n発言を拒否", 100 + dx, 300 + dy, Colors.Black, format);
            session.DrawText("[X]CLOSE", 500 + dx, 62 + dy, Colors.Black, format);

            session.FillRectangle(201 + dx, 389 + dy, 348, 20, Colors.White);
            session.DrawText("ドナルドがメッセージを書いています...", 200 + dx, 390 + dy, Colors.DarkCyan, format);
            session.DrawImage(context.Buffers.GetBitmap(BufferId.DonaFace), 90 + dx, 90 + dy);
        }

        public static void DrawLog(CanvasDrawingSession session, IReadOnlyList<(string Text, Color Color)> lines)
        {
            using var format = HspFont.Create(16f);
            var start = lines.Count > MaxVisibleLines ? lines.Count - MaxVisibleLines : 0;

            for (var i = start; i < lines.Count; i++)
            {
                var y = LogTop + (i - start) * LogLineHeight;
                session.DrawText(lines[i].Text, LogX, y, lines[i].Color, format);
            }
        }
    }
}
