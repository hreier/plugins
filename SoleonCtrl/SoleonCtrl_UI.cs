using BruTile.Wms;
using MissionPlanner.Plugin;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace MissionPlanner.Controls
{

    public partial class SoleonCtrl_UI : UserControl
    {
        Int32 spray_command = 0;
        public const Int32 BITMASK_BACK_R = 0x1;
        public const Int32 BITMASK_BACK_L = 0x2;
        public const Int32 BITMASK_FRONT_R = 0x4;
        public const Int32 BITMASK_FRONT_L = 0x8;

      //private static SoleonCtrl_Plugin _Instance;
        private byte target_mavlink_ID = 84;                  ///-Soleon Sprayer uses MAV_COMP_ID_USER60  (== 84)
        private Plugin.PluginHost _host = null;



        public void setVer(String msg)
        {
            SoleonCtrl.Text = msg;
        }
        public void setHost(Plugin.PluginHost host)
        {
            _host = host;
        }

        public SoleonCtrl_UI()
        {
            InitializeComponent();
        }
        private void UpdateDoSendScriptMsg(Int32 spray_cmd)
        {
            if (_host == null) return; 
            var mav = _host.comPort.MAVlist.FirstOrDefault(a => a.compid == (byte) target_mavlink_ID);
            // --- Send 'Int command' to payload; Note: doCommand sends 'long command' ---

            
            //if (mav != null) mav.parent.doCommandInt(mav.sysid, mav.compid, (MAVLink.MAV_CMD) 31090, 2, spray_cmd, 0, 0, 0, 0, 0, false);
            if (mav != null) mav.parent.doCommandInt (mav.sysid, mav.compid, MAVLink.MAV_CMD.DO_SEND_SCRIPT_MESSAGE, 2, spray_cmd, 0, 0, 0, 0, 0, false);
        }

        private void butBackR_On_Click(object sender, EventArgs e)
        {
            spray_command |= BITMASK_BACK_R;

            UpdateDoSendScriptMsg(spray_command);
        }

        private void butBackL_On_Click(object sender, EventArgs e)
        {
            spray_command |= BITMASK_BACK_L;

            UpdateDoSendScriptMsg(spray_command);
        }

        private void butFrontR_On_Click(object sender, EventArgs e)
        {
            spray_command |= BITMASK_FRONT_R;

            UpdateDoSendScriptMsg(spray_command);
        }

        private void butFrontL_On_Click(object sender, EventArgs e)
        {
            spray_command |= BITMASK_FRONT_L;

            UpdateDoSendScriptMsg(spray_command);
        }

        private void butBackR_Off_Click(object sender, EventArgs e)
        {
            spray_command &= ~BITMASK_BACK_R;

            UpdateDoSendScriptMsg(spray_command);
        }

        private void butBackL_Off_Click(object sender, EventArgs e)
        {
            spray_command &= ~BITMASK_BACK_L;

            UpdateDoSendScriptMsg(spray_command);
        }

        private void butFrontL_Off_Click(object sender, EventArgs e)
        {
            spray_command &= ~BITMASK_FRONT_L;

            UpdateDoSendScriptMsg(spray_command);
        }

        private void butFrontR_Off_Click(object sender, EventArgs e)
        {
            spray_command &= ~BITMASK_FRONT_R;

            UpdateDoSendScriptMsg(spray_command);
        }
    }
}
