using UnityEngine;

public class HunterProjectile : MonoBehaviour
{
    private const float Speed = 20f;
    private const float HitRadius = 0.25f;
    private const float MaxLifetime = 2f;
    private const float Scale = 0.4f;
    private const float HeightOffset = -0.25f;

    private Boid _target;
    private float _damage;
    private float _life;

    public static void Spawn(Vector3 origin, Boid target, float damage)
    {
        if (target == null) return;

        GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.name = "HunterProjectile";

        Collider collider = projectile.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
            Destroy(collider);
        }

        projectile.transform.position = origin + Vector3.up * HeightOffset;
        projectile.transform.localScale = Vector3.one * Scale;

        MeshRenderer renderer = projectile.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Color color = new(0.1f, 1f, 0.2f);
            MaterialPropertyBlock block = new();
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
        }

        HunterProjectile visual = projectile.AddComponent<HunterProjectile>();
        visual._target = target;
        visual._damage = damage;
    }

    private void Update()
    {
        _life += Time.deltaTime;

        if (_life >= MaxLifetime || _target == null || !_target.IsAlive || _target.IsGathered)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetPosition = _target.transform.position + Vector3.up * HeightOffset;
        Vector3 direction = targetPosition - transform.position;
        float distance = direction.magnitude;

        if (distance <= HitRadius)
        {
            _target.TakeDamage(_damage);
            Destroy(gameObject);
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, Speed * Time.deltaTime);
    }
}
