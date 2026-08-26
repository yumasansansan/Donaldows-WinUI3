using System;
using Microsoft.Graphics.Canvas;
using Windows.Foundation;
using Windows.System;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes
{
    public interface IScene
    {
        void Enter(SceneContext context, object? payload) { }

        SceneTransition? Update(TimeSpan delta) => null;

        void Draw(CanvasDrawingSession session, Size canvasSize);

        SceneTransition? OnPointerMoved(float x, float y) => null;

        SceneTransition? OnPointerPressed(float x, float y) => null;

        SceneTransition? OnKeyDown(VirtualKey key) => null;
    }
}
