using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Resources;
using System.Threading;
using Windows.Storage;
using nanoFramework.WebServer;
using NeoPixel;

namespace LEDLight
{
    public class Program
    {
        private const int GpioPin = 27;
        private const int Size = 24;
        private static readonly Color RedColor = new Color { R = 255 };
        private static readonly Color WaitColor = new Color { B = 255 };
        private static readonly Color SuccessColor = new Color { G = 255 };
        private static Color OnColor = new Color { R=255, G = 255, B = 255 };
        private static readonly Color BlackColor = new Color();
        private static NeopixelChain chain;
        private static byte brightness = 255;
        public static void Main()
        {
            chain = new NeopixelChain(GpioPin, Size);

            var network = WaitForWifiConnection();
            BlinkRing(SuccessColor);

            InitializeWebServer();
        }

        static void InitializeWebServer()
        {
            using (WebServer server = new WebServer(80, HttpProtocol.Http))
            {
                server.CommandReceived += WebServerCommandReceived;
                server.Start();
                Thread.Sleep(Timeout.Infinite);
            }
        }

        private static void WebServerCommandReceived(object obj, WebServerEventArgs e)
        {
            var url = e.Context.Request.RawUrl;
            Debug.WriteLine($"Command received: {url}, Method: {e.Context.Request.HttpMethod}");

            if (url.ToLower() == "/on")
            {
                SetRing(OnColor);
            }
            else if (url.ToLower() == "/off")
            {
                SetRing(BlackColor);
            }
            else if (url.ToLower().Contains("color"))
            {
                try
                {
                    var parts = url.ToLower().Split(':');
                    var colors = parts[1].Split('-');
                    Single newRed;
                    Single.TryParse(colors[0], out newRed);
                    Single newGreen;
                    Single.TryParse(colors[1], out newGreen);
                    Single newBlue;
                    Single.TryParse(colors[2], out newBlue);
                    Single newBrightness;
                    Single.TryParse(colors[3], out newBrightness);
                    brightness = (byte) newBrightness;

                    OnColor = new Color { R = (byte)newRed, G = (byte)newGreen, B = (byte)newBlue };
                    SetRing(OnColor);
                }
                catch (Exception exception)
                {
                    Debug.WriteLine(exception.Message);
                }
            }
            else
            {
                var htmlContent = Resources.GetString(Resources.StringResources.UI);
                //WebServer.SendFileOverHTTP(e.Context.Response, "UI.html", ((byte[])(nanoFramework.Runtime.Native.ResourceUtility.GetObject(Resources.ResourceManager, Resources.StringResources.UI))));
                WebServer.OutPutStream(e.Context.Response, htmlContent);
            }
        }

        static void SetRing(Color color)
        {
            var adjustedColor = new Color{
                R = ((byte)(color.R*brightness/255)), 
                G = ((byte)(color.G*brightness/255)), 
                B = ((byte)(color.B*brightness/255))};
            for (uint i = 0; i < Size; i++)
            {
                chain[i] = adjustedColor;
            }

            chain.Update();
        }

        static void BlinkRing(Color color)
        {
            for (uint i = 0; i < Size; i++)
            {
                chain[i] = color;
            }

            chain.Update();
            Thread.Sleep(500);

            for (uint i = 0; i < Size; i++)
            {
                chain[i] = BlackColor;
            }
            chain.Update();
        }

        static NetworkInterface WaitForWifiConnection()
        {
            Debug.WriteLine("Waiting for Wifi connection");

            while (true)
            {
                NetworkInterface ni = NetworkInterface.GetAllNetworkInterfaces()[0];
                if (ni.IPv4Address != null && ni.IPv4Address.Length > 0)
                {
                    if (ni.IPv4Address[0] != '0')
                    {
                        Debug.WriteLine("Connected successfully to Wifi, IP is " + ni.IPv4Address);
                        return ni;
                    }
                }
                for (uint i = 0; i < Size; i++)
                {
                    chain[i] = WaitColor;
                    chain.Update();
                    chain[i] = BlackColor;
                }
            }
        }
    }
}
