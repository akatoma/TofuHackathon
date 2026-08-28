using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// シーン内の空オブジェクト(例: "MobNameAssigner")にアタッチする。
// ゲーム開始時、味方(Ally)に名前を1体ずつ重複なく割り振る。
// プレイヤー登録名(MobNameRegistry)が足りない分は、Fallback Namesからランダムに補う。
public class MobNameAssigner : MonoBehaviour
{
    [Header("Fallback Names")]
    [Tooltip("プレイヤーの登録名が足りない場合、ここからランダムに補う(画面には表示されない)")]
    public List<string> fallbackNames = new List<string>();

    [Header("Ally Detection")]
    public string allyTag = "Ally";

    void Start()
    {
        // AllySpawnerとのStart()実行順序に依存しないよう、1フレーム待ってから探す
        StartCoroutine(AssignNamesNextFrame());
    }

    IEnumerator AssignNamesNextFrame()
    {
        yield return null;
        AssignNames();
    }

    void AssignNames()
    {
        GameObject[] allies = GameObject.FindGameObjectsWithTag(allyTag);

        List<string> playerNames = new List<string>(MobNameRegistry.Names);
        Shuffle(playerNames);

        List<string> fallbackPool = new List<string>(fallbackNames);
        Shuffle(fallbackPool);
        int fallbackIndex = 0;

        foreach (GameObject ally in allies)
        {
            AllyController allyController = ally.GetComponent<AllyController>();
            if (allyController == null)
            {
                Debug.LogWarning($"[MobNameAssigner] {ally.name} に AllyController がアタッチされていません。スキップします。");
                continue;
            }

            if (playerNames.Count > 0)
            {
                // プレイヤーが登録した名前を優先して、1体につき1つ・重複なく割り振る
                string name = playerNames[0];
                playerNames.RemoveAt(0);
                allyController.SetName(name, isPlayerNamed: true);
            }
            else if (fallbackPool.Count > 0)
            {
                // 足りない分は開発側の予備名から補う(表示はされない)
                string name = fallbackPool[fallbackIndex % fallbackPool.Count];
                fallbackIndex++;
                allyController.SetName(name, isPlayerNamed: false);
            }
        }
    }

    void Shuffle(List<string> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}