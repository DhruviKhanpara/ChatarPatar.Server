USE [ChatarPatar]
GO
INSERT [dbo].[NotificationTemplates] ([Id], [Name], [TemplateType], [SubjectText], [BodyText], [IsActive]) VALUES (N'6780a117-e32d-f111-9fe6-8c17596adfb9', N'Organization Invite', N'Email', N'You''ve been invited to join {{orgName}} on ChatarPatar', N'<!DOCTYPE html>
<html>
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">

  <meta name="color-scheme" content="light dark">
  <meta name="supported-color-schemes" content="light dark">

  <style>
    body {
      margin: 0;
      padding: 0;
      background-color: #f5f5f5;
    }

    @media only screen and (max-width: 600px) {
      .container { width: 100% !important; }
      .card-padding { padding: 20px !important; }
      .headline { font-size: 20px !important; }
    }

    @media (prefers-color-scheme: dark) {
      body { background-color: #09090b !important; }
      .email-bg { background-color: #0f0f10 !important; border-color: #1c1c1f !important; }
      .card { background-color: #1c1c1f !important; border-color: #2e2e32 !important; }
      .text-main { color: #ffffff !important; }
      .text-muted { color: #9f9fa8 !important; }
      .pill {
        background-color: #27272a !important;
        border-color: #3a3a3e !important;
        color: #9f9fa8 !important;
      }
    }
  </style>
</head>

<body>

<table width="100%" cellpadding="0" cellspacing="0" style="background:#f5f5f5;">
<tr>
<td align="center">

<table class="container" width="680" cellpadding="0" cellspacing="0" style="max-width:680px; width:100%;">
<tr>
<td style="padding:40px 16px;">

<table width="100%" cellpadding="0" cellspacing="0"
class="email-bg"
style="background:#ffffff; border:1px solid #e5e5e5; border-radius:14px;">

<tr>
<td align="center" style="padding:28px 0;">
<table>
<tr>
<td width="28" height="28"
style="background:linear-gradient(135deg,#7c6af7,#a78bfa); border-radius:6px;"></td>
<td width="8"></td>
<td class="text-main"
style="font-size:18px; font-weight:bold; font-family:Arial; color:#111;">
ChatarPatar
</td>
</tr>
</table>
</td>
</tr>

<tr>
<td class="card-padding" style="padding:32px;">
<table width="100%" cellpadding="0" cellspacing="0"
class="card"
style="background:#ffffff; border:1px solid #e5e5e5; border-radius:16px;">

<tr>
<td style="padding:28px;">

<table width="52" height="52"
style="background:linear-gradient(135deg,#312e81,#4c1d95); border-radius:12px;">
<tr>
<td align="center" style="color:#c4b5fd; font-size:20px;">
{{orgInitial}}
</td>
</tr>
</table>

<div style="height:16px;"></div>

<div class="text-muted"
style="font-size:11px; letter-spacing:1.5px; font-weight:bold; color:#6b7280;">
TEAM INVITATION
</div>

<div class="headline text-main"
style="font-size:24px; font-weight:bold; margin-top:8px; color:#111;">
You''re invited to join {{orgName}}
</div>

<div class="text-muted"
style="font-size:14px; line-height:1.6; margin-top:12px; color:#555;">
<strong>{{inviterName}}</strong> invited you to collaborate on
<strong>{{orgName}}</strong>.
Join to access shared channels, direct messages, and everything your team is working on.
</div>

<table style="margin-top:20px;">
<tr>
<td class="pill"
style="background:#f1f1f1; border:1px solid #ddd; border-radius:20px; padding:6px 12px; font-size:12px;">
{{orgName}}
</td>
<td width="8"></td>
<td class="pill"
style="background:#f1f1f1; border:1px solid #ddd; border-radius:20px; padding:6px 12px; font-size:12px;">
{{roleName}}
</td>
</tr>
</table>

<table style="margin-top:24px;">
<tr>
<td>
<a href="{{inviteLink}}"
style="background:linear-gradient(135deg,#6d5cf0,#9d87fa);
color:#ffffff; text-decoration:none;
padding:12px 24px; border-radius:10px;
font-weight:bold; display:inline-block;">
Accept Invitation →
</a>
</td>
</tr>
</table>

<div class="text-muted"
style="margin-top:16px; font-size:12px; color:#666;">
Button not working? Paste this link into your browser:<br>
<a href="{{inviteLink}}" style="color:#7c6af7;">
{{inviteLink}}
</a>
</div>

<table width="100%" style="margin-top:20px;">
<tr>
<td style="background:#fff7ed; border-left:3px solid #f59e0b; padding:10px; border-radius:6px;">
<span style="font-size:12px; color:#92400e;">
<strong>&#9203; Expires in {{expiryDays}} days.</strong>
This invitation link will stop working after that. Ask {{inviterName}} to resend if it expires.
</span>
</td>
</tr>
</table>

<div class="text-muted"
style="margin-top:24px; font-size:11px; text-align:center; color:#888;">
You received this because <strong>{{inviterName}}</strong> invited
<a href="mailto:{{recipientEmail}}">{{recipientEmail}}</a> to {{orgName}}.<br />
If this was a mistake, you can safely ignore this email.
<br><br>
© {{year}} ChatarPatar <a href="#">Privacy Policy</a>
</div>

</td>
</tr>

</table>
</td>
</tr>

</table>

</td>
</tr>
</table>

</td>
</tr>
</table>

</body>
</html>', 1)
GO
