using System.Collections.Generic;

// プレイヤーが入力した名前を、シーンをまたいで保持しておく静的なリスト。
// 最大16件。超えたら一番古いものから削除する(FIFO)。重複した名前は追加しない。
public static class MobNameRegistry
{
    public const int MaxNames = 16;

    static readonly List<string> names = new List<string>();

    public static IReadOnlyList<string> Names => names;

    public static bool AddName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return false;
        }

        fullName = fullName.Trim();

        if (names.Contains(fullName))
        {
            return false; // 重複は追加しない
        }

        if (names.Count >= MaxNames)
        {
            names.RemoveAt(0); // 一番古い名前を削除
        }

        names.Add(fullName);
        return true;
    }
}