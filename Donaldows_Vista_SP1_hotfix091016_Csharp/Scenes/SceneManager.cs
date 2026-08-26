using System;
using System.Collections.Generic;
using Microsoft.Graphics.Canvas;
using Windows.Foundation;
using Windows.System;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes
{
    // Replaces HSP's goto-based label soup: owns the current scene and swaps it
    // whenever a scene's Update/pointer handler returns a SceneTransition.
    //
    // CanvasAnimatedControl runs Update/Draw on its own game-loop thread while
    // pointer events arrive on the UI thread, so pointer input is captured into
    // a small lock-guarded buffer here and only applied to the current scene
    // from Update (on the game-loop thread) to avoid cross-thread scene access.
    public sealed class SceneManager
    {
        private readonly SceneContext _context;
        private readonly Dictionary<SceneId, Func<IScene>> _factories;
        private IScene _current;

        private readonly object _pointerLock = new();
        private bool _hasPendingMove;
        private float _pendingMoveX, _pendingMoveY;
        private bool _hasPendingPress;
        private float _pendingPressX, _pendingPressY;
        private readonly Queue<VirtualKey> _pendingKeys = new();
        private SceneTransition? _pendingForced;

        public const int VirtualWidth = 640;
        public const int VirtualHeight = 480;

        private CanvasRenderTarget? _frame;

        public SceneManager(SceneContext context, Dictionary<SceneId, Func<IScene>> factories, SceneId initial)
        {
            _context = context;
            _factories = factories;
            _current = _factories[initial]();
            _current.Enter(_context, null);
        }

        public void NotifyPointerMoved(float x, float y)
        {
            lock (_pointerLock)
            {
                _hasPendingMove = true;
                _pendingMoveX = x;
                _pendingMoveY = y;
            }
        }

        public void NotifyPointerPressed(float x, float y)
        {
            lock (_pointerLock)
            {
                _hasPendingPress = true;
                _pendingPressX = x;
                _pendingPressY = y;
            }
        }

        public void NotifyKeyDown(VirtualKey key)
        {
            lock (_pointerLock)
            {
                _pendingKeys.Enqueue(key);
            }
        }

        // Forces an immediate scene switch regardless of what the current
        // scene's own input/update logic would otherwise decide — used for
        // the AppWindow.Closing interception (onexit goto *virus), which can
        // fire from any scene at any time. Thread-safe: queued and applied
        // from Update() like pointer/key input, since callers may be on the
        // UI thread while Update/Draw run on the game-loop thread.
        public void ForceTransition(SceneId id, object? payload = null)
        {
            lock (_pointerLock)
            {
                _pendingForced = new SceneTransition(id, payload);
            }
        }

        public void Update(TimeSpan delta)
        {
            bool hasMove, hasPress;
            float moveX = 0, moveY = 0, pressX = 0, pressY = 0;
            VirtualKey[] keys;
            SceneTransition? forced;
            lock (_pointerLock)
            {
                hasMove = _hasPendingMove;
                moveX = _pendingMoveX;
                moveY = _pendingMoveY;
                _hasPendingMove = false;

                hasPress = _hasPendingPress;
                pressX = _pendingPressX;
                pressY = _pendingPressY;
                _hasPendingPress = false;

                keys = _pendingKeys.ToArray();
                _pendingKeys.Clear();

                forced = _pendingForced;
                _pendingForced = null;
            }

            if (forced is not null)
            {
                ApplyTransition(forced);
                return;
            }

            if (hasMove)
            {
                ApplyTransition(_current.OnPointerMoved(moveX, moveY));
            }

            if (hasPress)
            {
                ApplyTransition(_current.OnPointerPressed(pressX, pressY));
            }

            foreach (var key in keys)
            {
                ApplyTransition(_current.OnKeyDown(key));
            }

            ApplyTransition(_current.Update(delta));
        }

        // HSP draws into a framebuffer that PERSISTS between frames — `redraw`
        // only controls when it is presented, and nothing is erased until the
        // script explicitly calls cls/boxf. Scenes therefore draw into a
        // render target that is never implicitly cleared, which is what makes
        // the original's accumulating effects work (repeatedly alpha-blending
        // the same image until it turns opaque, dialogs piling up into a
        // swarm, sprites scattering over whatever was already on screen).
        public void Draw(CanvasDrawingSession target, ICanvasResourceCreatorWithDpi device, Size canvasSize)
        {
            _frame ??= new CanvasRenderTarget(device, VirtualWidth, VirtualHeight, device.Dpi);

            using (var frameSession = _frame.CreateDrawingSession())
            {
                _current.Draw(frameSession, new Size(VirtualWidth, VirtualHeight));
            }

            target.DrawImage(_frame, new Rect(0, 0, canvasSize.Width, canvasSize.Height));
        }

        private void ApplyTransition(SceneTransition? transition)
        {
            if (transition is null)
            {
                return;
            }

            _current = _factories[transition.Next]();
            _current.Enter(_context, transition.Payload);
        }
    }
}
