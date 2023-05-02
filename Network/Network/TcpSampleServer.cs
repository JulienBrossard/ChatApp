using System.Net.Sockets;

namespace NetworkServer
{
    struct Message
    {
        public string color;
        public string name;
        public string message;
    }
    
    internal class TcpSampleServer
    {        
        private Dictionary<TcpClient, StreamWriter> _clients = new ();
        Dictionary<string, StreamWriter> _clientsName = new ();
        Dictionary<string, List<string>> messages = new ();
        private List<string> allMessages = new ();

        public async Task Run()
        {
            var server = TcpListener.Create(666);
            server.Start();
            WriteLine("Starting Server");

            while(true)
            {
                var client = await server.AcceptTcpClientAsync();
                _ = Serve(client);
            }
        }

        private async Task Serve(TcpClient client)
        {    
            try
            {
                using (client)
                {
                    
                    //Initialize the client
                    using var stream = client.GetStream();
                    using var reader = new StreamReader(stream, leaveOpen: true);
                    using var writer = new StreamWriter(stream, leaveOpen: true);

                    // Ask for the client's name
                    SendMessage("Entrez votre nom", writer, true);
                    WriteLine("New client connected, asking for name");
                    
                    // Wait for the client to send a line
                    await CheckNames(reader, writer, client);
                    string name = _clientsName.Keys.ElementAt(_clientsName.Values.ToList().IndexOf(writer));

                    // Initialize History of the chat for the new client
                    foreach (var mes in messages[name])
                    {
                        SendMessage(mes, writer, false);
                    }
                    WriteLine("Initialize history for " + name);
                    
                    // Send client's name to all the other clients
                    WriteBeginServerMessage();
                    Console.WriteLine("List of clients : ");
                    int i = 0;
                    foreach (var kvp in _clients)
                    {
                        SendMessage( name + " a rejoint le chat", kvp.Value, true);
                        //messages[_clientsName.Keys.ElementAt(_clientsName.Values.ToList().IndexOf(kvp.Value))].Add("Red/c" + "[Système] " + name + " a rejoint le chat");
                        Console.WriteLine(_clientsName.Keys.ElementAt(i));
                        i++;
                    }

                    foreach (var messageHistory in messages.Values)
                    {
                        messageHistory.Add("Red/c" + "[Système] " + name + " a rejoint le chat");
                    }
                    allMessages.Add("Red/c" + "[Système] " + name + " a rejoint le chat");
                    WriteEndServerMessage();
                    
                    await writer.FlushAsync();


                    // Read lines from the client and broadcast them
                    var nextLine = await reader.ReadLineAsync();
                    string message = SplitMessage(nextLine).message;
                    string color = SplitMessage(nextLine).color;
                    string nameMessage = SplitMessage(nextLine).name;
                    while (nextLine != null)
                    {
                        message = SplitMessage(nextLine).message;
                        color = SplitMessage(nextLine).color;
                        nameMessage = SplitMessage(nextLine).name;
                        WriteLine("Message receive : " + message);
                        
                        // Cmd
                        if (message[0] == '/')
                        {
                            CheckCmd(nextLine, message, color, writer);
                            nextLine = await reader.ReadLineAsync();
                        }
                        
                        // Send the message
                        else
                        {
                            //message[_clientsName.Keys.ElementAt(_clientsName.Values.ToList().IndexOf(kvp.Value))].Add(nextLine);
                            foreach (var kvp in _clients)
                            {
                                //Console.WriteLine("Send : " + nextLine + " to " + _clientsName[kvp.Value]);
                                SendMessage(nextLine, kvp.Value, false);
                                //messages[_clientsName.Keys.ElementAt(_clientsName.Values.ToList().IndexOf(kvp.Value))].Add(nextLine);
                            }

                            foreach (var messagesHistory in messages.Values)
                            {
                                messagesHistory.Add(nextLine);
                            }
                            allMessages.Add(nextLine);

                            nextLine = await reader.ReadLineAsync();
                        }
                    }
                }                
            }    
            catch(Exception)
            {

            }
            finally
            {
                string name = _clientsName.Keys.ElementAt(_clientsName.Values.ToList().IndexOf(_clients[client]));
                // Remove the client from the list and tell the others
                foreach (var kvp in _clients)
                {
                    SendMessage(name + " a quitté le chat", kvp.Value, true);
                    //messages[_clientsName.Keys.ElementAt(_clientsName.Values.ToList().IndexOf(kvp.Value))].Add("Red/c" + "[Système] " + name + " a quitté le chat");
                }

                foreach (var message in messages.Values)
                {
                    message.Add("Red/c" + "[Système] " + name + " a quitté le chat");
                }
                allMessages.Add("Red/c" + "[Système] " + name + " a quitté le chat");

                WriteLine("[Système] " + name + " a quitté le chat");
                //allMessages.Add("Red/c" + "[Système] " + name + " a quitté le chat");
                _clients.Remove(client);
                _clientsName.Remove(name);
            }
        }

