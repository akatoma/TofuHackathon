using UnityEngine;

// Playerにアタッチする。位置・向き(yaw/pitch)・速度を保存/復元する。
// 体力など他のパラメータが増えたら、Stateクラスに項目を追加して
// CaptureSnapshot/RestoreSnapshotに書き足せば拡張できる。
[RequireComponent(typeof(Rigidbody))]
public class PlayerSnapshot : MonoBehaviour, ISnapshotable
{
    [Header("References")]
    public PlayerController playerController;
    public CameraController cameraController; // Head側にアタッチされているもの

    Rigidbody rb;

    class State
    {
        public Vector3 position;
        public float yaw;
        public float pitch;
        public Vector3 velocity;
        public Vector3 angularVelocity;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }
        if (cameraController == null)
        {
            cameraController = GetComponentInChildren<CameraController>();
        }
    }

    public object CaptureSnapshot()
    {
        return new State
        {
            position = rb.position,
            yaw = playerController.Yaw,
            pitch = cameraController.Pitch,
            velocity = rb.velocity,       // Unity 2023.2以前は rb.velocity に読み替え
            angularVelocity = rb.angularVelocity
        };
    }

    public void RestoreSnapshot(object snapshot)
    {
        if (snapshot is not State state)
        {
            return;
        }

        rb.position = state.position;
        rb.rotation = Quaternion.Euler(0f, state.yaw, 0f);
        rb.velocity = state.velocity;      // Unity 2023.2以前は rb.velocity に読み替え
        rb.angularVelocity = state.angularVelocity;

        // Rigidbodyの回転だけでなく、PlayerController/CameraControllerが
        // 内部で持っているyaw/pitchも一緒に書き戻す(これをしないと
        // 次のフレームで元の向きに上書きされてしまう)
        playerController.Yaw = state.yaw;
        cameraController.Pitch = state.pitch;
    }
}