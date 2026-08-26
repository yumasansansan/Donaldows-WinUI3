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
    // Ports *virus/*v_wait/*v_roo/*ed/*cd — the "can't close me" prank hooked
    // onto the window's close button.
    //
    // The original idles in *virus with the window hidden (`gsel 0,-1`) and
    // only pops the nag up when the mouse reaches the bottom-left corner of
    // the whole SCREEN. This port's window is a fixed, ordinary bordered
    // window, so that trigger doesn't translate: the nag dialog (*ed) shows
    // straight away, and its CLOSE[X] — which in the original returns to that
    // hidden-window idle — minimizes instead.
    //
    // The joystick-polled ambient sound gags in *v_wait are not ported; they
    // need a joystick attached.
    public sealed class VirusNagScene : IScene
    {
        private enum Phase { Nag, CdGag }

        private static readonly Rect CloseButton = new(300, 2, 56, 20);
        private static readonly Rect YesButton = new(150, 150, 100, 20);
        private static readonly Rect DesktopButton = new(260, 150, 100, 20);
        private static readonly Rect KaomojiButton = new(10, 160, 100, 20);

        // *cd beats: red flash, black, the IT-Donald fullscreen zoom with three
        // overlapping cries, then a run of voice clips before returning.
        private static readonly (TimeSpan At, SoundId[] Sounds)[] CdBeats =
        {
            (TimeSpan.FromMilliseconds(1000), Array.Empty<SoundId>()),
            (TimeSpan.FromMilliseconds(1400), new[] { SoundId.Izen, SoundId.Rurou, SoundId.Odo }),
            (TimeSpan.FromMilliseconds(4400), new[] { SoundId.Donadayo }),
            (TimeSpan.FromMilliseconds(5700), new[] { SoundId.Donadayo }),
            (TimeSpan.FromMilliseconds(7000), new[] { SoundId.Donadayo }),
            (TimeSpan.FromMilliseconds(8300), new[] { SoundId.Donadayo }),
            (TimeSpan.FromMilliseconds(9600), new[] { SoundId.Donadayo }),
            (TimeSpan.FromMilliseconds(10900), new[] { SoundId.Donarudodes }),
            (TimeSpan.FromMilliseconds(12200), new[] { SoundId.Heha }),
            (TimeSpan.FromMilliseconds(13400), new[] { SoundId.Uresiina }),
            (TimeSpan.FromMilliseconds(14700), Array.Empty<SoundId>()),
        };

        private SceneContext _context = null!;
        private Phase _phase;
        private TimeSpan _cdElapsed;
        private int _cdBeat;
        private float _mouseX, _mouseY;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _phase = Phase.Nag;
            _context.Sound.StopAll();
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize)
        {
            if (_phase == Phase.CdGag)
            {
                DrawCdGag(session);
                return;
            }

            session.Clear(Color.FromArgb(255, 0, 20, 20));
            session.FillRectangle(20, 2, 278, 20, Color.FromArgb(255, 255, 128, 0));

            using var titleFormat = HspFont.Create();
            session.DrawText("ドナルドからのメッセージ", 40, 2, Color.FromArgb(255, 0, 20, 20), titleFormat);

            session.DrawImage(_context.Buffers.GetBitmap(BufferId.DonaFace), 30, 50);

            using var bodyFormat = HspFont.Create();
            session.DrawText("「また今度一緒に遊ぼうね！☆」", 130, 70, Colors.White, bodyFormat);

            DrawButton(session, CloseButton, "CLOSE[X]");
            DrawButton(session, YesButton, "もちろんさあ");
            DrawButton(session, DesktopButton, "ﾃﾞｽｸﾄｯﾌﾟ表示");
            DrawButton(session, KaomojiButton, "（ﾟ⊿ﾟ）は？");
        }

        private void DrawButton(CanvasDrawingSession session, Rect rect, string label)
        {
            session.FillRectangle(rect, Color.FromArgb(255, 200, 200, 200));
            using var format = new CanvasTextFormat { FontSize = 12, VerticalAlignment = CanvasVerticalAlignment.Center };
            session.DrawText(label, new Rect(rect.X + 3, rect.Y, rect.Width, rect.Height), Colors.Black, format);

            if (rect.Contains(new Point(_mouseX, _mouseY)))
            {
                session.FillRectangle(rect, Color.FromArgb(120, 0, 255, 0));
            }
        }

        private void DrawCdGag(CanvasDrawingSession session)
        {
            if (_cdBeat == 0)
            {
                session.Clear(Colors.Red);
                return;
            }

            session.Clear(Colors.Black);
            var it = _context.Buffers.GetBitmap(BufferId.ItDonald);
            session.DrawImage(it, new Rect(0, 0, 640, 480), it.Bounds);
        }

        public SceneTransition? Update(TimeSpan delta)
        {
            if (_phase != Phase.CdGag)
            {
                return null;
            }

            _cdElapsed += delta;
            if (_cdBeat >= CdBeats.Length || _cdElapsed < CdBeats[_cdBeat].At)
            {
                return null;
            }

            foreach (var sound in CdBeats[_cdBeat].Sounds)
            {
                _context.Sound.PlayEffect(sound);
            }

            _cdBeat++;

            if (_cdBeat >= CdBeats.Length)
            {
                _phase = Phase.Nag;
                _cdBeat = 0;
                _cdElapsed = TimeSpan.Zero;
            }

            return null;
        }

        public SceneTransition? OnPointerMoved(float x, float y)
        {
            _mouseX = x;
            _mouseY = y;
            return null;
        }

        public SceneTransition? OnPointerPressed(float x, float y)
        {
            if (_phase != Phase.Nag)
            {
                return null;
            }

            var point = new Point(x, y);

            if (CloseButton.Contains(point))
            {
                _context.MinimizeWindow();
                return null;
            }

            if (DesktopButton.Contains(point))
            {
                return new SceneTransition(SceneId.IdleDesktop);
            }

            if (YesButton.Contains(point))
            {
                return new SceneTransition(SceneId.Endona);
            }

            if (KaomojiButton.Contains(point))
            {
                _phase = Phase.CdGag;
                _cdBeat = 0;
                _cdElapsed = TimeSpan.Zero;
            }

            return null;
        }
    }
}