        async Task CheckNames(StreamReader reader, StreamWriter writer, TcpClient client)
        {
            bool nameExist = true;
            string name = await reader.ReadLineAsync();
            name = SplitMessage(name).message;
            while (nameExist)
            {
                WriteLine("Name : " + name);
                if (_clientsName.ContainsKey(name))
                {
                    SendMessage("[Système] " + "Une personne est déjà connectée sous ce nom", writer, true);
                    name = await reader.ReadLineAsync();
                    name = SplitMessage(name).message;
                }
                else
                {
                    nameExist = false;
                }
            }
            WriteLine(nameExist.ToString());
            _clientsName.Add(name, writer);
            if (!messages.ContainsKey(name))
            {
                /*foreach (var mes in allMessages)
                {
                    SendMessage(mes, writer, false);
                }*/
                messages.Add(name, new List<string>());
                messages[name].AddRange(allMessages);
            }
            SendMessage("Bienvenue " + name, writer, true);
            _clients.Add(client, writer);
        }

        Message SplitMessage(string message)
        {
            string[] messageArray = message.Split(" : ");
            string[] colorArray = messageArray[0].Split("/c");
            string color = colorArray[0];
            string name = colorArray[1];
            string messageContent = messageArray[1];
            
            Message newMessage = new Message();
            newMessage.color = color;
            newMessage.name = name;
            newMessage.message = messageContent;
            
            return newMessage;
        }
        
        async Task SendMessage(string message, StreamWriter writer, bool isServer = false)
        {
            if (isServer)
            {
                writer.WriteLine("Red/c" + "[Système] " +  message);
            }
            else
            {
                writer.WriteLine(message);
            }
            await writer.FlushAsync();
        }

        void WriteLine(string message)
        {
            WriteBeginServerMessage();
            Console.WriteLine(message);
            WriteEndServerMessage();
        }

        void WriteBeginServerMessage()
        {
            Console.WriteLine("==================Server==================");
        }
        
        void WriteEndServerMessage()
        {
            Console.WriteLine("==========================================");
        }

        void CheckCmd(string line, string message, string color, StreamWriter writer)
        {
            message = message.Replace("/", String.Empty);
            switch (message[0])
            {
                case 'w' :
                    if (message.Length > 1 && message[1] == ' ')
                    {
                        Whisper(line, message, color, writer);
                    }
                    else
                    {
                        SendMessage("Commande invalide", writer, true);
                    }
                    break;
                default:
                    SendMessage("Commande invalide", writer, true);
                    break;
            }
        }
        
        void Whisper(string line, string message, string color, StreamWriter writer)
        {
            message = message.Replace("w ", String.Empty);
            string name = message.Split(" ")[0];
            string writerName = _clientsName.Keys.ElementAt(_clientsName.Values.ToList().IndexOf(writer));
            if (writerName.Equals(name))
            {
                SendMessage("Vous ne pouvez pas chuchoter avec vous-même", writer, true);
                return;
            }
            if (_clientsName.ContainsKey(name))
            {
                line = line.Replace(color, "Orange");
                line = line.Replace("/w " + name + " ", String.Empty);
                line = line.Replace(writerName, "[Chuchotement] " + writerName);
                SendMessage(line, _clientsName[name]);
                SendMessage(line, writer);
                messages[writerName].Add(line);
                messages[name].Add(line);
            }
            else
            {
                SendMessage("[Système] " + "Cet utilisateur n'existe pas", writer, true);
            }
        }
    }
}