using UnityEngine;

// シーン内の空オブジェクト(例: "AllySpawner")にアタッチする。
// 指定したAlly Prefab(複数種類可)を、指定した複数のSpawn Pointへそれぞれ配置する。
public class AllySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject[] allyPrefabs; // 配置したい味方のPrefab(複数種類でもOK。1種類だけでも可)
    public Transform[] spawnPoints;  // 配置したい場所。シーン上に空オブジェクトを置いてドラッグする
    public Transform spawnParent;    // 生成したインスタンスの親(任意。整理用。未設定ならHierarchy直下)
    public bool spawnOnStart = true;

    void Start()
    {
        if (spawnOnStart)
        {
            SpawnAll();
        }
    }

    // 手動で呼び出したい場合(リトライ時の再配置など)にも使えるよう公開しておく
    public void SpawnAll()
    {
        if (allyPrefabs == null || allyPrefabs.Length == 0)
        {
            Debug.LogWarning("[AllySpawner] Ally Prefabsが設定されていません。");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[AllySpawner] Spawn Pointsが設定されていません。");
            return;
        }

        foreach (Transform point in spawnPoints)
        {
            if (point == null)
            {
                continue;
            }

            SpawnAt(point);
        }
    }

    void SpawnAt(Transform point)
    {
        GameObject prefab = allyPrefabs[Random.Range(0, allyPrefabs.Length)];
        GameObject instance = Instantiate(prefab, point.position, point.rotation);

        if (spawnParent != null)
        {
            instance.transform.SetParent(spawnParent);
        }
    }

    // Sceneビューでスポーン地点を確認できるようにする
    void OnDrawGizmosSelected()
    {
        if (spawnPoints == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        foreach (Transform point in spawnPoints)
        {
            if (point == null)
            {
                continue;
            }

            Gizmos.DrawWireSphere(point.position, 0.5f);
            Gizmos.DrawLine(point.position, point.position + Vector3.up * 1.5f);
        }
    }
}