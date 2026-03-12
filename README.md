# WoW BG Filter


A specialized C# WinForms utility designed for real-time monitoring and automation of World of Warcraft (WoW) Battleground (BG) queues by intercepting game traffic and automating queue management.

![App Demo](./assets/anim1.gif)

## 🚀 Key Features
*   **Real-Time Packet Interception**: Uses Windows Sockets to peek into incoming game packets, identifying the specific battleground type before the invitation is even shown.
*   **Live Memory Integration**: Out of process memory reading, syncing intercepted data with real-time game client variables.
*   **Custom Filter System**: User-configurable filters to automatically "Ignore" unwanted battlegrounds (e.g., Isle of Conquest, Alterac Valley) based on personal preference.
*   **Workflow Automation**: 
    *   **Auto-Queue**: Automatically applies for Random Battleground invitations.
    *   **Auto-Accept/Leave**: Instantly accepts desired invitations or leaves the queue if the match doesn't meet filter criteria.

## ⚙️ How It Works
*   **Asynchronous Socket Handling**: Manages a non-blocking socket to monitor TCP/UDP payloads without interfering with game latency.
*   **Memory Offset Mapping**: Maps specific game client offsets to C# variables to track queue timers and character states in real-time.
*   **Automated State Machine**: Handles the transition between "Queued," "Invited," and "In-Progress" states automatically based on filtered criteria.

## 🏁 Getting Started
1. **Clone the Repository**: Use the GitHub Desktop client or run `git clone https://github.com/seg3214/WowBgFilter`.  
2. **Prerequisites**: Ensure you have the .NET Framework installed.  
  
## ⚠️ Disclaimer
This project was developed for educational purposes regarding network protocol analysis and memory management in C#. Use of such tools in live games may violate Terms of Service.
