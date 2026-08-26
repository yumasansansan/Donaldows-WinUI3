using System;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Microsoft.Graphics.Canvas;
using Microsoft.UI;
using Windows.Foundation;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Rpg
{
    // Ports *punch's three-stage attack cut-in.
    //
    //  1. Charge (repeat 140): dark red field streaked with 100 white speed
    //     lines, the 2560px fist strip sliding in from the right while the
    //     close-up face slides with it. Past frame 100 the hand accelerates
    //     (`if cnt>100 : a=a+(cnt-70)`).
    //  2. Impact (repeat 25): black field, the fist zoomed toward the viewer
    //     at 4a x 3a where a = 2*cnt².
    //  3. Knockback (repeat 75): the field again, the enemy hurled off to the
    //     right (`pos 640-a, 100-0.1a` with a = cnt²) while the screen fades
    //     to black. The window shakes on BOTH axes for the first five frames,
    //     scaled by the damage roll.
    public sealed class RpgPunchAnimation
    {
        public static readonly TimeSpan ChargeDuration = TimeSpan.FromMilliseconds(1400);
        public static readonly TimeSpan ImpactDuration = TimeSpan.FromMilliseconds(250);
        public static readonly TimeSpan KnockbackDuration = TimeSpan.FromMilliseconds(750);
        public static readonly TimeSpan Total = ChargeDuration + ImpactDuration + KnockbackDuration;

        private const int LineCount = 100;
        private static readonly Color Field = Color.FromArgb(255, 100, 0, 0);

        private readonly float[] _lineX = new float[LineCount];
        private readonly float[] _lineY = new float[LineCount];
        private readonly float[] _lineSpeed = new float[LineCount];

        public void Reset()
        {
            for (var i = 0; i < LineCount; i++)
            {
                _lineSpeed[i] = 1.1f * (Random.Shared.Next(0, 50) + 2);
                _lineX[i] = Random.Shared.Next(0, 640) - 200;
                _lineY[i] = Random.Shared.Next(0, 480);
            }
        }

        public void Advance(float frames)
        {
            for (var i = 0; i < LineCount; i++)
            {
                _lineX[i] += _lineSpeed[i] * frames;
                if (_lineX[i] > 640f)
                {
                    _lineX[i] -= 690f;
                    _lineY[i] = Random.Shared.Next(0, 480);
                }
            }
        }

        public void Draw(CanvasDrawingSession session, BufferManager buffers, TimeSpan elapsed)
        {
            if (elapsed < ChargeDuration)
            {
                DrawCharge(session, buffers, (float)(elapsed / ChargeDuration));
                return;
            }

            if (elapsed < ChargeDuration + ImpactDuration)
            {
                DrawImpact(session, buffers, (float)((elapsed - ChargeDuration) / ImpactDuration));
                return;
            }

            var t = Math.Clamp((float)((elapsed - ChargeDuration - ImpactDuration) / KnockbackDuration), 0f, 1f);
            DrawKnockback(session, buffers, t);
        }

        private void DrawField(CanvasDrawingSession session, bool trailing)
        {
            session.FillRectangle(0, 0, 640, 480, Field);

            for (var i = 0; i < LineCount; i++)
            {
                var x = _lineX[i];
                var (x0, x1) = trailing
                    ? (x, x + 50f - _lineSpeed[i])
                    : (x - _lineSpeed[i], x);
                session.DrawLine(x0, _lineY[i], x1, _lineY[i], Colors.White);
            }
        }

        private void DrawCharge(CanvasDrawingSession session, BufferManager buffers, float t)
        {
            DrawField(session, trailing: false);

            var frame = t * 140f;

            // The hand accelerates past frame 100: a accumulates (cnt-70).
            var a = 0f;
            if (frame > 100f)
            {
                var extra = frame - 100f;
                a = extra * (30f + extra / 2f);
            }

            var strip = buffers.GetBitmap(BufferId.PunchStrip);
            session.DrawImage(strip, new Rect(520f - 2.5f * frame - a, 100f - 0.1f * a, 2560, 480), strip.Bounds);

            var face = buffers.GetBitmap(BufferId.PunchFace);
            session.DrawImage(face, new Rect(500f - 2f * frame, 0, 640, 480), face.Bounds);
        }

        private static void DrawImpact(CanvasDrawingSession session, BufferManager buffers, float t)
        {
            session.Clear(Colors.Black);

            var a = 2f * MathF.Pow(t * 25f, 2f);
            var fist = buffers.GetBitmap(BufferId.PunchImpact);
            session.DrawImage(fist, new Rect(320f - 2f * a, 240f - 1.5f * a, 4f * a, 3f * a), fist.Bounds);
        }

        private void DrawKnockback(CanvasDrawingSession session, BufferManager buffers, float t)
        {
            DrawField(session, trailing: true);

            var frame = t * 75f;
            var a = frame * frame;

            var enemy = buffers.GetBitmap(BufferId.MascotSprite);
            session.DrawImage(enemy, new Rect(640f - a, 100f - 0.1f * a, 182, 270), new Rect(0, 0, 182, 270));

            session.FillRectangle(0, 0, 640, 480, Color.FromArgb((byte)Math.Clamp(frame * 4f, 0f, 255f), 0, 0, 0));
        }
    }
}
