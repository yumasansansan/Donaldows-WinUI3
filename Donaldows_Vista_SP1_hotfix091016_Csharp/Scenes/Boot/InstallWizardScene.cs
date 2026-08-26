using System;
using System.Collections.Generic;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Audio;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI;
using Windows.Foundation;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Boot
{
    // Ports *install. The original's progress loop is `repeat 100` with
    // `await 100`, i.e. one percent every 100ms — ten seconds, not the four
    // an earlier version of this port used.
    //
    // Reproduced: the hapset header art, the full status list (including the
    // ＼(＾o＾)／ｵﾜﾀ line), the four-frame ASCII spinner, the hamico marker
    // stepping down the status list at the original's percentage thresholds,
    // the row of Donald faces filling along the bottom, the scattered faces
    // that appear past 20% and fade out past 75%, and the completion +
    // ranran2 celebration scatter.
    //
    // Not reproduced: the closing red gzoom wipe (buffer 10) that bridges into
    // *kiss — the transition happens without it.
    public sealed class InstallWizardScene : IScene
    {
        private enum Phase { Header, Installing, Complete, Celebrate }

        private static readonly TimeSpan HeaderDuration = TimeSpan.FromMilliseconds(2000);
        private static readonly TimeSpan InstallDuration = TimeSpan.FromMilliseconds(10000);
        private static readonly TimeSpan CompleteHold = TimeSpan.FromMilliseconds(3000 + 2000);
        private static readonly TimeSpan CelebrateDuration = TimeSpan.FromMilliseconds(5400);
        private static readonly string[] Spinner = { "/", "-", "\\", "|" };

        private static readonly string[] StatusLines =
        {
            "準備中",
            "主記憶ドライブをフォーマット（洗脳）しています",
            "さっき洗脳したドライブにドナルドウズをコピーしています",
            "ブートローダーをインストーる～☆しています",
            "一時ファイルを消去しています",
            "＼(＾o＾)／ｵﾜﾀ",
        };

        private SceneContext _context = null!;
        private Phase _phase;
        private TimeSpan _elapsed;
        private int _percent;

        private readonly List<(float X, float Y)> _scatter = new();
        private float _scatterAlpha = 100f;

        private readonly List<(float X, float Y)> _celebrateStamps = new();
        private TimeSpan _sinceLastStamp;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _phase = Phase.Header;
            _elapsed = TimeSpan.Zero;
            _percent = 0;
            _scatterAlpha = 100f;
            _celebrateStamps.Clear();
            _sinceLastStamp = TimeSpan.Zero;

            _scatter.Clear();
            for (var i = 0; i < 20; i++)
            {
                _scatter.Add((Random.Shared.Next(0, 700) - 80, -80 - Random.Shared.Next(0, 40)));
            }

            _context.Sound.PlayEffect(SoundId.Cd);
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize)
        {
            session.Clear(Colors.Black);

            switch (_phase)
            {
                case Phase.Header:
                    session.DrawImage(_context.Buffers.GetBitmap(BufferId.InstallHeader), 256, 0);
                    return;

                case Phase.Celebrate:
                    foreach (var (x, y) in _celebrateStamps)
                    {
                        session.DrawImage(_context.Buffers.GetBitmap(BufferId.MascotSmall), x, y);
                    }

                    return;
            }

            DrawInstallBody(session);
        }

        private void DrawInstallBody(CanvasDrawingSession session)
        {
            var face = _context.Buffers.GetBitmap(BufferId.DonaFace);

            session.DrawImage(_context.Buffers.GetBitmap(BufferId.HddStatus), 120, 250);

            using var format = new CanvasTextFormat { FontSize = 13 };
            session.DrawText("全体の状況を表したグラフ▼", 10, 360, Colors.White, format);
            session.DrawText("インストーる～☆（洗脳）完了▲", 400, 460, Colors.White, format);

            session.DrawText("警告：もう逃げれません！嘘だと思うのであれば\n　　　試しに右上の「×」を押してみてください。", 200, 200, Colors.Red, format);
            session.DrawText("←インストール先のドライブの様子（イメージ）", 260, 300, Colors.White, format);

            for (var i = 0; i < StatusLines.Length; i++)
            {
                session.DrawText(StatusLines[i], 20, 20 + i * 36, Colors.White, format);
            }

            // Scattered faces raining down once past 20%, fading out past 75%.
            if (_percent > 20 && _scatterAlpha > 0)
            {
                foreach (var (x, y) in _scatter)
                {
                    session.DrawImage(face, new Rect(x, y, 84, 75), new Rect(0, 0, 84, 75), _scatterAlpha / 255f);
                }
            }

            // Row of faces filling along the bottom as the percentage climbs.
            for (var z = 1; z <= _percent; z++)
            {
                session.DrawImage(face, z * 6.4f - 20f, 382f);
            }

            // Marker stepping down the status list at the original's thresholds.
            var step = _percent switch
            {
                > 95 => 5,
                > 75 => 4,
                > 40 => 3,
                > 25 => 2,
                > 20 => 1,
                _ => 0,
            };
            session.DrawImage(_context.Buffers.GetBitmap(BufferId.TaskbarIcon), 0, 36 * step + 20);

            session.FillRectangle(10, 460, 140, 20, Colors.Black);
            if (_phase == Phase.Complete)
            {
                session.DrawText("インストーる～☆(洗脳)完了！！　再起動します。", 10, 460, Colors.Red, format);
            }
            else
            {
                var spin = Spinner[(int)(_elapsed.TotalSeconds * 10) % Spinner.Length];
                session.DrawText($"Installing.,.{spin}{_percent}%", 10, 460, Colors.White, format);
            }
        }

        public SceneTransition? Update(TimeSpan delta)
        {
            _elapsed += delta;

            switch (_phase)
            {
                case Phase.Header:
                    if (_elapsed >= HeaderDuration)
                    {
                        _phase = Phase.Installing;
                        _elapsed = TimeSpan.Zero;
                        _context.Sound.PlayEffect(SoundId.Urcd);
                    }

                    return null;

                case Phase.Installing:
                    UpdateInstalling(delta);
                    return null;

                case Phase.Complete:
                    if (_elapsed >= CompleteHold)
                    {
                        _phase = Phase.Celebrate;
                        _elapsed = TimeSpan.Zero;
                        _context.Sound.PlayEffect(SoundId.Donadayo);
                    }

                    return null;

                default:
                    UpdateCelebrate(delta);
                    return _elapsed >= CelebrateDuration ? new SceneTransition(SceneId.Kiss) : null;
            }
        }

        private void UpdateInstalling(TimeSpan delta)
        {
            _percent = Math.Min(100, (int)(_elapsed / InstallDuration * 100));

            if (_percent > 20)
            {
                for (var i = 0; i < _scatter.Count; i++)
                {
                    var (x, y) = _scatter[i];
                    x += Random.Shared.Next(0, 50) - Random.Shared.Next(0, 50);
                    y += Random.Shared.Next(0, 25);
                    if (x < y)
                    {
                        x = y / 2 + 10;
                    }

                    if (x > 640 - y * 2)
                    {
                        x = 640 - y * 2 + 10;
                    }

                    if (y > 262)
                    {
                        y = 240;
                    }

                    _scatter[i] = (x, y);
                }
            }

            if (_percent > 75)
            {
                _scatterAlpha = Math.Max(0, _scatterAlpha - 240f * (float)delta.TotalSeconds);
            }

            if (_elapsed >= InstallDuration)
            {
                _phase = Phase.Complete;
                _elapsed = TimeSpan.Zero;
                _context.Sound.PlayEffect(SoundId.Tara);
                _context.Save.IsInstalled = true;
                _context.SaveManager.Save(_context.Save);
            }
        }

        private void UpdateCelebrate(TimeSpan delta)
        {
            if (_celebrateStamps.Count >= 100)
            {
                return;
            }

            _sinceLastStamp += delta;
            // The original holds the first three stamps for a second each, then
            // machine-guns the rest.
            var interval = _celebrateStamps.Count < 3
                ? TimeSpan.FromMilliseconds(1000)
                : TimeSpan.FromMilliseconds(25);

            if (_sinceLastStamp >= interval)
            {
                _sinceLastStamp = TimeSpan.Zero;
                _context.Sound.PlayEffect(SoundId.Fu);
                _celebrateStamps.Add((Random.Shared.Next(0, 458), Random.Shared.Next(0, 212)));
            }
        }
    }
}
