using BruTile.Wms;
using MissionPlanner.Plugin;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Interop;
//using Xamarin.Forms;
using static Community.CsharpSqlite.Sqlite3;
using static MissionPlanner.Controls.SoleonService_UI;


namespace MissionPlanner.Controls
{

    public partial class SoleonService_UI : UserControl
    {
        public const ushort SO_PLTYPE_COMMAND = 0xF000;
        public const ushort SO_PLTYPE_COMMAND_RESP = 0xF001;
        public const ushort SO_PLTYPE_DEBUG = 0xF010;
        public const ushort SO_PLTYPE_COUNTERS = 0xF011;
        public const ushort SO_PLTYPE_MEASURES = 0xF012;
        public const ushort SO_PLTYPE_PRESS_CNTRL = 0xF013;
        public const ushort SO_PLTYPE_ERRORS = 0xF014;

        private byte target_mavlink_ID = 84;                  ///-Soleon Sprayer uses MAV_COMP_ID_USER60  (== 84)
        private Plugin.PluginHost _host = null;

        public void setVer(String msg)
        {
            if (groupBoxSw != null) groupBoxSw.Text = msg;
        }

        public void setHost(Plugin.PluginHost host)
        {
            _host = host;
            _host.comPort.OnPacketReceived += MavPacketReceived;
        }

        private void MavPacketReceived(object o, MAVLink.MAVLinkMessage linkMessage)
        {

            if (((MAVLink.MAVLINK_MSG_ID)linkMessage.msgid == MAVLink.MAVLINK_MSG_ID.TUNNEL)  &&  
                 (linkMessage.compid == target_mavlink_ID))
            {   //-- TUNNEL message from soleon payload arrived
                parseTunnelMsg(linkMessage);
            }

        }

        private bool parseTunnelMsg(MAVLink.MAVLinkMessage msg)
        {
            var tnl = (MAVLink.mavlink_tunnel_t)msg.data;

            switch (tnl.payload_type)
            {
                case SO_PLTYPE_COMMAND:
                case SO_PLTYPE_COMMAND_RESP:
                    break;

                case SO_PLTYPE_DEBUG:
                case SO_PLTYPE_COUNTERS:
                case SO_PLTYPE_MEASURES:
                case SO_PLTYPE_PRESS_CNTRL:
                case SO_PLTYPE_ERRORS:
                    break;
            }

            /* 

                        if (tnl.payload_type == 0xFF85)
                        {
                            parseWebFeed(msg);

                        }
                        else if (tnl.payload_type == 0xFF86)
                        {
                            parseGtgtRes(msg);

                        }
                        else if (tnl.payload_type == 0xFEF1)
                        { //update related data
                            Console.WriteLine("Tunnel! " + tnl.payload[0] + " - " + tnl.payload[1]);
                            if (tnl.payload[0] == 0x95)
                            {
                                if (ENT_fw_active == 0) return true;
                                //block request
                                int block = (tnl.payload[1] << 16) | (tnl.payload[2] << 8) | tnl.payload[3];
                                Console.WriteLine("BLOCK request: " + block);
                                updt_tmr.Enabled = false;
                                act_block = block;
                                ENT_FW_send_block(act_block);
                                updt_tmr.Enabled = true;

                            }
                            else if (tnl.payload[0] == 0x96)
                            { //feedback message

                                if (tnl.payload[1] == 0xF0)
                                { //file write error
                                    update_prgrs_label.Text = "Error";
                                    updt_tmr.Enabled = false;
                                    ShowFwUpdateQuestion = false;
                                    update_prgrs_label.Text = "File error!";
                                    _Instance.ENT_fw_active = 0;
                                    StopFwUpdate();

                                }
                                else if (tnl.payload[1] == 0xA1)
                                {//file write done
                                    if (ENT_fw_active == 0) return true;
                                    updt_tmr.Enabled = false;
                                    ShowFwUpdateQuestion = true;

                                }
                                else if (tnl.payload[1] == 0xFB)
                                {//file decompression error
                                    if (ENT_fw_active == 0) return true;
                                    updt_tmr.Enabled = false;
                                    ShowFwUpdateQuestion = false;
                                    update_prgrs_label.Text = "File error!";
                                    _Instance.ENT_fw_active = 0;
                                    StopFwUpdate();

                                }
                            }

                        }
            */

            return true;
        }



        public SoleonService_UI()
        {
            InitializeComponent();
        }


        delegate void SetTextCallback(Control ctrl, string text);
        public static void SetText(Control ctrl, string text)
        {
            // InvokeRequired required compares the thread ID of the 
            // calling thread to the thread ID of the creating thread. 
            // If these threads are different, it returns true. 
            if (ctrl.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                ctrl.Invoke(d, new object[] { ctrl, text });
            }
            else
            {
                ctrl.Text = text;
            }
        }

        private void doSetTunnelMsgInterval(Int32 interval_ms, UInt16 payLoadType)
        {
            float interval_us = interval_ms*1000; //- uSecs are used for the command

            if (_host == null) return;

            if (interval_us < 0) interval_us = -1;

            var mav = _host.comPort.MAVlist.FirstOrDefault(a => a.compid == (byte)target_mavlink_ID);

            if (mav != null) mav.parent.doCommand(mav.sysid, mav.compid, MAVLink.MAV_CMD.SET_MESSAGE_INTERVAL, (float) MAVLink.MAVLINK_MSG_ID.TUNNEL, (float) interval_us, (float) payLoadType, 0, 0, 0, 0, false);
        }



        private void tabDebug_Enter(object sender, EventArgs e)
        {
            //-- activate tunnel for tabDebug
        }


        private void tabMeasures_Enter(object sender, EventArgs e)
        {
            //-- activate tunnel for tabMeasures
        }

        private void tabCounters_Enter(object sender, EventArgs e)
        {
            //-- activate tunnel for tabCounters
        }

        private void tabController_Enter(object sender, EventArgs e)
        {
            //-- activate tunnel for tabController
        }

        private void tabErrors_Enter(object sender, EventArgs e)
        {
            //-- activate tunnel for tabErrors
        }

        private void butBackL_On_Click(object sender, EventArgs e)
        {
            doSetTunnelMsgInterval(500, 0xf010);
        }

        private void butBackL_Off_Click(object sender, EventArgs e)
        {
            doSetTunnelMsgInterval(-1, 0xf010);
        }
    }
}
