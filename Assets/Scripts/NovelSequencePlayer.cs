using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

// ノベルゲーム風のセリフ演出を再生する汎用スクリプト。
// Play()で再生開始、クリック/スペースキーで1行ずつ進む。
// 全セリフ再生後、onSequenceCompleteが呼ばれる。
public class NovelSequencePlayer : MonoBehaviour
{
    [System.Serializable]
    public class Line
    {
        public string speakerName; // 空でもOK(ナレーションなど)
        [TextArea(2, 5)]
        public string text;
        public Sprite portrait; // 任意
    }

    [Header("Sequence")]
    public List<Line> lines = new List<Line>();

    [Header("UI References")]
    public GameObject panel; // この演出全体の表示/非表示を切り替えるパネル
    public TMP_Text speakerNameText;
    public TMP_Text dialogueText;
    public Image portraitImage;
    public GameObject continuePrompt; // 「クリックで進む」の表示など(任意)

    [Header("Typing Effect")]
    public float charsPerSecond = 30f;

    [Header("Events")]
    public UnityEvent onSequenceComplete; // 全セリフ再生後に呼ばれる

    int currentIndex = -1;
    bool isTyping = false;
    Coroutine typingCoroutine;

    public void Play()
    {
        if (lines == null || lines.Count == 0)
        {
            onSequenceComplete?.Invoke();
            return;
        }

        if (panel != null)
        {
            panel.SetActive(true);
        }

        currentIndex = -1;
        ShowNextLine();
    }

    void Update()
    {
        if (panel == null || !panel.activeSelf)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            OnAdvance();
        }
    }

    void OnAdvance()
    {
        if (isTyping)
        {
            SkipTyping(); // タイピング中なら、まず全文表示だけする
            return;
        }

        ShowNextLine();
    }

    void ShowNextLine()
    {
        currentIndex++;

        if (currentIndex >= lines.Count)
        {
            EndSequence();
            return;
        }

        Line line = lines[currentIndex];

        if (speakerNameText != null)
        {
            speakerNameText.text = line.speakerName;
        }

        if (portraitImage != null)
        {
            portraitImage.enabled = line.portrait != null;
            portraitImage.sprite = line.portrait;
        }

        if (continuePrompt != null)
        {
            continuePrompt.SetActive(false);
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        typingCoroutine = StartCoroutine(TypeText(line.text));
    }

    IEnumerator TypeText(string text)
    {
        isTyping = true;

        if (dialogueText != null)
        {
            dialogueText.text = "";
        }

        float delay = charsPerSecond > 0f ? 1f / charsPerSecond : 0f;

        for (int i = 0; i < text.Length; i++)
        {
            if (dialogueText != null)
            {
                dialogueText.text += text[i];
            }

            if (delay > 0f)
            {
                yield return new WaitForSecondsRealtime(delay);
            }
        }

        isTyping = false;

        if (continuePrompt != null)
        {
            continuePrompt.SetActive(true);
        }
    }

    void SkipTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        if (dialogueText != null && currentIndex >= 0 && currentIndex < lines.Count)
        {
            dialogueText.text = lines[currentIndex].text;
        }

        isTyping = false;

        if (continuePrompt != null)
        {
            continuePrompt.SetActive(true);
        }
    }

    void EndSequence()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }

        onSequenceComplete?.Invoke();
    }
}