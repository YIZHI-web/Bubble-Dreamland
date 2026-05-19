using UnityEngine;

public class DeathZoneController : MonoBehaviour
{
    [Header("Death Zone Settings")]
    public bool useTrigger = true;
    public bool showGizmos = true;
    public Color gizmoColor = new Color(1f, 0f, 0f, 0.3f);

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!useTrigger) return;

        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null && !player.IsDead())
            {
                player.Die();
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (useTrigger) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null && !player.IsDead())
            {
                player.Die();
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = gizmoColor;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.DrawCube(transform.position, col.bounds.size);
        }
        else
        {
            Gizmos.DrawCube(transform.position, Vector3.one);
        }
    }
}
