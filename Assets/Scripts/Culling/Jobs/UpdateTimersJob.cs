using Unity.Collections;
using Unity.Jobs;

namespace Culling.Jobs
{
    public struct UpdateTimersJob : IJob
    {
        public NativeList<float> timers;
        [ReadOnly] public float deltaTime;

        public void Execute()
        {
            for (int i = 0; i < timers.Length; i++)
                timers[i] += deltaTime;
        }
    }
}