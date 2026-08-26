using System;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Audio;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI;
using Windows.Foundation;
using Windows.System;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Boot
{
    // Ports *start's not-yet-installed path. The install confirmation is a gag,
    // not a real choice: the original arms onkey to jump to *install on ANY key,
    // but also falls through into *install on its own after a fixed wait even
    // with no input at all ("問答無用る～～☆" — no need to ask).
    //
    // Timings follow HSP's `wait` unit of 10ms (so the source's `wait 500`
    // before the 問答無用 line is five seconds, not half a second).
    public sealed class StartBootScene : IScene
    {
        private static readonly TimeSpan DiskTextAt = TimeSpan.FromMilliseconds(1000);
        private static readonly TimeSpan BarStartsAt = DiskTextAt + TimeSpan.FromMilliseconds(1000);
        private static readonly TimeSpan BarDuration = TimeSpan.FromMilliseconds(1500);
        private static readonly TimeSpan ConfirmAt = BarStartsAt + BarDuration + TimeSpan.FromMilliseconds(1000);
        private static readonly TimeSpan ForcedLineAt = ConfirmAt + TimeSpan.FromMilliseconds(5000);
        private static readonly TimeSpan AdvanceAt = ForcedLineAt + TimeSpan.FromMilliseconds(3000);

        private SceneContext _context = null!;
        private TimeSpan _elapsed;
        private bool _diskSoundPlayed;
        private bool _forcedLineShown;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _elapsed = TimeSpan.Zero;
            _diskSoundPlayed = false;
            _forcedLineShown = false;
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize)
        {
            if (_elapsed >= ConfirmAt)
            {
                DrawConfirm(session);
                return;
            }

            session.Clear(Colors.Black);
            using var format = new CanvasTextFormat { FontSize = 14 };

            if (_elapsed < BarStartsAt)
            {
                if (_elapsed >= DiskTextAt)
                {
                    session.DrawText("loading bootable disk...", 0, 0, Colors.White, format);
                }

                return;
            }

            session.DrawText("donaldows is loading files...", 200, 380, Colors.Yellow, format);

            var progress = Math.Clamp((float)((_elapsed - BarStartsAt) / BarDuration), 0f, 1f);
            session.FillRectangle(40, 420, 560, 20, Color.FromArgb(255, 128, 128, 0));
            session.FillRectangle(40, 420, progress * 560f, 20, Colors.Yellow);
        }

        private void DrawConfirm(CanvasDrawingSession session)
        {
            session.Clear(Colors.Black);
            session.DrawImage(_context.Buffers.GetBitmap(BufferId.InstallBackdrop), new Rect(0, 0, 640, 480));
            session.DrawImage(_context.Buffers.GetBitmap(BufferId.DonaFrontFace), 300, 60);

            session.FillRectangle(0, 400, 640, 80, Color.FromArgb(255, 70, 155, 255));

            var ink = Color.FromArgb(255, 20, 25, 25);
            using var format = new CanvasTextFormat { FontSize = 14 };
            session.DrawText("D O N A L D O W S へようこそ!", 20, 405, ink, format);
            session.DrawText("　ドナルドウズをインストーる～☆します。インストーる～☆しますか？", 20, 423, ink, format);
            session.DrawText("　　　　　　（[Y]es：もちろんさ　[N]o：もちろんさ）", 20, 441, ink, format);

            if (_forcedLineShown)
            {
                session.DrawText("ドナルド「問答無用る～～☆」", 20, 459, Colors.White, format);
            }
        }

        public SceneTransition? Update(TimeSpan delta)
        {
            _elapsed += delta;

            if (!_diskSoundPlayed && _elapsed >= BarStartsAt)
            {
                _diskSoundPlayed = true;
                _context.Sound.PlayEffect(SoundId.Cd);
            }

            if (!_forcedLineShown && _elapsed >= ForcedLineAt)
            {
                _forcedLineShown = true;
                _context.Sound.PlayEffect(SoundId.Uresii);
            }

            return _elapsed >= AdvanceAt ? new SceneTransition(SceneId.InstallWizard) : null;
        }

        public SceneTransition? OnKeyDown(VirtualKey key) => AdvanceIfConfirming();

        public SceneTransition? OnPointerPressed(float x, float y) => AdvanceIfConfirming();

        private SceneTransition? AdvanceIfConfirming() =>
            _elapsed >= ConfirmAt ? new SceneTransition(SceneId.InstallWizard) : null;
    }
}
