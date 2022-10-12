using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using HWND = System.IntPtr;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Automation;
using System.Collections;
using System.Windows;

namespace WindowMover
{
    class Program
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(HandleRef hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        internal struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public ShowWindowCommands showCmd;
            public System.Drawing.Point ptMinPosition;
            public System.Drawing.Point ptMaxPosition;
            public System.Drawing.Rectangle rcNormalPosition;
        }

        internal enum ShowWindowCommands : int
        {
            Hide = 0,
            Normal = 1,
            Minimized = 2,
            Maximized = 3,
        }

        const int DSP_RIGHT  = 0;
        const int DSP_CENTER = 1;
        const int DSP_LEFT   = 2;

        const int SW_HIDE     = 0;
        const int SW_NORMAL   = 1;
        const int SW_MAXIMIZE = 3;
        const int SW_RESTORE  = 9;

        private static void Main()
        {
            IntPtr oneNoteHwnd = new IntPtr();
            IntPtr teamsHwnd = new IntPtr();
            IntPtr chromeHandle = new IntPtr();
            List<Process> pWithTitle = new List<Process>();
            List<WINDOWPLACEMENT> showStates = new List<WINDOWPLACEMENT>();
            List<window> windows = new List<window>();

            pWithTitle = GetProcessesWithTitles();

            Process[] processes = Process.GetProcesses();
            var pList = processes.Where(p => !(p.MainWindowTitle.Equals(""))).ToList();

            foreach (Process p in pWithTitle)
            {
                window w = new window();
                w.handle = p.Handle;
                w.showState = GetPlacement(p.Handle);
            
                windows.Add(w);
            }

            var monitor = MonitorChanger.GetDevice(DSP_RIGHT);
            var monitor2 = MonitorChanger.GetDevice(DSP_LEFT);

            List<string> commands = new List<string>();

            commands.Add(@"cd C:\multimonitortool-x64");
            commands.Add(@"MultiMonitorTool.exe /MoveWindow Primary All");

            RunCommands(commands, false);
            commands.Clear();

            // In case the monitor order gets change, set it back
            MonitorChanger.SetMonitorOrder();

            //chromeHandle = GetMainWindowHandle("chrome");
            //chromeHandle = GetHandleByTitle(pWithTitle, "github window");
            chromeHandle = Chrome.GetChromeWindowByTitle("github window");

            oneNoteHwnd = GetHandleByName(pWithTitle, "ONENOTE");
            teamsHwnd = GetHandleByName(pWithTitle, "Teams");

            // Need to first switch to normal before changing position
            // If this is not done and the window is maximized on another monitor it will move the window but not maximize it
            ShowWindow(chromeHandle, SW_NORMAL);
            //Thread.Sleep(1000);

            SetWindowPos(chromeHandle, 0, -1700, 100, 1300, 800, 0);
            ShowWindow(chromeHandle, SW_MAXIMIZE);
            SetForegroundWindow(chromeHandle);

            ShowWindow(oneNoteHwnd, SW_NORMAL);
            SetWindowPos(oneNoteHwnd, 0, -1800, 50, 1500, 900, 0);
            SetForegroundWindow(oneNoteHwnd);

            ShowWindow(teamsHwnd, SW_NORMAL);
            SetWindowPos(teamsHwnd, 0, -1700, 100, 1300, 800, 0);
            SetForegroundWindow(teamsHwnd);

            UserInterface.MoveMinimizedWindowsToMainMonitor();
        }

        private struct window
        {
            public IntPtr handle;
            public WINDOWPLACEMENT showState;
        }

        private static void CheckTime(Stopwatch watch, string number)
        {
            watch.Stop();
            Debug.WriteLine($"Execution Time {number}: {watch.ElapsedMilliseconds} ms");
            watch.Start();
        }

        private static List<Process> GetProcessesWithTitles()
        {
            Process[] processes = Process.GetProcesses();
            var pList = processes.Where(p => !(p.MainWindowTitle.Equals(""))).ToList();
#if DEBUG
            Debug.WriteLine("\n*******************************");
            Debug.WriteLine("* Processes with Title");
            Debug.WriteLine("*******************************");
            foreach (Process p in pList)
            {
                Debug.WriteLine("Process: {0,-25} ID: {1,-10}  Handle: {3,-10} Window title: {2,-10}", p.ProcessName, p.Id, p.MainWindowTitle, p.MainWindowHandle);
            }
#endif

            return pList;
        }

