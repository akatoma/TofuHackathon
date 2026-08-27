using System.Collections;
using UnityEngine;

// Rigidbodyを持つ任意のオブジェクト(BulletPrefabなど)にアタッチするだけで、
// TimeStopSkillによる「スロー」対象になる汎用コンポーネント。
// 完全停止ではなく、指定した速度倍率までなめらかに減速/加速する。
[RequireComponent(typeof(Rigidbody))]
public class FreezableRigidbody : MonoBehaviour, IFreezable
{
    [Header("Freeze Transition")]
    public float freezeDuration = 0.4f;   // スローになるまでの時間(秒)
    public float unfreezeDuration = 1.5f; // 通常速度に戻るまでの時間(秒)。長めに設定

    Rigidbody rb;

    Vector3 savedVelocity;
    Vector3 savedAngularVelocity;
    bool isFrozen = false;

    Coroutine transitionCoroutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Freeze(float slowFactor)
    {
        slowFactor = Mathf.Clamp01(slowFactor);

        if (!isFrozen)
        {
            // 初めてスローになる瞬間の速度だけを「元の速度」として保存する
            // (既にスロー中の場合、今の遅い速度を誤って"元の速度"にしないためのガード)
            savedVelocity = rb.velocity;
            savedAngularVelocity = rb.angularVelocity;
            isFrozen = true;
        }

        StartTransition(savedVelocity * slowFactor, savedAngularVelocity * slowFactor, freezeDuration);
    }

    public void Unfreeze()
    {
        isFrozen = false;
        StartTransition(savedVelocity, savedAngularVelocity, unfreezeDuration);
    }

    // 生成直後の弾などに使う。アニメーションなしで、即座にスロー状態から始める。
    // 呼び出す前に、必ず一度「本来の速度」をrb.velocityにセットしておくこと
    public void InitializeFrozen(float slowFactor)
    {
        slowFactor = Mathf.Clamp01(slowFactor);

        savedVelocity = rb.velocity;
        savedAngularVelocity = rb.angularVelocity;
        isFrozen = true;

        rb.velocity = savedVelocity * slowFactor;
        rb.angularVelocity = savedAngularVelocity * slowFactor;
    }

    void StartTransition(Vector3 targetVelocity, Vector3 targetAngularVelocity, float duration)
    {
        if (!gameObject.activeInHierarchy)
        {
            // 非アクティブはコルーチンを開始できないため、値だけ即座に反映する
            rb.velocity = targetVelocity;
            rb.angularVelocity = targetAngularVelocity;
            return;
        }

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
        transitionCoroutine = StartCoroutine(SpeedTransitionRoutine(targetVelocity, targetAngularVelocity, duration));
    }

    IEnumerator SpeedTransitionRoutine(Vector3 targetVelocity, Vector3 targetAngularVelocity, float duration)
    {
        Vector3 startVel = rb.velocity;
        Vector3 startAngVel = rb.angularVelocity;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = duration > 0f ? t / duration : 1f;
            rb.velocity = Vector3.Lerp(startVel, targetVelocity, p);
            rb.angularVelocity = Vector3.Lerp(startAngVel, targetAngularVelocity, p);
            yield return null;
        }

        rb.velocity = targetVelocity;
        rb.angularVelocity = targetAngularVelocity;
    }
}