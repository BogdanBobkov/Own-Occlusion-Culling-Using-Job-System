using Unity.Collections;
using Unity.Jobs;

namespace Culling.Jobs
{
    public struct CheckVisibleBehavioursJob : IJob
    {
        public NativeList<int> visibleObjects;
        [ReadOnly] public NativeList<int> hitObjects;
        [WriteOnly] public NativeList<float> timers;

        public void Execute()
        {
            foreach (var id in hitObjects)
            {
                var index = visibleObjects.IndexOf(id);

                if (index < 0)
                {
                    visibleObjects.Add(id);
                    timers.Add(0);
                }
                else
                {
                    timers[index] = 0;
                }
            }
        }
    }
}