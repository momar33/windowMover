﻿using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace WindowMover
{
    class MonitorChanger
    {
        private static Monitor leftMonitor = new Monitor();
        private static Monitor centerMonitor = new Monitor();
        private static Monitor rightMonitor = new Monitor();
        private const int DM_POSITION = 0x00000100;
        private const int DM_PELSWIDTH = 0x00080000;
        private const int DM_PELSHEIGHT = 0x00100000;
        public static void SetMonitorOrder()
        {
            DISPLAY_DEVICE device = new DISPLAY_DEVICE();
            var deviceMode = new DEVMODE();
            device.cb = Marshal.SizeOf(device);
            try
            {
                Debug.WriteLine("\n*******************************");
                Debug.WriteLine("* Device Info");
                Debug.WriteLine("*******************************");
                for (uint id = 0; NativeMethods.EnumDisplayDevices(null, id, ref device, 0); id++)
                {
                    Debug.WriteLine(
                        String.Format("{0}, {1}, {2}, {3}, {4}, {5}",
                                 id,
                                 device.DeviceName,
                                 device.DeviceString,
                                 device.StateFlags,
                                 device.DeviceID,
                                 device.DeviceKey
                                 )
                                  );
                    if ((device.StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) == DisplayDeviceStateFlags.AttachedToDesktop)
                    {
                        NativeMethods.EnumDisplaySettings(device.DeviceName, -1, ref deviceMode);

                        // If Intel then right monitor

                        // If primary then center monitor

                        // else left monitor

                        if (device.DeviceString == "Intel(R) UHD Graphics")
                        {
                            rightMonitor.name = device.DeviceName;
                            rightMonitor.devmode = deviceMode;
                        }
                        else if ((device.StateFlags & DisplayDeviceStateFlags.PrimaryDevice) == DisplayDeviceStateFlags.PrimaryDevice)
                        {
                            centerMonitor.name = device.DeviceName;
                            centerMonitor.devmode = deviceMode;
                        }
                        else
                        {
                            leftMonitor.name = device.DeviceName;
                            leftMonitor.devmode = deviceMode;
                        }
                    }
                    device.cb = Marshal.SizeOf(device);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("{0}", ex.ToString()));
            }

            // Arrange the displays using position x in case the order was messed up from reconnecting
            leftMonitor.devmode.dmPosition.x = -1920;
            centerMonitor.devmode.dmPosition.x = 0;
            rightMonitor.devmode.dmPosition.x = 2560;

            NativeMethods.ChangeDisplaySettingsEx(leftMonitor.name, ref leftMonitor.devmode, (IntPtr)null, 0, IntPtr.Zero);
            NativeMethods.ChangeDisplaySettingsEx(centerMonitor.name, ref centerMonitor.devmode, (IntPtr)null, 0, IntPtr.Zero);
            NativeMethods.ChangeDisplaySettingsEx(rightMonitor.name, ref rightMonitor.devmode, (IntPtr)null, 0, IntPtr.Zero);


            Debug.WriteLine("\n*******************************");
            Debug.WriteLine("* Device Positions");
            Debug.WriteLine("*******************************");

            NativeMethods.EnumDisplaySettings(leftMonitor.name, -1, ref leftMonitor.devmode);
            Debug.WriteLine("Left Monitor   --- deviceName: {0}   Position: {1}", leftMonitor.name, leftMonitor.devmode.dmPosition.x);
            NativeMethods.EnumDisplaySettings(centerMonitor.name, -1, ref centerMonitor.devmode);
            Debug.WriteLine("Center Monitor --- deviceName: {0}   Position: {1}", centerMonitor.name, centerMonitor.devmode.dmPosition.x);
            NativeMethods.EnumDisplaySettings(rightMonitor.name, -1, ref rightMonitor.devmode);
            Debug.WriteLine("Right Monitor  --- deviceName: {0}   Position: {1}", rightMonitor.name, rightMonitor.devmode.dmPosition.x);
        }

        struct Monitor
        {
            public string name;
            public DEVMODE devmode;
        }

        public static DISPLAY_DEVICE GetDevice(int display)
        {
            DISPLAY_DEVICE returnDevice = new DISPLAY_DEVICE();
            DISPLAY_DEVICE device = new DISPLAY_DEVICE();
            device.cb = Marshal.SizeOf(device);

            try
            {
                for (uint id = 0; NativeMethods.EnumDisplayDevices(null, id, ref device, 0); id++)
                {
                    if ((device.StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) == DisplayDeviceStateFlags.AttachedToDesktop)
                    {

                        // If Intel then right monitor
                        if ((device.DeviceString == "Intel(R) UHD Graphics") && (display == 0))
                        {
                            Debug.WriteLine("\nRight Monitor  = Device Id: {0}  Name: {1}", id, device.DeviceName);
                            returnDevice = device;
                            break;
                        }
                        // If primary then center monitor
                        else if (((device.StateFlags & DisplayDeviceStateFlags.PrimaryDevice) == DisplayDeviceStateFlags.PrimaryDevice) && (display == 1))
                        {
                            Debug.WriteLine("\nCenter Monitor = Device Id: {0}  Name: {1}", id, device.DeviceName);
                            returnDevice = device;
                            break;
                        }
                        // else left monitor
                        else if ((device.DeviceString == "NVIDIA Quadro T1000") && (display == 2))
                        {
                            Debug.WriteLine("\nLeft Monitor  = Device Id: {0}  Name: {1}", id, device.DeviceName);
                            returnDevice = device;
                            break;
                        }
                    }
                    device.cb = Marshal.SizeOf(device);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("{0}", ex.ToString()));
            }

            return returnDevice;
        }

        public class NativeMethods
        {
            [DllImport("user32.dll")]
            public static extern DISP_CHANGE ChangeDisplaySettingsEx(string lpszDeviceName, ref DEVMODE lpDevMode, IntPtr hwnd, ChangeDisplaySettingsFlags dwflags, IntPtr lParam);

            [DllImport("user32.dll")]
            // A signature for ChangeDisplaySettingsEx with a DEVMODE struct as the second parameter won't allow you to pass in IntPtr.Zero, so create an overload
            public static extern DISP_CHANGE ChangeDisplaySettingsEx(string lpszDeviceName, IntPtr lpDevMode, IntPtr hwnd, ChangeDisplaySettingsFlags dwflags, IntPtr lParam);

            [DllImport("user32.dll")]
            public static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

            [DllImport("user32.dll")]
            public static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);
        }

        public enum DISP_CHANGE : int
        {
            Successful = 0,
            Restart = 1,
            Failed = -1,
            BadMode = -2,
            NotUpdated = -3,
            BadFlags = -4,
            BadParam = -5,
            BadDualView = -6
        }

        [Flags()]
        public enum ChangeDisplaySettingsFlags : uint
        {
            CDS_NONE = 0,
            CDS_UPDATEREGISTRY = 0x00000001,
            CDS_TEST = 0x00000002,
            CDS_FULLSCREEN = 0x00000004,
            CDS_GLOBAL = 0x00000008,
            CDS_SET_PRIMARY = 0x00000010,
            CDS_VIDEOPARAMETERS = 0x00000020,
            CDS_ENABLE_UNSAFE_MODES = 0x00000100,
            CDS_DISABLE_UNSAFE_MODES = 0x00000200,
            CDS_RESET = 0x40000000,
            CDS_RESET_EX = 0x20000000,
            CDS_NORESET = 0x10000000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct DISPLAY_DEVICE
        {
            [MarshalAs(UnmanagedType.U4)]
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            [MarshalAs(UnmanagedType.U4)]
            public DisplayDeviceStateFlags StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [Flags()]
        public enum DisplayDeviceStateFlags : int
        {
            /// <summary>The device is part of the desktop.</summary>
            AttachedToDesktop = 0x1,
            MultiDriver = 0x2,
            /// <summary>The device is part of the desktop.</summary>
            PrimaryDevice = 0x4,
            /// <summary>Represents a pseudo device used to mirror application drawing for remoting or other purposes.</summary>
            MirroringDriver = 0x8,
            /// <summary>The device is VGA compatible.</summary>
            VGACompatible = 0x10,
            /// <summary>The device is removable; it cannot be the primary display.</summary>
            Removable = 0x20,
            /// <summary>The device has more display modes than its output devices support.</summary>
            ModesPruned = 0x8000000,
            Remote = 0x4000000,
            Disconnect = 0x2000000,
        }

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi)]
        public struct DEVMODE
        {
            public const int CCHDEVICENAME = 32;
            public const int CCHFORMNAME = 32;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
            [System.Runtime.InteropServices.FieldOffset(0)]
            public string dmDeviceName;
            [System.Runtime.InteropServices.FieldOffset(32)]
            public Int16 dmSpecVersion;
            [System.Runtime.InteropServices.FieldOffset(34)]
            public Int16 dmDriverVersion;
            [System.Runtime.InteropServices.FieldOffset(36)]
            public Int16 dmSize;
            [System.Runtime.InteropServices.FieldOffset(38)]
            public Int16 dmDriverExtra;
            [System.Runtime.InteropServices.FieldOffset(40)]
            public UInt32 dmFields;

            [System.Runtime.InteropServices.FieldOffset(44)]
            Int16 dmOrientation;
            [System.Runtime.InteropServices.FieldOffset(46)]
            Int16 dmPaperSize;
            [System.Runtime.InteropServices.FieldOffset(48)]
            Int16 dmPaperLength;
            [System.Runtime.InteropServices.FieldOffset(50)]
            Int16 dmPaperWidth;
            [System.Runtime.InteropServices.FieldOffset(52)]
            Int16 dmScale;
            [System.Runtime.InteropServices.FieldOffset(54)]
            Int16 dmCopies;
            [System.Runtime.InteropServices.FieldOffset(56)]
            Int16 dmDefaultSource;
            [System.Runtime.InteropServices.FieldOffset(58)]
            Int16 dmPrintQuality;

            [System.Runtime.InteropServices.FieldOffset(44)]
            public POINTL dmPosition;
            [System.Runtime.InteropServices.FieldOffset(52)]
            public Int32 dmDisplayOrientation;
            [System.Runtime.InteropServices.FieldOffset(56)]
            public Int32 dmDisplayFixedOutput;

            [System.Runtime.InteropServices.FieldOffset(60)]
            public short dmColor; // See note below!
            [System.Runtime.InteropServices.FieldOffset(62)]
            public short dmDuplex; // See note below!
            [System.Runtime.InteropServices.FieldOffset(64)]
            public short dmYResolution;
            [System.Runtime.InteropServices.FieldOffset(66)]
            public short dmTTOption;
            [System.Runtime.InteropServices.FieldOffset(68)]
            public short dmCollate; // See note below!
            [System.Runtime.InteropServices.FieldOffset(72)]
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
            public string dmFormName;
            [System.Runtime.InteropServices.FieldOffset(102)]
            public Int16 dmLogPixels;
            [System.Runtime.InteropServices.FieldOffset(104)]
            public Int32 dmBitsPerPel;
            [System.Runtime.InteropServices.FieldOffset(108)]
            public Int32 dmPelsWidth;
            [System.Runtime.InteropServices.FieldOffset(112)]
            public Int32 dmPelsHeight;
            [System.Runtime.InteropServices.FieldOffset(116)]
            public Int32 dmDisplayFlags;
            [System.Runtime.InteropServices.FieldOffset(116)]
            public Int32 dmNup;
            [System.Runtime.InteropServices.FieldOffset(120)]
            public Int32 dmDisplayFrequency;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINTL
        {
            public int x;
            public int y;
        }
    }
}
