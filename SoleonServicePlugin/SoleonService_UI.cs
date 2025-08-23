using BruTile.Wms;
using DeviceProgramming;
using Microsoft.Scripting.Metadata;
using MissionPlanner.Plugin;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Interop;
//using Xamarin.Forms;
using static Community.CsharpSqlite.Sqlite3;
using static IronPython.Modules._ast;
using static IronPython.Modules.PythonStruct;
using static MissionPlanner.Controls.SoleonService_UI;


namespace MissionPlanner.Controls
{

    public partial class SoleonService_UI : UserControl
    {
        public const ushort SO_PLTYPE_COMMAND = 0xF000;
        public const ushort SO_PLTYPE_COMMAND_RESP = 0xF001;
        public const ushort SO_PLTYPE_INTVAL_F010 = 0xF010;  //--Interval
        int tunnel_msg_interval_ms = 500;

        private byte target_mavlink_ID = 84;                  ///-Soleon Sprayer uses MAV_COMP_ID_USER60  (== 84)
        private Plugin.PluginHost _host = null;
        
        private UInt32 loopConnectCntr;
        private bool TunnelMsgDetected, MsgBoxDetected;
        static bool threadrun = false;
     
        System.Threading.Thread _thread_soleon;

        struct so_tunnel_f010_t
        {
         //--- 
         ulong timestamp;                   ///< Timestamp, in microseconds since UNIX epoch GMT

        //--- DEBUG
        float dbg1Float;
        float dbg2Float;
        uint dbg3Uint32;
        ushort dbg4Uint32;

        //--- COUNTERS
        uint cntrCtrLoops;
        ushort cntrRxMp;
        ushort cntrTxStatus;

        //--- MEASURES
        float pressureLeft;
        float pressureRight;
        byte owerRideSw;       //-- forcedOff; MissionPlan; forcedOn
        byte owerRideOnSw;     //-- front; all; back
        float offsetTrim;       //--5% to 5% (l/ha)

        //--- CONTROLLER
        ushort ppmPumpLeft;
        ushort ppmPumpRight;
        ushort ppmPumpLeftMax;
        ushort ppmPumpRightMax;
        float eValLeftMax;
        float eValRightMax;

        //--- ERRORS
        uint errorFlags;
        ushort cntPressLeftWindups;
        ushort cntPressRightWindups;
        ushort cntMavLinkErrors;

        //--- mavlink_status channel_0
        byte  msg_received_0;               ///< Number of received messages
        byte  buffer_overrun_0;             ///< Number of buffer overruns
        byte  parse_error_0;                ///< Number of parse errors
        ushort packet_rx_success_count_0;   ///< Received packets
        ushort packet_rx_drop_count_0;      ///< Number of packet drops

        //--- mavlink_status channel_1
        byte  msg_received_1;               ///< Number of received messages
        byte  buffer_overrun_1;             ///< Number of buffer overruns
        byte  parse_error_1;                ///< Number of parse errors
        ushort packet_rx_success_count_1;   ///< Received packets
        ushort packet_rx_drop_count_1;      ///< Number of packet drops
        };

        so_tunnel_f010_t so_tunnel_f010 = new so_tunnel_f010_t();


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
                TunnelMsgDetected = true;
                parseTunnelMsg(linkMessage);
            }

        }

        public static byte[] Serialize<T>(T data) where T : struct
        {   //----- NOT used; NOT tested
            int size = Marshal.SizeOf(data);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(data, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }

       
        public static T Deserialize<T>(byte[] array) where T : struct
        {
            T str = new T();

            int size = Marshal.SizeOf(default(T));
            IntPtr ptr = Marshal.AllocHGlobal(size);
            
            Marshal.Copy(array, 0, ptr, size);

            str = (T)Marshal.PtrToStructure(ptr, typeof(T));
            Marshal.FreeHGlobal(ptr);
            return str;
        }



        private bool parseTunnelMsg(MAVLink.MAVLinkMessage msg)
        {
            var tnl = (MAVLink.mavlink_tunnel_t)msg.data;

            switch (tnl.payload_type)
            {
                case SO_PLTYPE_COMMAND:
                case SO_PLTYPE_COMMAND_RESP:
                    break;

                case SO_PLTYPE_INTVAL_F010:   //-- tunnel interval message arrived
                    int size = Marshal.SizeOf(so_tunnel_f010);
                    if (size <= tnl.payload_length) so_tunnel_f010 = Deserialize<so_tunnel_f010_t>(tnl.payload);

                    //----- do bin i; die boxen sein zum updaten!!!


                    break;
            }

            return true;
        }



        public SoleonService_UI()
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
                Name = "SoleonServiceThread"
            };
            _thread_soleon.Start();
        }


        private void mainloop()
        {
            threadrun = true;
            while (threadrun)
            {
                loopConnectCntr++;

                //--- Monitor connection status ---//
                switch (loopConnectCntr % 4)
                {
                    case 0:
                        TunnelMsgDetected = false;
                        break;

                    case 3:
                        if (TunnelMsgDetected && (!MsgBoxDetected))
                        {
                            payloadConnLabel.ForeColor = Color.LawnGreen;
                            SetText(payloadConnLabel, "connected");
                        }
                        if (!TunnelMsgDetected && MsgBoxDetected)
                        {
                            payloadConnLabel.ForeColor = Color.IndianRed;
                            SetText(payloadConnLabel, "disconnected");
                        }
                        MsgBoxDetected = TunnelMsgDetected;
                        break;

                }


                System.Threading.Thread.Sleep((int)500);

            }
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
            doSetTunnelMsgInterval(tunnel_msg_interval_ms, SO_PLTYPE_INTVAL_F010);
        }


        private void tBinterval_Leave(object sender, EventArgs e)
        {
            int value, newValue;

            if (int.TryParse(tBinterval.Text, out value))
            {//parsing successful
                newValue = value;
                if (value > 5000) value = 5000;
                if (value < 100) value = 100;
                if (newValue != value) tBinterval.Text = value.ToString(); //- limited - update the box
                tunnel_msg_interval_ms = value;
            }
            else
            { //parsing failed.
                tBinterval.Text = tunnel_msg_interval_ms.ToString();
            }

        }

        private void butBackL_Off_Click(object sender, EventArgs e)
        {
            doSetTunnelMsgInterval(-1, SO_PLTYPE_INTVAL_F010);
        }
    }
}
