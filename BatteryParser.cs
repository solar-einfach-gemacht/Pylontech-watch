using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PylontechDeluxe
{
    public class BatteryParser
    {
        public static List<Battery> ParsePwr(string rawData)
        {
            List<Battery> list = new List<Battery>();
            string pattern = @"^(?<id>[1-9]|1[0-6])\s+(?<volt>-?\d+)\s+(?<curr>-?\d+)\s+(?<temp>-?\d+)\s+(?<tlow>-?\d+)\s+(?<thigh>-?\d+)\s+(?<vlow>\d+)\s+(?<vhigh>\d+)\s+(?<basest>\w+)\s+(?<voltst>\w+)\s+(?<currst>\w+)\s+(?<tempst>\w+)\s+(?<soc>\d+)%";
            string[] lines = rawData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach(string line in lines)
            {
                Match m = Regex.Match(line.Trim(), pattern);
                if (m.Success)
                {
                    double volt = double.Parse(m.Groups["volt"].Value) / 1000.0;
                    string status = m.Groups["basest"].Value;
                    
                    if (volt > 10.0 && status.ToLower() != "absent")
                    {
                        list.Add(new Battery()
                        {
                            Id = int.Parse(m.Groups["id"].Value),
                            Voltage = volt,
                            Current = double.Parse(m.Groups["curr"].Value) / 1000.0,
                            Temperature = double.Parse(m.Groups["temp"].Value) / 1000.0, 
                            CellTempMin = double.Parse(m.Groups["tlow"].Value) / 1000.0, 
                            CellTempMax = double.Parse(m.Groups["thigh"].Value) / 1000.0, 
                            Soc = int.Parse(m.Groups["soc"].Value),
                            Status = status,
                            VoltState = m.Groups["voltst"].Value,
                            CurrState = m.Groups["currst"].Value,
                            TempState = m.Groups["tempst"].Value
                        });
                    }
                }
            }
            return list;
        }

        public static void ParseBat(string rawData, Battery bat)
        {
            string pattern = @"^(?<cell>\d{1,2})\s+(?<volt>\d{4})";
            string[] lines = rawData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach(string line in lines)
            {
                Match m = Regex.Match(line.Trim(), pattern);
                if (m.Success)
                {
                    int cellId = int.Parse(m.Groups["cell"].Value);
                    double cellVolt = double.Parse(m.Groups["volt"].Value) / 1000.0;
                    bat.Cells[cellId] = cellVolt;
                    if (cellVolt < bat.CellMin) bat.CellMin = cellVolt;
                    if (cellVolt > bat.CellMax) bat.CellMax = cellVolt;
                }
            }
            if (bat.Cells.Count > 0) bat.CellDelta = Math.Round((bat.CellMax - bat.CellMin) * 1000, 0);
        }

        public static void ParseInfo(string rawData, Battery bat)
        {
            Match mName = Regex.Match(rawData, @"Device name\s*:\s*(?<val>[A-Za-z0-9_-]+)");
            if (mName.Success) bat.DeviceName = mName.Groups["val"].Value;
            Match mFw = Regex.Match(rawData, @"Main Soft version\s*:\s*(?<val>[A-Za-z0-9.-]+)");
            if (mFw.Success) bat.Firmware = mFw.Groups["val"].Value;
            Match mBar = Regex.Match(rawData, @"Barcode\s*:\s*(?<val>[A-Za-z0-9]+)");
            if (mBar.Success) bat.Barcode = mBar.Groups["val"].Value;
        }

        public static void ParseStat(string rawData, Battery bat)
        {
            Match mCycle = Regex.Match(rawData, @"CYCLE Times\s*:\s*(?<val>\d+)");
            if (mCycle.Success) bat.Cycles = int.Parse(mCycle.Groups["val"].Value);
            Match mSoh = Regex.Match(rawData, @"\bSOH\s*:\s*(?<val>\d+)");
            if (mSoh.Success) bat.Soh = int.Parse(mSoh.Groups["val"].Value);
        }

        public static void ParseLog(string rawData, Battery bat)
        {
            string pattern = @"Time\s*:\s*(?<time>[^\r\n]+)[\s\S]*?Info\s*:\s*(?<info>[^\r\n]+)";
            MatchCollection matches = Regex.Matches(rawData, pattern);
            foreach (Match m in matches)
            {
                bat.Events.Add(new LogEvent
                {
                    Time = m.Groups["time"].Value.Trim(),
                    Info = m.Groups["info"].Value.Trim()
                });
            }
        }
    }
}