        private static IntPtr GetHandleByTitle(List<Process> pList, string title)
        {
            IntPtr handle = new HWND();
            Process process = new Process();
            try
            {
                handle = pList.Where(p => p.MainWindowTitle.Equals(title)).Single().MainWindowHandle;
            }
            catch (Exception ex)
            {
                MessageBox.Show(@"Could not find a window named """ + title + @""".");
                Console.WriteLine(String.Format("{0}", ex.ToString()));
            }

            return handle;
        }

        private static IntPtr GetHandleByName(List<Process> pList, string name)
        {
            IntPtr handle = new HWND();
            Process process = new Process();

            try
            {
                handle = pList.Where(p => p.ProcessName.Equals(name)).Single().MainWindowHandle;
            }
            catch (Exception ex)
            {
                MessageBox.Show(@"Could not find a window named """ + name + @""".");
                Console.WriteLine(String.Format("{0}", ex.ToString()));
            }

            return handle;
        }

        private static void MoveWindow(IntPtr hWnd)
        {
            HandleRef hr = new HandleRef(null, hWnd);
            RECT rct;
            var placement = GetPlacement(hWnd);

            GetWindowRect(hr, out rct);

            ShowWindow(hWnd, SW_NORMAL);

            // Move window 1920 to the left
            SetWindowPos(hWnd, 0, rct.Left - 1920, rct.Top, rct.Right - rct.Left, rct.Bottom - rct.Top, 0);

            // set the nCmdShow back to what it was before
            ShowWindow(hWnd, (int)placement.showCmd);

            SetForegroundWindow(hWnd);
        }

        private static WINDOWPLACEMENT GetPlacement(IntPtr hwnd)
        {
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(placement);
            GetWindowPlacement(hwnd, ref placement);
            return placement;
        }

        private static void DisconnectLaptopDisplay(string deviceName)
        {
            List<string> commands = new List<string>();

            commands.Add(@"cd C:\multimonitortool-x64");
            commands.Add(@"MultiMonitorTool.exe /disable " + deviceName);

            RunCommands(commands, false);
            commands.Clear();
        }

        private static void ConnectLaptopDisplay(string deviceName)
        {
            List<string> commands = new List<string>();

            commands.Add(@"cd C:\multimonitortool-x64");
            commands.Add(@"MultiMonitorTool.exe /enable " + deviceName);

            RunCommands(commands, false);
            commands.Clear();
        }

        private static void MoveWindowsToPrimary()
        {
            List<string> commands = new List<string>();

            commands.Add(@"cd C:\multimonitortool-x64");
            commands.Add(@"MultiMonitorTool.exe /MoveWindow Primary All");

            RunCommands(commands, false);
            commands.Clear();
        }

        //******************************************************************
        //
        //  Name:   RunCommands
        //
        //  Description:    Takes a list of commands and runs them in cmd.exe
        //
        //  Parameters:     arguments - list of commands to run
        //                  showCmdWindow - flag to show the cmd window
        //                  (if this is true, you will need to close the
        //                  cmd window when it is done processing)
        //
        //******************************************************************
        private static void RunCommands(List<string> arguments, Boolean showCmdWindow)
        {
            Process p = new Process();
            ProcessStartInfo ps = new ProcessStartInfo();
            string commandString;

            if (showCmdWindow)
            {
                commandString = @"/k ";
            }
            else
            {
                commandString = @"/c ";
                ps.CreateNoWindow = true;
            }

            ps.FileName = "cmd.exe";

            ps.WindowStyle = ProcessWindowStyle.Normal;
            ps.UseShellExecute = false;

            foreach (string argument in arguments)
            {
                commandString += argument + "&";
            }

            ps.Arguments = commandString;

            p.StartInfo = ps;
            p.Start();

            p.WaitForExit();
        }
    }

    public class WindowFinder
    {
        // For Windows Mobile, replace user32.dll with coredll.dll
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        public static IntPtr FindWindow(string caption)
        {
            return FindWindow(null, caption);
        }
    }
}