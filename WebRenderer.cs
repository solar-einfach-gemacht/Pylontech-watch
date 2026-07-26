using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PylontechDeluxe
{
    public class WebRenderer
    {
        private static HttpListener _listener;
        private static List<Battery> _latestData = new List<Battery>();
        private static readonly object _dataLock = new object();

        public static void UpdateData(List<Battery> batteries)
        {
            lock (_dataLock)
            {
                foreach (var newBat in batteries)
                {
                    var oldBat = _latestData.Find(b => b.Id == newBat.Id);
                    if (oldBat != null)
                    {
                        if (newBat.DeviceName == "Unbekannt" && oldBat.DeviceName != "Unbekannt") newBat.DeviceName = oldBat.DeviceName;
                        if (newBat.Firmware == "Unbekannt" && oldBat.Firmware != "Unbekannt") newBat.Firmware = oldBat.Firmware;
                        if (newBat.Barcode == "Unbekannt" && oldBat.Barcode != "Unbekannt") newBat.Barcode = oldBat.Barcode;
                        if (newBat.Soh == 0 && oldBat.Soh > 0) newBat.Soh = oldBat.Soh;
                        if (newBat.Cycles == 0 && oldBat.Soh > 0) newBat.Cycles = oldBat.Cycles;
                        if (newBat.Events.Count == 0 && oldBat.Events.Count > 0) newBat.Events = oldBat.Events;
                    }
                }
                _latestData = batteries;
            }
        }

        public static void StartServer()
        {
            _listener = new HttpListener();
            try 
            {
                _listener.Prefixes.Add("http://+:5000/");
                _listener.Start();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(">>> WLAN-Modus AKTIV! Das Dashboard ist im ganzen Netzwerk erreichbar. <<<");
                Console.ResetColor();
            }
            catch (HttpListenerException)
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add("http://localhost:5000/");
                _listener.Start();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(">>> Lokaler Modus aktiv (Kein WLAN-Zugriff).");
                Console.WriteLine(">>> Tipp: Starte die .exe mit Rechtsklick -> 'Als Administrator ausführen', um das Dashboard fürs gesamte WLAN freizugeben! <<<");
                Console.ResetColor();
            }
            Task.Run(() => ListenAsync());
        }

        private static async Task ListenAsync()
        {
            while (_listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    var request = context.Request;
                    var response = context.Response;
                    byte[] buffer = new byte[0];
                    response.Headers.Add("Access-Control-Allow-Origin", "*");

                    if (request.Url.AbsolutePath == "/api/ports")
                    {
                        response.ContentType = "application/json";
                        buffer = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(SerialPort.GetPortNames()));
                    }
                    // NEUER ENDPUNKT FÜR DEN ZUSCHAUER-MODUS
                    else if (request.Url.AbsolutePath == "/api/status") 
                    {
                        response.ContentType = "application/json";
                        buffer = Encoding.UTF8.GetBytes($"{{\"connected\":{Program.IsConnected.ToString().ToLower()}}}");
                    }
                    else if (request.Url.AbsolutePath == "/api/interval")
                    {
                        response.ContentType = "application/json";
                        if (int.TryParse(request.QueryString["ms"], out int ms))
                        {
                            if (ms >= 2000 && ms <= 300000) Program.PollingIntervalMs = ms;
                        }
                        buffer = Encoding.UTF8.GetBytes("{\"success\":true}");
                    }
                    else if (request.Url.AbsolutePath == "/api/connect")
                    {
                        response.ContentType = "application/json";
                        string port = request.QueryString["port"];
                        try 
                        {
                            // Wenn schon verbunden, blockiere zweite Verbindungsversuche!
                            if (Program.IsConnected)
                            {
                                buffer = Encoding.UTF8.GetBytes("{\"success\":true}");
                            }
                            else
                            {
                                Program.Client = new PylontechClient(port);
                                Program.Client.Connect();
                                Program.IsConnected = true;
                                buffer = Encoding.UTF8.GetBytes("{\"success\":true}");
                            }
                        } 
                        catch (Exception ex) 
                        {
                            buffer = Encoding.UTF8.GetBytes($"{{\"success\":false, \"error\":\"{ex.Message}\"}}");
                        }
                    }
                    else if (request.Url.AbsolutePath == "/api/data")
                    {
                        response.ContentType = "application/json";
                        lock (_dataLock) { buffer = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(_latestData)); }
                    }
                    else
                    {
                        response.ContentType = "text/html; charset=utf-8";
                        buffer = Encoding.UTF8.GetBytes(GetHtmlTemplate());
                    }

                    response.ContentLength64 = buffer.Length;
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    response.OutputStream.Close();
                }
                catch (Exception) { }
            }
        }

        private static string GetHtmlTemplate()
        {
            return @"
<!DOCTYPE html>
<html lang='de'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Pylontech-Watch Dashboard</title>
    <style>
        :root { --bg: #f4f7f6; --card-bg: #ffffff; --text-main: #2c3e50; --text-muted: #7f8c8d; --primary: #3498db; --success: #2ecc71; --warning: #f1c40f; --danger: #e74c3c; --border-radius: 12px; }
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background-color: var(--bg); color: var(--text-main); margin: 0; padding: 20px; }
        .container { max-width: 1200px; margin: 0 auto; }
        
        .header-container { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; flex-wrap: wrap; gap: 10px; }
        h1 { margin: 0; font-weight: 800; color: var(--text-main); }
        
        .interval-select { padding: 8px 12px; border-radius: 8px; border: 1px solid #bdc3c7; font-size: 14px; font-weight: bold; background: white; cursor: pointer; }
        
        .btn { background: var(--primary); color: white; border: none; padding: 12px 24px; font-size: 16px; border-radius: 8px; cursor: pointer; font-weight: bold; transition: opacity 0.2s; display: block; margin: 0 auto 20px auto; }
        .btn:hover { opacity: 0.9; }

        #setup-screen { max-width: 400px; margin: 50px auto; background: var(--card-bg); padding: 30px; border-radius: var(--border-radius); box-shadow: 0 4px 15px rgba(0,0,0,0.05); text-align: center; }
        select { width: 100%; padding: 12px; margin: 20px 0; border-radius: 8px; border: 1px solid #bdc3c7; font-size: 16px; }

        .system-summary { background: linear-gradient(135deg, #2c3e50, #3498db); color: white; border-radius: var(--border-radius); padding: 25px; margin-bottom: 30px; box-shadow: 0 4px 15px rgba(0,0,0,0.1); }
        .summary-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 15px; text-align: center; }
        .summary-val { font-size: 34px; font-weight: 800; margin-top: 5px; }
        .summary-label { font-size: 12px; text-transform: uppercase; font-weight: 700; opacity: 0.8; letter-spacing: 1px; }

        .pack-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 20px; }
        .pack-card { background: var(--card-bg); border-radius: var(--border-radius); padding: 20px; box-shadow: 0 4px 10px rgba(0,0,0,0.05); cursor: pointer; transition: transform 0.2s, box-shadow 0.2s; border: 2px solid transparent; }
        .pack-card:hover { transform: translateY(-5px); box-shadow: 0 8px 20px rgba(0,0,0,0.1); border-color: var(--primary); }
        .pack-title { font-size: 18px; font-weight: 800; color: var(--text-main); margin-bottom: 15px; display: flex; justify-content: space-between; border-bottom: 1px solid #eee; padding-bottom: 10px; align-items: center;}
        .pack-info { display: flex; justify-content: space-between; margin-bottom: 8px; font-size: 14px; }
        
        .badge { padding: 3px 8px; border-radius: 6px; font-size: 11px; font-weight: bold; color: white; text-transform: uppercase; margin-left: 10px; }
        .badge-normal { background: var(--text-muted); }
        .badge-charge { background: var(--success); }
        .badge-dischg { background: var(--warning); color: #333; }
        .badge-alarm { background: var(--danger); animation: pulse 1.5s infinite; font-size: 12px; display: inline-block; margin-top: 10px; margin-left: 0; width: 100%; text-align: center; box-sizing: border-box; }
        
        @keyframes pulse { 0% { opacity: 1; } 50% { opacity: 0.5; } 100% { opacity: 1; } }

        .modal-overlay { display: none; position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.5); z-index: 1000; overflow-y: auto; padding: 20px; box-sizing: border-box; }
        .modal-content { background: var(--card-bg); max-width: 900px; margin: 20px auto; border-radius: var(--border-radius); padding: 30px; position: relative; box-shadow: 0 10px 30px rgba(0,0,0,0.2); }
        .close-btn { position: absolute; top: 20px; right: 25px; font-size: 28px; font-weight: bold; cursor: pointer; color: var(--text-muted); line-height: 1; }
        .close-btn:hover { color: var(--danger); }

        .header-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(130px, 1fr)); gap: 15px; margin-bottom: 25px; }
        .stat-card { background: #f8fafc; padding: 15px; border-radius: 10px; border: 1px solid #e2e8f0; text-align: center; }
        .stat-value { font-size: 20px; font-weight: 700; margin-top: 5px; color: var(--primary); }
        .stat-label { font-size: 11px; text-transform: uppercase; font-weight: 700; color: var(--text-muted); }
        .cells-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(80px, 1fr)); gap: 8px; margin-bottom: 20px; }
        .cell { padding: 10px 5px; border-radius: 6px; text-align: center; font-weight: 700; font-size: 13px; color: white; }
        
        .log-section { background: #f1f5f9; padding: 15px; border-radius: 10px; font-size: 14px; margin-top: 20px; }
        .log-item { padding: 6px 0; border-bottom: 1px solid #e2e8f0; }
        .log-time { font-weight: bold; color: var(--primary); margin-right: 10px; }
        .meta-info { display: flex; justify-content: space-between; font-size: 14px; color: var(--text-muted); margin-bottom: 25px; }
    </style>
</head>
<body>
    <div class='container'>
        
        <div id='setup-screen'>
            <h1>⚡ Pylontech-Watch</h1>
            <h2>System verbinden</h2>
            <p style='color:var(--text-muted);'>Wähle den USB-Anschluss aus.</p>
            <select id='portSelect' style='margin: 20px 0;'><option>Suche Ports...</option></select>
            <button class='btn' style='width:100%; margin-bottom:0;' onclick='connectBms()'>Verbinden</button>
            <div id='setup-error' style='color:var(--danger); margin-top:15px; font-weight:bold;'></div>
        </div>

        <div id='dashboard-screen' style='display:none;'>
            <div class='header-container'>
                <h1>⚡ Pylontech-Watch <span id='viewer-badge' style='display:none; font-size:12px; background:var(--success); color:white; padding:3px 8px; border-radius:5px; vertical-align:middle;'>ZUSCHAUER</span></h1>
                <div>
                    <label for='intervalSelect' style='font-size:14px; font-weight:bold; color:var(--text-muted); margin-right:8px;'>Aktualisierung:</label>
                    <select id='intervalSelect' class='interval-select' onchange='changeInterval()'>
                        <option value='2000'>2 Sekunden (Live)</option>
                        <option value='5000'>5 Sekunden</option>
                        <option value='10000'>10 Sekunden</option>
                        <option value='30000'>30 Sekunden</option>
                        <option value='60000'>1 Minute</option>
                        <option value='300000'>5 Minuten</option>
                    </select>
                </div>
            </div>

            <div class='system-summary'>
                <div class='summary-grid'>
                    <div><div class='summary-label'>System SOH</div><div class='summary-val' id='sys-soh'>-- %</div></div>
                    <div><div class='summary-label'>System SOC</div><div class='summary-val' id='sys-soc'>-- %</div></div>
                    <div><div class='summary-label'>System Spannung</div><div class='summary-val' id='sys-volt'>-- V</div></div>
                    <div><div class='summary-label'>Gesamtstrom</div><div class='summary-val' id='sys-curr'>-- A</div></div>
                    <div><div class='summary-label'>Aktive Akkus</div><div class='summary-val' id='sys-packs'>--</div></div>
                </div>
            </div>
            <div class='pack-grid' id='pack-list'></div>
        </div>
    </div>

    <div class='modal-overlay' id='detail-modal' onclick='closeModal(event)'>
        <div class='modal-content' onclick='event.stopPropagation()'>
            <span class='close-btn' onclick='closeModal(true)'>&times;</span>
            <div id='modal-body'>Lade Details...</div>
        </div>
    </div>

    <script>
        let globalBatteries = [];
        let activeModalId = null;
        let fetchIntervalId = null;

        window.onload = async function() {
            try {
                // 1. ZUSCHAUER-CHECK: Läuft das System bereits?
                const statRes = await fetch('/api/status');
                const stat = await statRes.json();
                
                if (stat.connected) {
                    // DIREKT INS DASHBOARD WINKEN!
                    document.getElementById('setup-screen').style.display = 'none';
                    document.getElementById('dashboard-screen').style.display = 'block';
                    document.getElementById('viewer-badge').style.display = 'inline-block';
                    changeInterval();
                    fetchApi('/api/data');
                    return; // Stoppt hier, lädt keine COM-Ports
                }

                // 2. WENN NICHT VERBUNDEN: Normaler Setup-Screen
                const res = await fetch('/api/ports');
                const ports = await res.json();
                const select = document.getElementById('portSelect');
                select.innerHTML = '';
                if(ports.length === 0) select.innerHTML = '<option>Keine Ports</option>';
                else ports.forEach(p => select.innerHTML += `<option value='${p}'>${p}</option>`);
            } catch(e) {}
        };

        async function connectBms() {
            const port = document.getElementById('portSelect').value;
            document.getElementById('setup-error').innerText = 'Verbinde... (Initiale Daten werden geladen)';
            try {
                const res = await fetch('/api/connect?port=' + encodeURIComponent(port));
                const result = await res.json();
                if(result.success) {
                    document.getElementById('setup-screen').style.display = 'none';
                    document.getElementById('dashboard-screen').style.display = 'block';
                    changeInterval();
                    fetchApi('/api/data');
                } else {
                    document.getElementById('setup-error').innerText = result.error;
                }
            } catch(e) { document.getElementById('setup-error').innerText = 'Netzwerkfehler.'; }
        }

        async function changeInterval() {
            const ms = document.getElementById('intervalSelect').value;
            await fetch('/api/interval?ms=' + ms);
            if(fetchIntervalId) clearInterval(fetchIntervalId);
            fetchIntervalId = setInterval(() => fetchApi('/api/data'), parseInt(ms));
        }

        async function fetchApi(url) {
            try {
                const response = await fetch(url);
                globalBatteries = await response.json();
                if(globalBatteries && globalBatteries.length > 0) {
                    renderSummary();
                    renderPackList();
                    if(activeModalId !== null) renderModal(activeModalId);
                }
            } catch (e) { console.error(e); }
        }

        function getCellColor(voltage, minVoltage) {
            const diff = (voltage - minVoltage) * 1000;
            if (diff >= 30) return 'var(--danger)';
            if (diff >= 15) return 'var(--warning)';
            return 'var(--success)';
        }

        function getStatusBadge(status) {
            if(!status) return '';
            let st = status.toLowerCase();
            if(st === 'charge') return `<span class='badge badge-charge'>LÄDT</span>`;
            if(st === 'dischg') return `<span class='badge badge-dischg'>ENTLÄDT</span>`;
            if(st === 'idle') return `<span class='badge badge-normal'>STANDBY</span>`;
            return `<span class='badge badge-normal'>${status}</span>`;
        }

        function renderSummary() {
            if(globalBatteries.length === 0) return;
            let sumSoc = 0, sumVolt = 0, sumCurr = 0, sumSoh = 0;
            let validSohCount = 0;

            globalBatteries.forEach(b => { 
                sumSoc += b.Soc; 
                sumVolt += b.Voltage; 
                sumCurr += b.Current; 
                if (b.Soh > 0) {
                    sumSoh += b.Soh;
                    validSohCount++;
                }
            });

            if (validSohCount > 0) {
                document.getElementById('sys-soh').innerText = Math.round(sumSoh / validSohCount) + ' %';
            } else {
                document.getElementById('sys-soh').innerText = 'Lade...';
            }

            document.getElementById('sys-soc').innerText = (sumSoc / globalBatteries.length).toFixed(1) + ' %';
            document.getElementById('sys-volt').innerText = (sumVolt / globalBatteries.length).toFixed(2) + ' V';
            document.getElementById('sys-curr').innerText = sumCurr.toFixed(2) + ' A';
            document.getElementById('sys-packs').innerText = globalBatteries.length;
        }

        function renderPackList() {
            let html = '';
            globalBatteries.forEach(bat => {
                let alarms = [];
                if(bat.VoltState && bat.VoltState.toLowerCase() !== 'normal') alarms.push('Volt: ' + bat.VoltState);
                if(bat.CurrState && bat.CurrState.toLowerCase() !== 'normal') alarms.push('Strom: ' + bat.CurrState);
                if(bat.TempState && bat.TempState.toLowerCase() !== 'normal') alarms.push('Temp: ' + bat.TempState);

                let alarmHtml = '';
                if(alarms.length > 0) {
                    alarmHtml = `<div class='badge badge-alarm'>⚠️ ALARM: ${alarms.join(' | ')}</div>`;
                }

                html += `
                <div class='pack-card' onclick='openModal(${bat.Id})'>
                    <div class='pack-title'>
                        <div>Akku #${bat.Id} ${getStatusBadge(bat.Status)}</div>
                        <span style='color:var(--primary)'>${bat.Soc}%</span>
                    </div>
                    <div class='pack-info'><span>Spannung:</span> <strong>${bat.Voltage.toFixed(2)} V</strong></div>
                    <div class='pack-info'><span>Strom:</span> <strong>${bat.Current.toFixed(2)} A</strong></div>
                    <div class='pack-info'><span>Temp (BMS/Zelle):</span> <strong>${bat.Temperature.toFixed(1)}°C / ${bat.CellTempMax.toFixed(1)}°C</strong></div>
                    <div class='pack-info'><span>Zell-Drift:</span> <strong>${bat.CellDelta} mV</strong></div>
                    ${alarmHtml}
                </div>`;
            });
            document.getElementById('pack-list').innerHTML = html;
        }

        function openModal(id) {
            activeModalId = id;
            document.getElementById('detail-modal').style.display = 'block';
            renderModal(id);
        }

        function closeModal(force) {
            if(force === true || force.target.id === 'detail-modal') {
                document.getElementById('detail-modal').style.display = 'none';
                activeModalId = null;
            }
        }

        function renderModal(id) {
            const bat = globalBatteries.find(b => b.Id === id);
            if(!bat) return;

            let sohText = (bat.Soh > 0) ? bat.Soh + '%' : 'Lade...';
            let cyclesText = (bat.Soh > 0) ? bat.Cycles : 'Lade...';
            let metaText = `SOH: ${sohText} | Zyklen: ${cyclesText} | FW: ${bat.Firmware} | SN: ${bat.Barcode}`;

            let html = `
                <h2 style='margin-top:0;'>Akku #${bat.Id} Details ${getStatusBadge(bat.Status)}</h2>
                <div class='meta-info'><span>${bat.DeviceName}</span><span>${metaText}</span></div>
                
                <div class='header-grid'>
                    <div class='stat-card'><div class='stat-label'>SOC</div><div class='stat-value'>${bat.Soc}%</div></div>
                    <div class='stat-card'><div class='stat-label'>Spannung</div><div class='stat-value'>${bat.Voltage.toFixed(3)} V</div></div>
                    <div class='stat-card'><div class='stat-label'>Strom</div><div class='stat-value'>${bat.Current.toFixed(2)} A</div></div>
                    <div class='stat-card'><div class='stat-label'>Zell-Drift</div><div class='stat-value'>${bat.CellDelta} mV</div></div>
                    <div class='stat-card'><div class='stat-label'>BMS Temp</div><div class='stat-value'>${bat.Temperature.toFixed(1)} °C</div></div>
                    <div class='stat-card'><div class='stat-label'>Zell Temp</div><div class='stat-value'>${bat.CellTempMin.toFixed(1)} - ${bat.CellTempMax.toFixed(1)} °C</div></div>
                </div>

                <div class='stat-label' style='margin-bottom:10px;'>Einzelzellen (Min: ${bat.CellMin.toFixed(3)} V | Max: ${bat.CellMax.toFixed(3)} V)</div>
                <div class='cells-grid'>
            `;

            Object.entries(bat.Cells).forEach(([cellId, voltage]) => {
                const bgColor = getCellColor(voltage, bat.CellMin);
                html += `
                    <div class='cell' style='background-color: ${bgColor}'>
                        <div style='font-size:10px; opacity:0.9; margin-bottom:2px;'>Zelle ${parseInt(cellId) + 1}</div>
                        ${voltage.toFixed(3)}V
                    </div>
                `;
            });
            html += `</div>`;

            html += `<div class='log-section'><div class='stat-label' style='margin-bottom:10px;'>Letzte Ereignisse (Logbuch)</div>`;
            if (bat.Events && bat.Events.length > 0) {
                const recentLogs = bat.Events.slice(-4); 
                recentLogs.forEach(ev => {
                    html += `<div class='log-item'><span class='log-time'>${ev.Time}</span> ${ev.Info}</div>`;
                });
            } else {
                html += `<div class='log-item' style='color: var(--text-muted)'>Keine Einträge vorhanden (oder laden noch...).</div>`;
            }
            html += `</div>`;

            document.getElementById('modal-body').innerHTML = html;
        }
    </script>
</body>
</html>";
        }
    }
}