using System;
using System.Collections.Generic;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Audio;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI;
using Windows.Foundation;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Messenger
{
    // Ports *messenger (the fake UAC prompt) and *messtart (window setup plus
    // the scripted Donald dialogue spam), handing off to *messelect.
    //
    // Timings follow HSP's 10ms `wait` unit: the UAC dialog holds still for
    // four seconds before the glitch jitter starts, and the online/offline
    // and toast beats are seconds rather than the fractions an earlier
    // version of this port used.
    //
    // Not reproduced: the dialogue's scroll-accumulation (HSP's `mes` appends
    // into a scrolling text buffer; here each beat replaces the previous line).
    public sealed class MessengerIntroScene : IScene
    {
        private readonly record struct Beat(string Text, TimeSpan Wait, SoundId? Sound);

        private enum Phase { Uac, Prologue, Dialogue }

        // *messenger: wait 100, draw, mmplay 50, wait 400, jitter loop, wait 100.
        private static readonly TimeSpan UacSoundAt = TimeSpan.FromMilliseconds(1000);
        private static readonly TimeSpan UacJitterAt = TimeSpan.FromMilliseconds(5000);
        private static readonly TimeSpan UacSettleAt = TimeSpan.FromMilliseconds(6000);
        private static readonly TimeSpan UacDoneAt = TimeSpan.FromMilliseconds(7000);

        // *messtart: wait 300, mmplay 47 + toast, wait 400, wait 100, then the
        // chat frame is drawn and wait 300 precedes the first dialogue line.
        private static readonly TimeSpan OnlineAt = TimeSpan.FromMilliseconds(3000);
        private static readonly TimeSpan ToastGoneAt = TimeSpan.FromMilliseconds(7000);
        private static readonly TimeSpan ChatFrameAt = TimeSpan.FromMilliseconds(8000);
        private static readonly TimeSpan PrologueDoneAt = TimeSpan.FromMilliseconds(11000);

        private SceneContext _context = null!;
        private MessengerState _state = null!;
        private Phase _phase;
        private TimeSpan _phaseElapsed;

        private Beat[] _beats = Array.Empty<Beat>();
        private int _beatIndex;
        private TimeSpan _beatElapsed;
        private bool _onlineSoundPlayed;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _state = payload as MessengerState ?? new MessengerState();
            _phase = Phase.Uac;
            _phaseElapsed = TimeSpan.Zero;
            _onlineSoundPlayed = false;

            _beats = BuildBeats(TimeSpan.FromMilliseconds(_state.MespMilliseconds));
            _beatIndex = 0;
            _beatElapsed = TimeSpan.Zero;
        }

        private static Beat[] BuildBeats(TimeSpan mesp)
        {
            const string spam = "ドナルド「汚話しようよ！！汚話しようよ！！」";

            // The original's first line waits `wait 100` (one second); every
            // repeat after that waits `mesp`.
            var beats = new List<Beat>
            {
                new("ドナルド「汚話しようよ！！」", TimeSpan.FromMilliseconds(1000), SoundId.Type),
            };

            for (var i = 0; i < 10; i++)
            {
                beats.Add(new Beat(spam, mesp, SoundId.Type));
            }

            beats.Add(new Beat("ドナルド「汚話しようよ！！男話しようよ♂！」", mesp, SoundId.Type));

            for (var i = 0; i < 3; i++)
            {
                beats.Add(new Beat(spam, mesp, SoundId.Type));
            }

            for (var i = 0; i < 10; i++)
            {
                beats.Add(new Beat(spam, mesp, SoundId.Type));
            }

            beats.Add(new Beat("ドナルド「ドナルドの事好き？」", TimeSpan.Zero, SoundId.Type));

            return beats.ToArray();
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize)
        {
            switch (_phase)
            {
                case Phase.Uac:
                    DrawUac(session);
                    break;
                case Phase.Prologue:
                    DrawPrologue(session);
                    break;
                case Phase.Dialogue:
                    DrawDialogue(session);
                    break;
            }
        }

        private void DrawUac(CanvasDrawingSession session)
        {
            session.Clear(Colors.Black);

            var jittering = _phaseElapsed >= UacJitterAt && _phaseElapsed < UacSettleAt;
            var offsetX = jittering ? Random.Shared.Next(-40, 40) : 0;
            var offsetY = jittering ? Random.Shared.Next(-40, 40) : 0;

            session.FillRectangle(180 + offsetX, 130 + offsetY, 300, 200, Color.FromArgb(255, 0, 20, 20));

            using var titleFormat = new CanvasTextFormat { FontSize = 14 };
            session.FillRectangle(200 + offsetX, 131 + offsetY, 220, 18, Color.FromArgb(255, 255, 50, 0));
            session.DrawText("ユーザーアカウント制御", 200 + offsetX, 131 + offsetY, Colors.White, titleFormat);

            using var bodyFormat = new CanvasTextFormat { FontSize = 13 };
            session.DrawText("続行するにはあなたの許可が必要です", 190 + offsetX, 150 + offsetY, Colors.White, bodyFormat);

            session.FillRectangle(190 + offsetX, 168 + offsetY, 280, 132, Colors.White);
            session.DrawText("あなたが開始した操作である場合は\n続行してください。", 190 + offsetX, 168 + offsetY, Colors.Black, bodyFormat);
            session.DrawImage(_context.Buffers.GetBitmap(BufferId.DonaFace), 190 + offsetX, 210 + offsetY);
            session.DrawText("　　　ドナルドウズメッセンジャー(ver0.09\n\n　　　MicroosoftDonaldows", 190 + offsetX, 220 + offsetY, Colors.Black, bodyFormat);

            session.FillRectangle(400 + offsetX, 302 + offsetY, 70, 18, Colors.White);
            session.DrawText("続　行", 410 + offsetX, 302 + offsetY, Colors.Black, bodyFormat);
        }

        private void DrawPrologue(CanvasDrawingSession session)
        {
            session.Clear(Colors.Black);

            if (_phaseElapsed >= ChatFrameAt)
            {
                DrawChatWindowFrame(session, _context.Buffers);
                return;
            }

            // Contact-list window.
            session.FillRectangle(182, 81, 256, 300, Color.FromArgb(255, 0, 20, 20));
            session.FillRectangle(200, 83, 220, 18, Color.FromArgb(255, 255, 50, 0));
            using var titleFormat = new CanvasTextFormat { FontSize = 13 };
            session.DrawText("ドナルドウズ　メッセンジャー(ver0.09", 200, 84, Colors.White, titleFormat);
            session.FillRectangle(188, 102, 244, 273, Colors.White);

            using var format = new CanvasTextFormat { FontSize = 13 };
            if (_phaseElapsed < OnlineAt)
            {
                session.DrawText("オンライン(0)\nオフライン(4444)", 193, 107, Colors.Black, format);
                return;
            }

            session.DrawText("オンライン(1)\n ドナルド・マクドナルド\nオフライン(4443)", 193, 107, Colors.Black, format);

            if (_phaseElapsed < ToastGoneAt)
            {
                session.FillRectangle(440, 370, 200, 90, Color.FromArgb(255, 0, 20, 20));
                session.DrawImage(_context.Buffers.GetBitmap(BufferId.DonaFace), 445, 380);
                session.DrawText("ドナルドが\nオンラインに\nなりました。", 535, 390, Colors.White, format);
            }
        }

        internal static void DrawChatWindowFrame(CanvasDrawingSession session, BufferManager buffers, bool offlineLabel = false)
        {
            session.FillRectangle(80, 60, 480, 360, Color.FromArgb(255, 0, 20, 20));
            session.FillRectangle(98, 62, 360, 18, Color.FromArgb(255, 255, 50, 0));
            using var titleFormat = new CanvasTextFormat { FontSize = 13 };
            session.DrawText("ドナルドウズ　メッセンジャー 会話画面", 98, 63, Colors.White, titleFormat);

            session.FillRectangle(200, 90, 350, 320, Colors.White);
            session.FillRectangle(496, 62, 54, 18, Colors.White);
            session.FillRectangle(98, 300, 81, 36, Colors.White);

            using var format = new CanvasTextFormat { FontSize = 13 };
            session.DrawText(offlineLabel ? "ドナルドの\n発言を許可" : "ドナルドの\n発言を拒否", 100, 300, Colors.Black, format);
            session.DrawText("[X]CLOSE", 500, 62, Colors.Black, format);

            session.FillRectangle(201, 389, 348, 20, Colors.White);
            session.DrawText("ドナルドがメッセージを書いています...", 200, 390, Colors.DarkCyan, format);
            session.DrawImage(buffers.GetBitmap(BufferId.DonaFace), 90, 90);
        }

        private void DrawDialogue(CanvasDrawingSession session)
        {
            session.Clear(Colors.Black);
            DrawChatWindowFrame(session, _context.Buffers);

            using var format = new CanvasTextFormat { FontSize = 14 };
            var beat = _beats[Math.Min(_beatIndex, _beats.Length - 1)];
            session.DrawText(beat.Text, 200, 342, Colors.Black, format);
        }

        public SceneTransition? Update(TimeSpan delta)
        {
            _phaseElapsed += delta;

            switch (_phase)
            {
                case Phase.Uac:
                    if (_phaseElapsed >= UacSoundAt && !_onlineSoundPlayed)
                    {
                        _onlineSoundPlayed = true;
                        _context.Sound.PlayEffect(SoundId.Uac);
                    }

                    if (_phaseElapsed >= UacDoneAt)
                    {
                        _phase = Phase.Prologue;
                        _phaseElapsed = TimeSpan.Zero;
                        _onlineSoundPlayed = false;
                    }

                    return null;

                case Phase.Prologue:
                    if (_phaseElapsed >= OnlineAt && !_onlineSoundPlayed)
                    {
                        _onlineSoundPlayed = true;
                        _context.Sound.PlayEffect(SoundId.Online);
                    }

                    if (_phaseElapsed >= PrologueDoneAt)
                    {
                        _phase = Phase.Dialogue;
                        _phaseElapsed = TimeSpan.Zero;
                        _beatIndex = 0;
                        _beatElapsed = TimeSpan.Zero;
                        _context.Sound.PlayEffect(_beats[0].Sound ?? SoundId.Type);
                    }

                    return null;

                default:
                    return UpdateDialogue(delta);
            }
        }

        private SceneTransition? UpdateDialogue(TimeSpan delta)
        {
            _beatElapsed += delta;
            if (_beatElapsed < _beats[_beatIndex].Wait)
            {
                return null;
            }

            _beatIndex++;
            _beatElapsed = TimeSpan.Zero;

            if (_beatIndex >= _beats.Length)
            {
                return new SceneTransition(SceneId.MessengerChat, _state);
            }

            if (_beats[_beatIndex].Sound is { } sound)
            {
                _context.Sound.PlayEffect(sound);
            }

            return null;
        }
    }
}
