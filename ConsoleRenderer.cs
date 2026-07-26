using System;
using System.Collections.Generic;
using System.Linq;

namespace PylontechDeluxe
{
    public class ConsoleRenderer
    {
        public static void Render(List<Battery> batteries)
        {
            Console.Clear();
            Console.WriteLine("=== PYLONTECH DELUXE - EXPERTEN DIAGNOSE ===\n");
            
            if (batteries.Count == 0)
            {
                Console.WriteLine("Keine Batterien gefunden.");
                return;
            }

            foreach (var bat in batteries)
            {
                Console.WriteLine($"\n┌────────────────────────────────────────────────────────┐");
                string line1 = $"BATTERIE #{bat.Id:D2} | {bat.DeviceName} | SOH: {bat.Soh}% | Zyklen: {bat.Cycles}";
                string line2 = $"SOC: {bat.Soc,3}% | {bat.Voltage:F3} V | {bat.Current:F2} A | {bat.Temperature:F1} °C";
                string line3 = $"FW: {bat.Firmware} | SN: {bat.Barcode}";

                Console.WriteLine($"│ {line1.PadRight(54)} │");
                Console.WriteLine($"├────────────────────────────────────────────────────────┤");
                Console.WriteLine($"│ {line2.PadRight(54)} │");
                Console.WriteLine($"│ {line3.PadRight(54)} │");
                Console.WriteLine($"├────────────────────────────────────────────────────────┤");
                
                if (bat.Cells.Count == 0)
                {
                    Console.WriteLine($"│ {"Keine Zelldaten empfangen.".PadRight(54)} │");
                }
                else
                {
                    string deltaLine = $"Zell-Delta: {bat.CellDelta} mV (Max: {bat.CellMax:F3} V / Min: {bat.CellMin:F3} V)";
                    Console.WriteLine($"│ {deltaLine.PadRight(54)} │");
                    Console.WriteLine($"├────────────────────────────────────────────────────────┤");
                    
                    foreach (var cell in bat.Cells.OrderBy(c => c.Key))
                    {
                        double diffToMin = (cell.Value - bat.CellMin) * 1000;
                        string emoji = "🟩"; 
                        if (diffToMin >= 30) emoji = "🟥";      
                        else if (diffToMin >= 15) emoji = "🟨"; 

                        Console.WriteLine($"│ Zelle {cell.Key + 1:D2}: {cell.Value:F3} V  {emoji}                                  │");
                    }
                }

                // --- NEU: Logbuch-Anzeige ---
                Console.WriteLine($"├────────────────────────────────────────────────────────┤");
                if (bat.Events.Count > 0)
                {
                    Console.WriteLine($"│ Letzte Ereignisse im Logbuch:                          │");
                    var recentEvents = bat.Events.Skip(Math.Max(0, bat.Events.Count - 3));
                    foreach (var ev in recentEvents)
                    {
                        string logLine = $"[{ev.Time}] {ev.Info}";
                        if (logLine.Length > 52) logLine = logLine.Substring(0, 49) + "...";
                        Console.WriteLine($"│ {logLine.PadRight(54)} │");
                    }
                }
                else
                {
                    Console.WriteLine($"│ {"Logbuch leer (Keine Fehler aufgezeichnet)".PadRight(54)} │");
                }
                Console.WriteLine($"└────────────────────────────────────────────────────────┘");
            }
        }
    }
}