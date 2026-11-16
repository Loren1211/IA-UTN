using UnityEngine;

public class HunterDebugGizmos : MonoBehaviour
{
    [SerializeField] private HunterAgent _hunter;
    [SerializeField] private bool _drawGizmos = true;

    private void OnDrawGizmos()
    {
        if (!_drawGizmos) return;

        HunterAgent hunter = _hunter != null ? _hunter : GetComponent<HunterAgent>();
        if (hunter == null) return;

        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(hunter.transform.position, hunter.MeleeAttackRadius);

        Gizmos.color = new Color(1f, 0.25f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(hunter.transform.position, hunter.RangeAttackRadius);

        if (hunter.AttackTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(hunter.transform.position, hunter.AttackTarget.transform.position);
        }

        if (hunter.GatherTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(hunter.transform.position, hunter.GatherTarget.transform.position);
        }

        if (hunter.SelectedSafeZone != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(hunter.transform.position, hunter.SelectedSafeZone.transform.position);
        }
    }
}
