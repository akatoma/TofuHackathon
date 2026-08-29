using System.Collections;
using UnityEngine;
using TMPro;

// 味方(モブ)にアタッチする。AllySpawnerで配置されるPrefabに付けておく。
// 敵から距離をとって逃げる/怖くて震えて動けなくなる/クリア時に喜んでジャンプする、を行う。
[RequireComponent(typeof(Rigidbody))]
public class AllyController : MonoBehaviour, ISnapshotable
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
    public float detectionRadius = 8f;
    public float fleeDistance = 5f;
    public float checkInterval = 0.3f;

    [Header("Flee Movement")]
    public float fleeSpeed = 4f;

    [Header("Cowardly (震え)")]
    public float trembleAmount = 0.05f;
    public float trembleSpeed = 25f;

    [Header("Victory Jump")]
    public float jumpForce = 5f;
    public float jumpInterval = 0.6f;
    public int jumpCount = 3;

    [Header("Name Tag")]
    public GameObject nameCanvas;
    public TMP_Text nameText;

    Rigidbody rb;
    Transform nearestEnemy;
    Vector3 originalLocalPosition;
    bool isCleared = false;
    bool isTrembling = false;
    Camera mainCamera;

    class State
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public Vector3 angularVelocity;
        public bool isCleared;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        originalLocalPosition = transform.localPosition;

        mainCamera = Camera.main;
        if (nameCanvas != null)
        {
            nameCanvas.SetActive(false);
        }
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
        StopAllCoroutines();
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
            return;
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
            return;
        }

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

    void LateUpdate()
    {
        if (nameCanvas == null || !nameCanvas.activeSelf)
        {
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }
        }

        nameCanvas.transform.forward = mainCamera.transform.forward;
    }

    public void SetName(string mobName, bool isPlayerNamed)
    {
        if (nameText != null)
        {
            nameText.text = mobName;
        }

        if (nameCanvas != null)
        {
            nameCanvas.SetActive(isPlayerNamed);
        }
    }

    //保存
    public object CaptureSnapshot()
    {
        return new State
        {
            position = rb.position,
            rotation = rb.rotation,
            velocity = rb.velocity,
            angularVelocity = rb.angularVelocity,
            isCleared = isCleared
        };
    }

    //復元
    public void RestoreSnapshot(object snapshot)
    {
        if (snapshot is not State state)
        {
            return;
        }

        rb.position = state.position;
        rb.rotation = state.rotation;
        rb.velocity = state.velocity;
        rb.angularVelocity = state.angularVelocity;

        // クリア後のジャンプ演出中に巻き戻された場合は、震え演出などが
        // 二重に走らないよう震え状態もリセットしておく
        if (isTrembling)
        {
            isTrembling = false;
            StopAllCoroutines();
            transform.localPosition = originalLocalPosition;

            // OnEnableで開始した敵探索コルーチンが巻き込まれて止まってしまうため再開する
            StartCoroutine(EnemySearchRoutine());
        }

        isCleared = state.isCleared;

        if (isCleared)
        {
            // クリア済み状態に巻き戻した場合、通常挙動を止めてジャンプ演出済みの静止状態にする
            StopAllCoroutines();
        }
    }
}