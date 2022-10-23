using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace SocketTcpServer
{
    public class Users
    {
        public DateTime Date { get; set; }
        public string Name { get; set; }
        public string Massage { get; set; }
    }

    class Program
    {
        public static List<Users> LogList = new();
        static int port = 8005; // порт для приема входящих запросов
        static string address = "127.0.0.1";

        static void Main(string[] args)
        {
            if (File.Exists("LogList.json"))
            {
                LoadJsonToList();
            }

            // получаем адреса для запуска сокета
            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(address), port);
            // создаем сокет
            Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                // связываем сокет с локальной точкой, по которой будем принимать данные
                listenSocket.Bind(ipPoint);
                // начинаем прослушивание
                listenSocket.Listen(10);
                Console.WriteLine("Сервер запущен. Ожидание подключений...");
                int userCount = 0;
                while (true)
                {
                    Socket handler = listenSocket.Accept();
                    StringBuilder User = new StringBuilder();
                    userCount++;
                    byte[] data = new byte[256];
                    var bytes = handler.Receive(data);
                    User.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    var nameUser = User.ToString() + userCount;
                    Console.WriteLine($" Пользователь: {nameUser} подключился");

                    Task.Run(() =>
                    {
                        while (true)
                        {
                            // получаем сообщение
                            StringBuilder builder = new StringBuilder();
                            // количество полученных байтов
                            byte[] data = new byte[256]; // буфер для получаемых данных

                            do
                            {
                                var bytes = handler.Receive(data);
                                builder.Append(Encoding.Unicode.GetString(data, 0, bytes));

                            }
                            while (listenSocket.Available > 0);

                            if (builder.ToString() == "exit")
                            {
                                Console.WriteLine($"{DateTime.Now} Пользователь {nameUser} вышел из чата");
                                LogJSON(nameUser, builder.ToString());
                                handler.Dispose();
                                break;
                            }
                            Console.WriteLine($"{DateTime.Now} Пользователь {nameUser} отправил сообщение: {builder}");
                            LogJSON(nameUser, builder.ToString());
                            // отправляем ответ
                            string message = "ваше сообщение доставлено";
                            data = Encoding.Unicode.GetBytes(message);
                            handler.Send(data);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static void SaveJson()
        {
            string fileName = "LogList.json";
            File.WriteAllText(fileName, JsonConvert.SerializeObject(LogList));
        }

        public static void LoadJsonToList()
        {
            LogList.Clear();
            if (File.Exists("LogList.json"))
            {
                var json = File.ReadAllText("LogList.json");
                var loading = JsonConvert.DeserializeObject<List<Users>>(json);
                foreach (var list in loading)
                {
                    LogList.Add(list);
                }
            }
        }

        public static void LogJSON(string nameUser, string builder)
        {
            Users log = new();
            log.Date = DateTime.Now;
            log.Name = nameUser;
            log.Massage = builder;
            LogList.Add(log);
            SaveJson();
            WriteInTXT(log);
        }

        public static string WriteLog(Users log)
        {
            return $"[{log.Date}] [{log.Name}] сообщение: [{log.Massage}]";
        }

        public static void WriteInTXT(Users log)
        {
            StreamWriter data = new("ListLog.txt", true);
            data.WriteLine(WriteLog(log));
            data.Close();
        }
    }
}
