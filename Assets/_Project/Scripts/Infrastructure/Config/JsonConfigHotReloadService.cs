using System;
using System.IO;
using Tsukuyomi.Application.Config;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tsukuyomi.Infrastructure.Config
{
    public sealed class JsonConfigHotReloadService : IConfigHotReloadService
    {
        private readonly string _watchDirectory;
        private FileSystemWatcher _watcher;
        private bool _isStarted;

        public JsonConfigHotReloadService(string watchDirectory = null)
        {
            var candidate = watchDirectory ?? ConfigPaths.GetConfigSourcePath("placeholder");
            if (candidate.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                candidate = Path.GetDirectoryName(candidate);
            }

            _watchDirectory = candidate;
        }

        public event Action<string> Reloaded;

        public void Start()
        {
            if (_isStarted)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Directory.Exists(_watchDirectory))
            {
                _isStarted = true;
                return;
            }

            _watcher = new FileSystemWatcher(_watchDirectory, "*.json")
            {
                IncludeSubdirectories = false,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
            };
            _watcher.Changed += OnWatcherEvent;
            _watcher.Created += OnWatcherEvent;
            _watcher.Renamed += OnWatcherRenamed;
            _watcher.EnableRaisingEvents = true;
#endif
            _isStarted = true;
        }

        public void Stop()
        {
            if (!_isStarted)
            {
                return;
            }

#if UNITY_EDITOR
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Changed -= OnWatcherEvent;
                _watcher.Created -= OnWatcherEvent;
                _watcher.Renamed -= OnWatcherRenamed;
                _watcher.Dispose();
                _watcher = null;
            }
#endif

            _isStarted = false;
        }

        public void Dispose()
        {
            Stop();
        }

#if UNITY_EDITOR
        private void OnWatcherEvent(object sender, FileSystemEventArgs eventArgs)
        {
            RaiseReloadEvent(eventArgs.FullPath);
        }

        private void OnWatcherRenamed(object sender, RenamedEventArgs eventArgs)
        {
            RaiseReloadEvent(eventArgs.FullPath);
        }

        private void RaiseReloadEvent(string fullPath)
        {
            var configName = Path.GetFileNameWithoutExtension(fullPath);
            if (string.IsNullOrWhiteSpace(configName))
            {
                return;
            }

            EditorApplication.delayCall += () => Reloaded?.Invoke(configName);
        }
#endif
    }
}
