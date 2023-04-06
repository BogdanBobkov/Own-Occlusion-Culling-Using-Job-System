using UnityEngine;

namespace Culling.Behaviours
{
    public class CullingRenderersBehaviour : MonoBehaviour
    {
        [SerializeField] private Collider _referenceCollider;
        public Collider ReferenceCollider => _referenceCollider;

        [SerializeField] private Renderer[] _referenceRenderers;

        internal int id = -1;
        internal bool isRendering = true;

        private void Start()
        {
            LightCulling.Instance.AddObjectForCulling(this);
        }

        public void SetRenderingState(bool state)
        {
            if (isRendering == state) return;
            isRendering = state;

            foreach (var referenceRenderer in _referenceRenderers)
            {
                referenceRenderer.enabled = state;
            }
        }

        private void OnDestroy()
        {
            LightCulling.Instance.RemoveObjectFromCulling(this);
        }
    }
}