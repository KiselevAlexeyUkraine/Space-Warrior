using UnityEngine;

namespace Components.Player
{
    public class PlayerSpawn : MonoBehaviour
    {
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(transform.position, 0.5f);
        }
#endif
    }
}
