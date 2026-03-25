using Tsukuyomi.Generated.Config;
using UnityEngine;

namespace Tsukuyomi.Bootstrap.World
{
    internal sealed class AutoChessCameraDirector
    {
        private readonly Camera _camera;
        private readonly CameraRigConfig _config;

        private Vector3 _targetPosition;
        private float _targetSize;
        private Vector3 _positionVelocity;
        private float _sizeVelocity;
        private float _shakeTimer;
        private float _shakeSeed;

        public AutoChessCameraDirector(Camera camera, CameraRigConfig config)
        {
            _camera = camera;
            _config = config;
            _shakeSeed = 0.37f;
            ResetImmediate();
        }

        public void SetVisible(bool visible)
        {
            _camera.enabled = visible;
        }

        public void SetBoardView()
        {
            _targetPosition = new Vector3(_config.positionX, _config.positionY, _config.positionZ);
            _targetSize = _config.orthographicSize;
        }

        public void SetBattleFocus(Vector3 worldPoint)
        {
            _targetPosition = new Vector3(worldPoint.x, worldPoint.y, _config.positionZ);
            _targetSize = _config.battleZoomSize;
        }

        public void Pulse(Vector3 worldPoint)
        {
            SetBattleFocus(worldPoint);
            _shakeTimer = Mathf.Max(_shakeTimer, _config.shakeDuration);
        }

        public void Tick(float deltaTime)
        {
            var smoothTime = Mathf.Max(0.01f, _config.smoothTime);
            var nextPosition = Vector3.SmoothDamp(
                _camera.transform.position,
                _targetPosition,
                ref _positionVelocity,
                smoothTime,
                Mathf.Infinity,
                deltaTime);
            var nextSize = Mathf.SmoothDamp(
                _camera.orthographicSize,
                _targetSize,
                ref _sizeVelocity,
                smoothTime,
                Mathf.Infinity,
                deltaTime);

            if (_shakeTimer > 0f)
            {
                _shakeTimer = Mathf.Max(0f, _shakeTimer - deltaTime);
                _shakeSeed += deltaTime * 17.3f;
                var fade = _config.shakeDuration <= 0f ? 0f : _shakeTimer / _config.shakeDuration;
                var amplitude = _config.shakeAmplitude * fade;
                nextPosition.x += (Mathf.PerlinNoise(_shakeSeed, 1.1f) - 0.5f) * 2f * amplitude;
                nextPosition.y += (Mathf.PerlinNoise(2.7f, _shakeSeed) - 0.5f) * 2f * amplitude;
            }

            _camera.transform.position = nextPosition;
            _camera.orthographicSize = nextSize;
        }

        public void ResetImmediate()
        {
            _targetPosition = new Vector3(_config.positionX, _config.positionY, _config.positionZ);
            _targetSize = _config.orthographicSize;
            _positionVelocity = Vector3.zero;
            _sizeVelocity = 0f;
            _shakeTimer = 0f;
            _camera.transform.position = _targetPosition;
            _camera.orthographicSize = _targetSize;
            _camera.orthographic = true;
        }
    }
}
