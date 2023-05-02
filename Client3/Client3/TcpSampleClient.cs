using System.Diagnostics;
using System.Net.Sockets;
using Client3;

namespace NetworkClient
{
    struct Message
    {
        public string color;
        public string name;
        public string message;
    }
    
    internal class TcpSampleClient
    {
        
        private string ip = "0.0.0.0";
        private string name = "Name";
        private Color color = Color.Black;
        private bool sendToServer = true;
        private string message;
        private bool firstMessage = true;
        private Form1 window;
        private string lastSender;

        public async Task Run(Form1 window)
        {
            using var client = new TcpClient();
            this.window = window;
            //window.Text += " Ip : " + ip;
            WriteLine(" Connexion avec le serveur...", false, true);
            CheckConnection(client);
            await client.ConnectAsync(ip, 666);
            WriteLine(" Connexion réussie...", false, true);

            //Initialize the client
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, leaveOpen: true);
            using var writer = new StreamWriter(stream, leaveOpen: true);
            //Console.ForegroundColor = color;
            window.ChangeColor(color);

            // Display the messages
            async Task DisplayLines()
            {
                var newLine = await reader.ReadLineAsync();
                while(newLine != null)
                {
                    // Color the message;
                    Color color = this.color;
                    if (newLine.Contains("/c"))
                    {
                        string colorStr = SplitMessage(newLine).color;
                        colorStr = colorStr.Replace("Color ", String.Empty);
                        // Convert the string to a Color
                        color = Color.FromName(colorStr);
                        newLine = newLine.Replace(colorStr+"/c", String.Empty);
                        newLine = newLine.Replace("Color ", String.Empty);
                    }
                    WriteLine("[" +DateTime.Now + "] " + newLine, true, false, color);
                    //Console.ForegroundColor = color;
                    window.ChangeColor(color);
                    newLine = await reader.ReadLineAsync();
                }
            }

            _=DisplayLines();

            while (true)
            {
                message = window.GetMessage();

                string[] split1 = message.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

                if (!window.sendMessage && !message.Contains(Environment.NewLine))
                {
                    await Task.Delay(100); // Attendre 100 ms avant de vérifier à nouveau
                    continue;
                }
                message = split1[0];

                window.ClearMessage();

                // Message Empty
                if (message == String.Empty)
                {
                    //Console.SetCursorPosition(0, Console.CursorTop - 1);
                    WriteLine("Message vide", false, true);
                    sendToServer = true;
                    window.sendMessage = false;
                    continue;
                }
                
                // Cmd
                if (message[0] == '/')
                {
                    if (CheckCmd(message))
                    {
                        sendToServer = false;
                    }
                }

                // Send the message
                if (sendToServer)
                {
                    string lineToSend = color + "/c" + name + " : " + message;
                    lineToSend = lineToSend.Replace("[", String.Empty);
                    lineToSend = lineToSend.Replace("]", String.Empty);

                    writer.WriteLine(lineToSend);
                    
                    await writer.FlushAsync();
                    if (firstMessage)
                    {
                        name = message;
                        firstMessage = false;
                        window.ClearChat();
                        window.SetLabel(name);
                    }
                    message = String.Empty;
                }
                sendToServer = true;
                message = null;
                window.sendMessage = false;
            }
        }

        bool CheckCmd(string line)
        {
            string newLine = line.Replace("/", String.Empty);
            
            switch (newLine[0])
            {
                case 'c' :
                    if (newLine.Length > 1 && newLine[1] == ' ')
                    {
                        ChangeColor(newLine.Replace("c ", String.Empty));
                    }
                    else
                    {
                        WriteLine("Commande invalide", false, true);
                    }
                    break;
                case 'a' :
                    if (newLine.Length == 1)
                    {
                        DisplayAllCommands();
                    }
                    else
                    {
                        WriteLine("Commande invalide", false, true);
                    }
                    break;
                default:
                    return false;
            }
            return true;
        }

        void ChangeColor(string line)
        {
            window.ChangeColor(color);
            if (line.Equals("Red"))
            {
                WriteLine("La couleur rouge est réservé au système", false, true);
                return;
            }
            if (line.Equals("Orange"))
            {
                WriteLine("La couleur orange est réservé aux chuchotements", false, true);
                return;
            }
            if (!Color.FromName(line).IsKnownColor)
            {
                WriteLine("Couleur introuvable " + line, false, true);
            }
            else
            {
                color = Color.FromName(line);
                WriteLine("Couleur changée en " + line, false, true);
            }
        }
        
        void DisplayAllCommands()
        {
            WriteLine("Liste des commandes :", false, true);
            WriteLine("/c [couleur] : Changer la couleur du texte", false, true);
            WriteLine("/w [nom] [message] : Envoyer un message privé", false, true);
        }
        
        void WriteLine(string message, bool sendToServer, bool isClient = false, Color messageColor = default)
        {
            string newMessage = message;
            Color color = messageColor;
            if (isClient)
            {
                //Console.ForegroundColor = ConsoleColor.Red;
                color = Color.Red;

                newMessage = "[" +DateTime.Now + "] " + "[Système]" + message;

            }
            else
            {
                string[] split1 = newMessage.Split(new char[] { ' ' }, StringSplitOptions.None);
                if (split1[2] == lastSender && lastSender != "[Système]")
                {
                    newMessage = "";
                    for (int i = 4; i < split1.Length; i++)
                    {
                        newMessage += split1[i] + " ";
                    }
                }
                lastSender = split1[2];
            }
            window.WriteLine(newMessage, color);

            this.sendToServer = sendToServer;
            window.ChangeColor(color);
        }
        
        Message SplitMessage(string message)
        {
            string color;
            string name;
            string messageContent;
            if (message.Contains(" : "))
            {
                string[] messageArray = message.Split(" : ");
                string[] colorArray = messageArray[0].Split("/c");
                color = colorArray[0];
                name = colorArray[1];
                messageContent = messageArray[1];
            }
            else
            {
                string[] colorArray = message.Split("/c");
                color = colorArray[0];
                name = String.Empty;
                messageContent = colorArray[1];
            }
            
            Message newMessage = new Message();
            newMessage.color = color;
            newMessage.name = name;
            newMessage.message = messageContent;
            return newMessage;
        }

        async void CheckConnection(TcpClient client)
        {
            await Task.Delay(1000);
            if (!client.Connected)
            {
                WriteLine(" Connexion impossible...", false, true);
                await Task.Delay(1000);
                WriteLine(" Fermeture de l'application dans 3 secondes...", false, true);
                await Task.Delay(3000);
                Environment.Exit(0);
            }
        }
    }
}