using System;

namespace Tsukuyomi.Domain.UI
{
    public sealed class ScreenDefinition
    {
        public ScreenDefinition(
            ScreenId screenId,
            string uxmlPath,
            string[] ussPaths,
            ScreenLayer layer,
            bool cacheInstance = true,
            bool useUguiFallback = false)
        {
            ScreenId = screenId;
            UxmlPath = uxmlPath ?? string.Empty;
            UssPaths = ussPaths ?? Array.Empty<string>();
            Layer = layer;
            CacheInstance = cacheInstance;
            UseUguiFallback = useUguiFallback;
        }

        public ScreenId ScreenId { get; }

        public string UxmlPath { get; }

        public string[] UssPaths { get; }

        public ScreenLayer Layer { get; }

        public bool CacheInstance { get; }

        public bool UseUguiFallback { get; }
    }
}
