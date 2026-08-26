using System;
using System.Collections.Generic;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Audio;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Microsoft.Graphics.Canvas;
using Microsoft.UI;
using Windows.Foundation;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Messenger
{
    // Ports *messenger (the fake UAC prompt) and *messtart (window setup plus
    // the scripted Donald dialogue spam), handing off to *messelect.
    //
    // The UAC gag is a swarm, not a shake: the original's 100-iteration jitter
    // loop redraws the dialog at a fresh random offset every frame WITHOUT
    // clearing, so copies pile up all over the screen. Each drawn copy is kept
    // here for the same effect.
    //
    // All of this is drawn over the captured desktop, not over black.
    public sealed class MessengerIntroScene : IScene
    {
        private readonly record struct Beat(string Text, TimeSpan Wait, SoundId? Sound);

        private enum Phase { Uac, Prologue, Dialogue }

        // *messenger: wait 100, draw, mmplay 50, wait 400, jitter loop, wait 100.
        private static readonly TimeSpan UacSoundAt = TimeSpan.FromMilliseconds(1000);
        private static readonly TimeSpan UacJitterAt = TimeSpan.FromMilliseconds(5000);
        private static readonly TimeSpan UacSettleAt = TimeSpan.FromMilliseconds(6500);
        private static readonly TimeSpan UacDoneAt = TimeSpan.FromMilliseconds(7500);
        private static readonly TimeSpan UacJitterInterval = TimeSpan.FromMilliseconds(15);

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

        private readonly List<(float X, float Y)> _uacSwarm = new();
        private TimeSpan _sinceJitter;

        private Beat[] _beats = Array.Empty<Beat>();
        private int _beatIndex;
        private TimeSpan _beatElapsed;
        private bool _cueSoundPlayed;
        private readonly List<(string Text, Color Color)> _log = new();

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _state = payload as MessengerState ?? new MessengerState();
            _phase = Phase.Uac;
            _phaseElapsed = TimeSpan.Zero;
            _cueSoundPlayed = false;
            _uacSwarm.Clear();
            _sinceJitter = TimeSpan.Zero;
            _log.Clear();

            _beats = BuildBeats(TimeSpan.FromMilliseconds(_state.MespMilliseconds));
            _beatIndex = 0;
            _beatElapsed = TimeSpan.Zero;
        }

        private static Beat[] BuildBeats(TimeSpan mesp)
        {
            const string spam = "ドナルド「汚話しようよ！！汚話しようよ！！」";

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
            DesktopBackdrop.Draw(session, _context);

            switch (_phase)
            {
                case Phase.Uac:
                    DrawUac(session);
                    break;
                case Phase.Prologue:
                    DrawPrologue(session);
                    break;
                case Phase.Dialogue:
                    MessengerChrome.DrawChatWindow(session, _context);
                    MessengerChrome.DrawLog(session, _log);
                    break;
            }
        }

        private void DrawUac(CanvasDrawingSession session)
        {
            foreach (var (x, y) in _uacSwarm)
            {
                DrawUacDialog(session, x, y);
            }

            if (_phaseElapsed < UacJitterAt)
            {
                DrawUacDialog(session, 0, 0);
            }
        }

        private void DrawUacDialog(CanvasDrawingSession session, float dx, float dy)
        {
            session.FillRectangle(180 + dx, 130 + dy, 300, 200, Color.FromArgb(255, 0, 20, 20));

            using var format = HspFont.Create();
            session.FillRectangle(200 + dx, 131 + dy, 220, 18, Color.FromArgb(255, 255, 50, 0));
            session.DrawText("ユーザーアカウント制御", 200 + dx, 131 + dy, Colors.White, format);
            session.DrawText("続行するにはあなたの許可が必要です", 190 + dx, 150 + dy, Colors.White, format);

            session.FillRectangle(190 + dx, 168 + dy, 280, 132, Colors.White);
            session.DrawText("あなたが開始した操作である場合は\n続行してください。", 190 + dx, 168 + dy, Colors.Black, format);
            session.DrawImage(_context.Buffers.GetBitmap(BufferId.DonaFace), 190 + dx, 210 + dy);
            session.DrawText("　　　ドナルドウズメッセンジャー(ver0.09\n\n　　　MicroosoftDonaldows", 190 + dx, 220 + dy, Colors.Black, format);

            session.FillRectangle(400 + dx, 302 + dy, 70, 18, Colors.White);
            session.DrawText("続　行", 410 + dx, 302 + dy, Colors.Black, format);
        }

        private void DrawPrologue(CanvasDrawingSession session)
        {
            if (_phaseElapsed >= ChatFrameAt)
            {
                MessengerChrome.DrawChatWindow(session, _context);
                return;
            }

            session.FillRectangle(182, 81, 256, 300, Color.FromArgb(255, 0, 20, 20));
            session.FillRectangle(200, 83, 220, 18, Color.FromArgb(255, 255, 50, 0));

            using var format = HspFont.Create();
            session.DrawText("ドナルドウズ　メッセンジャー(ver0.09", 200, 83, Colors.White, format);
            session.FillRectangle(188, 102, 244, 273, Colors.White);

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

        public SceneTransition? Update(TimeSpan delta)
        {
            _phaseElapsed += delta;

            switch (_phase)
            {
                case Phase.Uac:
                    return UpdateUac(delta);

                case Phase.Prologue:
                    if (_phaseElapsed >= OnlineAt && !_cueSoundPlayed)
                    {
                        _cueSoundPlayed = true;
                        _context.Sound.PlayEffect(SoundId.Online);
                    }

                    if (_phaseElapsed >= PrologueDoneAt)
                    {
                        _phase = Phase.Dialogue;
                        _phaseElapsed = TimeSpan.Zero;
                        _beatIndex = 0;
                        _beatElapsed = TimeSpan.Zero;
                        _log.Add((_beats[0].Text, Colors.Black));
                        _context.Sound.PlayEffect(_beats[0].Sound ?? SoundId.Type);
                    }

                    return null;

                default:
                    return UpdateDialogue(delta);
            }
        }

        private SceneTransition? UpdateUac(TimeSpan delta)
        {
            if (_phaseElapsed >= UacSoundAt && !_cueSoundPlayed)
            {
                _cueSoundPlayed = true;
                _context.Sound.PlayEffect(SoundId.Uac);
            }

            if (_phaseElapsed >= UacJitterAt && _phaseElapsed < UacSettleAt)
            {
                _sinceJitter += delta;
                if (_sinceJitter >= UacJitterInterval && _uacSwarm.Count < 100)
                {
                    _sinceJitter = TimeSpan.Zero;
                    _context.Sound.PlayEffect(SoundId.Uac);
                    _uacSwarm.Add((
                        Random.Shared.Next(0, 640) - Random.Shared.Next(0, 700),
                        Random.Shared.Next(0, 480) - Random.Shared.Next(0, 350)));
                }
            }

            if (_phaseElapsed >= UacDoneAt)
            {
                _phase = Phase.Prologue;
                _phaseElapsed = TimeSpan.Zero;
                _cueSoundPlayed = false;
            }

            return null;
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

            _log.Add((_beats[_beatIndex].Text, Colors.Black));
            if (_beats[_beatIndex].Sound is { } sound)
            {
                _context.Sound.PlayEffect(sound);
            }

            return null;
        }
    }
}
