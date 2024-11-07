using System.Linq;
using UnityEngine;

namespace Digger
{
    public static class DiggerPhysics
    {
        private static readonly Collider[] BufferOverlap = new Collider[128];

        public static bool Raycast(Ray ray, out RaycastHit raycastHit, float maxDistance)
        {
            var hits = Physics.RaycastAll(ray, maxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                              .OrderBy(h => h.distance)
                              .ToArray();

            foreach (var hit in hits) {
                if (hit.collider is TerrainCollider && IsInColliderHole(hit.point)) {
                    continue;
                }

                raycastHit = hit;
                return true;
            }

            raycastHit = default;
            return false;
        }

        private static bool IsInColliderHole(Vector3 point)
        {
            var count = Physics.OverlapSphereNonAlloc(point, 0.1f, BufferOverlap, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            for (var i = 0; i < count; i++) {
                var collider = BufferOverlap[i];
                if (collider.isTrigger && collider.GetComponent<TerrainColliderHoleComponent>()) return true;
            }

            return false;
        }
    }
}