using System;
using UnityEngine;

public class ObjectPicker : MonoBehaviour
{
    [Header("Pick Settings")]
    public string targetTag = "Pickable";
    public float pickRange = 3f;
    public Transform holdPosition;
    public KeyCode pickKey = KeyCode.F;
    public int throwButton = 0;             // 0: 左クリック（投げ）
    public float throwForce = 15f;          // 投擲力

    [Header("Centrifugal Force (Swing Auto-Extend)")]
    public float maxExtendDistance = 2.5f;  // 視点を最速で振った時に伸びる最大距離
    public float rotationSpeedThreshold = 100f; // 伸び始めに必要な最低旋回速度(度/秒)
    public float maxRotationSpeed = 800f;   // 最大まで伸び切る旋回速度(度/秒)
    public float returnSmoothness = 10f;   // 元の位置に戻る時のスムーズさ（追従速度）

    private Quaternion lastRotation;
    private float currentExtend = 0f;

    [Header("Raycast Layer")]
    public LayerMask raycastLayer = ~0;

    // GameManager通知用イベント
    public event Action OnObjectPicked;
    public event Action OnObjectDropped;
    public event Action OnObjectThrown;

    private GameObject heldObject = null;
    private Rigidbody heldRigidbody = null;

    // 手持ち状態フラグ（外部参照用）
    public bool IsHoldingObject => heldObject != null;

    // 手持ち中の移動速度計算用
    private Vector3 lastHoldPosition;
    public Vector3 HeldObjectVelocity { get; private set; } // 手持ちオブジェクトの移動速度

    void Start()
    {
        Transform camTransform = Camera.main != null ? Camera.main.transform : transform;
        lastRotation = camTransform.rotation;
    }

    void Update()
    {
        // Fキーで持ち上げる・離す
        if (Input.GetKeyDown(pickKey))
        {
            if (heldObject == null)
            {
                TryPickObject();
            }
            else
            {
                DropObject();
            }
        }
        // 保持中に左クリックで投げる
        else if (heldObject != null && Input.GetMouseButtonDown(throwButton))
        {
            ThrowObject();
        }

        // 保持中の遠心力計算と位置同期
        if (heldObject != null && holdPosition != null)
        {
            UpdateCentrifugalOffset();
        }
        else
        {
            // 保持していない時もカメラ回転の差分を記録しておく
            Transform camTransform = Camera.main != null ? Camera.main.transform : transform;
            lastRotation = camTransform.rotation;
            HeldObjectVelocity = Vector3.zero;
        }
    }

    void UpdateCentrifugalOffset()
    {
        Transform camTransform = Camera.main != null ? Camera.main.transform : transform;

        // 1フレームあたりの回転角度の差分（角速度: deg/sec）を算出
        float angleDiff = Quaternion.Angle(camTransform.rotation, lastRotation);
        float rotationSpeed = Time.deltaTime > 0f ? angleDiff / Time.deltaTime : 0f;
        lastRotation = camTransform.rotation;

        // 旋回速度に応じて目標の伸び量を計算
        float speedFactor = Mathf.InverseLerp(rotationSpeedThreshold, maxRotationSpeed, rotationSpeed);
        float targetExtend = speedFactor * maxExtendDistance;

        // スムーズに補間
        currentExtend = Mathf.Lerp(currentExtend, targetExtend, Time.deltaTime * returnSmoothness);

        // カメラの前方ベクトル方向に遠心力分だけ位置を伸ばす
        Vector3 extendVector = camTransform.forward * currentExtend;

        Vector3 newPos = holdPosition.position + extendVector;

        // 手持ちオブジェクトの1フレームあたりの移動速度(m/s)を算出
        if (Time.deltaTime > 0f)
        {
            HeldObjectVelocity = (newPos - lastHoldPosition) / Time.deltaTime;
        }
        lastHoldPosition = newPos;

        heldObject.transform.position = newPos;
        heldObject.transform.rotation = holdPosition.rotation;
    }

    void TryPickObject()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickRange, raycastLayer))
        {
            if (hit.collider.CompareTag(targetTag))
            {
                PickUp(hit.collider.gameObject);
            }
        }
    }

    void PickUp(GameObject obj)
    {
        heldObject = obj;
        heldRigidbody = obj.GetComponent<Rigidbody>();

        if (heldRigidbody != null)
        {
            heldRigidbody.isKinematic = true;
        }

        currentExtend = 0f;
        Transform camTransform = Camera.main != null ? Camera.main.transform : transform;
        lastRotation = camTransform.rotation;
        lastHoldPosition = holdPosition.position;

        heldObject.transform.position = holdPosition.position;
        heldObject.transform.rotation = holdPosition.rotation;

        OnObjectPicked?.Invoke(); // イベント通知
    }

    void DropObject()
    {
        if (heldObject == null) return;

        if (heldRigidbody != null)
        {
            heldRigidbody.isKinematic = false;
        }

        heldObject = null;
        heldRigidbody = null;
        currentExtend = 0f;
        HeldObjectVelocity = Vector3.zero;

        OnObjectDropped?.Invoke(); // イベント通知
    }

    void ThrowObject()
    {
        if (heldObject == null) return;

        GameObject thrownObj = heldObject;
        Rigidbody rb = heldRigidbody;

        heldObject = null;
        heldRigidbody = null;
        currentExtend = 0f;
        HeldObjectVelocity = Vector3.zero;

        if (rb != null)
        {
            rb.isKinematic = false;

            // メインカメラの視線方向、無ければ自身の前方に飛ばす
            Vector3 throwDirection = Camera.main != null ? Camera.main.transform.forward : transform.forward;
            rb.AddForce(throwDirection * throwForce, ForceMode.Impulse);
        }

        OnObjectThrown?.Invoke(); // イベント通知（ゲージ+5処理）
    }
}