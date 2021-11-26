﻿using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Xml.Linq;

namespace LenovoLegionToolkit.Lib.Utils
{
    public struct MachineInformation
    {
        public string Vendor;
        public string Model;
    }

    public struct VideoCardInformation
    {
        public string Manufacturer;
        public string PnpDeviceId;
    }

    public struct NVidiaInformation
    {
        public bool DisplayActive;
        public int ProcessCount;
        public IEnumerable<string> ProcessNames;
    }

    public static class VideoCardInformationExtensions
    {
        public static bool IsNVidia(this VideoCardInformation vci) => vci.Manufacturer.Contains("nvidia", System.StringComparison.OrdinalIgnoreCase);
    }

    public static class OS
    {
        public static void Restart() => ExecuteProcess("shutdown", "-r -t 0");

        public static void RestartDevice(string _pnpDeviceId) => ExecuteProcess("pnputil", $"-restart-device \"{_pnpDeviceId}\"");

        public static void SetPowerPlan(string guid) => ExecuteProcess("powercfg", $"-setactive {guid}");

        public static NVidiaInformation GetNVidiaInformation()
        {
            var output = ExecuteProcessForOutput("nvidia-smi", "-q -x");

            var xdoc = XDocument.Parse(output);
            var gpu = xdoc.Element("nvidia_smi_log").Element("gpu");
            var displayActive = gpu.Element("display_active").Value == "Enabled";
            var processInfo = gpu.Element("processes").Elements("process_info");
            var processesCount = processInfo.Count();
            var processNames = processInfo.Select(e => e.Element("process_name").Value).Select(Path.GetFileName);

            return new NVidiaInformation
            {
                DisplayActive = displayActive,
                ProcessCount = processesCount,
                ProcessNames = processNames,
            };

        }

        public static MachineInformation GetMachineInformation()
        {
            var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_ComputerSystemProduct");
            foreach (var queryObj in searcher.Get())
            {
                var vendor = queryObj["Vendor"].ToString();
                var model = queryObj["Version"].ToString();
                return new MachineInformation
                {
                    Vendor = vendor,
                    Model = model,
                };
            }
            return default;
        }
        public static IEnumerable<VideoCardInformation> GetVideoControllersInformation()
        {
            var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_VideoController");
            foreach (var queryObj in searcher.Get())
            {
                var manufacturer = queryObj["AdapterCompatibility"].ToString();
                var pnpDeviceId = queryObj["PNPDeviceID"].ToString();
                yield return new VideoCardInformation
                {
                    Manufacturer = manufacturer,
                    PnpDeviceId = pnpDeviceId,
                };
            }
        }

        private static void ExecuteProcess(string file, string arguments)
        {
            var cmd = new Process();
            cmd.StartInfo.UseShellExecute = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            cmd.StartInfo.FileName = file;
            cmd.StartInfo.Arguments = arguments;
            cmd.Start();
            cmd.WaitForExit();
        }

        private static string ExecuteProcessForOutput(string file, string arguments)
        {
            var cmd = new Process();
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            cmd.StartInfo.FileName = file;
            cmd.StartInfo.Arguments = arguments;
            cmd.Start();
            var output = cmd.StandardOutput.ReadToEnd();
            cmd.WaitForExit();
            return output;
        }
    }
}