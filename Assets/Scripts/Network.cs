
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using UnityEngine;

namespace Network
{
    class ObstacleID
    {
        static UInt64 counter = 1;
        public ObstacleID()
        {
            id = counter++;
        }

        public ObstacleID(UInt64 id_manual)
        {
            id = id_manual;
        }
        UInt64 id = 1;

        public byte[] getBytesOfId()
        {
            return BitConverter.GetBytes(id);
        }

        public UInt64 getId()
        {
            return id;
        }
    }
    class Server
    {
        static SortedDictionary<string, float[]> buff_dict_mat4;
        static SortedDictionary<string, ObstacleID> dict_id;
        static TcpListener _server = null;
        static Thread _threadConnection = null;
        static bool _is_running = true;
        static CarVision _carVision = null;
        private static Mutex mut = new Mutex();
        public Server(CarVision carVision, string host, Int32 port)
        {
            IPAddress localAddress = IPAddress.Parse(host);
            buff_dict_mat4 = new SortedDictionary<string, float[]>();
            dict_id = new SortedDictionary<string, ObstacleID>();
            _server = new TcpListener(localAddress, port);
            _threadConnection = new Thread(new ThreadStart(ThreadConnection));
            _carVision = carVision;
        }

        public void fillBuffMat4(SortedDictionary<string, float[]> dict_mat4)
        {
            mut.WaitOne();
            buff_dict_mat4 = new SortedDictionary<string, float[]>(dict_mat4);
            mut.ReleaseMutex();
        }

        public void Start()
        {
            _threadConnection.Start();
        }
        ~Server()
        {
            _is_running = false;
            _threadConnection.Join();
        }

        public static void ThreadConnection()
        {
            Byte[] bytes = new Byte[256];
            _server.Start();

            while (_is_running)
            {
                Debug.Log("Waiting for a connection... ");
                using TcpClient client = _server.AcceptTcpClient();
                Debug.Log("Connected!");
                NetworkStream stream = client.GetStream();
                int i;
                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    if (_carVision.checkResponseConnection(bytes))
                    {
                        stream.Write(_carVision.getRequestConnectionBytes());
                    }
                    else if (_carVision.checkClientRequest(bytes))
                    {
                        mut.WaitOne();
                        stream.Write(packData().ToArray());
                        mut.ReleaseMutex();
                    }
                }

                // Shutdown and end the connection
                client.Close();

            }
        }

        private static List<byte> packData() 
        {

            List<byte> listDataSent = new List<byte>();
            listDataSent.AddRange(_carVision.bt_client_response);
            listDataSent.AddRange(CarVision.floatToBytes(_carVision.getCarSpeed()));
            
            listDataSent.AddRange(CarVision.intToBytes((UInt64)buff_dict_mat4.Count));
            bool flag = false;
            foreach(var item in buff_dict_mat4)
            {
                
                if(!dict_id.ContainsKey(item.Key))
                {
                    if(item.Key == "car")
                        dict_id.Add(item.Key, new ObstacleID(0));
                    else
                        dict_id.Add(item.Key, new ObstacleID());
                }
                byte[] byte_id = dict_id[item.Key].getBytesOfId();

                listDataSent.AddRange(byte_id);
                if(byte_id[0] == 0 &&
                   byte_id[1] == 0 &&
                   byte_id[2] == 0 &&
                   byte_id[3] == 0 &&
                   byte_id[4] == 0 &&
                   byte_id[5] == 0 &&
                   byte_id[6] == 0 &&
                   byte_id[7] == 0)
                {
                    if(flag)
                    {
                        flag = false;
                    }
                    flag = true;
                }
                foreach( var cell in item.Value)
                { 
                    listDataSent.AddRange(CarVision.floatToBytes(cell));
                }
            }
            
            return listDataSent;
        }
    }
}