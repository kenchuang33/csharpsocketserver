using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using Newtonsoft.Json;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Markup;
using Newtonsoft.Json.Linq;

namespace socket
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {


        public List<Socket> clientsmember = new List<Socket>();
        private bool serversocketshow = true ;
       
        public MainWindow()
        {
            InitializeComponent();

        }


        private void StartServer_Click(object sender, RoutedEventArgs e)
        {
            IPAddress ip = IPAddress.Parse(ServerIP.Text);
            int Port = int.Parse(ServerPort.Text);


            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(ip, Port); // 設置Server的IP和Port
            serverSocket.Bind(endPoint);
            serverSocket.Listen(10);

            Thread thread = new Thread(() => Listen(serverSocket));
            thread.Start();
            
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                chatbox.Text += "Server Start" + Environment.NewLine;
                // 更新UI的程式碼
            });
        }

        private void StopListen_Click(object sender, RoutedEventArgs e)
        {
            string send_msg = "Server close !";
            string jsonData = JsonConvert.SerializeObject(new { Data = send_msg });

            byte[] data = Encoding.ASCII.GetBytes(jsonData);
            foreach (var c in clientsmember)
            {
               
                c.Send(data);
            }
            clientsmember.Clear();
            serversocketshow = false;
        }

        
        private void Listen(Socket curServer)
        {
            while (true)
            {
                Socket ssclient = curServer.Accept();               
                clientsmember.Add(ssclient);
                Thread receive = new Thread(() => ReceiveMsg(ssclient));
                receive.Start();
                
            }
        }

        private void ReceiveMsg(Socket client)
        {

            while (client.Connected)
            {

                byte[] result = new byte[client.Available];
                //取得byte array長度
                int receive_num = client.Receive(result);
                //byte array轉回json string
                string receive_str = Encoding.ASCII.GetString(result, 0, receive_num);

                if (receive_num > 0 && serversocketshow)
                {
                    // 將 JSON 字串轉換為 JObject
                    JObject obj = JObject.Parse(receive_str);
                    if (obj["Type"] != null && obj["Type"].ToString() == "Disconnect")
                    {
                        // 從列表中移除客戶端
                        clientsmember.Remove(client);

                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            chatbox.Text += obj["Data"].ToString() +"Disconnect !"+ Environment.NewLine;
                            // 更新UI的程式碼
                        });
                        // 關閉Socket
                        client.Shutdown(SocketShutdown.Both);
                            client.Close();
                            break; // 跳出循環
                    }
                    // 讀取 "Data" 屬性的值
                    string dataValue = obj["Data"].ToString();

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        chatbox.Text += dataValue + Environment.NewLine;
                    // 更新UI的程式碼
                    });
                }
            // 將 JSON 資料傳送給所有客戶端
                foreach (var c in clientsmember)
                {
                    byte[] jsonBytes = Encoding.ASCII.GetBytes(receive_str);
                    c.Send(jsonBytes);
                }
            }
           
        }
    }
}
