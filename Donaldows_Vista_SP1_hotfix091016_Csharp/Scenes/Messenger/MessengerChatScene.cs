using System;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Audio;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI;
using Windows.Foundation;
using Windows.System;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Messenger
{
    // Ports *messelect/*meskey/*mes1/*mes2.
    public sealed class MessengerChatScene : IScene
    {
        private readonly record struct Beat(string Text, TimeSpan Wait, SoundId? Sound, Color Color);

        private enum Phase { Idle, Accept, Reject }

        private static readonly Rect LogoffBox = new(98, 300, 81, 36);
        private static readonly Rect CloseBox = new(496, 62, 54, 18);

        private SceneContext _context = null!;
        private MessengerState _state = null!;
        private Phase _phase;

        private Beat[] _beats = Array.Empty<Beat>();
        private int _beatIndex;
        private TimeSpan _beatElapsed;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _state = payload as MessengerState ?? new MessengerState();
            _phase = Phase.Idle;
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize)
        {
            session.Clear(Colors.Black);
            MessengerIntroScene.DrawChatWindowFrame(session, _context.Buffers);

            using var format = new CanvasTextFormat { FontSize = 14 };

            if (_phase == Phase.Idle)
            {
                session.DrawText("ドナルド「ドナルドの事好き？」", 200, 342, Colors.Black, format);

                session.FillRectangle(0, 400, 640, 80, Colors.White);
                using var promptFormat = new CanvasTextFormat { FontSize = 13 };
                session.DrawText(
                    "どうする？？\n1.「もちろんさあ～☆」とコメントをうつ\n2.「すまん、また今度話さねえ？」とコメントをうつ\n「１」キーか「２」キーを押してください。",
                    0, 400, Colors.Black, promptFormat);
                return;
            }

            var beat = _beats[Math.Min(_beatIndex, _beats.Length - 1)];
            session.DrawText(beat.Text, 200, 342, beat.Color, format);
        }

        public SceneTransition? Update(TimeSpan delta)
        {
            if (_phase == Phase.Idle)
            {
                return null;
            }

            _beatElapsed += delta;
            var current = _beats[_beatIndex];
            if (_beatElapsed < current.Wait)
            {
                return null;
            }

            _beatIndex++;
            _beatElapsed = TimeSpan.Zero;

            if (_beatIndex < _beats.Length)
            {
                if (_beats[_beatIndex].Sound is { } sound)
                {
                    _context.Sound.PlayEffect(sound);
                }

                return null;
            }

            if (_phase == Phase.Accept)
            {
                return new SceneTransition(SceneId.Kiss);
            }

            // Reject: original halves mesp and loops the whole scripted
            // dialogue again from *messtart.
            _state.MespMilliseconds /= 2;
            return new SceneTransition(SceneId.MessengerIntro, _state);
        }

        public SceneTransition? OnPointerPressed(float x, float y)
        {
            if (_phase != Phase.Idle)
            {
                return null;
            }

            var point = new Point(x, y);
            if (LogoffBox.Contains(point))
            {
                return new SceneTransition(SceneId.MessengerOffline, _state);
            }

            if (CloseBox.Contains(point))
            {
                return new SceneTransition(SceneId.MessengerCloseNag);
            }

            return null;
        }

        public SceneTransition? OnKeyDown(VirtualKey key)
        {
            if (_phase != Phase.Idle)
            {
                return null;
            }

            var name = string.IsNullOrEmpty(_context.Save.PlayerName) ? "君" : _context.Save.PlayerName;

            if (key is VirtualKey.Number1 or VirtualKey.NumberPad1)
            {
                _phase = Phase.Accept;
                _beatIndex = 0;
                _beatElapsed = TimeSpan.Zero;
                _beats = new[]
                {
                    new Beat($"{name}「もちろんさあ～☆」", TimeSpan.FromMilliseconds(3000), null, Colors.Black),
                    new Beat("ドナルド「ドナルドの事が大好きだなんて……」", TimeSpan.FromMilliseconds(3000), SoundId.Uresiina, Colors.Black),
                };
                return null;
            }

            if (key is VirtualKey.Number2 or VirtualKey.NumberPad2)
            {
                _phase = Phase.Reject;
                _beatIndex = 0;
                _beatElapsed = TimeSpan.Zero;
                _beats = new[]
                {
                    new Beat($"{name}「すまん、また今度話さねえ？」", TimeSpan.FromMilliseconds(3000), null, Colors.Black),
                    new Beat("ドナルド「もちろんさあ～☆今度一緒に汚話しよ\nうよ！」", TimeSpan.FromMilliseconds(5000), SoundId.Motikon, Colors.Black),
                    new Beat("ドナルドがログアウトしました。", TimeSpan.FromMilliseconds(3000), null, Color.FromArgb(255, 100, 100, 100)),
                };
                return null;
            }

            _context.Sound.PlayEffect(SoundId.Kusy);
            return null;
        }
    }
}
