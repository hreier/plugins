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


namespace SoleonCtrl_Plugin
{
    internal class SoleonCtrl_Plugin : MissionPlanner.Plugin.Plugin
    {
        
        //TabPage
        private System.Windows.Forms.TabPage tab = new System.Windows.Forms.TabPage();
        private TabControl tabctrl;
        private SoleonCtrl_UI mySoCtrl_UI = new SoleonCtrl_UI();


        public override string Name { get; } = "SoleonCtrl Plugin";
        public override string Version { get; } = "1.0.0";
        public override string Author { get; } = "soleon.it";
        
        public override bool Init() 
        {
            mySoCtrl_UI.setHost(Host);
            mySoCtrl_UI.setVer("SoleonCtrl V" + Version);

            return true; 
        }
        
        public override bool Loaded()
        {
            forceSettings();  //???

            
            Host.MainForm.FlightData.TabListOriginal.Add(tab);

        //    Console.WriteLine("------- Soleon Plugin started up ---------");
            tabctrl = Host.MainForm.FlightData.tabControlactions;
            // set the display name
            tab.Text = "Soleon Control";
            // set the internal id
            tab.Name = "tabSoleonCtrl";
            // add the usercontrol to the tabpage
            tab.Controls.Add(mySoCtrl_UI);

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
            return mySoCtrl_UI.Loop();
        }


        public override bool Exit() { return true; }

        private void forceSettings()
        {
            string tabs = Settings.Instance["tabcontrolactions"];

            // setup default if doesnt exist
            if (tabs == null)
            {
                CustomMessageBox.Show("Restart Mission Planner to enable Drone ID Tab. Disable Plugin if Not Required CTRL-P");
                Host.MainForm.FlightData.saveTabControlActions();
                tabs = Settings.Instance["tabcontrolactions"];
                Settings.Instance.Save();
            }
        }

    }
}
