# ⚡ Pylontech-Watch

Leichtgewichtiges Windows-Dashboard zur Live-Überwachung von Pylontech-Batterien (BMS) über USB/RS232. Bietet einen lokalen Web-Server, direkten WLAN-Zugriff und eine nahtlose Node-RED-Integration.

## ✨ Features
* **Live-Daten:** Übersicht über SOC, SOH, Gesamtspannung, Strom und Temperaturen.
* **Zell-Ebene:** Detaillierte Ansicht aller Einzelzellenspannungen inklusive Zell-Drift.
* **Plug & Play:** Keine komplizierte Installation. Alles läuft über eine einzelne ausführbare Datei.
* **REST API:** Direkter Datenabruf als sauberes JSON.

---

## 🚀 Schnellstart (Lokaler PC)
1. Lade dir die aktuelle `PylontechWatch.exe` aus dem Bereich **Releases** herunter.
2. Verbinde deinen Windows-PC über ein RS232-zu-USB-Kabel mit dem Konsolen-Port der Pylontech-Batterie.
3. Starte die `.exe`. Das Dashboard öffnet sich automatisch in deinem Browser.
4. Wähle im Menü den passenden COM-Port aus und klicke auf "Verbinden".

---

## 🌍 WLAN-Zugriff & Zuschauer-Modus (Multi-User)

Das Pylontech-Watch Dashboard kann problemlos im gesamten Heimnetzwerk aufgerufen werden (z. B. auf dem Handy, Tablet oder über Node-RED). Dank des intelligenten **Zuschauer-Modus** können beliebig viele Geräte gleichzeitig auf die Live-Daten zugreifen, ohne dass sich der USB-COM-Port blockiert!

Damit Windows den Zugriff aus dem lokalen Netzwerk erlaubt, müssen auf dem PC, an dem der Akku per USB angeschlossen ist, **einmalig** folgende Einstellungen vorgenommen werden:

### 1. Programm als Administrator starten
Die ausführbare Datei muss zwingend mit einem Rechtsklick -> **"Als Administrator ausführen"** gestartet werden. Nur so darf der integrierte Webserver die Daten im Netzwerk bereitstellen (er leuchtet in der Konsole dann grün).

### 2. Netzwerkprofil auf "Privat" stellen
Windows blockiert externe Zugriffe strikt, wenn das Netzwerk als "Öffentlich" deklariert ist.
* Klicke unten rechts in der Windows-Taskleiste auf das WLAN- oder Netzwerk-Symbol.
* Klicke auf deine aktuelle Verbindung und öffne die **Eigenschaften** (oder das Info-Symbol).
* Stelle das Netzwerkprofil zwingend auf **Privat**.

### 3. Port 5000 in der Firewall freigeben
Die Windows-Firewall blockiert den benötigten Port standardmäßig. Du kannst ihn mit einem einfachen Befehl öffnen:
1. Drücke die Windows-Taste und tippe **`powershell`** ein.
2. Klicke auf der rechten Seite auf **"Als Administrator ausführen"**.
3. Kopiere den folgenden Befehl, füge ihn mit einem Rechtsklick in das blaue Fenster ein und drücke Enter:

```powershell
New-NetFirewallRule -DisplayName "Pylontech-Dashboard Port 5000" -Direction Inbound -LocalPort 5000 -Protocol TCP -Action Allow
```

### 4. Dashboard auf dem Handy/Tablet aufrufen
Sobald das Programm auf dem Haupt-PC läuft, kannst du auf jedem beliebigen Gerät in deinem WLAN den Browser öffnen. Gib dort einfach die lokale IP-Adresse deines Haupt-PCs gefolgt von `:5000` ein.

> **Beispiel-URL:** `http://192.168.178.45:5000` *(Achte darauf, `http://` und nicht `https://` zu verwenden!)*

---

## 🤖 Node-RED Integration
Dieses Tool ist zu 100 % kompatibel mit dem inoffiziellen Node-RED Baustein `node-red-contrib-pylontech-monitor`.
* Installiere den Node in Node-RED.
* Trage in den Node-Einstellungen im Feld "ESP32 IP/Host" die IP-Adresse deines Windows-PCs **inklusive Port 5000** ein (z. B. `192.168.178.45:5000`).
* Der Node ruft die JSON-Daten über die Route `/api/data` ab.
* Das fertige Daten-Objekt wird automatisch unter `msg.payload` ausgegeben.

---
**Disclaimer / Rechtlicher Hinweis:** Dies ist ein inoffizielles Community-Projekt. Es steht in keinerlei Verbindung zur Firma Pylontech.
