using System;
using System.Collections.Generic;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Audio;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Microsoft.Graphics.Canvas;
using Microsoft.UI;
using Windows.Foundation;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Boot
{
    // Ports *install.
    //
    // The header art (hapset.bmp) is NOT cleared when the install proper
    // starts: the original only darkens it with 25 passes of a 10/255 black
    // blend, which lands around 63% and leaves the picture visible behind
    // everything for the rest of the sequence. The same is true of the closing
    // ranran2 celebration and the red love.bmp zoom, which both scatter over
    // whatever is already on screen rather than over black.
    //
    // The progress loop is `repeat 100` with `await 100` — one percent every
    // 100ms, and the falling-face simulation advances once per those ticks,
    // not once per rendered frame.
    public sealed class InstallWizardScene : IScene
    {
        private enum Phase { Header, Installing, Complete, Celebrate, LoveZoom }

        private static readonly TimeSpan HeaderDuration = TimeSpan.FromMilliseconds(2000);
        private static readonly TimeSpan FadeDuration = TimeSpan.FromMilliseconds(400);
        private static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(100);
        private static readonly TimeSpan CompleteHold = TimeSpan.FromMilliseconds(3000 + 2000);
        private static readonly TimeSpan LoveZoomDuration = TimeSpan.FromMilliseconds(1500);
        private const float FadeDarkness = 0.63f;
        private static readonly string[] Spinner =
        {
            "Installing,../", "Installing.,.-", "Installing..,\\", "Installing.,.|",
        };

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
        private TimeSpan _sinceTick;
        private int _percent;
        private int _spinnerFrame;

        private readonly List<(float X, float Y)> _scatter = new();
        private float _scatterAlpha = 100f;

        private readonly List<(float X, float Y)> _celebrateStamps = new();
        private TimeSpan _sinceLastStamp;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _phase = Phase.Header;
            _elapsed = TimeSpan.Zero;
            _sinceTick = TimeSpan.Zero;
            _percent = 0;
            _spinnerFrame = 0;
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
            // The static backdrop is redrawn every frame rather than captured,
            // but it is identical to what the original snapshots into its
            // scratch buffer and restores each iteration.
            session.Clear(Colors.Black);
            session.DrawImage(_context.Buffers.GetBitmap(BufferId.InstallHeader), 256, 0);

            if (_phase == Phase.Header)
            {
                return;
            }

            var fade = Math.Clamp((float)(_elapsed / FadeDuration), 0f, 1f) * FadeDarkness;
            if (_phase != Phase.Installing)
            {
                fade = FadeDarkness;
            }

            session.FillRectangle(0, 0, 640, 480, Color.FromArgb((byte)(fade * 255), 0, 0, 0));
            session.FillRectangle(0, 380, 640, 80, Color.FromArgb((byte)(fade * 255), 255, 0, 0));

            DrawInstallBody(session);

            if (_phase is Phase.Celebrate or Phase.LoveZoom)
            {
                foreach (var (x, y) in _celebrateStamps)
                {
                    session.DrawImage(_context.Buffers.GetBitmap(BufferId.MascotSmall), x, y);
                }
            }

            if (_phase == Phase.LoveZoom)
            {
                DrawLoveZoom(session);
            }
        }

        private void DrawInstallBody(CanvasDrawingSession session)
        {
            var face = _context.Buffers.GetBitmap(BufferId.DonaFace);

            session.DrawImage(_context.Buffers.GetBitmap(BufferId.HddStatus), 120, 250);

            using var format = HspFont.Create();
            session.DrawText("全体の状況を表したグラフ▼", 10, 360, Colors.White, format);
            session.DrawText("インストーる～☆（洗脳）完了▲", 400, 460, Colors.White, format);

            session.DrawText("警告：もう逃げれません！嘘だと思うのであれば\n　　　試しに右上の「×」を押してみてください。", 200, 200, Colors.Red, format);
            session.DrawText("←インストール先のドライブの様子（イメージ）", 260, 300, Colors.White, format);

            for (var i = 0; i < StatusLines.Length; i++)
            {
                session.DrawText(StatusLines[i], 20, 20 + i * 36, Colors.White, format);
            }

            if (_percent > 20 && _scatterAlpha > 0)
            {
                foreach (var (x, y) in _scatter)
                {
                    session.DrawImage(face, new Rect(x, y, 84, 75), new Rect(0, 0, 84, 75), _scatterAlpha / 255f);
                }
            }

            for (var z = 1; z <= _percent; z++)
            {
                session.DrawImage(face, z * 6.4f - 20f, 382f);
            }

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
            if (_phase == Phase.Installing)
            {
                session.DrawText($"{Spinner[_spinnerFrame]}{_percent}%", 10, 460, Colors.White, format);
            }
            else
            {
                session.DrawText("インストーる～☆(洗脳)完了！！　再起動します。", 10, 460, Colors.Red, format);
            }
        }

        // repeat 150 { w+=2 ; gzoom w*4,w*4 of buffer 10 centred, over a red field }
        private void DrawLoveZoom(CanvasDrawingSession session)
        {
            var t = Math.Clamp((float)(_elapsed / LoveZoomDuration), 0f, 1f);
            var w = t * 300f;
            var size = w * 4f;

            session.FillRectangle(0, 0, 640, 480, Color.FromArgb((byte)(t * 255), 255, 0, 0));

            var love = _context.Buffers.GetBitmap(BufferId.LoveZoom);
            session.DrawImage(love, new Rect(320 - w * 2, 240 - w * 3.99f, size, size), love.Bounds);
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
                    }

                    return null;

                case Phase.Celebrate:
                    UpdateCelebrate(delta);
                    if (_celebrateStamps.Count >= 100)
                    {
                        _phase = Phase.LoveZoom;
                        _elapsed = TimeSpan.Zero;
                        _context.Sound.PlayEffect(SoundId.Donadayo);
                    }

                    return null;

                default:
                    return _elapsed >= LoveZoomDuration ? new SceneTransition(SceneId.Kiss) : null;
            }
        }

        private void UpdateInstalling(TimeSpan delta)
        {
            _sinceTick += delta;
            if (_sinceTick < Tick)
            {
                return;
            }

            _sinceTick = TimeSpan.Zero;
            _percent = Math.Min(100, _percent + 1);
            _spinnerFrame = (_spinnerFrame + 1) % Spinner.Length;

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
                _scatterAlpha = Math.Max(0, _scatterAlpha - 4f);
            }

            if (_percent >= 100)
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
