using System.Collections;
using UnityEngine;

// 味方(モブ)にアタッチする。AllySpawnerで配置されるPrefabに付けておく。
// 敵から距離をとって逃げる/怖くて震えて動けなくなる/クリア時に喜んでジャンプする、を行う。
[RequireComponent(typeof(Rigidbody))]
public class AllyController : MonoBehaviour
{
    public enum BehaviorType
    {
        Flee,     // 敵から逃げる
        Cowardly  // 敵が近づくと震えて動けなくなる
    }

    [Header("Behavior")]
    public BehaviorType behavior = BehaviorType.Flee;

    [Header("Enemy Detection")]
    public string enemyTag = "Enemy";
    public float detectionRadius = 8f;   // この範囲内の敵を意識する
    public float fleeDistance = 5f;      // この距離より近い敵に反応する
    public float checkInterval = 0.3f;   // 敵の再探索間隔(秒。毎フレーム探すと重いので間引く)

    [Header("Flee Movement")]
    public float fleeSpeed = 4f;

    [Header("Cowardly (震え)")]
    public float trembleAmount = 0.05f;  // 震え幅
    public float trembleSpeed = 25f;     // 震えの速さ

    [Header("Victory Jump")]
    public float jumpForce = 5f;
    public float jumpInterval = 0.6f; // ジャンプとジャンプの間隔
    public int jumpCount = 3;         // 何回ジャンプするか

    Rigidbody rb;
    Transform nearestEnemy;
    Vector3 originalLocalPosition; // 震え演出の基準位置
    bool isCleared = false;
    bool isTrembling = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        originalLocalPosition = transform.localPosition;
    }

    void OnEnable()
    {
        MissionManager.OnMissionCleared += HandleMissionCleared;
        StartCoroutine(EnemySearchRoutine());
    }

    void OnDisable()
    {
        MissionManager.OnMissionCleared -= HandleMissionCleared;
    }

    void HandleMissionCleared()
    {
        isCleared = true;
        StopAllCoroutines(); // 敵探索・震えなど、通常時の行動を止める
        SetTrembling(false);
        StartCoroutine(VictoryJumpRoutine());
    }

    IEnumerator EnemySearchRoutine()
    {
        while (true)
        {
            FindNearestEnemy();
            yield return new WaitForSeconds(checkInterval);
        }
    }

    void FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        float nearestDist = float.MaxValue;
        Transform nearest = null;

        foreach (GameObject enemy in enemies)
        {
            if (!enemy.activeInHierarchy)
            {
                continue;
            }

            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < nearestDist && dist <= detectionRadius)
            {
                nearestDist = dist;
                nearest = enemy.transform;
            }
        }

        nearestEnemy = nearest;
    }

    void FixedUpdate()
    {
        if (isCleared)
        {
            return; // クリア後はVictoryJumpRoutineに任せる
        }

        if (nearestEnemy == null)
        {
            SetTrembling(false);
            return;
        }

        float distance = Vector3.Distance(transform.position, nearestEnemy.position);

        if (behavior == BehaviorType.Cowardly)
        {
            SetTrembling(distance <= fleeDistance);
            return; // 震えるだけで移動はしない
        }

        // Flee行動: 近ければ敵と反対方向へ逃げる
        if (distance <= fleeDistance)
        {
            Vector3 direction = transform.position - nearestEnemy.position;
            direction.y = 0f;
            direction.Normalize();

            rb.MovePosition(rb.position + direction * fleeSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(Quaternion.LookRotation(direction, Vector3.up));
        }
    }

    void SetTrembling(bool on)
    {
        if (isTrembling == on)
        {
            return;
        }

        isTrembling = on;

        if (on)
        {
            StartCoroutine(TrembleRoutine());
        }
    }

    IEnumerator TrembleRoutine()
    {
        while (isTrembling)
        {
            float offsetX = (Mathf.PerlinNoise(Time.time * trembleSpeed, 0f) - 0.5f) * 2f * trembleAmount;
            float offsetZ = (Mathf.PerlinNoise(0f, Time.time * trembleSpeed) - 0.5f) * 2f * trembleAmount;

            transform.localPosition = originalLocalPosition + new Vector3(offsetX, 0f, offsetZ);

            yield return null;
        }

        transform.localPosition = originalLocalPosition;
    }

    IEnumerator VictoryJumpRoutine()
    {
        for (int i = 0; i < jumpCount; i++)
        {
            Vector3 v = rb.velocity;
            v.y = 0f;
            rb.velocity = v;

            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            yield return new WaitForSeconds(jumpInterval);
        }
    }
}