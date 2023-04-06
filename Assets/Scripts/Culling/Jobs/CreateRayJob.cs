using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Culling.Jobs
{
    public struct CreateRayJob : IJobParallelFor
    {
        [ReadOnly] public float3 position;
        [ReadOnly] public quaternion rotation;
        [ReadOnly] public float maxDistance;
        [ReadOnly] public int directionsOffsetIndex;
        [ReadOnly] public NativeArray<float3> rayDirs;
        [WriteOnly] public NativeArray<RaycastCommand> rayCommands;

        public void Execute(int index)
        {
            var direction = math.mul(rotation, rayDirs[directionsOffsetIndex + index]);
            var command = new RaycastCommand(position, direction, maxDistance);
            rayCommands[index] = command;
        }
    }
}