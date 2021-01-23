using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using SteamworksSharp;
using SteamworksSharp.Native;
using Valve.Steamworks;
using Alba.CsConsoleFormat;
using Newtonsoft.Json.Linq;

using static System.ConsoleColor;
using System.IO;

namespace DS3ConnectionInfo
{
    class Program
    {
        static MemoryManager mem;

        static void Main(string[] args)
        {
            JObject settings = JObject.Parse(File.ReadAllText("settings.txt"));

            Console.CursorVisible = false;
            Console.Title = "DS3 Connection Info V3.3";

            SteamNative.Initialize();
            mem = new MemoryManager();

            Console.WriteLine("Dark Souls III: Closed");
            do
            {
                try { mem.OpenProcess("DarkSoulsIII"); }
                catch { }
                Thread.Sleep(2000);
            } while (mem.ProcHandle == IntPtr.Zero);

            if (!SteamApi.Initialize(374320))
            {
                Console.WriteLine("ERROR: Could not initalize SteamAPI.");
                Console.Read();
                return;
            }

            ETWPingMonitor.Start();

            if ((bool)settings["overlay"])
            {
                Overlay.Enable(settings);
            }
            

            Console.Clear();
            while (!mem.HasExited)
            {
                Player.UpdatePlayerList();
                Player.UpdateInGameInfo(mem.Process);
                PrintConnInfo();

                Thread.Sleep(1000);
            }

            Overlay.Disable();
            ETWPingMonitor.Stop();
        }

        static void PrintConnInfo()
        {
            Console.SetCursorPosition(0, 0);
            Console.CursorVisible = false;

            LineThickness StrokeRight = LineThickness.Heavy;
            LineThickness StrokeHeader = LineThickness.Double;

            var doc = new Document(
                new Span("Dark Souls III: Open\n") { Color = Yellow },
                new Grid
                {
                    Stroke = StrokeHeader,
                    StrokeColor = DarkGray,
                    Columns = { GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto, 
                                GridLength.Auto, GridLength.Auto, GridLength.Star(1)},
                    Children =
                    {
                        new Cell("Char. Name") { Stroke = StrokeHeader, Color = White },
                        new Cell("Team") { Stroke = StrokeHeader, Color = White },
                        new Cell("Ping") { Stroke = StrokeHeader, Color = White },
                        Player.ActivePlayers().Select(player =>
                        {
                            var cells = new List<Cell>();
                            cells.Add(new Cell(player.CharName));
                            cells.Add(new Cell(player.TeamName));
                            if (player.Ip != null)
                            {
                                ConsoleColor pingColor;
                                int ping = (int)ETWPingMonitor.GetPing(player.Ip);
                                switch (ping)
                                {
                                    case -1:
                                        pingColor = White;
                                        break;
                                    case int n when (n <= 50):
                                        pingColor = Blue;
                                        break;
                                    case int n when (n <= 100):
                                        pingColor = Green;
                                        break;
                                    case int n when (n <= 200):
                                        pingColor = Yellow;
                                        break;
                                    default:
                                        pingColor = Red;
                                        break;
                                }
                                cells.Add(new Cell(ping) { Color = pingColor, Align = Align.Center});
                            }
                            else
                            {
                                cells.Add(new Cell("N/A"));
                            }
                            return cells;
                        })
                    }
            });

            ConsoleRenderer.RenderDocument(doc);
            for (int i = Console.CursorTop; i < Console.WindowHeight - 1; i++)
            {
                Console.CursorTop = i;
                Console.WriteLine("".PadRight(Console.BufferWidth - 1));
            }
        }
    }
}
