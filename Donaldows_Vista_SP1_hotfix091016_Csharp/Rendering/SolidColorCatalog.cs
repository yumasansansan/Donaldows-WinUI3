using System.Collections.Generic;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering
{
    public static class SolidColorCatalog
    {
        public static readonly IReadOnlyDictionary<BufferId, Color> Colors = new Dictionary<BufferId, Color>
        {
            [BufferId.TaskbarBackdrop] = Color.FromArgb(255, 0, 20, 20),
            [BufferId.MenuRowBackdrop] = Color.FromArgb(255, 0, 32, 64),
            [BufferId.MenuHoverHighlight] = Color.FromArgb(255, 0, 255, 200),
            [BufferId.Black] = Color.FromArgb(255, 0, 0, 0),
            [BufferId.Orange] = Color.FromArgb(255, 255, 50, 0),
        };
    }
}
