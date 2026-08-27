using System.Collections;
using UnityEngine;

// Rigidbodyを持つ任意のオブジェクト(BulletPrefabなど)にアタッチするだけで、
// TimeStopSkillによる「停止」対象になる汎用コンポーネント。
// 瞬間停止ではなく、短時間で減速して止まる/再加速して戻る。
[RequireComponent(typeof(Rigidbody))]
public class FreezableRigidbody : MonoBehaviour, IFreezable
{
    [Header("Freeze Transition")]
    public float freezeDuration = 0.4f;   // 停止までの減速時間(秒)
    public float unfreezeDuration = 0.4f; // 再開までの加速時間(秒)

    Rigidbody rb;

    Vector3 savedVelocity;
    Vector3 savedAngularVelocity;
    bool wasKinematic;

    Coroutine transitionCoroutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Freeze()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
        transitionCoroutine = StartCoroutine(FreezeRoutine());
    }

    public void Unfreeze()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
        transitionCoroutine = StartCoroutine(UnfreezeRoutine());
    }

    IEnumerator FreezeRoutine()
    {
        // 減速を始める瞬間の速度を保存しておく(再開時に戻すため)
        wasKinematic = rb.isKinematic;
        savedVelocity = rb.velocity;
        savedAngularVelocity = rb.angularVelocity;

        Vector3 startVel = rb.velocity;
        Vector3 startAngVel = rb.angularVelocity;
        float t = 0f;

        while (t < freezeDuration)
        {
            t += Time.deltaTime;
            float p = freezeDuration > 0f ? t / freezeDuration : 1f;
            rb.velocity = Vector3.Lerp(startVel, Vector3.zero, p);
            rb.angularVelocity = Vector3.Lerp(startAngVel, Vector3.zero, p);
            yield return null;
        }

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true; // 完全に静止させ、重力などの影響も断つ
    }

    IEnumerator UnfreezeRoutine()
    {
        rb.isKinematic = wasKinematic;

        float t = 0f;
        while (t < unfreezeDuration)
        {
            t += Time.deltaTime;
            float p = unfreezeDuration > 0f ? t / unfreezeDuration : 1f;
            rb.velocity = Vector3.Lerp(Vector3.zero, savedVelocity, p);
            rb.angularVelocity = Vector3.Lerp(Vector3.zero, savedAngularVelocity, p);
            yield return null;
        }

        rb.velocity = savedVelocity;
        rb.angularVelocity = savedAngularVelocity;
    }
}