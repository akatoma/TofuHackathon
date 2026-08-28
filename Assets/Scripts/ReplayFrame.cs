using UnityEngine;

// リプレイの1フレーム分のデータ。ReplayRecorderが作り、ReplayPlayerが再生に使う。
[System.Serializable]
public class ReplayFrame
{
    public float time;
    public Vector3[] positions;
    public Quaternion[] rotations;
    public bool[] activeStates;
}