using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace PylontechDeluxe
{
    class Program
    {
        public static PylontechClient Client { get; set; }
        public static bool IsConnected { get; set; } = false;
        
        // NEU: Die dynamische Wartezeit (Standard: 2000 ms)
        public static int PollingIntervalMs { get; set; } = 2000;
        
        private static HashSet<int> _statFetchedForBattery = new HashSet<int>();
        private static bool _masterInfoFetched = false;

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("Starte Pylontech-Watch Motor...");
            
            WebRenderer.StartServer();
            OpenDashboardApp();

            Console.WriteLine("Warte auf Verbindung über das Web-Dashboard...");
            Console.WriteLine("Das Dashboard läuft weiter, auch wenn du das App-Fenster schließt.");
            Console.WriteLine("Drücke STRG+C in diesem schwarzen Fenster, um alles komplett zu beenden.");

            while (true)
            {
                if (IsConnected && Client != null)
                {
                    try 
                    {
                        List<Battery> batteries = new List<Battery>();

                        string pwrText = Client.SendCommand("pwr", 1000);
                        batteries = BatteryParser.ParsePwr(pwrText);

                        if (batteries.Count > 0)
                        {
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Lese Live-Daten... {batteries.Count} echte Akkus gefunden.");
                        }

                        foreach (var bat in batteries)
                        {
                            string batText = Client.SendCommand($"bat {bat.Id}", 800);
                            BatteryParser.ParseBat(batText, bat);

                            lock (_statFetchedForBattery)
                            {
                                if (!_statFetchedForBattery.Contains(bat.Id))
                                {
                                    string statText = Client.SendCommand($"stat {bat.Id}", 1000);
                                    BatteryParser.ParseStat(statText, bat);

                                    if (bat.Soh > 0 || bat.Cycles > 0)
                                    {
                                        _statFetchedForBattery.Add(bat.Id);
                                    }
                                }
                            }
                        }

                        if (!_masterInfoFetched && batteries.Count > 0)
                        {
                            var master = batteries[0]; 
                            
                            string infoText = Client.SendCommand($"info {master.Id}", 1200);
                            BatteryParser.ParseInfo(infoText, master);

                            string logText = Client.SendCommand($"log {master.Id}", 2000);
                            BatteryParser.ParseLog(logText, master);

                            if (master.DeviceName != "Unbekannt" || master.Events.Count > 0)
                            {
                                _masterInfoFetched = true;
                            }
                        }

                        WebRenderer.UpdateData(batteries);
                    }
                    catch (Exception loopEx)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Warnung beim Lesen: {loopEx.Message}");
                        IsConnected = false;
                        lock (_statFetchedForBattery) { _statFetchedForBattery.Clear(); }
                        _masterInfoFetched = false;
                    }
                }

                // NEU: Intelligentes Warten. Statt stur z.B. 5 Minuten am Stück zu schlafen,
                // schläft er in 200ms-Schritten. So reagiert er sofort, wenn der Nutzer im
                // Dashboard die Zeit wieder verkürzt!
                int waited = 0;
                while (waited < PollingIntervalMs)
                {
                    Thread.Sleep(200);
                    waited += 200;
                }
            }
        }

        static void OpenDashboardApp()
        {
            string url = "http://localhost:5000/";
            try { Process.Start(new ProcessStartInfo { FileName = "msedge", Arguments = $"--app={url}", UseShellExecute = true }); }
            catch
            {
                try { Process.Start(new ProcessStartInfo { FileName = "chrome", Arguments = $"--app={url}", UseShellExecute = true }); }
                catch { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
            }
        }
    }
}