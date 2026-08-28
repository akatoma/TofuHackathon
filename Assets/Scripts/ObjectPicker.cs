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

    [Header("Layer Settings")]
    public string heldObjectLayerName = "HeldObject"; // 持ち手時に適用するレイヤー名
    private int originalLayer;                        // 持ち上げる前の元レイヤーを記憶

    [Header("Camera Clipping Prevention")]
    public float minHoldDistance = 1.2f;    // カメラからの最低距離（めり込み防止）

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

    private Vector3 lastHoldPosition;
    public Vector3 HeldObjectVelocity { get; private set; }

    void Start()
    {
        Transform camTransform = Camera.main != null ? Camera.main.transform : transform;
        lastRotation = camTransform.rotation;
    }

    void Update()
    {
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
        else if (heldObject != null && Input.GetMouseButtonDown(throwButton))
        {
            ThrowObject();
        }

        if (heldObject != null && holdPosition != null)
        {
            UpdateCentrifugalOffset();
        }
        else
        {
            Transform camTransform = Camera.main != null ? Camera.main.transform : transform;
            lastRotation = camTransform.rotation;
            HeldObjectVelocity = Vector3.zero;
        }
    }

    void UpdateCentrifugalOffset()
    {
        Transform camTransform = Camera.main != null ? Camera.main.transform : transform;

        float angleDiff = Quaternion.Angle(camTransform.rotation, lastRotation);
        float rotationSpeed = Time.deltaTime > 0f ? angleDiff / Time.deltaTime : 0f;
        lastRotation = camTransform.rotation;

        float speedFactor = Mathf.InverseLerp(rotationSpeedThreshold, maxRotationSpeed, rotationSpeed);
        float targetExtend = speedFactor * maxExtendDistance;

        currentExtend = Mathf.Lerp(currentExtend, targetExtend, Time.deltaTime * returnSmoothness);

        Vector3 basePos = holdPosition.position;
        Vector3 minCamPos = camTransform.position + camTransform.forward * minHoldDistance;

        if (Vector3.Distance(camTransform.position, basePos) < minHoldDistance)
        {
            basePos = minCamPos;
        }

        Vector3 targetPos = basePos + camTransform.forward * currentExtend;
        Vector3 finalPos = PreventCameraClipping(camTransform.position, targetPos);

        if (Time.deltaTime > 0f)
        {
            HeldObjectVelocity = (finalPos - lastHoldPosition) / Time.deltaTime;
        }
        lastHoldPosition = finalPos;

        heldObject.transform.position = finalPos;
        heldObject.transform.rotation = holdPosition.rotation;
    }

    Vector3 PreventCameraClipping(Vector3 camPos, Vector3 targetPos)
    {
        Vector3 dir = targetPos - camPos;
        float dist = dir.magnitude;

        Collider heldCol = heldObject != null ? heldObject.GetComponent<Collider>() : null;
        if (heldCol != null) heldCol.enabled = false;

        RaycastHit hit;
        if (Physics.Raycast(camPos, dir.normalized, out hit, dist, raycastLayer))
        {
            targetPos = hit.point - dir.normalized * 0.2f;
        }

        if (heldCol != null) heldCol.enabled = true;

        return targetPos;
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

        // 元のレイヤーを保存して HeldObject レイヤーへ変更（子オブジェクト含む）
        originalLayer = obj.layer;
        SetLayerRecursively(heldObject, LayerMask.NameToLayer(heldObjectLayerName));

        if (heldRigidbody != null)
        {
            heldRigidbody.isKinematic = true;
        }

        currentExtend = 0f;
        Transform camTransform = Camera.main != null ? Camera.main.transform : transform;
        lastRotation = camTransform.rotation;
        lastHoldPosition = holdPosition.position;

        OnObjectPicked?.Invoke();
    }

    void DropObject()
    {
        if (heldObject == null) return;

        // レイヤーを元に戻す
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

        // レイヤーを元に戻す
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

    // 子オブジェクト含め再帰的にレイヤーを変更するヘルパー関数
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