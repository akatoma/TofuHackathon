using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance { get; private set; }

    readonly List<EnemyBullet> pool = new();

    void Awake()
    {
        Instance = this;
    }

    public EnemyBullet Spawn(GameObject prefab)
    {
        foreach (var b in pool)
        {
            if (!b.gameObject.activeSelf)
            {
                return b;
            }
        }

        GameObject obj = Instantiate(prefab);
        EnemyBullet bullet = obj.GetComponent<EnemyBullet>();
        if (bullet == null)
        {
            bullet = obj.AddComponent<EnemyBullet>();
        }

        obj.SetActive(false);
        pool.Add(bullet);
        return bullet;
    }
}

public class EnemyBullet : MonoBehaviour, ISnapshotable
{
    public int damage;
    public float lifetime;
    float remainingLifetime;
    Rigidbody bulletRb;

    class BulletState
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public int damage;
        public float remainingLifetime;
        public bool isActive;
    }

    void Awake()
    {
        bulletRb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        remainingLifetime -= Time.deltaTime;
        if (remainingLifetime <= 0f)
        {
            gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        gameObject.SetActive(false); 
    }

    // 発射
    public void Fire(Vector3 position, Quaternion rotation, Vector3 velocity, int damage, float lifetime)
    {
        transform.SetPositionAndRotation(position, rotation);
        this.damage = damage;
        this.lifetime = lifetime;
        remainingLifetime = lifetime;
        gameObject.SetActive(true);
        bulletRb.velocity = velocity;
    }

    //保存
    public object CaptureSnapshot()
    {
        return new BulletState
        {
            position = transform.position,
            rotation = transform.rotation,
            velocity = bulletRb.velocity,
            damage = damage,
            remainingLifetime = remainingLifetime,
            isActive = gameObject.activeSelf
        };
    }

    //復元
    public void RestoreSnapshot(object snapshot)
    {
        if (snapshot is not BulletState state)
        {
            return;
        }

        transform.position = state.position;
        transform.rotation = state.rotation;
        damage = state.damage;
        remainingLifetime = state.remainingLifetime;

        gameObject.SetActive(state.isActive);

        if (state.isActive)
        {
            bulletRb.velocity = state.velocity;
        }
    }
}