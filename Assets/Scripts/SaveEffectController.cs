// using System.Collections;
// using UnityEngine;
// using UnityEngine.UI;

// // SnapshotManagerのセーブ/削除イベントに合わせて、画面暗転と波紋演出を出す。
// public class SaveEffectController : MonoBehaviour
// {
//     [Header("Wave")]
//     public Transform player;        // 波紋の発生位置(Playerをアサイン)
//     public GameObject ripplePrefab; // RippleEffectを付けたQuadのプレハブ

//     [Header("Darken Overlay")]
//     public Image darkenOverlay;     // 画面全体を覆うUI Image(初期アルファ0にしておく)
//     public float darkenTargetAlpha = 0.6f;
//     public float darkenFadeDuration = 0.5f;

//     Coroutine fadeCoroutine;

//     void OnEnable()
//     {
//         SnapshotManager.OnSnapshotSaved += HandleSaved;
//         SnapshotManager.OnSnapshotCleared += HandleCleared;

//         // 起動時、既にセーブがある状態なら暗転も即座に反映しておく
//         bool currentlySaved = SnapshotManager.Instance != null && SnapshotManager.Instance.HasSnapshot;
//         SetDarkenImmediate(currentlySaved ? darkenTargetAlpha : 0f);
//     }

//     void OnDisable()
//     {
//         SnapshotManager.OnSnapshotSaved -= HandleSaved;
//         SnapshotManager.OnSnapshotCleared -= HandleCleared;
//     }

//     void HandleSaved()
//     {
//         SpawnRipple();
//         FadeDarken(darkenTargetAlpha);
//     }

//     void HandleCleared()
//     {
//         FadeDarken(0f);
//     }

//     void SpawnRipple()
//     {
//         if (ripplePrefab == null || player == null)
//         {
//             return;
//         }

//         Instantiate(ripplePrefab, player.position - Vector3.down * 0.4f, Quaternion.identity);
//     }

//     void FadeDarken(float targetAlpha)
//     {
//         if (fadeCoroutine != null)
//         {
//             StopCoroutine(fadeCoroutine);
//         }
//         fadeCoroutine = StartCoroutine(FadeDarkenRoutine(targetAlpha));
//     }

//     IEnumerator FadeDarkenRoutine(float targetAlpha)
//     {
//         if (darkenOverlay == null)
//         {
//             yield break;
//         }

//         float startAlpha = darkenOverlay.color.a;
//         float t = 0f;

//         while (t < darkenFadeDuration)
//         {
//             t += Time.deltaTime;
//             float a = Mathf.Lerp(startAlpha, targetAlpha, t / darkenFadeDuration);
//             SetDarkenImmediate(a);
//             yield return null;
//         }

//         SetDarkenImmediate(targetAlpha);
//     }

//     void SetDarkenImmediate(float alpha)
//     {
//         if (darkenOverlay == null)
//         {
//             return;
//         }

//         Color c = darkenOverlay.color;
//         c.a = alpha;
//         darkenOverlay.color = c;
//     }
// }