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
using static MissionPlanner.Controls.SoleonCtrl_UI;



namespace MissionPlanner.Controls
{

    public partial class SoleonCtrl_UI : UserControl
    {
        Int32 spray_command = 0;
        public const Int32 BITMASK_BACK_R = 0x1;
        public const Int32 BITMASK_BACK_L = 0x2;
        public const Int32 BITMASK_FRONT_R = 0x4;
        public const Int32 BITMASK_FRONT_L = 0x8;

        public const Int32 CONN_TIMEOUT_SECS = 4;

        Color myGreen = Color.FromArgb(153, 255, 54);
        Color myBlue = Color.FromArgb(54, 153, 255);
        Color myRed = Color.FromArgb(255, 153, 54);


        //private static SoleonCtrl_Plugin _Instance;
        private byte target_mavlink_ID = 84;                  ///-Soleon Sprayer uses MAV_COMP_ID_USER60  (== 84)
        private Plugin.PluginHost _host = null;
        public struct so_status_data_t
        {
            public float mp_status;       /*<  Parameter 1 */
            public float fill_level;      /*<  Parameter 2 */
            public float mp_sprayrate;    /*<  Parameter 3 */
            public float mp_liter_ha;     /*<  Parameter 4 */
            public float mp_line_dist;    /*<  Parameter 5 */
            public float mp_planned_spd;  /*<  Parameter 6 */
            public float param7;          /*<  Parameter 7 */
            public bool  connected;
        };
        
        private UInt32 SoStatusRxCnt;


        public so_status_data_t so_status_data = new so_status_data_t();
        private UInt32 loopConnectCntr;
        private Int32 connTimeOut;

        System.Threading.Thread _thread_soleon;
        static bool threadrun = false;
        System.DateTime _last_time_1 = System.DateTime.Now;



        public void setVer(String msg)
        {
            if (SoleonCtrl!=null) SoleonCtrl.Text = msg;
        }
        public void setHost(Plugin.PluginHost host)
        {
            _host = host;
            _host.comPort.OnPacketReceived += MavPacketReceived;
        }

        //--- we dont use this currently
        public bool Loop()
        {
            if ((!so_status_data.connected) && (connTimeOut < CONN_TIMEOUT_SECS))  connTimeOut++;
            if (so_status_data.connected)
            {
                so_status_data.connected = false;
                connTimeOut = 0;
            }

            if (connTimeOut >= CONN_TIMEOUT_SECS){
                so_status_data.connected = false;
                loopConnectCntr = 0;
                connTimeOut = 0;
             //   clearAllStatusViews();
                return true;
            }

            //---- we are connected ---//
            loopConnectCntr++;
            //doToggleTheLeds(loopConnectCntr);
            return true;
        }

        public SoleonCtrl_UI()
        {
            InitializeComponent(); 
            start();
        }

        //- This starts the mainloop - task
        public void start()  
        {
            //Console.WriteLine();
            _thread_soleon = new System.Threading.Thread(new System.Threading.ThreadStart(mainloop))
            {
                IsBackground = true,
                Name = "SoleonThread"
            };
            _thread_soleon.Start();
        }

        delegate void SetTextCallback( Control ctrl, string text);
        /// <summary>
        /// Set text property of various controls (thread safe)
        /// </summary>
        /// <param name="ctrl"></param>
        /// <param name="text"></param>
        public static void SetText( Control ctrl, string text)
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


        private void UpdateDoSendScriptMsg(Int32 spray_cmd)
        {
            if (_host == null) return; 
            var mav = _host.comPort.MAVlist.FirstOrDefault(a => a.compid == (byte) target_mavlink_ID);
            // --- Send 'Int command' to payload; Note: doCommand sends 'long command' ---

            
            //if (mav != null) mav.parent.doCommandInt(mav.sysid, mav.compid, (MAVLink.MAV_CMD) 31090, 2, spray_cmd, 0, 0, 0, 0, 0, false);
            if (mav != null) mav.parent.doCommandInt (mav.sysid, mav.compid, MAVLink.MAV_CMD.DO_SEND_SCRIPT_MESSAGE, 2, spray_cmd, 0, 0, 0, 0, 0, false);
        }
 

        private void mainloop()
        {
            threadrun = true;
            while (threadrun)
            {
                loopConnectCntr++;
                doToggleTheLeds(loopConnectCntr);


                System.Threading.Thread.Sleep((int)500);

            }
        }

        private void MavPacketReceived(object o, MAVLink.MAVLinkMessage linkMessage)
        {
            if (((MAVLink.MAVLINK_MSG_ID) linkMessage.msgid == MAVLink.MAVLINK_MSG_ID.SO_STATUS))
            {
                mav_decode_soleon_status(linkMessage);
            }

            if (((MAVLink.MAVLINK_MSG_ID) linkMessage.msgid == MAVLink.MAVLINK_MSG_ID.STATUSTEXT))
            {
                mav_decode_statustext(linkMessage);
            }

        }

