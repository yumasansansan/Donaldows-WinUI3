using System;
using System.Collections.Generic;
using System.Text;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Audio;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Microsoft.Graphics.Canvas;
using Microsoft.UI;
using Windows.Foundation;
using Windows.System;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Cmd
{
    // Ports *cmdp/*cmdhelp/*cmd/*cmdd/*cmdst/*cmdtype/*chantei and the FORMAT
    // branch's warning/confirm/format sequence in full. The original
    // recognizes typed commands via a rolling XOR/multiply hash of raw
    // per-key virtual-key codes compared against precomputed magic constants;
    // that scheme's exact key-code semantics couldn't be reproduced with
    // confidence from the source alone (its own hyphen-key mapping doesn't
    // match standard Win32 VK codes), so this instead matches the fully-typed
    // word directly against the known command strings — behaviorally
    // equivalent for real input, and arguably more robust than a hash that
    // could in principle collide.
    //
    public sealed class CmdPromptScene : IScene
    {
        private enum Phase { Help, Typing, FormatWarning, Formatting, Ranranroo }

        private const string HelpText =
            "ドナルドプロンプトへようこそ！\n" +
            "好きなコマンドを入力しよう コマンド入力したらENTERキーだ\n" +
            "ドナルドプロンプトをやめたいときは EXIT と入力\n" +
            "もう一度この文章を見たければ HELP\n\n" +
            "使えるコマンド一覧\n\n" +
            "EXIT (デスクトップに戻る)\n" +
            "RANRANROO (？？？)\n" +
            "GAME（ドナルドウズゲーム)\n" +
            "NOTEPAD (メモ帳)\n" +
            "FORMAT (ドナルドウズを完全消去)\n" +
            "REBOOT (再起動)\n\n" +
            "早速コマンドを入力しよう！どれかキーを押すとコマンド入力画面に切り替わります。";

        private const string FormatWarningText =
            "！！ＷＡＲＮＩＮＧ！！\n\n\n" +
            "このコマンドはドナルドウズ自体を完全消去するプログラムです\n" +
            "つまりあなたはドナルドと絶交するようなことを今からしようとしています\n" +
            "これはドナルドに我慢できなくなった人用に作られた最終兵器です\n" +
            "フォーマット中にドナルドが乱入できなくなっているので安全です。\n" +
            "これからドナルドウズのパーティションに乱数を書き込み、物理フォーマットをします。\n\n" +
            "さあ！ドナルドとおさらばして通常の世界に帰りましょう！！\n\n" +
            "フォーマットをしますか？（Y:はい N:いいえ）";

        private SceneContext _context = null!;
        private Phase _phase;
        private readonly StringBuilder _typed = new();
        private string _message = "";

        private int _confirmCount;

        private TimeSpan _formatElapsed;
        private int _formatPercent;
        private bool _formatComplete;
        private TimeSpan _formatCompleteElapsed;

        // *forstart: "逝ってみよう！！" (wait 300), the header (wait 300), the
        // bar (100 steps of `wait rnd(12)` plus a one-second stall at 99%),
        // then the reboot notice (wait 50 + wait 300 + wait 100).
        private static readonly TimeSpan FormatGoAt = TimeSpan.FromMilliseconds(3000);
        private static readonly TimeSpan FormatHeaderAt = FormatGoAt + TimeSpan.FromMilliseconds(3000);
        private static readonly TimeSpan FormatBarDuration = TimeSpan.FromMilliseconds(5500);
        private static readonly TimeSpan FormatStallAt99 = TimeSpan.FromMilliseconds(1000);
        private static readonly TimeSpan FormatCompleteHold = TimeSpan.FromMilliseconds(500 + 3000 + 1000);

        // *chantei RANRANROO: eight columns rise (mmplay 13 each), wait 300,
        // 100 scattered faces with mmplay 9, wait 200, mmplay 7, wait 500.
        private static readonly TimeSpan RanranrooRiseDuration = TimeSpan.FromSeconds(1.6);
        private static readonly TimeSpan RanranrooHoldAfterRise = TimeSpan.FromMilliseconds(3000);
        private static readonly TimeSpan RanranrooScatterDuration = TimeSpan.FromMilliseconds(1000);
        private static readonly TimeSpan RanranrooTailDuration = TimeSpan.FromMilliseconds(2000 + 5000);
        private TimeSpan _ranranrooElapsed;
        private readonly List<(float X, float Y)> _ranranrooStamps = new();
        private TimeSpan _sinceLastStamp;
        private bool _ranranrooTailSoundPlayed;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            // The start menu's "ドナルドプロンプト" item jumps straight to
            // *cmd in the original — *cmdhelp (the intro/help text) is only
            // reachable by typing the HELP command, not on first entry.
            _phase = Phase.Typing;
            _typed.Clear();
            _message = "";
            _context.Sound.PlayEffect(SoundId.Jyan);
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize)
        {
            switch (_phase)
            {
                case Phase.Help:
                    session.Clear(Colors.Black);
                    DrawWrapped(session, HelpText, Colors.White);
                    break;

                case Phase.Typing:
                    session.Clear(Colors.Black);
                    using (var format = HspFont.Create())
                    {
                        session.DrawText(
                            "使えるコマンドを知りたい時はHELPと入力、終わるときはEXITと入力\nコマンドを入力したらＥＮＴＥＲキーを押そう。",
                            0, 0, Colors.Cyan, format);
                        session.DrawText("コマンドを入力しよう#" + _typed, 0, 36, Colors.Cyan, format);
                        if (_message.Length > 0)
                        {
                            session.DrawText(_message, 0, 55, Colors.Red, format);
                        }
                    }

                    break;

                case Phase.FormatWarning:
                    session.Clear(Color.FromArgb(255, 200, 0, 0));
                    DrawWrapped(session, FormatWarningText, Colors.White);
                    if (_message.Length > 0)
                    {
                        using var format = HspFont.Create();
                        session.DrawText(_message, 0, 300, Colors.Yellow, format);
                    }

                    break;

                case Phase.Formatting:
                    using (var format = HspFont.Create())
                    {
                        if (_formatElapsed < FormatGoAt)
                        {
                            session.Clear(Color.FromArgb(255, 200, 0, 0));
                            session.DrawText("逝ってみよう！！", 0, 0, Colors.White, format);
                            break;
                        }

                        session.Clear(Color.FromArgb(255, 0, 0, 128));
                        session.DrawText("物理フォーマット中", 0, 0, Colors.White, format);

                        if (_formatElapsed < FormatHeaderAt)
                        {
                            break;
                        }

                        if (!_formatComplete)
                        {
                            session.FillRectangle(0, 420, 10 + _formatPercent * 6, 20, Colors.White);
                            session.DrawText($"{_formatPercent}%", 609, 421, Colors.White, format);
                        }
                        else
                        {
                            session.FillRectangle(0, 420, 10 + 100 * 6, 20, Colors.White);
                            session.DrawText("フォーマットが完了したので再起動します。", 0, 440, Colors.White, format);
                        }
                    }

                    break;

                case Phase.Ranranroo:
                    DrawRanranroo(session);
                    break;
            }
        }

        private void DrawRanranroo(CanvasDrawingSession session)
        {
            session.Clear(Colors.Black);
            var mascot = _context.Buffers.GetBitmap(BufferId.MascotSprite);

            if (_ranranrooElapsed < RanranrooRiseDuration + RanranrooHoldAfterRise)
            {
                // Eight columns: the first four rise in 40px steps, the rest in
                // 20px steps, matching the original's `a<4` split.
                var progress = Math.Clamp((float)(_ranranrooElapsed / RanranrooRiseDuration), 0f, 1f);
                for (var a = 0; a < 8; a++)
                {
                    var columnProgress = Math.Clamp(progress * 8f - a, 0f, 1f);
                    if (columnProgress <= 0f)
                    {
                        continue;
                    }

                    var step = a < 4 ? 40f : 20f;
                    var x = (a < 4 ? a : a - 4) * 160f;
                    var y = 480f - columnProgress * 13f * step;
                    session.DrawImage(mascot, x, y);
                }

                return;
            }

            foreach (var (x, y) in _ranranrooStamps)
            {
                session.DrawImage(_context.Buffers.GetBitmap(BufferId.DonaFace), x, y);
            }
        }

        private static void DrawWrapped(CanvasDrawingSession session, string text, Color color)
        {
            using var format = HspFont.Create();
            session.DrawText(text, new Rect(0, 0, 640, 480), color, format);
        }

        public SceneTransition? Update(TimeSpan delta)
        {
            if (_phase == Phase.Ranranroo)
            {
                return UpdateRanranroo(delta);
            }

            if (_phase != Phase.Formatting)
            {
                return null;
            }

            if (_formatComplete)
            {
                _formatCompleteElapsed += delta;
                if (_formatCompleteElapsed >= FormatCompleteHold)
                {
                    _context.AppState.Deldona = true;
                    return new SceneTransition(SceneId.BiosPost);
                }

                return null;
            }

            _formatElapsed += delta;
            if (_formatElapsed < FormatHeaderAt)
            {
                return null;
            }

            var barElapsed = _formatElapsed - FormatHeaderAt;
            var target = Math.Min(99, (int)(barElapsed / FormatBarDuration * 100));
            if (target > _formatPercent)
            {
                _formatPercent = target;
            }

            // The original stalls a full second on 99% before finishing.
            if (_formatPercent >= 99 && barElapsed >= FormatBarDuration + FormatStallAt99)
            {
                _formatPercent = 100;
                _formatComplete = true;
                _formatCompleteElapsed = TimeSpan.Zero;
            }

            return null;
        }

        private SceneTransition? UpdateRanranroo(TimeSpan delta)
        {
            _ranranrooElapsed += delta;

            var scatterStart = RanranrooRiseDuration + RanranrooHoldAfterRise;
            if (_ranranrooElapsed < scatterStart)
            {
                return null;
            }

            var scatterElapsed = _ranranrooElapsed - scatterStart;

            if (scatterElapsed >= RanranrooScatterDuration + RanranrooTailDuration)
            {
                _phase = Phase.Typing;
                _typed.Clear();
                _message = "";
                return null;
            }

            if (scatterElapsed < RanranrooScatterDuration)
            {
                _sinceLastStamp += delta;
                if (_sinceLastStamp >= TimeSpan.FromMilliseconds(10) && _ranranrooStamps.Count < 100)
                {
                    _sinceLastStamp = TimeSpan.Zero;
                    _context.Sound.PlayEffect(SoundId.Fu);
                    _ranranrooStamps.Add((Random.Shared.Next(0, 620), Random.Shared.Next(0, 460)));
                }

                return null;
            }

            // wait 200 -> mmplay 7 -> wait 500 -> back to the prompt.
            if (!_ranranrooTailSoundPlayed && scatterElapsed >= RanranrooScatterDuration + TimeSpan.FromMilliseconds(2000))
            {
                _ranranrooTailSoundPlayed = true;
                _context.Sound.PlayEffect(SoundId.Uresii);
            }

            return null;
        }

        public SceneTransition? OnKeyDown(VirtualKey key)
        {
            switch (_phase)
            {
                case Phase.Help:
                    _phase = Phase.Typing;
                    _typed.Clear();
                    _message = "";
                    return null;

                case Phase.Typing:
                    return HandleTypingKey(key);

                case Phase.FormatWarning:
                    return HandleFormatWarningKey(key);

                default:
                    return null;
            }
        }

        private SceneTransition? HandleTypingKey(VirtualKey key)
        {
            if (key == VirtualKey.Back)
            {
                _typed.Clear();
                _message = "";
                _context.Sound.PlayEffect(SoundId.Kusy);
                return null;
            }

            if (key == VirtualKey.Enter)
            {
                _context.Sound.PlayEffect(SoundId.Jyan);
                return Dispatch(_typed.ToString());
            }

            var ch = key switch
            {
                >= VirtualKey.A and <= VirtualKey.Z => (char)('A' + (key - VirtualKey.A)),
                >= VirtualKey.Number0 and <= VirtualKey.Number9 => (char)('0' + (key - VirtualKey.Number0)),
                _ => '\0',
            };

            if (ch != '\0')
            {
                _typed.Append(ch);
                _message = "";
            }

            return null;
        }

        private SceneTransition? Dispatch(string command)
        {
            switch (command)
            {
                case "EXIT":
                    return new SceneTransition(SceneId.IdleDesktop);
                case "GAME":
                    return new SceneTransition(SceneId.DodgeGame);
                case "REBOOT":
                    return new SceneTransition(SceneId.BiosPost);
                case "HELP":
                    _phase = Phase.Help;
                    return null;
                case "NOTEPAD":
                    return new SceneTransition(SceneId.Notepad);
                case "RANRANROO":
                    _phase = Phase.Ranranroo;
                    _ranranrooElapsed = TimeSpan.Zero;
                    _sinceLastStamp = TimeSpan.Zero;
                    _ranranrooStamps.Clear();
                    _ranranrooTailSoundPlayed = false;
                    _context.Sound.PlayEffect(SoundId.U);
                    return null;
                case "FORMAT":
                    _phase = Phase.FormatWarning;
                    _confirmCount = 0;
                    _message = "";
                    return null;
                case "":
                    // hantei stays 1 when nothing was typed, and the original
                    // shows this hint instead of the wrong-command error.
                    _message = "何かコマンドを入力しよう。　(例)RANRANROO";
                    return null;
                default:
                    _typed.Clear();
                    _message = "コマンドが間違っているかバグかもしれません。もう一度入力してみてください。";
                    return null;
            }
        }

        private SceneTransition? HandleFormatWarningKey(VirtualKey key)
        {
            if (key != VirtualKey.Y)
            {
                _phase = Phase.Typing;
                _typed.Clear();
                _message = "";
                return null;
            }

            _confirmCount++;
            if (_confirmCount < 2)
            {
                _message = "確認：本当によろしいですか？(Y/N)";
                return null;
            }

            _phase = Phase.Formatting;
            _formatElapsed = TimeSpan.Zero;
            _formatPercent = 0;
            _formatComplete = false;
            return null;
        }
    }
}
