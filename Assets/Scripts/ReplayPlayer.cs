using System.Collections;
using UnityEngine;

// ReplayRecorderが記録した内容を、部屋の隅などに置いた固定カメラの視点で再生する。
public class ReplayPlayer : MonoBehaviour
{
    [Header("References")]
    public ReplayRecorder recorder;
    public Camera fixedCamera; // 部屋の隅に置いた固定カメラ(再生時のみ有効化)
    public Camera mainCamera;  // 通常プレイ中のカメラ(再生時のみ無効化)

    [Header("Playback")]
    public float playbackSpeed = 1f;

    bool isPlaying = false;

    // リプレイ再生ボタンのOnClick()に登録する
    public void PlayReplay()
    {
        if (isPlaying)
        {
            return;
        }

        if (recorder == null || recorder.Frames.Count == 0)
        {
            Debug.LogWarning("[ReplayPlayer] 再生できる記録がありません。");
            return;
        }

        StartCoroutine(PlaybackRoutine());
    }

    IEnumerator PlaybackRoutine()
    {
        isPlaying = true;

        var tracked = recorder.GetTrackedTransforms();
        var controllers = new MonoBehaviour[tracked.Count];

        // 各オブジェクトの通常動作(入力/AI/物理)を止めて、記録データだけで動かせるようにする
        for (int i = 0; i < tracked.Count; i++)
        {
            if (tracked[i] == null)
            {
                continue;
            }

            MonoBehaviour controller = tracked[i].GetComponent<PlayerController>();
            if (controller == null) controller = tracked[i].GetComponent<EnemyController>();
            if (controller == null) controller = tracked[i].GetComponent<AllyController>();

            controllers[i] = controller;
            if (controller != null)
            {
                controller.enabled = false;
            }

            Rigidbody rb = tracked[i].GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }
        }

        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(false);
        }
        if (fixedCamera != null)
        {
            fixedCamera.gameObject.SetActive(true);
        }

        var frames = recorder.Frames;

        for (int f = 0; f < frames.Count; f++)
        {
            ReplayFrame frame = frames[f];

            for (int i = 0; i < tracked.Count && i < frame.positions.Length; i++)
            {
                if (tracked[i] == null)
                {
                    continue;
                }

                tracked[i].gameObject.SetActive(frame.activeStates[i]);
                if (frame.activeStates[i])
                {
                    tracked[i].position = frame.positions[i];
                    tracked[i].rotation = frame.rotations[i];
                }
            }

            float waitTime = recorder.recordInterval / Mathf.Max(playbackSpeed, 0.01f);
            yield return new WaitForSeconds(waitTime);
        }

        // 再生終了後、カメラと各オブジェクトの制御を元に戻す
        // 再生終了後、カメラと各オブジェクトの制御を元に戻す
        if (fixedCamera != null)
        {
            fixedCamera.gameObject.SetActive(false);
        }
        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(true);

            EnemyController[] enemies = FindObjectsOfType<EnemyController>(includeInactive: true);
            foreach (EnemyController enemy in enemies)
            {
                if (enemy != null)
                {
                    Destroy(enemy.gameObject);
                }
            }
        }

        for (int i = 0; i < controllers.Length; i++)
        {
            if (controllers[i] != null)
            {
                controllers[i].enabled = true;
            }
        }

        isPlaying = false;
    }
}