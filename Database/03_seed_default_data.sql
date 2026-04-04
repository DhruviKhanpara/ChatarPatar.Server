-- ══════════════════════════════════════════════════════════════
--  SECTION 1 — Notification Template Data
-- ══════════════════════════════════════════════════════════════

 
USE [ChatarPatar]
GO
INSERT [dbo].[NotificationTemplates] ([Id], [Name], [TemplateType], [SubjectText], [BodyText], [IsActive]) VALUES (N'dbc6351e-a92d-f111-bab6-a0d3c12b0a12', N'Organization Invite', N'Email', N'You''ve been invited to join {{OrgName}} on ChatarPatar', N'<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
  <title>Organisation Invite</title>
  <style>
    body { margin: 0; padding: 0; background-color: #f4f4f7; font-family: Arial, sans-serif; }
    .wrapper { max-width: 600px; margin: 40px auto; background: #ffffff; border-radius: 8px; overflow: hidden; }
    .header { background-color: #4f46e5; padding: 32px 40px; text-align: center; }
    .header h1 { color: #ffffff; margin: 0; font-size: 24px; }
    .body { padding: 32px 40px; color: #374151; }
    .body p { font-size: 16px; line-height: 1.6; margin: 0 0 16px; }
    .token-box {
      margin: 16px 0;
      padding: 14px;
      background-color: #f3f4f6;
      border: 1px dashed #d1d5db;
      border-radius: 6px;
      font-size: 16px;
      font-weight: bold;
      text-align: center;
      word-break: break-all;
    }
    .footer { padding: 20px 40px; text-align: center; color: #9ca3af; font-size: 13px; }
  </style>
</head>
<body>
  <div class="wrapper">
    <div class="header"><h1>ChatarPatar</h1></div>
    <div class="body">
      <p>Hi there,</p>
 
      <p>You have been invited to join <strong>{{OrgName}}</strong> on ChatarPatar.</p>
 
      <p>To accept the invitation, please copy the invite token below and paste it into the invitation field in the app.</p>
 
      <div class="token-box">
        {{InviteToken}}
      </div>
 
      <p>This token will expire in 7 days.</p>
 
      <p style="font-size:14px; color:#6b7280;">
        Open the app → Go to "Join Organisation" → Paste the token → Submit.
      </p>
 
      <p>If you were not expecting this invitation, you can safely ignore this email.</p>
    </div>
 
    <div class="footer">
      &copy; 2025 ChatarPatar. All rights reserved.
    </div>
  </div>
</body>
</html>', 1)
GO
 
 