using GMap.NET.WindowsForms;
using MissionPlanner;
using MissionPlanner.Controls;
using MissionPlanner.GCSViews;
using MissionPlanner.Maps;
using MissionPlanner.Plugin;
using MissionPlanner.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;


namespace SoleonServicePlugin
{
    internal class SoleonService_Plugin : Plugin
    {
        //TabPage
        private System.Windows.Forms.TabPage tab = new System.Windows.Forms.TabPage();
        private TabControl tabctrl;
        private SoleonService_UI mySoService_UI = new SoleonService_UI();


        public override string Name { get; } = "Soleon Service Plugin";
        public override string Version { get; } = "0.0.0";
        public override string Author { get; } = "www.soleon.it";
        public override bool Init() 
        {
            mySoService_UI.setHost(Host);
            mySoService_UI.setVer("SoleonService V" + Version);
            return true; 
        }

        public override bool Loaded()
        {
            Host.MainForm.FlightData.TabListOriginal.Add(tab);

            //    Console.WriteLine("------- Soleon Plugin started up ---------");
            tabctrl = Host.MainForm.FlightData.tabControlactions;
            // set the display name
            tab.Text = "Soleon Service";
            // set the internal id
            tab.Name = "tabSoleonService";
            // add the usercontrol to the tabpage
            tab.Controls.Add(mySoService_UI);

            tabctrl.TabPages.Insert(5, tab);
            // tabctrl.TabPages.Insert(0, tab);

            //Host.MainForm.FlightPlanner.updateDisplayView();

            ThemeManager.ApplyThemeTo(tab);


            //- Set Loop() to be called at 1Hz (1Hz is max allowed) NOT USED!!
            //loopratehz = 1;
            return true;
        }

        public override bool Loop()
        {
            return true;
        }


        public override bool Exit() { return true; }

    }
}
