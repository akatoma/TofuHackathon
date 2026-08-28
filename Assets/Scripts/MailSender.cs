using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

// メール送信専用スクリプト。空オブジェクト(例: "MailSender")にアタッチする。
// MailKit(要NuGetForUnity導入)を使用。System.Net.Mailの日本語エンコードバグを回避するため
// こちらに切り替えている。
//
// ⚠ 重要な注意:
// SMTPのアカウント情報(ユーザー名・パスワード)はビルドしたゲームの中に
// そのまま埋め込まれます。第三者がゲームを解析すれば読み取れてしまうため、
// 普段使っている個人メールアカウントではなく、この用途専用の
// 使い捨て/専用アカウントを用意することを強く推奨します。
public class MailSender : MonoBehaviour
{
    [System.Serializable]
    public class MailTemplate
    {
        public string subject;
        [TextArea(3, 10)]
        public string body;
    }

    [Header("SMTP Settings")]
    public string smtpHost = "smtp.gmail.com";
    public int smtpPort = 587;
    public string smtpUser = "your-account@gmail.com";
    public string smtpPassword = ""; // Gmailの場合、通常のログインパスワードではなく「アプリパスワード」を使用

    [Header("Mail Content")]
    public string fromAddress = "your-account@gmail.com";
    public string fromDisplayName = "ゲーム名"; // MobNameRegistryに登録名が無い場合のフォールバック

    [Header("Mail Templates")]
    [Tooltip("ここに複数登録しておくと、送信のたびにランダムで1つ選ばれる")]
    public List<MailTemplate> templates = new List<MailTemplate>();

    [Header("Default Subject/Body")]
    [Tooltip("Templatesが空の場合に使われるフォールバック")]
    public string subject = "【称号獲得】ゲームクリアおめでとうございます！";
    [TextArea(3, 10)]
    public string body = "この度はゲームクリア、おめでとうございます！\nあなたに称号「○○」を贈ります。";

    [Header("Events")]
    public UnityEvent onMailSent;   // 送信成功時(例: 「送信しました」表示)
    public UnityEvent onMailFailed; // 送信失敗時(例: 「送信に失敗しました」表示)

    // Buttonなどから呼び出す。送信中もゲームが固まらないよう非同期で送る
    public async void SendClearMail(string toAddress)
    {
        if (string.IsNullOrEmpty(toAddress))
        {
            Debug.LogWarning("[MailSender] 送信先メールアドレスが空です。");
            onMailFailed?.Invoke();
            return;
        }

        // Templatesが登録されていればランダムに1つ選ぶ。無ければ従来のSubject/Bodyを使う
        string mailSubject = subject;
        string mailBody = body;

        if (templates != null && templates.Count > 0)
        {
            MailTemplate template = templates[Random.Range(0, templates.Count)];
            mailSubject = template.subject;
            mailBody = template.body;
        }

        // プレイヤーが登録した名前があれば、その中からランダムに送信者名を選ぶ
        string senderName = fromDisplayName;
        if (MobNameRegistry.Names.Count > 0)
        {
            senderName = MobNameRegistry.Names[Random.Range(0, MobNameRegistry.Names.Count)];
        }

        MimeMessage message = new MimeMessage();
        message.From.Add(new MailboxAddress(senderName, fromAddress));
        message.To.Add(MailboxAddress.Parse(toAddress));
        message.Subject = mailSubject;
        message.Body = new TextPart(TextFormat.Plain)
        {
            Text = mailBody
        };

        using SmtpClient client = new SmtpClient();
        // Unity環境だとローカルホスト名の自動取得が不正な値になり、
        // GmailなどがEHLOコマンドを拒否することがあるため、明示的に指定しておく
        client.LocalDomain = "localhost";

        try
        {
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUser, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            Debug.Log($"[MailSender] Mail sent to {toAddress}");
            onMailSent?.Invoke();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MailSender] Failed to send mail: {e.Message}");
            onMailFailed?.Invoke();
        }
    }
}