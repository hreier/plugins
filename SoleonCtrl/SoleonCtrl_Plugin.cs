using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Text;
using MissionPlanner.Utilities;
using MissionPlanner.Controls;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using MissionPlanner;
using System.Drawing;
using GMap.NET.WindowsForms;
using MissionPlanner.GCSViews;
using MissionPlanner.Maps;
using System.Timers;

using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Drawing.Drawing2D;


namespace SoleonCtrl_Plugin
{
    internal class SoleonCtrl_Plugin : MissionPlanner.Plugin.Plugin
    {
        public override string Name { get; } = "SoleonCtrl Plugin";
        public override string Version { get; } = "0.0.0";
        public override string Author { get; } = "soleon.it";
        public override bool Init() { return true; }
        public override bool Loaded()
        {
            return true;
        }
        public override bool Exit() { return true; }


    }
}
