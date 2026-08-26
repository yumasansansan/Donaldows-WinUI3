using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering
{
    public sealed class BufferManager
    {
        private readonly Dictionary<BufferId, CanvasBitmap> _bitmaps = new();

        // Unlike SoundManager, CanvasBitmap.LoadAsync is inherently async, so
        // loading happens in an explicit init step rather than the constructor.
        public async Task InitializeAsync(ICanvasResourceCreator device)
        {
            var imgDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Img");
            foreach (var (id, fileName) in BufferCatalog.FileNames)
            {
                var path = Path.Combine(imgDir, fileName);
                _bitmaps[id] = await CanvasBitmap.LoadAsync(device, path);
            }
        }

        public CanvasBitmap GetBitmap(BufferId id) => _bitmaps[id];

        public Color GetColor(BufferId id) => SolidColorCatalog.Colors[id];
    }
}
