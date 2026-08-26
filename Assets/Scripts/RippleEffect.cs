using UnityEngine;

// 波紋エフェクト用プレハブにアタッチする。
// RippleRingシェーダーを使ったMaterialを持つRenderer(Quadなど)が同じオブジェクトに必要。
public class RippleEffect : MonoBehaviour
{
    [Header("Timing")]
    public float duration = 1.2f;
    public float maxRadius = 5f;

    Renderer rend;
    MaterialPropertyBlock propBlock;
    float timer;

    static readonly int ProgressId = Shader.PropertyToID("_Progress");
    static readonly int MaxRadiusId = Shader.PropertyToID("_MaxRadius");

    void Awake()
    {
        rend = GetComponent<Renderer>();
        propBlock = new MaterialPropertyBlock();
    }

    void Update()
    {
        timer += Time.deltaTime;
        float progress = Mathf.Clamp01(timer / duration);

        if (rend != null)
        {
            rend.GetPropertyBlock(propBlock);
            propBlock.SetFloat(ProgressId, progress);
            propBlock.SetFloat(MaxRadiusId, maxRadius);
            rend.SetPropertyBlock(propBlock);
        }

        if (progress >= 1f)
        {
            Destroy(gameObject);
        }
    }
}