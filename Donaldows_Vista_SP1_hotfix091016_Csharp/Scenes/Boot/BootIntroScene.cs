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
    // Ports buffer.hsp's opening animation, which runs before *setumei: Donald
    // blinks with 「読み込み中」 written down the right edge, blinks again under
    // 「ちょっとまってね」 while the sound bank loads, then lip-syncs
    // 「お話しようよ」 through the sp1-sp8 frames before settling back on the
    // waiting pose.
    //
    // The original interleaves this with its actual asset loading, so its
    // random `wait rnd(300)` pause stands in for load time; here the assets are
    // already resident, so that pause is a fixed short beat instead.
    public sealed class BootIntroScene : IScene
    {
        private readonly record struct Frame(BufferId Image, string Caption, TimeSpan Duration, SoundId? Sound);

        private const string CaptionLoading = "読\nみ\n込\nみ\n中";
        private const string CaptionWait = "ち\nょ\nっ\nと\nま\nっ\nて\nね";

        // pos 580,10 : font "",50,16
        private const float CaptionX = 580f;
        private const float CaptionY = 10f;
        private const float CaptionFontSize = 50f;

        // buffer.hsp issues no `color` before these captions, and HSP's draw
        // colour starts out black — drawing them white made them invisible
        // against the light part of the artwork.
        private static readonly Color CaptionColor = Colors.Black;

        private SceneContext _context = null!;
        private Frame[] _frames = Array.Empty<Frame>();
        private int _index;
        private TimeSpan _elapsed;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _index = 0;
            _elapsed = TimeSpan.Zero;
            _frames = BuildFrames();
        }

        private static Frame[] BuildFrames()
        {
            var frames = new List<Frame>();
            var blink = TimeSpan.FromMilliseconds(40);

            // repeat 5 over tmp = 1,2,3,2,1
            foreach (var id in new[] { BufferId.Wait1, BufferId.Wait2, BufferId.Wait3, BufferId.Wait2, BufferId.Wait1 })
            {
                frames.Add(new Frame(id, CaptionLoading, blink, null));
            }

            // pos 0,0 : wait rnd(300) — stands in for the sound bank loading.
            frames.Add(new Frame(BufferId.Wait1, CaptionLoading, TimeSpan.FromMilliseconds(900), null));

            // repeat 2 { repeat 5 over tmp = 2,3,2,1 }
            for (var round = 0; round < 2; round++)
            {
                foreach (var id in new[] { BufferId.Wait2, BufferId.Wait3, BufferId.Wait2, BufferId.Wait1, BufferId.Wait1 })
                {
                    frames.Add(new Frame(id, CaptionWait, blink, null));
                }
            }

            frames.Add(new Frame(BufferId.Wait1, CaptionWait, TimeSpan.FromMilliseconds(2000), null));

            // repeat 2 { gcopy 58-cnt } — sp8 then sp7, then the voice starts.
            frames.Add(new Frame(BufferId.Sp8, "", TimeSpan.FromMilliseconds(10), null));
            frames.Add(new Frame(BufferId.Sp7, "", TimeSpan.FromMilliseconds(10), null));

            // mmplay 14 (ohana) then repeat 5 { gcopy 51+cnt } — sp1..sp5.
            var talk = TimeSpan.FromMilliseconds(50);
            frames.Add(new Frame(BufferId.Sp1, "", talk, SoundId.Ohana));
            frames.Add(new Frame(BufferId.Sp2, "", talk, null));
            frames.Add(new Frame(BufferId.Sp3, "", talk, null));
            frames.Add(new Frame(BufferId.Sp4, "", talk, null));
            frames.Add(new Frame(BufferId.Sp5, "", talk, null));

            // repeat 2 { gcopy 55+cnt } — sp5, sp6.
            frames.Add(new Frame(BufferId.Sp5, "", talk, null));
            frames.Add(new Frame(BufferId.Sp6, "", talk, null));

            frames.Add(new Frame(BufferId.Sp6, "", TimeSpan.FromMilliseconds(40), null));
            frames.Add(new Frame(BufferId.Sp1, "", TimeSpan.FromMilliseconds(70), null));
            frames.Add(new Frame(BufferId.Sp6, "", TimeSpan.FromMilliseconds(500), null));

            frames.Add(new Frame(BufferId.Sp7, "", talk, null));
            frames.Add(new Frame(BufferId.Sp8, "", talk, null));

            frames.Add(new Frame(BufferId.Wait1, "", TimeSpan.FromMilliseconds(500), null));

            return frames.ToArray();
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize)
        {
            session.Clear(Colors.Black);

            var frame = _frames[Math.Min(_index, _frames.Length - 1)];
            session.DrawImage(_context.Buffers.GetBitmap(frame.Image), new Rect(0, 0, 640, 480));

            if (frame.Caption.Length == 0)
            {
                return;
            }

            // font "",50,16 — size 50; style bit 16 is antialiasing, not bold.
            // The caption is one character per line down the right edge, which
            // is exactly how `mes "読\nみ\n込\nみ\n中"` lays it out. Each glyph
            // is placed individually rather than relying on a multi-line text
            // format, so line metrics can't push it off-screen.
            using var format = new CanvasTextFormat
            {
                FontSize = CaptionFontSize,
                WordWrapping = CanvasWordWrapping.NoWrap,
            };

            var line = 0;
            foreach (var ch in frame.Caption)
            {
                if (ch == '\n')
                {
                    continue;
                }

                session.DrawText(
                    ch.ToString(),
                    CaptionX,
                    CaptionY + line * CaptionFontSize,
                    CaptionColor,
                    format);
                line++;
            }
        }

        public SceneTransition? Update(TimeSpan delta)
        {
            if (_index == 0 && _elapsed == TimeSpan.Zero && _frames[0].Sound is { } first)
            {
                _context.Sound.PlayEffect(first);
            }

            _elapsed += delta;
            if (_elapsed < _frames[_index].Duration)
            {
                return null;
            }

            _elapsed = TimeSpan.Zero;
            _index++;

            if (_index >= _frames.Length)
            {
                return new SceneTransition(SceneId.Setumei);
            }

            if (_frames[_index].Sound is { } sound)
            {
                _context.Sound.PlayEffect(sound);
            }

            return null;
        }
    }
}
