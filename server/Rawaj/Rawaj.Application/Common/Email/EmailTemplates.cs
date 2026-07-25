namespace Rawaj.Application.Common.Email;

public record EmailContent(string Subject, string Html, string PlainText);

/// <summary>
/// Every template renders both an HTML and a plain-text body — a real, hand-written plain-text
/// alternative (not an auto-stripped one) is one of the strongest signals for landing in the
/// inbox instead of spam, alongside table-based layout, a meaningful text-to-link ratio, and
/// avoiding spam-trigger wording (ALL CAPS, "free", excessive exclamation marks, etc.).
/// </summary>
public static class EmailTemplates
{
    public static EmailContent Welcome(string fullName)
    {
        var html = $$"""
            <table dir="rtl" width="100%" cellpadding="0" cellspacing="0" style="font-family: Tahoma, Arial, sans-serif; background:#f4f4f7; padding:24px 0;">
              <tr><td align="center">
                <table width="480" cellpadding="0" cellspacing="0" style="background:#ffffff; border-radius:12px; padding:32px;">
                  <tr><td>
                    <h2 style="margin:0 0 16px; color:#111827;">أهلاً بك في رواج، {{fullName}}</h2>
                    <p style="margin:0 0 12px; color:#374151; line-height:1.6;">
                      تم إنشاء حسابك بنجاح. يمكنك الآن البدء في إدارة حملاتك التسويقية وتوليد المحتوى بالذكاء الاصطناعي.
                    </p>
                    <p style="margin:24px 0 0; color:#9ca3af; font-size:13px;">فريق رواج</p>
                  </td></tr>
                </table>
              </td></tr>
            </table>
            """;

        var text = $"""
            أهلاً بك في رواج، {fullName}

            تم إنشاء حسابك بنجاح. يمكنك الآن البدء في إدارة حملاتك التسويقية وتوليد المحتوى بالذكاء الاصطناعي.

            فريق رواج
            """;

        return new EmailContent("أهلاً بك في رواج", html, text);
    }

    public static EmailContent TeamInvite(string inviterName, string tenantName, string role, string inviteUrl)
    {
        var html = $$"""
            <table dir="rtl" width="100%" cellpadding="0" cellspacing="0" style="font-family: Tahoma, Arial, sans-serif; background:#f4f4f7; padding:24px 0;">
              <tr><td align="center">
                <table width="480" cellpadding="0" cellspacing="0" style="background:#ffffff; border-radius:12px; padding:32px;">
                  <tr><td>
                    <h2 style="margin:0 0 16px; color:#111827;">دعوة للانضمام إلى {{tenantName}}</h2>
                    <p style="margin:0 0 12px; color:#374151; line-height:1.6;">
                      دعاك <strong>{{inviterName}}</strong> للانضمام إلى فريق <strong>{{tenantName}}</strong> على رواج بصلاحية {{role}}.
                    </p>
                    <table cellpadding="0" cellspacing="0" style="margin:20px 0;">
                      <tr><td style="background:#7c3aed; border-radius:8px;">
                        <a href="{{inviteUrl}}" style="display:inline-block; padding:12px 28px; color:#ffffff; text-decoration:none; font-weight:bold;">
                          عرض الدعوة
                        </a>
                      </td></tr>
                    </table>
                    <p style="margin:0; color:#9ca3af; font-size:13px;">
                      إذا لم يعمل الزر، انسخ هذا الرابط: {{inviteUrl}}
                    </p>
                    <p style="margin:24px 0 0; color:#9ca3af; font-size:13px;">فريق رواج</p>
                  </td></tr>
                </table>
              </td></tr>
            </table>
            """;

        var text = $"""
            دعوة للانضمام إلى {tenantName}

            دعاك {inviterName} للانضمام إلى فريق {tenantName} على رواج بصلاحية {role}.

            لعرض الدعوة، افتح هذا الرابط:
            {inviteUrl}

            فريق رواج
            """;

        return new EmailContent($"دعوة للانضمام إلى {tenantName} على رواج", html, text);
    }

    public static EmailContent EmailVerificationOtp(string code)
    {
        var html = $$"""
            <table dir="rtl" width="100%" cellpadding="0" cellspacing="0" style="font-family: Tahoma, Arial, sans-serif; background:#f4f4f7; padding:24px 0;">
              <tr><td align="center">
                <table width="480" cellpadding="0" cellspacing="0" style="background:#ffffff; border-radius:12px; padding:32px;">
                  <tr><td>
                    <h2 style="margin:0 0 16px; color:#111827;">تأكيد البريد الإلكتروني</h2>
                    <p style="margin:0 0 12px; color:#374151; line-height:1.6;">
                      استخدم الرمز التالي لتأكيد بريدك الإلكتروني. صلاحية الرمز 10 دقائق.
                    </p>
                    <table cellpadding="0" cellspacing="0" style="margin:20px 0;">
                      <tr><td style="background:#f4f4f7; border-radius:8px; padding:16px 28px;">
                        <span style="font-size:32px; font-weight:bold; letter-spacing:8px; color:#111827;">{{code}}</span>
                      </td></tr>
                    </table>
                    <p style="margin:0; color:#9ca3af; font-size:13px;">
                      إذا لم تطلب هذا الرمز، يمكنك تجاهل هذه الرسالة بأمان.
                    </p>
                    <p style="margin:24px 0 0; color:#9ca3af; font-size:13px;">فريق رواج</p>
                  </td></tr>
                </table>
              </td></tr>
            </table>
            """;

        var text = $"""
            تأكيد البريد الإلكتروني

            استخدم الرمز التالي لتأكيد بريدك الإلكتروني. صلاحية الرمز 10 دقائق.

            {code}

            إذا لم تطلب هذا الرمز، يمكنك تجاهل هذه الرسالة بأمان.

            فريق رواج
            """;

        return new EmailContent("رمز تأكيد البريد الإلكتروني - رواج", html, text);
    }
}
