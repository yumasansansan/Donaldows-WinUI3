using System.Collections.Generic;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Audio;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Microsoft.Graphics.Canvas;
using Microsoft.UI;
using Windows.Foundation;
using Windows.System;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Cmd
{
    // Ports *notepad/*type: a fake Notepad where every keystroke echoes the
    // next character of a fixed "ランランル～" cycle into a text grid,
    // regardless of which key was actually pressed.
    public sealed class NotepadScene : IScene
    {
        private static readonly Rect CloseButton = new(575, 0, 64, 20);
        private static readonly char[] Cycle = { 'ラ', 'ン', 'ラ', 'ン', 'ル', '～' };

        private SceneContext _context = null!;
        private readonly List<(float X, float Y, char Ch)> _chars = new();
        private float _x, _y;
        private int _cycleIndex;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _chars.Clear();
            _x = 0;
            _y = 40;
            _cycleIndex = 5; // matches original's g=6 initial state (next char is index 0, 'ラ')
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize)
        {
            session.Clear(Color.FromArgb(255, 64, 64, 64)); // cls 3 = dark gray
            session.FillRectangle(0, 20, 640, 15, Colors.White);
            session.FillRectangle(20, 60, 600, 380, Colors.White);
            session.FillRectangle(0, 460, 640, 20, Colors.Black);

            using var format = HspFont.Create();
            session.DrawText("UntitLOO!(無題) - NOTEPAD", 0, 0, Colors.White, format);
            session.DrawText("ファイる～☆|編集|書式|表示|ヘルプ|　←このメニュは出来てません。", 10, 20, Colors.Black, format);

            session.FillRectangle(CloseButton, Color.FromArgb(255, 200, 200, 200));
            session.DrawText("CLOSE", CloseButton, Colors.Black, format);

            foreach (var (x, y, ch) in _chars)
            {
                session.DrawText(ch.ToString(), x, y, Colors.Black, format);
            }
        }

        public SceneTransition? OnKeyDown(VirtualKey key)
        {
            _context.Sound.PlayEffect(SoundId.U);

            _x += 20;
            if (_x >= 620)
            {
                _x = 20;
                _y += 20;
            }

            _cycleIndex = (_cycleIndex + 1) % Cycle.Length;
            if (_chars.Count < 2000)
            {
                _chars.Add((_x, _y, Cycle[_cycleIndex]));
            }

            // The original's `if y=440 : goto *blue` sits before its onkey hook
            // and is only ever evaluated once, while y is still 40 — so typing
            // past the bottom of the page never closes the window there either.
            return null;
        }

        public SceneTransition? OnPointerPressed(float x, float y) =>
            CloseButton.Contains(new Point(x, y)) ? new SceneTransition(SceneId.Bsod) : null;
    }
}
