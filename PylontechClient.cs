using System;
using System.IO.Ports;
using System.Threading;

namespace PylontechDeluxe
{
    public class PylontechClient
    {
        private SerialPort _port;

        public PylontechClient(string portName)
        {
            // Standard-Geschwindigkeit (115200 Baud). 
            // WICHTIG: Ältere US2000 (ohne 'C') brauchen hier oft 1200 oder 9600!
            _port = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One);
            
            _port.ReadTimeout = 1000;
            _port.WriteTimeout = 1000;
        }

        public void Connect()
        {
            if (!_port.IsOpen)
            {
                _port.Open();
            }
        }

        public void Disconnect()
        {
            if (_port.IsOpen)
            {
                _port.Close();
            }
        }

        public string SendCommand(string command, int delayMs = 1000)
        {
            if (!_port.IsOpen) return "";
            
            try 
            {
                _port.DiscardInBuffer(); 
                
                // Normales "Enter" (Newline), das die meisten Pylontechs erwarten
                _port.Write(command + "\n"); 
                
                Thread.Sleep(delayMs); 
                
                string response = "";
                if (_port.BytesToRead > 0)
                {
                    response = _port.ReadExisting();
                }
                return response;
            }
            catch
            {
                return ""; 
            }
        }
    }
}