using UnityEngine;

public class SpawnPointGizmo : MonoBehaviour
{
    [SerializeField] private Color gizmoColor = Color.cyan;
    [SerializeField] private float gizmoSize = 0.5f;

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;

        // Dessiner une sphère
        Gizmos.DrawWireSphere(transform.position, gizmoSize);

        // Dessiner une flèche pour indiquer la direction
        Gizmos.DrawLine(transform.position, transform.position + transform.forward);

        // Afficher le nom
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * (gizmoSize + 0.2f), gameObject.name);
        #endif
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, gizmoSize * 1.2f);
    }
}