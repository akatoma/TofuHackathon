using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 15f;

    [Header("Mouse Look (Yaw)")]
    public float mouseSensitivity = 220f;
    public bool lockCursor = true;

    // セーブ/巻き戻し(PlayerSnapshot)からこの値を読み書きできるようにする。
    // これをRigidbodyの回転と一緒に復元しないと、次のFixedUpdateで
    // 巻き戻し前のyawに上書きされてしまう
    public float Yaw
    {
        get => yaw;
        set => yaw = value;
    }

    Rigidbody rb;
    Vector3 inputDirection = Vector3.zero;
    float yaw;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // 衝突などで物理的に転倒しないようにする

        yaw = transform.eulerAngles.y;

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        inputDirection = new Vector3(h, 0f, v);

        if (inputDirection.sqrMagnitude > 1f)
        {
            inputDirection.Normalize();
        }

        // 一人称視点なので、体の左右回転(Yaw)はプレイヤー自身がマウスXから直接受け取る
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
    }

    void FixedUpdate()
    {
        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
        rb.MoveRotation(rotation);

        Vector3 forward = rotation * Vector3.forward;
        Vector3 right = rotation * Vector3.right;
        Vector3 move = forward * inputDirection.z + right * inputDirection.x;

        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();
        }

        rb.MovePosition(rb.position + move * moveSpeed * Time.fixedDeltaTime);
    }
}