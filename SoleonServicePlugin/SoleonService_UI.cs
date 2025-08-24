using BruTile.Wms;
using DeviceProgramming;
using Microsoft.Scripting.Metadata;
using MissionPlanner.Plugin;
using OpenTK.Audio.OpenAL;
using SharpAdbClient;
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
        public const ushort SO_PLTYPE_COMMAND = 0xF000;         //-- this goes to payload
        public const ushort SO_PLTYPE_COMMAND_RESP = 0xF001;    //-- anwer from payload (if needed)
        public const ushort SO_PLTYPE_INTVAL_F010 = 0xF010;     //-- from payload (send after interval request

        // ---- Soleon commands (SO_PLTYPE_COMMAND)
        public const byte SO_PLCMD_RES_ALL   = 1;             //-- Reset all
        public const byte SO_PLCMD_RES_CNTRS = 2;             //-- Reset counters
        public const byte SO_PLCMD_RES_CONTROLLER = 3;        //-- Reset controller
        public const byte SO_PLCMD_RES_ERRORS = 4;            //-- Reset Error counters + flags
        public const byte SO_PLCMD_RES_MAVLINK = 5;           //-- Reset mavlink diagnose 

        int tunnel_msg_interval_ms = 500;                       //-- update interval for payload messages (>=SO_PLTYPE_INTVAL_F010)

        private byte target_mavlink_ID = 84;                  ///-Soleon Sprayer uses MAV_COMP_ID_USER60  (== 84)
        private Plugin.PluginHost _host = null;
        
        private UInt32 loopConnectCntr;
        private bool TunnelMsgDetected, MsgBoxDetected;
        static bool threadrun = false;
     
        System.Threading.Thread _thread_soleon;

        enum activeTab_t
        {
            None = 0,
            Debug,
            Counters,
            Measures,
            Controller,
            Errors,
            Mavlink,
        };

        activeTab_t activeTab;

        struct so_tunnel_f010_t
        {
        //--- 
        public ulong timestamp;                   ///< Timestamp, in microseconds since UNIX epoch GMT

        //--- DEBUG
        public float dbg1Float;
        public float dbg2Float;
        public uint dbg3Uint32;
        public ushort dbg4Uint32;

        //--- COUNTERS
        public uint cntrCtrLoops;
        public ushort cntrRxMp;
        public ushort cntrTxStatus;

        //--- MEASURES
        public float pressureLeft;
        public float pressureRight;
        public byte owerRideSw;       //-- forcedOff; MissionPlan; forcedOn
        public byte owerRideOnSw;     //-- front; all; back
        public float offsetTrim;       //--5% to 5% (l/ha)

        //--- CONTROLLER
        public ushort ppmPumpLeft;
        public ushort ppmPumpRight;
        public ushort ppmPumpLeftMax;
        public ushort ppmPumpRightMax;
        public float eValLeftMax;
        public float eValRightMax;

        //--- ERRORS
        public uint errorFlags;
        public ushort cntPressLeftWindups;
        public ushort cntPressRightWindups;
        public ushort cntMavLinkErrors;

        //--- mavlink_status channel_0
        public byte  msg_received_0;               ///< Number of received messages
        public byte  buffer_overrun_0;             ///< Number of buffer overruns
        public byte  parse_error_0;                ///< Number of parse errors
        public ushort packet_rx_success_count_0;   ///< Received packets
        public ushort packet_rx_drop_count_0;      ///< Number of packet drops

        //--- mavlink_status channel_1
        public byte  msg_received_1;               ///< Number of received messages
        public byte  buffer_overrun_1;             ///< Number of buffer overruns
        public byte  parse_error_1;                ///< Number of parse errors
        public ushort packet_rx_success_count_1;   ///< Received packets
        public ushort packet_rx_drop_count_1;      ///< Number of packet drops
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
                    updateTheActiveTab();

                    break;
            }

            return true;
        }

        private void updateTheActiveTab()
        {
            SetText(labelTimestamp, so_tunnel_f010.timestamp.ToString());

            switch (activeTab)
            {
                default:
                case activeTab_t.Debug:
                    SetText(tBdbg1Float, so_tunnel_f010.dbg1Float.ToString());
                    SetText(tBdbg2Float, so_tunnel_f010.dbg2Float.ToString());
                    SetText(tBdbg3Uint32, so_tunnel_f010.dbg3Uint32.ToString());
                    SetText(tBdbg4Uint32, so_tunnel_f010.dbg4Uint32.ToString());
                  //  SetText(tBdbg1Float, so_status.fill_level.ToString() + " l");
                    break;

                case activeTab_t.Counters:
                    SetText(tBcntrCtrLoops, so_tunnel_f010.cntrCtrLoops.ToString());
                    SetText(tBcntrRxMp, so_tunnel_f010.cntrRxMp.ToString());
                    SetText(tBcntrTxStatus, so_tunnel_f010.cntrTxStatus.ToString());
                    break;

                case activeTab_t.Measures:
                    SetText(tBpressureLeft, so_tunnel_f010.pressureLeft.ToString() + " bar");
                    SetText(tBpressureRight, so_tunnel_f010.pressureRight.ToString() + " bar");
                    switch (so_tunnel_f010.owerRideSw)
                    {
                        case 0:
                            SetText(tBowerRideSw, "forced OFF");
                            break;
                        case 1:
                            SetText(tBowerRideSw, "MissionPlan control");
                            break;
                        case 2:
                            SetText(tBowerRideSw, "forced ON");
                            break;
                        default:
                            SetText(tBowerRideSw, "---- error [" + so_tunnel_f010.owerRideOnSw.ToString() + "] ----");
                            break;
                    }

                    switch (so_tunnel_f010.owerRideOnSw)
                    {
                        case 0:
                            SetText(tBowerRideOnSw, "front");
                            break;
                        case 1:
                            SetText(tBowerRideOnSw, "all");
                            break;
                        case 2:
                            SetText(tBowerRideOnSw, "back");
                            break;
                        default:
                            SetText(tBowerRideOnSw, "---- error [" + so_tunnel_f010.owerRideOnSw.ToString() + "] ----");
                            break;
                    }

                    SetText(tBoffsetTrim, so_tunnel_f010.offsetTrim.ToString() + " %");
                    break;

                case activeTab_t.Controller:
                    SetText(tBppmPumpLeft, so_tunnel_f010.ppmPumpLeft.ToString());
                    SetText(tBppmPumpRight, so_tunnel_f010.ppmPumpRight.ToString());
                    SetText(tBppmPumpLeftMax, so_tunnel_f010.ppmPumpLeftMax.ToString());
                    SetText(tBppmPumpRightMax, so_tunnel_f010.ppmPumpRightMax.ToString());
                    SetText(tBeValLeftMax, so_tunnel_f010.eValLeftMax.ToString());
                    SetText(tBeValRightMax, so_tunnel_f010.eValRightMax.ToString());
                    break;

                case activeTab_t.Errors:
                    SetText(tBerrorFlags, "b" + Convert.ToString(so_tunnel_f010.errorFlags, 2));  //--- binary representation
                    SetText(tBleftWindups, so_tunnel_f010.cntPressLeftWindups.ToString());
                    SetText(tBrightWindups, so_tunnel_f010.cntPressRightWindups.ToString());
                    SetText(tBmavLinkErrors, so_tunnel_f010.cntMavLinkErrors.ToString());
                    break;


                case activeTab_t.Mavlink:
                    SetText(tBmsg_received_0, so_tunnel_f010.msg_received_0.ToString());
                    SetText(tBmsg_received_1, so_tunnel_f010.msg_received_1.ToString());
                    SetText(tBbuffer_overrun_0, so_tunnel_f010.buffer_overrun_0.ToString());
                    SetText(tBbuffer_overrun_1, so_tunnel_f010.buffer_overrun_1.ToString());
                    SetText(tBparse_errors_0, so_tunnel_f010.parse_error_0.ToString());
                    SetText(tBparse_errors_1, so_tunnel_f010.parse_error_1.ToString());
                    SetText(tBpacket_rx_success_0, so_tunnel_f010.packet_rx_success_count_0.ToString());
                    SetText(tBpacket_rx_success_1, so_tunnel_f010.packet_rx_success_count_1.ToString());
                    SetText(tBpacket_rx_drop_0, so_tunnel_f010.packet_rx_drop_count_0.ToString());
                    SetText(tBpacket_rx_drop_1, so_tunnel_f010.packet_rx_drop_count_1.ToString());
                    break;  

            }
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

        //--- this sends Tunnel message to payload; with payloadtype SO_PLTYPE_COMMAND; 
        private void doSendTunnelCommand(byte command)
        {
            byte[] tunnel_buf = new byte[128];
            tunnel_buf[0] = (byte)command;

            var mav = _host.comPort.MAVlist.FirstOrDefault(a => a.compid == (byte)target_mavlink_ID);
            if (mav != null)
            {
                MAVLink.mavlink_tunnel_t msg = new MAVLink.mavlink_tunnel_t
                {
                    payload_type = SO_PLTYPE_COMMAND,
                    target_system = mav.sysid,
                    target_component = mav.compid,
                    payload_length = 1,
                    payload = tunnel_buf
                };

                mav.parent.generatePacket((int)MAVLink.MAVLINK_MSG_ID.TUNNEL, msg, mav.sysid, mav.compid, true, false);
            }
        }


        private void tabDebug_Enter(object sender, EventArgs e)
        {
            //-- activate tunnel for tabDebug (currently not needed; all data comes with an unique Tunnel message)
            activeTab = activeTab_t.Debug;
        }


        private void tabMeasures_Enter(object sender, EventArgs e)
        {
            //-- activate tunnel for tabMeasures (currently not needed)
            activeTab = activeTab_t.Measures;
        }

        private void tabCounters_Enter(object sender, EventArgs e)
        {
            //-- activate tunnel for tabCounters (currently not needed)
            activeTab = activeTab_t.Counters;
        }

        private void tabController_Enter(object sender, EventArgs e)
        {
            //-- activate tunnel for tabController
            activeTab = activeTab_t.Controller;
        }

        private void tabErrors_Enter(object sender, EventArgs e)
        {
            //-- activate tunnel for tabErrors (currently not needed)
            activeTab = activeTab_t.Errors;
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
                if (newValue != value) tBinterval.Text = value.ToString(); //- value has been limited -> update the box
                tunnel_msg_interval_ms = value;
            }
            else
            { //parsing failed.
                tBinterval.Text = tunnel_msg_interval_ms.ToString();
            }

        }

        private void countersRes_Click(object sender, EventArgs e)
        {
            doSendTunnelCommand(SO_PLCMD_RES_CNTRS);
        }

        private void controllerRes_Click(object sender, EventArgs e)
        {
            doSendTunnelCommand(SO_PLCMD_RES_CONTROLLER);
        }

        private void errorsRes_Click(object sender, EventArgs e)
        {
            doSendTunnelCommand(SO_PLCMD_RES_ERRORS);
        }

        private void mavlinkRes_Click(object sender, EventArgs e)
        {
            doSendTunnelCommand(SO_PLCMD_RES_MAVLINK);
        }

        private void tabMavLink_Enter(object sender, EventArgs e)
        {
            activeTab = activeTab_t.Mavlink;
        }


        private void butBackL_Off_Click(object sender, EventArgs e)
        {
            doSetTunnelMsgInterval(-1, SO_PLTYPE_INTVAL_F010);
        }
    }
}
