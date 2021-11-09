﻿using System;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Assets.Resources;
using System.Net.Sockets;
using System.IO;
using System.Threading;

#if WINDOWS_UWP
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth;
#endif
//using System.Runtime.InteropServices.WindowsRuntime;
//using Windows.Storage.Streams;

namespace Assets.DataManagement
{
    abstract class Connection
    {
        public bool connected = false;
        protected Connection()
        {
            this.connect();
        }

        protected abstract void connect();
        public abstract Task<string> get(params string[] param);
        public virtual void OnDestroy()
        {

        }
    }

    class HardcodedGPSConnection : Connection
    {
        protected override void connect()
        {
            this.connected = true;
        }
        public override Task<string> get(params string[] param)
        {
            // FROM ED: return "$GPRMC,071228.00,A,5402.6015,N,00025.9797,E,0.2,332.1,180921,0.2,W,A,S*50";
            return Task.Run(() => "$GPRMC,071228.00,A,60.403029,N,5.322799,E,0.2,0,180921,0.2,W,A,S*50");
        }
    }

    class BarentswatchAISConnection : Connection
    {
        private HttpClient httpClient = new HttpClient();
        private string token = "";

        protected override void connect()
        {
            this.myConnect();
        }

        protected async void myConnect()
        {
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(Config.Instance.barentswatch.token_url),
                Content = new StringContent(
                    String.Format(
                        Config.Instance.barentswatch.auth_format,
                        Config.Instance.barentswatch.client_id,
                        Config.Instance.barentswatch.client_secret
                    ),
                    Encoding.UTF8,
                    "application/x-www-form-urlencoded"
                )
            };

            string content = "";

            try
            {
                HttpResponseMessage response = await httpClient.SendAsync(httpRequestMessage);
                response.EnsureSuccessStatusCode();
                content = await response.Content.ReadAsStringAsync();
                this.token = JObject.Parse(content)["access_token"].ToString();
                this.connected = true;
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }

        private Uri getUriwithParams(string lonMin, string lonMax, string latMin, string latMax)
        {
            string uri = String.Format(Config.Instance.barentswatch.ais_url, lonMin, lonMax, latMin, latMax);
            return new Uri(uri);
        }

        // Lat Min, Lon Min, Lat Max, Lon Max
        public override async Task<string> get(params string[] param)
        {
            //return await Task.Run(() => "[{\"timeStamp\":\"2021-10-26T18:04:11Z\",\"sog\":0.0,\"rot\":0.0,\"navstat\":5,\"mmsi\":258465000,\"cog\":142.3,\"geometry\":{\"type\":\"Point\",\"coordinates\":[5.317615,60.398463]},\"shipType\":60,\"name\":\"TROLLFJORD\",\"imo\":9233258,\"callsign\":\"LLVT\",\"country\":\"Norge\",\"eta\":\"2021-05-03T17:00:00\",\"destination\":\"BERGEN\",\"isSurvey\":false,\"heading\":142,\"draught\":5.5,\"a\":19,\"b\":117,\"c\":11,\"d\":11}]");


            HttpRequestMessage httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = getUriwithParams(param[1], param[3], param[0], param[2])
            };

            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this.token);

            try
            {
                HttpResponseMessage response = await httpClient.SendAsync(httpRequestMessage);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                Debug.Log(e);
                return await Task.Run(() => "[]");

            }

        }
    }

    class BluetoothGPSConnection : Connection
    {
        private TcpClient tcpClient;
        private Thread clientReceiveThread;
        public volatile bool running;
        private string lastReading = "";

        protected override void connect()
        {
            try
            {
                running = true;
                clientReceiveThread = new Thread(new ThreadStart(ListenForData));
                clientReceiveThread.IsBackground = true;
                clientReceiveThread.Start();
            } catch (Exception e)
            {
                Debug.Log("On client connect exception " + e);
            }

            this.connected = true;
        }

        private void ListenForData()
        {
            try
            {
                tcpClient = new TcpClient("10.0.0.17", int.Parse("6000"));

                Byte[] bytes = new Byte[1024];
                while (running)
                {
                    // Get a stream object for reading 				
                    using (NetworkStream stream = tcpClient.GetStream())
                    {
                        int length;
                        // Read incomming stream into byte arrary. 					
                        while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            var incommingData = new byte[length];
                            Array.Copy(bytes, 0, incommingData, 0, length);
                            // Convert byte array to string message. 						
                            string gpsString = Encoding.ASCII.GetString(incommingData);
                            // Only store GPRMC strings
                            if (gpsString.Length > 5 && gpsString.Substring(0, 5) == "GPRMC")
                            {
                                lastReading = gpsString.Substring(0, gpsString.IndexOf(Environment.NewLine));
                            }
                        }
                    }
                }
            } catch (SocketException e)
            {
                Debug.Log("Socket Exception: " + e);
            }
        }

        public override void OnDestroy()
        {
            running = false;
        }

        public override async Task<string> get(params string[] param)
        {
            return await Task.Run(() => lastReading);
        }
    }
}
