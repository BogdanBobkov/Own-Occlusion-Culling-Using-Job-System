using System.Collections.Generic;
using Common.Utils;
using Culling.Behaviours;
using Culling.Jobs;
using Culling.Utils;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Culling
{
    [RequireComponent(typeof(Camera))]
    public class LightCulling : MonoBehaviour
    {
        public static LightCulling Instance { get; private set; }

        [Header("Amount of raycasts per frame")] [SerializeField]
        public int _raycastPerFrame = 500;

        [Header("Seconds before an object without any raycast hits is considered hidden")] [SerializeField]
        public float _objectsLifetime = 1.5f;

        [Header("Screen size total count of rays divisor")] [SerializeField]
        public int _rayDirectionsDivisor = 4;

        private readonly Dictionary<int, CullingRenderersBehaviour> _behaviours = new();

        private NativeArray<float3> _rayDirections;
        private NativeList<int> _visibleObjects;
        private NativeList<int> _hitObjects;
        private NativeList<float> _timers;

        private NativeArray<RaycastCommand> _rayCommands;
        private NativeArray<RaycastHit> _hitResults;

        private JobHandle _jobHandle;

        private Camera _camera;
        private Transform _cameraTransform;

        private int _directionsOffsetIndex;

        private void CreateRayDirs()
        {
            var widthScreen = Screen.width;
            var heightScreen = Screen.height;

            var dirsCount = widthScreen * heightScreen / _rayDirectionsDivisor;

            _rayDirections = new NativeArray<float3>(dirsCount, Allocator.Persistent);

            for (var i = 0; i < dirsCount; i++)
            {
                _rayDirections[i] = _camera
                    .ViewportPointToRay(new Vector3(CullingUtils.HaltonSequence(i, 2),
                        CullingUtils.HaltonSequence(i, 3))).direction;
            }
        }

        private void Awake()
        {
            Instance = this;

            _camera = GetComponent<Camera>();
            _cameraTransform = _camera.transform;

            CreateRayDirs();

            _visibleObjects = new NativeList<int>(Allocator.Persistent);
            _hitObjects = new NativeList<int>(Allocator.Persistent);
            _rayCommands = new NativeArray<RaycastCommand>(_raycastPerFrame, Allocator.Persistent);
            _hitResults = new NativeArray<RaycastHit>(_raycastPerFrame, Allocator.Persistent);
            _timers = new NativeList<float>(Allocator.Persistent);
        }

        private void Update()
        {
            using var methodScope = new ProfilerScope("Update");

            using (new ProfilerScope("WaitForRaycasts"))
            {
                _jobHandle.Complete();
            }

            using (new ProfilerScope("CheckVisibleObjects"))
            {
                ProcessRaycastsHandler();

                new CheckVisibleBehavioursJob()
                {
                    visibleObjects = _visibleObjects,
                    hitObjects = _hitObjects,
                    timers = _timers
                }.Schedule().Complete();
                _hitObjects.Clear();

                UpdateVisibleBehaviours();
            }

            using (new ProfilerScope("UpdateTimers"))
            {
                new UpdateTimersJob
                {
                    timers = _timers,
                    deltaTime = Time.deltaTime,
                }.Schedule().Complete();
            }

#if UNITY_EDITOR

            var rayColor = new Color(0.2f, 0.9f, 0.4f, 0.5f);
            var emptyRayColor = new Color(0.9f, .25f, 0.4f, 0.5f);

            foreach (var hit in _hitResults)
            {
                Debug.DrawLine(_cameraTransform.position, hit.point, hit.collider ? rayColor : emptyRayColor, 1f);
            }

#endif
        }

        private void LateUpdate()
        {
            using var methodScope = new ProfilerScope("LateUpdate");

            if ((_directionsOffsetIndex += _raycastPerFrame) >= (_rayDirections.Length - _raycastPerFrame))
                _directionsOffsetIndex = 0;

            var createRayJob = new CreateRayJob()
            {
                position = _cameraTransform.position,
                rotation = _cameraTransform.rotation,
                maxDistance = _camera.farClipPlane,
                directionsOffsetIndex = _directionsOffsetIndex,
                rayDirs = _rayDirections,
                rayCommands = _rayCommands
            }.Schedule(_raycastPerFrame, 32);

            _jobHandle = RaycastCommand.ScheduleBatch(_rayCommands, _hitResults, 16, createRayJob);
        }

        private void ProcessRaycastsHandler()
        {
            using var methodScope = new ProfilerScope("ProcessRaycastsHandler");

            foreach (var hitResult in _hitResults)
            {
                var id = hitResult.colliderInstanceID;

                if (id == 0 || !_behaviours.ContainsKey(id)) continue;

                _hitObjects.Add(id);
            }
        }

        private void UpdateVisibleBehaviours()
        {
            for (var i = 0; i < _visibleObjects.Length; i++)
            {
                if (!_behaviours.TryGetValue(_visibleObjects[i], out var behaviour))
                {
                    _visibleObjects.RemoveAt(i);
                    _timers.RemoveAt(i);
                    continue;
                }

                if (_timers[i] > _objectsLifetime)
                {
                    behaviour.SetRenderingState(false);
                    _visibleObjects.RemoveAt(i);
                    _timers.RemoveAt(i);
                }
                else
                {
                    behaviour.SetRenderingState(true);
                }
            }
        }

        private void OnDestroy()
        {
            Instance = null;

            _rayDirections.Dispose();
            _visibleObjects.Dispose();
            _hitObjects.Dispose();

            _rayCommands.Dispose();
            _hitResults.Dispose();

            _jobHandle.Complete();

            _camera = null;
        }

        public void RemoveObjectFromCulling(CullingRenderersBehaviour behaviour)
        {
            if (!_behaviours.ContainsKey(behaviour.id)) return;
            _behaviours.Remove(behaviour.id);
        }

        public void AddObjectForCulling(CullingRenderersBehaviour behaviour)
        {
            if (_behaviours.ContainsKey(behaviour.id)) return;

            behaviour.id = behaviour.ReferenceCollider.GetInstanceID();
            _behaviours.Add(behaviour.id, behaviour);

            if (behaviour.isRendering)
                behaviour.SetRenderingState(false);
        }
    }
}