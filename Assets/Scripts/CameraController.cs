using UnityEngine;

// Playerの子オブジェクト(目の高さに配置した"Head"など)にアタッチする想定。
// このスクリプト自身のTransformはPlayerに追従するので、位置は一切いじらない。
// マウスYによる上下の見回し(ピッチ)だけをローカル回転として扱う。
public class CameraController : MonoBehaviour
{
    [Header("Mouse Look (Pitch)")]
    public float mouseSensitivity = 220f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    float pitch = 0f;

    void Start()
    {
        pitch = transform.localEulerAngles.x;
    }

    void Update()
    {
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}