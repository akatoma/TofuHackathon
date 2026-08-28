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
    [Header("SMTP Settings")]
    public string smtpHost = "smtp.gmail.com";
    public int smtpPort = 587;
    public string smtpUser = "your-account@gmail.com";
    public string smtpPassword = ""; // Gmailの場合、通常のログインパスワードではなく「アプリパスワード」を使用

    [Header("Mail Content")]
    public string fromAddress = "your-account@gmail.com";
    public string fromDisplayName = "ゲーム名";
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

        MimeMessage message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromDisplayName, fromAddress));
        message.To.Add(MailboxAddress.Parse(toAddress));
        message.Subject = subject;
        message.Body = new TextPart(TextFormat.Plain)
        {
            Text = body
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