        private bool mav_decode_soleon_status( MAVLink.MAVLinkMessage msg)
        {
            so_status_data_t rxSoStatus = so_status_data;
            var pckt = (MAVLink.mavlink_so_status_t)msg.data;

            rxSoStatus.mp_status =      pckt.status;
            rxSoStatus.fill_level =     pckt.filllevel;
            rxSoStatus.mp_sprayrate =   pckt.flowliter;
            rxSoStatus.mp_liter_ha =    pckt.flowha;
            rxSoStatus.mp_line_dist =   pckt.distlines;
            rxSoStatus.mp_planned_spd = pckt.speed;

            rxSoStatus.connected = true;

            SoStatusRxCnt++;

            updateStatusFields(rxSoStatus);
            return true;
        }

        
        private void updateStatusFields(so_status_data_t so_status)
        {
            if (so_status_data.Equals(so_status)) return; //-- no need to update

             if ((so_status.connected == false) && (so_status_data.connected == true)){
                clearAllStatusViews();
                so_status_data = so_status;
                return;
            }


            if (so_status.fill_level != so_status_data.fill_level)      SetText(fill, so_status.fill_level.ToString() + " l");
            if (so_status.mp_sprayrate != so_status_data.mp_sprayrate)  SetText(flow, so_status.mp_sprayrate.ToString() + " l/h");
            if (so_status.mp_line_dist != so_status_data.mp_line_dist)  SetText(line_dist, so_status.mp_line_dist.ToString() + " m");
            if (so_status.mp_planned_spd != so_status_data.mp_planned_spd) SetText(speed, so_status.mp_planned_spd.ToString() + " m/h");
            if (so_status.mp_liter_ha != so_status_data.mp_liter_ha)    SetText(speed, so_status.mp_liter_ha.ToString() + " l/ha");


            if (so_status.mp_status != so_status_data.mp_status) updateTheLedStatus((Int32)so_status.mp_status);

            so_status_data = so_status;
        }

        private void updateTheLedStatus(Int32 theStatus)
        {
            BitVector32 statusBits = new BitVector32(theStatus);

            ledSprR.Color = (statusBits[1 << 0] ? myGreen : Color.White);
            ledSprL.Color = (statusBits[1 << 1] ? myGreen : Color.White);
            ledSprReady.Color = (statusBits[1 << 2] ? myGreen : Color.White);
            ledPumpErr.Color = (statusBits[1 << 3] ? myRed : Color.White);
            ledNozzleErr.Color = (statusBits[1 << 4] ? myRed : Color.White);

        }

        private void doToggleTheLeds(UInt32 cntr)
        {
            var Color1 = myBlue;
            var Color2 = Color.White;
            
            BitVector32 statusBits = new BitVector32((Int32)so_status_data.mp_status);

            if ((cntr % 2) == 1){
                Color1 = Color.White;
                Color2 = myBlue;
            }

            if (!statusBits[1 << 0]) {ledBackR_1.Color = Color.White; ledBackR_2.Color = Color.White;} 
            else                {ledBackR_1.Color = Color1; ledBackR_2.Color = Color2;}
            if (!statusBits[1 << 1]) {ledBackL_1.Color = Color.White; ledBackL_2.Color = Color.White;} 
            else                {ledBackL_1.Color = Color1; ledBackL_2.Color = Color2;}
            if (!statusBits[1 << 6]) {ledFrontR_1.Color = Color.White; ledFrontR_2.Color = Color.White;} 
            else                 {ledFrontR_1.Color = Color1; ledFrontR_2.Color = Color2;}
            if (!statusBits[1 << 7]) {ledFrontL_1.Color = Color.White; ledFrontL_2.Color = Color.White;} 
            else                 {ledFrontL_1.Color = Color1; ledFrontL_2.Color = Color2;}
        }

        private void clearAllStatusViews()
        {
            fill.Text = "";
            flow.Text = "";
            line_dist.Text = "";
            speed.Text = "";

            ledSprR.Color = Color.White;
            ledSprL.Color = Color.White;
            ledNozzleErr.Color = Color.White;
            ledPumpErr.Color = Color.White;
            ledSprReady.Color = Color.White;
            ledFrontL_1.Color = Color.White;
            ledFrontL_2.Color = Color.White;
            ledFrontR_1.Color = Color.White;
            ledFrontR_2.Color = Color.White;
            ledBackR_2.Color = Color.White;
            ledBackR_1.Color = Color.White;
            ledBackL_2.Color = Color.White;
            ledBackL_1.Color = Color.White;
        }

        private bool mav_decode_statustext( MAVLink.MAVLinkMessage msg)
        {

            var pckt = (MAVLink.mavlink_statustext_t)msg.data;
            string txt = System.Text.Encoding.UTF8.GetString(pckt.text);
            if (txt.Contains("Soleon Payload"))
            {
                String[] ver = txt.Split(':');
                var output = new string(ver[1].Where(char.IsNumber).ToArray());
                Console.WriteLine("Firmware rcvd! FW (" + output + ")");       //-- write this to the tab somewere; for future backward compatibiltity
            }
            return true;
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
