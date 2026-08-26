using System;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Audio;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Microsoft.Graphics.Canvas;
using Microsoft.UI;
using Windows.Foundation;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Messenger
{
    // Ports *mesclose: clicking [X] doesn't close the messenger, it spirals
    // into an uninstall-nag swarm before crashing to *blue. The original
    // re-rolls two independent random offsets every single frame for ~50
    // "ticks"; this uses a fixed tick interval instead of raw frame count so
    // pacing doesn't depend on frame rate.
    public sealed class MessengerCloseNagScene : IScene
    {
        private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(110);
        private const int NotRespondingTick = 20;
        private const int EndTick = 50;

        private static readonly string[] TauntLines =
        {
            "ドナルド「何でそんなことすんの？」",
            "ドナルド「らんらんる～らんらんる～」",
            "ドナルド「洗脳してやル～☆」",
        };

        private SceneContext _context = null!;
        private TimeSpan _tickElapsed;
        private int _tick;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _tickElapsed = TimeSpan.Zero;
            _tick = 0;
            _context.Sound.PlayEffect(SoundId.Ran);
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize)
        {
            DesktopBackdrop.Draw(session, _context);

            var wx = Random.Shared.Next(-200, 200);
            var wy = Random.Shared.Next(-120, 120);
            var ix = Random.Shared.Next(-200, 200);
            var iy = Random.Shared.Next(-90, 90);

            session.FillRectangle(80 + wx, 60 + wy, 480, 360, Color.FromArgb(255, 0, 20, 20));
            session.FillRectangle(200 + wx, 90 + wy, 350, 320, Colors.White);
            session.DrawImage(_context.Buffers.GetBitmap(BufferId.DonaFace), 90 + wx, 90 + wy);

            using var format = HspFont.Create();
            var lineIndex = Math.Min(_tick / 6, TauntLines.Length - 1);
            session.DrawText(TauntLines[lineIndex], 200 + wx, 342 + wy, Colors.Black, format);

            session.FillRectangle(0 + ix, 0 + iy, 200, 90, Color.FromArgb(255, 0, 20, 20));
            session.DrawImage(_context.Buffers.GetBitmap(BufferId.DonaFace), 5 + ix, 10 + iy);
            session.DrawText("ドナルドが\nオンラインに\nなりました。", 95 + ix, 20 + iy, Colors.White, format);

            if (_tick >= NotRespondingTick)
            {
                session.FillRectangle(180, 130, 300, 200, Color.FromArgb(255, 0, 20, 20));
                session.FillRectangle(190, 150, 280, 150, Colors.White);
                session.DrawText(
                    "ドナルドウズメッセンジャー(ver0.09は\n応答していません。\n閉じるを押すと作業中のデータが\n全て失われます。",
                    190, 150, Colors.Black, format);
                session.FillRectangle(400, 302, 70, 18, Colors.White);
                session.DrawText("閉じる", 410, 302, Colors.Black, format);
            }
        }

        public SceneTransition? Update(TimeSpan delta)
        {
            _tickElapsed += delta;
            if (_tickElapsed < TickInterval)
            {
                return null;
            }

            _tickElapsed = TimeSpan.Zero;
            _tick++;

            return _tick >= EndTick ? new SceneTransition(SceneId.Bsod) : null;
        }
    }
}
