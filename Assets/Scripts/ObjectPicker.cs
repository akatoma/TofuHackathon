using System;
using UnityEngine;

public class ObjectPicker : MonoBehaviour
{
    [Header("Pick Settings")]
    public string targetTag = "Pickable";
    public float pickRange = 3f;
    public KeyCode pickKey = KeyCode.F;
    public int throwButton = 0;             // 0: 左クリック（投げ）
    public float throwForce = 15f;          // 投擲力

    [Header("Camera Offset Settings")]
    // カメラから見た画面上の固定相対位置（X: 右+, Y: 上+, Z: 前方+）
    public Vector3 holdOffset = new Vector3(0.3f, -0.2f, 1.5f);

    [Header("Layer Settings")]
    public string heldObjectLayerName = "HeldObject";
    private int originalLayer;

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

    public bool IsHoldingObject => heldObject != null;

    private Vector3 lastWorldPosition;
    public Vector3 HeldObjectVelocity { get; private set; } // 敵への攻撃判定に使う速度(m/s)

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
    }

    // カメラの視点移動（Update / LateUpdate）が完了した後に実行して位置ずれを防ぐ
    void LateUpdate()
    {
        if (heldObject != null)
        {
            UpdateHoldObjectPosition();
        }
        else
        {
            Transform camTransform = Camera.main != null ? Camera.main.transform : transform;
            lastRotation = camTransform.rotation;
            HeldObjectVelocity = Vector3.zero;
        }
    }

    void UpdateHoldObjectPosition()
    {
        Transform camTransform = Camera.main != null ? Camera.main.transform : transform;

        // 1. カメラの旋回速度（角速度 deg/s）を算出
        float angleDiff = Quaternion.Angle(camTransform.rotation, lastRotation);
        float rotationSpeed = Time.deltaTime > 0f ? angleDiff / Time.deltaTime : 0f;
        lastRotation = camTransform.rotation;

        // 2. 遠心力による伸び量を計算（Z軸前方方向）
        float speedFactor = Mathf.InverseLerp(rotationSpeedThreshold, maxRotationSpeed, rotationSpeed);
        float targetExtend = speedFactor * maxExtendDistance;
        currentExtend = Mathf.Lerp(currentExtend, targetExtend, Time.deltaTime * returnSmoothness);

        // 3. カメラの現在の「位置」と「向き」から、正確なワールド目標位置を直接計算
        // 子要素にせず毎フレーム計算することで、カメラ回転によるおかしな移動半径の影響を受けません
        Vector3 localOffset = holdOffset;
        localOffset.z += currentExtend; // 振り回した時だけ前方（Z）に伸ばす

        // カメラのワールド位置 ＋ (カメラの回転 * ローカルオフセット)
        Vector3 targetWorldPos = camTransform.position + (camTransform.rotation * localOffset);

        // 4. オブジェクトのワールド移動速度（m/s）を記録（敵との衝突ダメージ計算用）
        if (Time.deltaTime > 0f)
        {
            HeldObjectVelocity = (targetWorldPos - lastWorldPosition) / Time.deltaTime;
        }
        lastWorldPosition = targetWorldPos;

        // 5. ワールド座標と回転を直接適用（親子関係なし）
        heldObject.transform.position = targetWorldPos;
        heldObject.transform.rotation = camTransform.rotation;
    }

    void TryPickObject()
    {
        Transform camTransform = Camera.main != null ? Camera.main.transform : transform;
        Ray ray = new Ray(camTransform.position, camTransform.forward);
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

        // プレイヤー等の衝突で吹っ飛ばないようレイヤー変更
        originalLayer = obj.layer;
        SetLayerRecursively(heldObject, LayerMask.NameToLayer(heldObjectLayerName));

        if (heldRigidbody != null)
        {
            heldRigidbody.isKinematic = true;
        }

        currentExtend = 0f;
        Transform camTransform = Camera.main != null ? Camera.main.transform : transform;
        lastRotation = camTransform.rotation;

        // 初回位置のセット
        Vector3 initialWorldPos = camTransform.position + (camTransform.rotation * holdOffset);
        heldObject.transform.position = initialWorldPos;
        heldObject.transform.rotation = camTransform.rotation;
        lastWorldPosition = initialWorldPos;

        OnObjectPicked?.Invoke();
    }

    void DropObject()
    {
        if (heldObject == null) return;

        SetLayerRecursively(heldObject, originalLayer);

        if (heldRigidbody != null)
        {
            heldRigidbody.isKinematic = false;
        }

        heldObject = null;
        heldRigidbody = null;
        currentExtend = 0f;
        HeldObjectVelocity = Vector3.zero;

        OnObjectDropped?.Invoke();
    }

    void ThrowObject()
    {
        if (heldObject == null) return;

        GameObject thrownObj = heldObject;
        Rigidbody rb = heldRigidbody;

        SetLayerRecursively(thrownObj, originalLayer);

        heldObject = null;
        heldRigidbody = null;
        currentExtend = 0f;
        HeldObjectVelocity = Vector3.zero;

        if (rb != null)
        {
            rb.isKinematic = false;

            Vector3 throwDirection = Camera.main != null ? Camera.main.transform.forward : transform.forward;
            rb.AddForce(throwDirection * throwForce, ForceMode.Impulse);
        }

        OnObjectThrown?.Invoke();
    }

    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}