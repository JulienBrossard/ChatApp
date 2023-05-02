namespace Client3;
using NetworkClient;

public partial class Form1 : Form
{
    private TcpSampleClient client;
    public Form1()
    {
        client = new TcpSampleClient();
        InitializeComponent();
        chatBox.BackColor = Color.White;
        messageBox.AcceptsReturn = true;
        MinimumSize = new Size(400, 200);
        Run();
    }

    async Task Run()
    {
        await client.Run(this);
    }

    public void WriteLine(string message, Color color)
    {
        ChangeColor(color);
        chatBox.AppendText(message);
        chatBox.AppendText(Environment.NewLine);

        if (autoScrollCheckBox.Checked)
        {
            chatBox.SelectionStart = chatBox.TextLength;
            chatBox.ScrollToCaret();
        }
    }

    public void ChangeColor(Color color)
    {
        chatBox.SelectionColor = color;
    }

    public string GetMessage()
    {
        return messageBox.Text;
        //string message = messageBox.Text;
        //messageBox.Text = String.Empty;
        //return message;
    }

    public void ClearMessage()
    {
        messageBox.Text = String.Empty;
    }

    public void SetLabel (string _label)
    {
        this.Text = _label + " - " + Text;
    }
    
    public void ClearChat()
    {
        chatBox.Text = String.Empty;
    }
}