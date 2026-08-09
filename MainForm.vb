Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.IO
Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Text
Imports System.Text.Json.Nodes
Imports System.Threading.Tasks
Imports System.Windows.Forms

Public Class MainForm
    Inherits Form

    Private ReadOnly httpClient As New HttpClient()
    Private ReadOnly chatHistory As New List(Of (Role As String, Text As String))

    Private txtApiKey As TextBox
    Private txtModel As TextBox
    Private rtbChat As RichTextBox
    Private txtInput As TextBox
    Private btnSend As Button
    Private btnClear As Button
    Private lblStatus As Label
    Private chkShowKey As CheckBox
    Private txtSystemPrompt As TextBox

    Private ReadOnly configPath As String =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GrokChatApp", "config.txt")

    Public Sub New()
        Me.Text = "Grok Chat - VB.NET (.NET 9)"
        Me.Width = 800
        Me.Height = 720
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.Font = New Font("Segoe UI", 10)

        InitializeUI()
        LoadSavedApiKey()
    End Sub

    Private Sub InitializeUI()
        Dim topPanel As New TableLayoutPanel()
        topPanel.Dock = DockStyle.Top
        topPanel.Height = 108
        topPanel.ColumnCount = 4
        topPanel.RowCount = 3
        topPanel.Padding = New Padding(8)

        Dim lblKey As New Label() With {.Text = "API Key:", .AutoSize = True, .Anchor = AnchorStyles.Left, .Margin = New Padding(3, 8, 3, 3)}
        txtApiKey = New TextBox() With {.Width = 320, .PasswordChar = "*"c}
        chkShowKey = New CheckBox() With {.Text = "Hiện", .AutoSize = True, .Margin = New Padding(3, 8, 3, 3)}
        AddHandler chkShowKey.CheckedChanged, Sub()
                                                   txtApiKey.PasswordChar = If(chkShowKey.Checked, ControlChars.NullChar, "*"c)
                                               End Sub

        Dim lblModel As New Label() With {.Text = "Model:", .AutoSize = True, .Anchor = AnchorStyles.Left, .Margin = New Padding(3, 8, 3, 3)}
        txtModel = New TextBox() With {.Width = 220, .Text = "grok-4-fast"}

        Dim btnSaveKey As New Button() With {.Text = "Lưu Key", .AutoSize = True}
        AddHandler btnSaveKey.Click, AddressOf BtnSaveKey_Click

        Dim lblSystem As New Label() With {.Text = "System:", .AutoSize = True, .Anchor = AnchorStyles.Left, .Margin = New Padding(3, 8, 3, 3)}
        txtSystemPrompt = New TextBox() With {.Width = 560, .Text = "Bạn là một trợ lý hữu ích, trả lời ngắn gọn, rõ ràng."}

        topPanel.Controls.Add(lblKey, 0, 0)
        topPanel.Controls.Add(txtApiKey, 1, 0)
        topPanel.Controls.Add(chkShowKey, 2, 0)
        topPanel.Controls.Add(btnSaveKey, 3, 0)
        topPanel.Controls.Add(lblModel, 0, 1)
        topPanel.Controls.Add(txtModel, 1, 1)
        topPanel.Controls.Add(lblSystem, 0, 2)
        topPanel.SetColumnSpan(txtSystemPrompt, 3)
        topPanel.Controls.Add(txtSystemPrompt, 1, 2)

        rtbChat = New RichTextBox() With {
            .Dock = DockStyle.Fill,
            .ReadOnly = True,
            .BackColor = Color.White,
            .Font = New Font("Segoe UI", 10)
        }

        Dim bottomPanel As New Panel() With {.Dock = DockStyle.Bottom, .Height = 90, .Padding = New Padding(8)}
        txtInput = New TextBox() With {.Multiline = True, .Height = 60, .Dock = DockStyle.Fill}
        AddHandler txtInput.KeyDown, AddressOf TxtInput_KeyDown

        Dim btnPanel As New FlowLayoutPanel() With {.Dock = DockStyle.Right, .Width = 100, .FlowDirection = FlowDirection.TopDown}
        btnSend = New Button() With {.Text = "Gửi", .Width = 90, .Height = 28}
        AddHandler btnSend.Click, AddressOf BtnSend_Click
        btnClear = New Button() With {.Text = "Xóa chat", .Width = 90, .Height = 28}
        AddHandler btnClear.Click, Sub()
                                        chatHistory.Clear()
                                        rtbChat.Clear()
                                    End Sub
        btnPanel.Controls.Add(btnSend)
        btnPanel.Controls.Add(btnClear)

        bottomPanel.Controls.Add(txtInput)
        bottomPanel.Controls.Add(btnPanel)

        lblStatus = New Label() With {
            .Dock = DockStyle.Bottom,
            .Height = 24,
            .Text = "Sẵn sàng.",
            .TextAlign = ContentAlignment.MiddleLeft,
            .Padding = New Padding(8, 0, 0, 0)
        }

        Me.Controls.Add(rtbChat)
        Me.Controls.Add(bottomPanel)
        Me.Controls.Add(lblStatus)
        Me.Controls.Add(topPanel)
    End Sub

    Private Sub TxtInput_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter AndAlso Not e.Shift Then
            e.SuppressKeyPress = True
            BtnSend_Click(sender, e)
        End If
    End Sub

    Private Async Sub BtnSend_Click(sender As Object, e As EventArgs)
        Dim apiKey = txtApiKey.Text.Trim()
        Dim model = txtModel.Text.Trim()
        Dim userText = txtInput.Text.Trim()

        If String.IsNullOrEmpty(apiKey) Then
            MessageBox.Show("Vui lòng nhập xAI (Grok) API Key.", "Thiếu API Key", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        If String.IsNullOrEmpty(model) Then model = "grok-4-fast"
        If String.IsNullOrEmpty(userText) Then Return

        AppendMessage("Bạn", userText, Color.DarkBlue)
        chatHistory.Add(("user", userText))
        txtInput.Clear()
        SetBusy(True)

        Try
            Dim reply = Await SendToGrokAsync(apiKey, model, txtSystemPrompt.Text.Trim(), chatHistory)
            chatHistory.Add(("assistant", reply))
            AppendMessage("Grok", reply, Color.DarkGreen)
        Catch ex As Exception
            AppendMessage("Lỗi", ex.Message, Color.Red)
        Finally
            SetBusy(False)
        End Try
    End Sub

    Private Sub SetBusy(busy As Boolean)
        btnSend.Enabled = Not busy
        txtInput.Enabled = Not busy
        lblStatus.Text = If(busy, "Đang gửi...", "Sẵn sàng.")
    End Sub

    Private Sub AppendMessage(sender As String, text As String, color As Color)
        rtbChat.SelectionStart = rtbChat.TextLength
        rtbChat.SelectionLength = 0
        rtbChat.SelectionColor = color
        rtbChat.SelectionFont = New Font(rtbChat.Font, FontStyle.Bold)
        rtbChat.AppendText($"{sender}:{Environment.NewLine}")
        rtbChat.SelectionColor = Color.Black
        rtbChat.SelectionFont = New Font(rtbChat.Font, FontStyle.Regular)
        rtbChat.AppendText($"{text}{Environment.NewLine}{Environment.NewLine}")
        rtbChat.ScrollToCaret()
    End Sub

    ''' <summary>
    ''' Gửi lịch sử hội thoại tới xAI Grok API (endpoint tương thích OpenAI Chat Completions).
    ''' </summary>
    Private Async Function SendToGrokAsync(apiKey As String, model As String, systemPrompt As String,
                                            history As List(Of (Role As String, Text As String))) As Task(Of String)
        Const url As String = "https://api.x.ai/v1/chat/completions"

        Dim messagesArray As New JsonArray()

        If Not String.IsNullOrEmpty(systemPrompt) Then
            messagesArray.Add(New JsonObject From {
                {"role", "system"},
                {"content", systemPrompt}
            })
        End If

        For Each turn In history
            messagesArray.Add(New JsonObject From {
                {"role", turn.Role},
                {"content", turn.Text}
            })
        Next

        Dim requestBody As New JsonObject From {
            {"model", model},
            {"messages", messagesArray}
        }

        Dim jsonPayload = requestBody.ToJsonString()

        Using request As New HttpRequestMessage(HttpMethod.Post, url)
            request.Headers.Authorization = New AuthenticationHeaderValue("Bearer", apiKey)
            request.Content = New StringContent(jsonPayload, Encoding.UTF8, "application/json")

            Using response = Await httpClient.SendAsync(request)
                Dim responseText = Await response.Content.ReadAsStringAsync()

                If Not response.IsSuccessStatusCode Then
                    Throw New Exception($"HTTP {CInt(response.StatusCode)}: {responseText}")
                End If

                Dim node = JsonNode.Parse(responseText)
                Dim replyText = node?("choices")?(0)?("message")?("content")?.ToString()

                If String.IsNullOrEmpty(replyText) Then
                    Return "(Không có phản hồi từ mô hình. Phản hồi thô: " & responseText & ")"
                End If

                Return replyText
            End Using
        End Using
    End Function

    Private Sub BtnSaveKey_Click(sender As Object, e As EventArgs)
        Try
            Dim dir = Path.GetDirectoryName(configPath)
            If Not String.IsNullOrEmpty(dir) AndAlso Not Directory.Exists(dir) Then
                Directory.CreateDirectory(dir)
            End If
            File.WriteAllText(configPath, txtApiKey.Text.Trim())
            MessageBox.Show("Đã lưu API Key trên máy bạn (dạng văn bản thường, chỉ dùng cho mục đích cá nhân).",
                             "Đã lưu", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            MessageBox.Show($"Không thể lưu key: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub LoadSavedApiKey()
        Try
            If File.Exists(configPath) Then
                txtApiKey.Text = File.ReadAllText(configPath).Trim()
            End If
        Catch
            ' Bỏ qua lỗi đọc key, người dùng có thể nhập lại thủ công
        End Try
    End Sub

End Class
