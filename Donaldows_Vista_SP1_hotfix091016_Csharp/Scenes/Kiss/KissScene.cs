using System;
using System.Collections.Generic;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Audio;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI;
using Windows.Foundation;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Kiss
{
    // Ports *kiss, including the full `kis`-counter escalation (tracked in
    // AppState.KissCount, since this scene is reached from three different
    // flows in the original: install complete, messenger accept, and RPG
    // "give in"). The 270-stamp sneeze scatter is spread over real time
    // rather than the original's per-frame picload loop.
    public sealed class KissScene : IScene
    {
        private static readonly TimeSpan SceneDuration = TimeSpan.FromSeconds(4);
        private static readonly TimeSpan StampInterval = TimeSpan.FromMilliseconds(12);
        private const int MaxStamps = 270;

        private SceneContext _context = null!;
        private TimeSpan _elapsed;
        private TimeSpan _sinceLastStamp;
        private readonly List<(float X, float Y)> _stamps = new();
        private string _line1 = "";
        private string _line2 = "";

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _elapsed = TimeSpan.Zero;
            _sinceLastStamp = TimeSpan.Zero;
            _stamps.Clear();
            _context.Sound.PlayEffect(SoundId.Buchu);

            _context.AppState.KissCount++;
            var kis = _context.AppState.KissCount;
            var name = string.IsNullOrEmpty(_context.Save.PlayerName) ? "君" : _context.Save.PlayerName;
            (_line1, _line2) = BuildLines(kis, name);
        }

        private static (string Line1, string Line2) BuildLines(int kis, string name)
        {
            if (kis == 1)
            {
                return (
                    $"{name}はドナルドにぶっちゅされてしまった！",
                    $"{name}「うわあああああああああああああああああああ！！\n俺のファーストキッスの相手がぁあああああああああああ！！！！」");
            }

            var line1 = $"{name}はドナルドに{kis}回ぶっちゅされてしまった！";
            var line2 = kis switch
            {
                2 => $"{name}「うわあああああああああああああああああああ！！\n一生取れない赤い口紅つけられたああああああああああああああ！！！！」",
                3 => $"{name}「うわあああああああああああああああああああ！！\n人生＼（＾o＾）／オワタあああああああああああああああああ！！！！」",
                4 => $"{name}「・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・\n・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・・」",
                5 => $"{name}「るうううううううううううううううううううううううううううう\nううううううううううううううううううううううううううううう」",
                6 => $"{name}「らんらんるうううううううううううううううううううううううう！」\nあなたはドナルド語しかしゃべれなくなりました",
                _ => $"{name}「らんらんるらんらんるらんらんるらんらんるらんらんるらんらんる\nらんらんるらんらんるらんらんるらんらんるらんらんるらんらんる」",
            };

            return (line1, line2);
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize)
        {
            session.Clear(Color.FromArgb(255, 255, 0, 128));

            foreach (var (x, y) in _stamps)
            {
                session.DrawImage(_context.Buffers.GetBitmap(BufferId.SneezeStamp), x, y);
            }

            using var format = new CanvasTextFormat { FontSize = 15 };
            session.DrawText($"{_line1}\n{_line2}", new Rect(0, 370, 640, 110), Colors.White, format);
        }

        public SceneTransition? Update(TimeSpan delta)
        {
            _elapsed += delta;

            if (_stamps.Count < MaxStamps)
            {
                _sinceLastStamp += delta;
                if (_sinceLastStamp >= StampInterval)
                {
                    _sinceLastStamp = TimeSpan.Zero;
                    _stamps.Add((Random.Shared.Next(0, 600), Random.Shared.Next(0, 320)));
                }
            }

            return _elapsed >= SceneDuration ? new SceneTransition(SceneId.Bsod) : null;
        }
    }
}
