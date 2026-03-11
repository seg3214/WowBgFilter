# WoW Battleground Queue Automator

A specialized C# WinForms utility designed to optimize the World of Warcraft PvP experience by intercepting game traffic and automating queue management.

## 🚀 Key Features
*   **Real-Time Packet Interception**: Uses Windows Sockets to peek into incoming game packets, identifying the specific battleground type before the invitation is even shown.
*   **Live Memory Integration**: Leverages the **BlackMagic** library for external memory reading, syncing intercepted data with real-time game client variables.
*   **Custom Filter System**: User-configurable filters to automatically "Ignore" unwanted battlegrounds (e.g., Isle of Conquest, Alterac Valley) based on personal preference.
*   **Workflow Automation**: 
    *   **Auto-Queue**: Automatically applies for Random Battleground invitations.
    *   **Auto-Accept/Leave**: Instantly accepts desired invitations or leaves the queue if the match doesn't meet filter criteria.

## 🛠️ Technical Implementation
*   **Language/Framework**: C# .NET WinForms.
*   **Networking**: Implemented raw socket sniffing to capture and deserialize TCP/UDP payloads in real-time.
*   **Process Manipulation**: Utilized the `BlackMagic.dll` wrapper for safe, managed external memory access (Reading pointers/offsets).
*   **Architecture**: Built using a central **Data Controller** class that orchestrates memory-read variables and network-captured packet data into a unified state machine.

## ⚠️ Disclaimer
This project was developed for educational purposes regarding network protocol analysis and memory management in C#. Use of such tools in live games may violate Terms of Service.
