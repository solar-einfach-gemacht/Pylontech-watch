using System.Collections.Generic;

namespace PylontechDeluxe
{
    public class LogEvent
    {
        public string Time { get; set; }
        public string Info { get; set; }
    }

    public class Battery
    {
        public int Id { get; set; }
        public string DeviceName { get; set; } = "Unbekannt";
        public string Firmware { get; set; } = "Unbekannt";
        public string Barcode { get; set; } = "Unbekannt";
        public int Cycles { get; set; } = 0;
        public int Soh { get; set; } = 0;
        public double Voltage { get; set; }
        public double Current { get; set; }
        public int Soc { get; set; }
        
        // --- STATUS & ALARME ---
        public string Status { get; set; } = ""; 
        public string VoltState { get; set; } = "Normal";
        public string CurrState { get; set; } = "Normal";
        public string TempState { get; set; } = "Normal";
        
        // --- TEMPERATUR WERTE ---
        public double Temperature { get; set; } 
        public double CellTempMin { get; set; } 
        public double CellTempMax { get; set; } 
        
        public Dictionary<int, double> Cells { get; set; } = new Dictionary<int, double>();
        public double CellMin { get; set; } = 999.0;
        public double CellMax { get; set; } = 0.0;
        public double CellDelta { get; set; } = 0.0;

        public List<LogEvent> Events { get; set; } = new List<LogEvent>();
    }
}