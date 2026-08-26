using System;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Audio;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI;
using Windows.Foundation;
using Windows.System;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Dodge
{
    // Ports *game/*g_start/*gameover/*ged. The original never disarms its
    // onclick/onkey handlers once armed in *game, so a click always restarts
    // and any key always quits — even mid-round. That's deliberately NOT
    // reproduced during live play here: it made accidental restarts/quits too
    // easy while dodging, so click/key only act on the Intro and GameOver
    // screens, matching what's actually shown on-screen at each of those.
    public sealed class DodgeGameScene : IScene
    {
        private enum Phase { Intro, Countdown, Playing, Dying, GameOver }

        private const int EnemyCount = 100;
        private const float EnemySize = 20f;
        private const int CollisionGraceScore = 100;
        private const float ScorePerSecond = 50f;

        private static readonly TimeSpan CountdownStepDuration = TimeSpan.FromSeconds(1);
        // The original freezes on the death frame for `wait 150` (1.5s) before
        // switching to the game-over screen.
        private static readonly TimeSpan DyingDuration = TimeSpan.FromMilliseconds(1500);
        private TimeSpan _dyingElapsed;

        private SceneContext _context = null!;
        private Phase _phase;
        private readonly DodgeEnemy[] _enemies = new DodgeEnemy[EnemyCount];

        private TimeSpan _countdownElapsed;
        private int _countdownStep;

        private float _scoreAccumulator;
        private int _score;
        private float _mouseX, _mouseY;

        private float _deathX, _deathY;
        private string _resultMessage = "";
        private SceneId? _pendingReward;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _phase = Phase.Intro;
            _context.HideCursor = false;
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize)
        {
            switch (_phase)
            {
                case Phase.Intro:
                    DrawIntro(session);
                    break;
                case Phase.Countdown:
                    DrawCountdown(session);
                    break;
                case Phase.Playing:
                    DrawPlaying(session);
                    break;
                case Phase.Dying:
                    DrawPlaying(session);
                    break;
                case Phase.GameOver:
                    DrawGameOver(session);
                    break;
            }
        }

        private static void DrawIntro(CanvasDrawingSession session)
        {
            session.Clear(Colors.Black);
            using var format = HspFont.Create();
            session.DrawText(
                "説明：グリマスをマウスで動かして、ドナルドをよけてください\n" +
                "得点があがるとドナルドの動きがはやくなってきます。\n" +
                "2000点以上、余裕があれば3000点以上を目指してがんばろう！！\n" +
                "3000点以上になると、ドナルドからご褒美があるよ。！！！\n" +
                "クリックすると始まります。どれかキーを押すとゲームを終了します。",
                new Rect(0, 0, 640, 200), Colors.White, format);
        }

        private void DrawCountdown(CanvasDrawingSession session)
        {
            session.Clear(Colors.Black);
            using var format = new CanvasTextFormat { FontSize = 24 };
            var text = _countdownStep < 3 ? (3 - _countdownStep).ToString() : "逝ってみよう！！";
            session.DrawText(text, 0, 0, Colors.White, format);
        }

        private void DrawPlaying(CanvasDrawingSession session)
        {
            session.Clear(Colors.Black);

            foreach (var enemy in _enemies)
            {
                session.DrawImage(_context.Buffers.GetBitmap(BufferId.EnemySprite), enemy.X, enemy.Y);
            }

            session.DrawImage(_context.Buffers.GetBitmap(BufferId.PlayerCursorSprite), _mouseX - 15, _mouseY - 19);

            using var format = HspFont.Create();
            session.DrawText($"H,SCORE:{_context.Save.HighScore}pt", 0, 0, Colors.White, format);
            session.DrawText($"SCORE  :{_score}pt", 0, 16, Colors.White, format);
        }

        private void DrawGameOver(CanvasDrawingSession session)
        {
            session.Clear(Colors.Black);
            session.DrawImage(_context.Buffers.GetBitmap(BufferId.PlayerCursorSprite), _deathX, _deathY);

            foreach (var enemy in _enemies)
            {
                session.DrawImage(_context.Buffers.GetBitmap(BufferId.EnemySprite), enemy.X, enemy.Y);
            }

            using var format = HspFont.Create();
            session.DrawText("ＧＡＭＥ　ＯＶＥＲＯＯ☆", 120, 200, Colors.White, format);

            using var small = HspFont.Create();
            session.DrawText(
                $"HIGH SCORE = {_context.Save.HighScore}POINT\nYOUR SCORE = {_score}POINT\n\n{_resultMessage}\n\nもう一回遊ぶ → クリック\nデスクトップに戻る → どれかキーを押す",
                new Rect(120, 224, 400, 160), Colors.White, small);
        }

        public SceneTransition? Update(TimeSpan delta)
        {
            switch (_phase)
            {
                case Phase.Countdown:
                    return UpdateCountdown(delta);
                case Phase.Playing:
                    return UpdatePlaying(delta);
                case Phase.Dying:
                    return UpdateDying(delta);
                case Phase.GameOver:
                    return UpdateGameOver(delta);
                default:
                    return null;
            }
        }

        private SceneTransition? UpdateCountdown(TimeSpan delta)
        {
            _countdownElapsed += delta;
            if (_countdownElapsed < CountdownStepDuration)
            {
                return null;
            }

            _countdownElapsed = TimeSpan.Zero;
            if (_countdownStep < 3)
            {
                _context.Sound.PlayEffect(SoundId.N);
            }

            _countdownStep++;

            if (_countdownStep == 3)
            {
                _context.Sound.PlayEffect(SoundId.Itte);
            }
            else if (_countdownStep > 3)
            {
                BeginPlaying();
            }

            return null;
        }

        private void BeginPlaying()
        {
            _phase = Phase.Playing;
            _score = 0;
            _scoreAccumulator = 0;
            _context.Sound.PlayBgm(SoundId.GameBgm1);
            _context.HideCursor = true;

            for (var i = 0; i < EnemyCount; i++)
            {
                _enemies[i].X = Random.Shared.Next(0, 640);
                _enemies[i].Y = Random.Shared.Next(0, 480);
                _enemies[i].VelX = RandomNonZeroStep();
                _enemies[i].VelY = RandomNonZeroStep();
            }
        }

        private static float RandomNonZeroStep()
        {
            float v;
            do
            {
                v = 0.3f * (Random.Shared.Next(0, 7) - 4);
            } while (v == 0f);

            return v;
        }

        private SceneTransition? UpdatePlaying(TimeSpan delta)
        {
            var speed = 0.7f + 0.0001f * _score;
            // Clamp so a frame hitch (GC pause, window drag, etc.) can't make
            // enemies leap a large distance in one Update and cause an
            // unfair-feeling "teleport" collision.
            var clampedSeconds = Math.Min(delta.TotalSeconds, 1.0 / 20.0);
            var dtFrames = (float)(clampedSeconds * 50); // original paced ~50 updates/sec (wait 2)

            for (var i = 0; i < EnemyCount; i++)
            {
                ref var enemy = ref _enemies[i];
                if (enemy.X < -20f)
                {
                    enemy.VelX = speed * (Random.Shared.Next(0, 3) + 1);
                }
                else if (enemy.X > 640f)
                {
                    enemy.VelX = -speed * (Random.Shared.Next(0, 3) + 1);
                }

                if (enemy.Y < -20f)
                {
                    enemy.VelY = speed * (Random.Shared.Next(0, 3) + 1);
                }
                else if (enemy.Y > 480f)
                {
                    enemy.VelY = -speed * (Random.Shared.Next(0, 3) + 1);
                }

                enemy.X += enemy.VelX * dtFrames;
                enemy.Y += enemy.VelY * dtFrames;
            }

            _scoreAccumulator += ScorePerSecond * (float)clampedSeconds;
            _score = (int)_scoreAccumulator;

            if (_score > CollisionGraceScore)
            {
                foreach (var enemy in _enemies)
                {
                    if (_mouseX > enemy.X && _mouseX < enemy.X + EnemySize &&
                        _mouseY > enemy.Y && _mouseY < enemy.Y + EnemySize)
                    {
                        EnterDying();
                        break;
                    }
                }
            }

            return null;
        }

        private void EnterDying()
        {
            _deathX = _mouseX - 15;
            _deathY = _mouseY - 19;
            _context.Sound.StopAll();
            _context.Sound.PlayEffect(SoundId.Tara);
            _phase = Phase.Dying;
            _dyingElapsed = TimeSpan.Zero;
        }

        private SceneTransition? UpdateDying(TimeSpan delta)
        {
            _dyingElapsed += delta;
            return _dyingElapsed >= DyingDuration ? EnterGameOver() : null;
        }

        private SceneTransition? EnterGameOver()
        {
            _phase = Phase.GameOver;
            _context.Sound.PlayEffect(SoundId.Gameover);

            _pendingReward = null;

            if (_score > _context.Save.HighScore)
            {
                _context.Save.HighScore = _score;
                _context.SaveManager.Save(_context.Save);
                _resultMessage = "ハイスコア更新おめ。";

                if (_score > 2999)
                {
                    _pendingReward = SceneId.Kiss;
                }
                else if (_score > 1999)
                {
                    _pendingReward = SceneId.Bsod;
                }
            }
            else
            {
                var diff = _context.Save.HighScore - _score;
                _resultMessage = $"ハイスコアまであと{diff}POINT";
                if (diff is > 0 and < 10)
                {
                    _resultMessage += "\nおしい！　がんば。";
                }
            }

            // The original evaluates the reward jumps inside the very first
            // frame of *gameover, so a qualifying score never shows the
            // game-over screen at all.
            return _pendingReward is { } reward ? new SceneTransition(reward) : null;
        }

        private SceneTransition? UpdateGameOver(TimeSpan delta)
        {
            var dtFrames = (float)(delta.TotalSeconds * 50);
            foreach (ref var enemy in _enemies.AsSpan())
            {
                if (enemy.X > _deathX)
                {
                    enemy.X -= Random.Shared.Next(0, 6) * dtFrames;
                }
                else if (enemy.X < _deathX)
                {
                    enemy.X += Random.Shared.Next(0, 6) * dtFrames;
                }

                if (enemy.Y > _deathY)
                {
                    enemy.Y -= Random.Shared.Next(0, 6) * dtFrames;
                }
                else if (enemy.Y < _deathY)
                {
                    enemy.Y += Random.Shared.Next(0, 6) * dtFrames;
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
            if (_phase == Phase.Intro || _phase == Phase.GameOver)
            {
                _context.Sound.StopAll();
                _context.HideCursor = true;
                _phase = Phase.Countdown;
                _countdownElapsed = TimeSpan.Zero;
                _countdownStep = 0;
            }

            return null;
        }

        public SceneTransition? OnKeyDown(VirtualKey key)
        {
            if (_phase == Phase.Intro || _phase == Phase.GameOver)
            {
                _context.Sound.StopAll();
                _context.HideCursor = false;
                return new SceneTransition(SceneId.IdleDesktop);
            }

            return null;
        }
    }
}
