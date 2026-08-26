using System;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Audio;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI;
using Windows.Foundation;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Shutdown
{
    // Ports *shutdown. Besides the three labelled buttons there is a fourth,
    // unlabelled hotspot to the right of 「まだ遊んでる」: hovering it starts a
    // 「　　は!?」 button that runs away upward and off the screen and can never
    // be clicked. Once it starts moving it keeps going on its own.
    //
    // The entrance is the original's `repeat 50`: the whole screen dims a
    // little each frame while a dark rectangle shrinks in toward the centre,
    // and past frame 24 a teal box jitters in the middle.
    public sealed class ShutdownDialogScene : IScene
    {
        // 50 frames of `await 1`, which is 10ms per unit.
        private static readonly TimeSpan EntranceDuration = TimeSpan.FromMilliseconds(500);
        private const int EntranceFrames = 50;

        private static readonly Rect YesButton1 = new(200, 280, 100, 20);
        private static readonly Rect YesButton2 = new(340, 280, 100, 20);
        private static readonly Rect CancelButton = new(200, 320, 100, 20);
        private static readonly Rect RunawayHotspot = new(340, 320, 100, 20);

        private const int RunawayMaxSteps = 35;
        private const float RunawayStepsPerSecond = 60f;

        private SceneContext _context = null!;
        private TimeSpan _entranceElapsed;
        private float _runawayProgress;
        private bool _runawayStarted;
        private bool _runawayThudPlayed;
        private float _mouseX, _mouseY;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _entranceElapsed = TimeSpan.Zero;
            _runawayProgress = 0f;
            _runawayStarted = false;
            _runawayThudPlayed = false;
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize)
        {
            DesktopBackdrop.Draw(session, _context);

            if (_entranceElapsed < EntranceDuration)
            {
                DrawEntrance(session);
                return;
            }

            session.FillRectangle(150, 100, 340, 260, Color.FromArgb(255, 30, 30, 30));
            session.FillRectangle(180, 122, 280, 18, _context.Buffers.GetColor(BufferId.Orange));

            using var titleFormat = HspFont.Create();
            session.DrawText("ドナルドからのメッセージ", 190, 124, Colors.White, titleFormat);

            session.DrawImage(_context.Buffers.GetBitmap(BufferId.DonaFace), 200, 170);

            using var messageFormat = HspFont.Create();
            session.DrawText("また今度一緒に\n遊ぼうね！！☆", new Rect(320, 190, 160, 60), Colors.White, messageFormat);

            using var buttonFormat = new CanvasTextFormat { FontSize = 12, HorizontalAlignment = CanvasHorizontalAlignment.Center, VerticalAlignment = CanvasVerticalAlignment.Center };
            DrawButton(session, YesButton1, "もちろんさあ", buttonFormat);
            DrawButton(session, YesButton2, "もちろんさあ", buttonFormat);
            DrawButton(session, CancelButton, "まだ遊んでる", buttonFormat);

            var runawayRect = RunawayRect();
            session.FillRectangle(runawayRect, Color.FromArgb(255, 200, 200, 200));
            session.DrawText("　　は!?", runawayRect, Colors.Black, buttonFormat);
        }

        private void DrawEntrance(CanvasDrawingSession session)
        {
            var frame = (float)(_entranceElapsed / EntranceDuration) * EntranceFrames;

            // The overall dim accumulates 3/255 per frame in the original.
            var overall = 1f - MathF.Pow(1f - 3f / 255f, frame);
            session.FillRectangle(0, 0, 640, 480, Color.FromArgb((byte)(overall * 255), 0, 0, 0));

            // A dark rectangle closing in on the centre, 10/255 per frame.
            var edge = 1f - MathF.Pow(1f - 10f / 255f, frame);
            session.FillRectangle(
                frame * 6.4f,
                frame * 4.8f,
                Math.Max(0f, 640f - frame * 12.8f),
                Math.Max(0f, 480f - frame * 9.6f),
                Color.FromArgb((byte)(edge * 255), 0, 0, 0));

            if (frame > 24)
            {
                var teal = _context.Buffers.GetColor(BufferId.MenuHoverHighlight);
                for (var i = 0; i < 3; i++)
                {
                    session.FillRectangle(
                        160 + Random.Shared.Next(0, 3),
                        120 + Random.Shared.Next(0, 3),
                        320, 240,
                        Color.FromArgb(3, teal.R, teal.G, teal.B));
                }
            }
        }

        private Rect RunawayRect() => new(340, 320 - _runawayProgress * 10, 100, 20);

        private void DrawButton(CanvasDrawingSession session, Rect rect, string label, CanvasTextFormat format)
        {
            session.FillRectangle(rect, Color.FromArgb(255, 200, 200, 200));
            session.DrawText(label, rect, Colors.Black, format);

            if (rect.Contains(new Point(_mouseX, _mouseY)))
            {
                session.FillRectangle(rect, Color.FromArgb(120, 0, 255, 0));
            }
        }

        public SceneTransition? Update(TimeSpan delta)
        {
            if (_entranceElapsed < EntranceDuration)
            {
                _entranceElapsed += delta;
                return null;
            }

            var hoveringRunaway = RunawayHotspot.Contains(new Point(_mouseX, _mouseY));
            if (!_runawayStarted && hoveringRunaway)
            {
                _runawayStarted = true;
                _context.Sound.PlayEffect(SoundId.Fii);
            }

            if (_runawayStarted && _runawayProgress < RunawayMaxSteps)
            {
                _runawayProgress = Math.Min(RunawayMaxSteps, _runawayProgress + RunawayStepsPerSecond * (float)delta.TotalSeconds);

                if (!_runawayThudPlayed && _runawayProgress >= 34f)
                {
                    _runawayThudPlayed = true;
                    _context.Sound.PlayEffect(SoundId.Heha);
                }
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
            if (_entranceElapsed < EntranceDuration)
            {
                return null;
            }

            var point = new Point(x, y);

            if (YesButton1.Contains(point) || YesButton2.Contains(point))
            {
                return new SceneTransition(SceneId.Endona);
            }

            if (CancelButton.Contains(point))
            {
                return new SceneTransition(SceneId.StartMenu);
            }

            return null;
        }
    }
}
