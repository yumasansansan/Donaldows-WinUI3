using System;
using System.Collections.Generic;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Audio;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Microsoft.Graphics.Canvas;
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


        private SceneContext _context = null!;
        private MessengerState _state = null!;
        private Phase _phase;

        private Beat[] _beats = Array.Empty<Beat>();
        private int _beatIndex;
        private TimeSpan _beatElapsed;
        private readonly List<(string Text, Color Color)> _log = new();
        private bool _imeHint;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _state = payload as MessengerState ?? new MessengerState();
            _phase = Phase.Idle;
            _log.Clear();
            _log.Add(("ドナルド「ドナルドの事好き？」", Colors.Black));
            _imeHint = false;
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize)
        {
            DesktopBackdrop.Draw(session, _context);
            MessengerChrome.DrawChatWindow(session, _context);
            MessengerChrome.DrawLog(session, _log);

            if (_phase == Phase.Idle)
            {
                session.FillRectangle(0, 400, 640, 80, Colors.White);
                using var promptFormat = HspFont.Create();
                session.DrawText(
                    "どうする？？\n1.「もちろんさあ～☆」とコメントをうつ\n2.「すまん、また今度話さねえ？」とコメントをうつ\n「１」キーか「２」キーを押してください。",
                    0, 400, Colors.Black, promptFormat);

                if (_imeHint)
                {
                    session.FillRectangle(60, 200, 520, 60, Color.FromArgb(255, 60, 60, 60));
                    session.DrawText("ドナルド「入力モードを半角英数字にしてやってみてね。」", 70, 220, Colors.White, promptFormat);
                }
            }
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
                _log.Add((_beats[_beatIndex].Text, _beats[_beatIndex].Color));
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
            if (MessengerChrome.LogoffBox.Contains(point))
            {
                return new SceneTransition(SceneId.MessengerOffline, _state);
            }

            if (MessengerChrome.CloseBox.Contains(point))
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

            // wparam 229 is VK_PROCESSKEY — the IME swallowed the key, so the
            // original tells the player to switch to half-width alphanumeric.
            if ((int)key == 229)
            {
                _imeHint = true;
                return null;
            }

            _imeHint = false;
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
                _log.Add((_beats[0].Text, _beats[0].Color));
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
                _log.Add((_beats[0].Text, _beats[0].Color));
                return null;
            }

            _context.Sound.PlayEffect(SoundId.Kusy);
            return null;
        }
    }
}
