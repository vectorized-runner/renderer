using Unity.Collections;
using Unity.Entities;

namespace Renderer
{
    public partial class RenderStatsSystem : SystemBase
    {
        private float _lastUpdateTime;
        private NativeArray<float> _averageMsList;
        private const int _collectedFrames = 60;

        protected override void OnCreate()
        {
            _averageMsList = new NativeArray<float>(_collectedFrames, Allocator.Persistent);

            for (int i = 0; i < _collectedFrames; i++)
            {
                // Initialize to 0, will be populated in 60 frames anyway
                _averageMsList[i] = 0;
            }
        }

        protected override void OnDestroy()
        {
            _averageMsList.Dispose();
        }

        protected override void OnUpdate()
        {
            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            var deltaTime = currentTime - _lastUpdateTime;

            // 100ms -> 1000/10 -> 10fps
            // 33ms -> 1000/33 -> 30fps
            const float secondsToMilliseconds = 1000.0f;
            var thisFrameMs = deltaTime * secondsToMilliseconds;
            var lastIndex = _collectedFrames - 1;
            _averageMsList[0] = _averageMsList[lastIndex];
            _averageMsList[lastIndex] = thisFrameMs;

            var totalMs = 0f;

            for (int i = 0; i < _collectedFrames; i++)
            {
                totalMs += _averageMsList[i];
            }

            var averageMs = totalMs / _collectedFrames;

            _lastUpdateTime = currentTime;
        }
    }
}