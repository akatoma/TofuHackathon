using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// シーン開始時から、Player・Enemy・Allyの位置/回転/表示状態を一定間隔で記録し続ける。
// 記録した内容はReplayPlayerが固定カメラ視点で再生する。
public class ReplayRecorder : MonoBehaviour
{
    [Header("Recording Targets")]
    public Transform player;
    public string enemyTag = "Enemy";
    public string allyTag = "Ally";

    [Header("Settings")]
    public float recordInterval = 0.1f; // 何秒ごとに記録するか(短いほど滑らかだが記録量が増える)
    public bool stopOnMissionCleared = true; // クリアしたら記録を止める(記録が際限なく伸びないように)

    List<Transform> trackedTransforms = new List<Transform>();
    public List<ReplayFrame> Frames { get; private set; } = new List<ReplayFrame>();

    Coroutine recordCoroutine;

    void Start()
    {
        BuildTrackedList();
        recordCoroutine = StartCoroutine(RecordRoutine());
    }

    void OnEnable()
    {
        if (stopOnMissionCleared)
        {
            MissionManager.OnMissionCleared += HandleMissionCleared;
        }
    }

    void OnDisable()
    {
        if (stopOnMissionCleared)
        {
            MissionManager.OnMissionCleared -= HandleMissionCleared;
        }
    }

    void HandleMissionCleared()
    {
        if (recordCoroutine != null)
        {
            StopCoroutine(recordCoroutine);
            recordCoroutine = null;
        }
    }

    void BuildTrackedList()
    {
        trackedTransforms.Clear();

        if (player != null)
        {
            trackedTransforms.Add(player);
        }

        foreach (GameObject enemy in GameObject.FindGameObjectsWithTag(enemyTag))
        {
            trackedTransforms.Add(enemy.transform);
        }

        foreach (GameObject ally in GameObject.FindGameObjectsWithTag(allyTag))
        {
            trackedTransforms.Add(ally.transform);
        }
    }

    IEnumerator RecordRoutine()
    {
        while (true)
        {
            RecordFrame();
            yield return new WaitForSeconds(recordInterval);
        }
    }

    void RecordFrame()
    {
        ReplayFrame frame = new ReplayFrame
        {
            time = Time.time,
            positions = new Vector3[trackedTransforms.Count],
            rotations = new Quaternion[trackedTransforms.Count],
            activeStates = new bool[trackedTransforms.Count]
        };

        for (int i = 0; i < trackedTransforms.Count; i++)
        {
            Transform t = trackedTransforms[i];
            if (t == null)
            {
                continue;
            }

            frame.positions[i] = t.position;
            frame.rotations[i] = t.rotation;
            frame.activeStates[i] = t.gameObject.activeInHierarchy;
        }

        Frames.Add(frame);
    }

    public List<Transform> GetTrackedTransforms()
    {
        return trackedTransforms;
    }
}