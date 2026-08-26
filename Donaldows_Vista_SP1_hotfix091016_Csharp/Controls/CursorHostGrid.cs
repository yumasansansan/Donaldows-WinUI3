using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Controls
{
    // UIElement.ProtectedCursor is the supported way to control the pointer
    // cursor in WinUI 3, but it is protected — so hiding the cursor requires a
    // subclass that exposes it. Assigning null hides the cursor entirely for
    // this element and any descendant that doesn't set its own, which is what
    // the dodge game's `mouse -1` needs. Plain Win32 SetCursor does not work:
    // the XAML input system reasserts its own cursor on every pointer message.
    //
    // This is deliberately instantiated from code and wrapped around the
    // window's XAML content rather than declared in the XAML itself: this
    // project's markup compiler runs without a LocalAssembly reference (see
    // build warning WMC1509), so it cannot resolve project-local types used
    // in markup.
    public sealed partial class CursorHostGrid : Grid
    {
        private readonly InputCursor _arrow = InputSystemCursor.Create(InputSystemCursorShape.Arrow);

        public static CursorHostGrid Wrap(FrameworkElement content)
        {
            var host = new CursorHostGrid();
            host.Children.Add(content);
            return host;
        }

        public void SetCursorHidden(bool hidden) => ProtectedCursor = hidden ? null : _arrow;
    }
}
