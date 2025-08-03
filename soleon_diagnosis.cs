//--------------------------
// This is _MavPorter_plugin.cs renamed to soleon_diagnosis.cs
// This is starting point for soleon development
// 
// Link: https://airpixel.cz/docs/missionplanner-plugin-for-mavporter/
//--------------------------
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
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
using static MAVLink;

using System.Drawing.Drawing2D;

namespace MavPorter_plugin
{

    public class Plugin : MissionPlanner.Plugin.Plugin
    {

        private static Plugin _Instance;
        private TabControl tabctrl;
        private System.Windows.Forms.TabPage tab = new System.Windows.Forms.TabPage();
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;

        private System.Windows.Forms.PictureBox logo = new System.Windows.Forms.PictureBox();
        private System.Windows.Forms.Label label_ver = new System.Windows.Forms.Label();


        private byte target_mavlink_ID = 241;
        private bool _porter_online = false;
        private bool porter_online = false;
        private int checkcycles = 0;

        private float[] param_value = new float[200];
        private int[] param_type = new int[200];
        private string[] param_id = new string[200];

        private int port_to_show = 1;
        private bool refresh_display = true;



        private static System.Timers.Timer maintmr;
        private static System.Timers.Timer maintmr2;


        public override string Name
        {
            get { return "\n-----------------------------\nMavPorter plugin by AirPixel.cz\n-----------------------------\n"; }
        }

        public override string Version
        {
            get { return "1.01"; }
        }

        public override string Author
        {
            get { return "airpixel.cz"; }
        }

        public override bool Init()
        {
            return true;
        }

        private void ApplyDefaultStyleButton(System.Windows.Forms.Button b, int active)
        {           
            if(active == 1){
                    b.BackColor = System.Drawing.Color.DarkOrange;
                }else{
                    b.BackColor = System.Drawing.SystemColors.Control;
                }
        }
        private bool ButtonStyleChanged()
        {           
            if(port_to_show == 1){
                    return prt2_btn.BackColor != System.Drawing.SystemColors.Control;
                }else{
                    return prt1_btn.BackColor != System.Drawing.SystemColors.Control;
                }
        }
        private static Image Base64ToImage(string base64String)

        {
            byte[] imageBytes = Convert.FromBase64String(base64String);
            MemoryStream ms = new MemoryStream(imageBytes, 0, imageBytes.Length);
            ms.Write(imageBytes, 0, imageBytes.Length);
            Image image = Image.FromStream(ms, true);
            return image;
        }

        public override bool Loaded()
        {


            Host.comPort.OnPacketReceived += MavPacketReceived;

            tabctrl = Host.MainForm.FlightData.tabControlactions;

            tab.Text = "MavPorter";


            //tab.Controls.Add(btn_FWupdate); 
            //btn_FWupdate.Click += fwUpdateClicked; 

            InitializeComponent();
            ClearDisplay();

            logo.Image = Base64ToImage("iVBORw0KGgoAAAANSUhEUgAAAF4AAABkCAYAAAAPM4elAAAACXBIWXMAAAsTAAALEwEAmpwYAAAKdGlUWHRYTUw6Y29tLmFkb2JlLnhtcAAAAAAAPD94cGFja2V0IGJlZ2luPSLvu78iIGlkPSJXNU0wTXBDZWhpSHpyZVN6TlRjemtjOWQiPz4gPHg6eG1wbWV0YSB4bWxuczp4PSJhZG9iZTpuczptZXRhLyIgeDp4bXB0az0iQWRvYmUgWE1QIENvcmUgOS4xLWMwMDEgNzkuMTQ2Mjg5OSwgMjAyMy8wNi8yNS0yMDowMTo1NSAgICAgICAgIj4gPHJkZjpSREYgeG1sbnM6cmRmPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5LzAyLzIyLXJkZi1zeW50YXgtbnMjIj4gPHJkZjpEZXNjcmlwdGlvbiByZGY6YWJvdXQ9IiIgeG1sbnM6eG1wPSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvIiB4bWxuczpkYz0iaHR0cDovL3B1cmwub3JnL2RjL2VsZW1lbnRzLzEuMS8iIHhtbG5zOnhtcE1NPSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvbW0vIiB4bWxuczpzdEV2dD0iaHR0cDovL25zLmFkb2JlLmNvbS94YXAvMS4wL3NUeXBlL1Jlc291cmNlRXZlbnQjIiB4bWxuczpzdFJlZj0iaHR0cDovL25zLmFkb2JlLmNvbS94YXAvMS4wL3NUeXBlL1Jlc291cmNlUmVmIyIgeG1sbnM6cGhvdG9zaG9wPSJodHRwOi8vbnMuYWRvYmUuY29tL3Bob3Rvc2hvcC8xLjAvIiB4bWxuczp0aWZmPSJodHRwOi8vbnMuYWRvYmUuY29tL3RpZmYvMS4wLyIgeG1sbnM6ZXhpZj0iaHR0cDovL25zLmFkb2JlLmNvbS9leGlmLzEuMC8iIHhtcDpDcmVhdG9yVG9vbD0iQWRvYmUgUGhvdG9zaG9wIDIyLjAgKFdpbmRvd3MpIiB4bXA6Q3JlYXRlRGF0ZT0iMjAyMS0wNi0xNlQwOTo1Mzo0NSswMjowMCIgeG1wOk1ldGFkYXRhRGF0ZT0iMjAyNC0wMi0wNlQxMDoyNzo0MSswMTowMCIgeG1wOk1vZGlmeURhdGU9IjIwMjQtMDItMDZUMTA6Mjc6NDErMDE6MDAiIGRjOmZvcm1hdD0iaW1hZ2UvcG5nIiB4bXBNTTpJbnN0YW5jZUlEPSJ4bXAuaWlkOjE3NGViMDBhLWYwZmQtMTg0Mi1hMzk2LWM5MWM0ZDBhMDYzOSIgeG1wTU06RG9jdW1lbnRJRD0iYWRvYmU6ZG9jaWQ6cGhvdG9zaG9wOmY1MzU5YzRmLWQ2ZGYtYzU0Ni1iYTg4LTdmNmFlNzE0NTUyYSIgeG1wTU06T3JpZ2luYWxEb2N1bWVudElEPSJ4bXAuZGlkOmMwMTRjMjRlLTRiZjAtOTk0Ni1hNzVjLTM1NTg2ZGJkNDVlYiIgcGhvdG9zaG9wOkNvbG9yTW9kZT0iMyIgdGlmZjpPcmllbnRhdGlvbj0iMSIgdGlmZjpYUmVzb2x1dGlvbj0iNzIwMDAwLzEwMDAwIiB0aWZmOllSZXNvbHV0aW9uPSI3MjAwMDAvMTAwMDAiIHRpZmY6UmVzb2x1dGlvblVuaXQ9IjIiIGV4aWY6Q29sb3JTcGFjZT0iNjU1MzUiIGV4aWY6UGl4ZWxYRGltZW5zaW9uPSI2MDAiIGV4aWY6UGl4ZWxZRGltZW5zaW9uPSI2NDEiPiA8eG1wTU06SGlzdG9yeT4gPHJkZjpTZXE+IDxyZGY6bGkgc3RFdnQ6YWN0aW9uPSJjcmVhdGVkIiBzdEV2dDppbnN0YW5jZUlEPSJ4bXAuaWlkOmMwMTRjMjRlLTRiZjAtOTk0Ni1hNzVjLTM1NTg2ZGJkNDVlYiIgc3RFdnQ6d2hlbj0iMjAyMS0wNi0xNlQwOTo1Mzo0NSswMjowMCIgc3RFdnQ6c29mdHdhcmVBZ2VudD0iQWRvYmUgUGhvdG9zaG9wIDIyLjAgKFdpbmRvd3MpIi8+IDxyZGY6bGkgc3RFdnQ6YWN0aW9uPSJzYXZlZCIgc3RFdnQ6aW5zdGFuY2VJRD0ieG1wLmlpZDpkNDQ4ZDI3Ni00ZjA2LTdhNDYtOTRlMC1iNTNlZWJmMmU3MmMiIHN0RXZ0OndoZW49IjIwMjEtMDYtMTZUMTA6MDA6MDIrMDI6MDAiIHN0RXZ0OnNvZnR3YXJlQWdlbnQ9IkFkb2JlIFBob3Rvc2hvcCAyMi4wIChXaW5kb3dzKSIgc3RFdnQ6Y2hhbmdlZD0iLyIvPiA8cmRmOmxpIHN0RXZ0OmFjdGlvbj0ic2F2ZWQiIHN0RXZ0Omluc3RhbmNlSUQ9InhtcC5paWQ6OTdmMDUwYWUtYzdiMy00MzRiLTgyMDYtOWNhMGU4Y2ZlZTNhIiBzdEV2dDp3aGVuPSIyMDI0LTAyLTA2VDEwOjI3OjQxKzAxOjAwIiBzdEV2dDpzb2Z0d2FyZUFnZW50PSJBZG9iZSBQaG90b3Nob3AgMjUuMCAoV2luZG93cykiIHN0RXZ0OmNoYW5nZWQ9Ii8iLz4gPHJkZjpsaSBzdEV2dDphY3Rpb249ImNvbnZlcnRlZCIgc3RFdnQ6cGFyYW1ldGVycz0iZnJvbSBhcHBsaWNhdGlvbi92bmQuYWRvYmUucGhvdG9zaG9wIHRvIGltYWdlL3BuZyIvPiA8cmRmOmxpIHN0RXZ0OmFjdGlvbj0iZGVyaXZlZCIgc3RFdnQ6cGFyYW1ldGVycz0iY29udmVydGVkIGZyb20gYXBwbGljYXRpb24vdm5kLmFkb2JlLnBob3Rvc2hvcCB0byBpbWFnZS9wbmciLz4gPHJkZjpsaSBzdEV2dDphY3Rpb249InNhdmVkIiBzdEV2dDppbnN0YW5jZUlEPSJ4bXAuaWlkOjE3NGViMDBhLWYwZmQtMTg0Mi1hMzk2LWM5MWM0ZDBhMDYzOSIgc3RFdnQ6d2hlbj0iMjAyNC0wMi0wNlQxMDoyNzo0MSswMTowMCIgc3RFdnQ6c29mdHdhcmVBZ2VudD0iQWRvYmUgUGhvdG9zaG9wIDI1LjAgKFdpbmRvd3MpIiBzdEV2dDpjaGFuZ2VkPSIvIi8+IDwvcmRmOlNlcT4gPC94bXBNTTpIaXN0b3J5PiA8eG1wTU06RGVyaXZlZEZyb20gc3RSZWY6aW5zdGFuY2VJRD0ieG1wLmlpZDo5N2YwNTBhZS1jN2IzLTQzNGItODIwNi05Y2EwZThjZmVlM2EiIHN0UmVmOmRvY3VtZW50SUQ9ImFkb2JlOmRvY2lkOnBob3Rvc2hvcDpkYzMyMWNiNC1mZDRjLTI3NDEtYWFjNy0xNDRmNWI2ZmJhOTIiIHN0UmVmOm9yaWdpbmFsRG9jdW1lbnRJRD0ieG1wLmRpZDpjMDE0YzI0ZS00YmYwLTk5NDYtYTc1Yy0zNTU4NmRiZDQ1ZWIiLz4gPC9yZGY6RGVzY3JpcHRpb24+IDwvcmRm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");
            logo.Size = new System.Drawing.Size(33, 35);
            logo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            tab.Controls.Add(logo);
            logo.Top = 10;
            logo.Left = 385;


            label_ver.Top = 42;
            label_ver.Left = 353;
            //label_ver.Size = new System.Drawing.Size(40, 21);
            label_ver.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            label_ver.Text = "ver: 1.01";
            tab.Controls.Add(label_ver);


            tabctrl.TabPages.Add(tab);
            Host.MainForm.FlightPlanner.updateDisplayView();

            tab.Controls.Add(tableLayoutPanel1);

            ThemeManager.ApplyThemeTo(tab);
            _Instance = this;

            ApplyDefaultStyleButton(prt1_btn, 1);
            ApplyDefaultStyleButton(prt2_btn, 0);
            ApplyDefaultStyleButton(prt3_btn, 0);
            ApplyDefaultStyleButton(prt4_btn, 0);
            ApplyDefaultStyleButton(prt5_btn, 0);

            maintmr = new System.Timers.Timer(1000);
            maintmr.Elapsed += loop_1s;
            maintmr.AutoReset = true;
            maintmr.Enabled = true;

            maintmr2 = new System.Timers.Timer(100);
            maintmr2.Elapsed += loop_100ms;
            maintmr2.AutoReset = true;
            maintmr2.Enabled = true;

            GroupBox_portStatus.Visible = false;

            return true;
        }

        private float get_param(string par_id){

            for(int i=0; i<param_type.Length; i++){
                if(param_id[i].Contains(par_id)){
                    Console.WriteLine("GOT IT:" + param_value[i]);
                    return param_value[i];
                }
            }
            //Console.WriteLine("I DO NOT HAVE IT HERE");
            return -1;
        }

        private void set_param(int idx, string par_id, float par_value, int par_type){

            if(idx >= param_type.Length)return;
            byte[] bytes = BitConverter.GetBytes(par_value);

            if(par_type == 1){
                param_value[idx] = (float)bytes[0];
            }else if(par_type == 4){
                param_value[idx] = (float)((uint)bytes[0] + ((uint)bytes[1] << 8));
            }else if(par_type == 9){
                param_value[idx] = par_value;
            }else if(par_type == 5){
                param_value[idx] = (float)((uint)bytes[0] + ((uint)bytes[1] << 8) + ((uint)bytes[2] << 16) + ((uint)bytes[3] << 24));
            }
            if(float.IsNaN(par_value))param_value[idx]=par_value;

            Console.WriteLine("SAVED:"+par_id+"(type" + par_type + ") | "+param_value[idx]);

            param_type[idx] = par_type;
            param_id[idx] = par_id;

            refreshed_param(param_id[idx], param_value[idx]);

        }

        private void refreshed_param(string id, float value){


            if(id.Contains("PRT1")){
                if(port_to_show == 1)refresh_display = true;

            }else if(id.Contains("PRT2")){
                if(port_to_show == 2)refresh_display = true;

            }else if(id.Contains("PRT3")){
                if(port_to_show == 3)refresh_display = true;

            }else if(id.Contains("PRT4")){
                if(port_to_show == 4)refresh_display = true;

            }else if(id.Contains("PRT5")){
                if(port_to_show == 5)refresh_display = true;
            }




            switch(id){
                case "PRT1_BAUD":
                    //if(port_to_show == 2)refresh_display = true;
                break;
                case "PRT2_BAUD":
                    //if(port_to_show == 2)refresh_display = true;
                break;

                default:
                    //Console.WriteLine("Whaaat? " + id);
                    break;

            }

        }

        private static void loop_100ms(Object source, ElapsedEventArgs e)
        {

            if(_Instance.refresh_display){
                _Instance.RefreshData();
                _Instance.refresh_display = false;
            }

        }

        private static void loop_1s(Object source, ElapsedEventArgs e)
        {
            if(_Instance.ButtonStyleChanged())_Instance.RefreshButtons();

            _Instance.Reposition();
            if(_Instance.checkcycles++ >2){
                _Instance.checkcycles = 0;

                if(_Instance._porter_online != _Instance.porter_online){
                    if(_Instance._porter_online){
                            Console.WriteLine("PORTER ONLINE");
                            _Instance.label_connection.Text = "ONLINE";
                            _Instance.GroupBox_portStatus.Visible = true;
                            _Instance.label1.ForeColor = System.Drawing.SystemColors.AppWorkspace;
                            _Instance.label2.ForeColor = System.Drawing.SystemColors.AppWorkspace;
                        }else{
                            Console.WriteLine("PORTER OFFLINE");
                            _Instance.label_connection.Text = "OFFLINE";
                            _Instance.GroupBox_portStatus.Visible = false;

                        }
                        _Instance.porter_online = _Instance._porter_online;
                        
                }
                _Instance._porter_online = false;
            }

            if(_Instance.porter_online)mav_send_stat_command();
        }


        public void RefreshButtons()
        {
            if(_Instance.port_to_show == 1){_Instance.ApplyDefaultStyleButton(_Instance.prt1_btn, 1);}else{_Instance.ApplyDefaultStyleButton(_Instance.prt1_btn, 0);}
            if(_Instance.port_to_show == 2){_Instance.ApplyDefaultStyleButton(_Instance.prt2_btn, 1);}else{_Instance.ApplyDefaultStyleButton(_Instance.prt2_btn, 0);}
            if(_Instance.port_to_show == 3){_Instance.ApplyDefaultStyleButton(_Instance.prt3_btn, 1);}else{_Instance.ApplyDefaultStyleButton(_Instance.prt3_btn, 0);}
            if(_Instance.port_to_show == 4){_Instance.ApplyDefaultStyleButton(_Instance.prt4_btn, 1);}else{_Instance.ApplyDefaultStyleButton(_Instance.prt4_btn, 0);}
            if(_Instance.port_to_show == 5){_Instance.ApplyDefaultStyleButton(_Instance.prt5_btn, 1);}else{_Instance.ApplyDefaultStyleButton(_Instance.prt5_btn, 0);}
        }


        public void RefreshData()
        {
            //float val = ((get_param("PRT" + port_to_show + "_BAUD"))*100);
            //Console.WriteLine("REFRESHING "+port_to_show + " | " + ((get_param("PRT" + port_to_show + "_BAUD"))*100).ToString());
            label_baud.Text = ((get_param("PRT" + port_to_show + "_BAUD"))*100).ToString();
        }
        public void ClearDisplay()
        {
            label_baud.Text = "";
            label_status.Text = "";

            label_in_rate.Text = "";
            label_out_rate.Text = "";

            label_RXstat_1.Text = "";
            label_RXstat_2.Text = "";
            label_RXstat_3.Text = "";
            label_RXstat_4.Text = "";
            label_RXstat_5.Text = "";

            label_TXstat_1.Text = "";
            label_TXstat_2.Text = "";
            label_TXstat_3.Text = "";
            label_TXstat_4.Text = "";
            label_TXstat_5.Text = "";


        }




        
        public override bool Loop()
        {
            return true;
        }

        int lasttab = 99;
        public void Reposition()
        {

            //Console.WriteLine("reposition");

        }
        public override bool Exit()
        {
            return true;
        }



        private void MavPacketReceived(object o, MAVLink.MAVLinkMessage linkMessage)
        {

            if (((MAVLink.MAVLINK_MSG_ID) linkMessage.msgid == MAVLink.MAVLINK_MSG_ID.DATA96)){
                if(linkMessage.compid == (int)_Instance.target_mavlink_ID)mav_decode_data96(linkMessage);

            }else if(((MAVLink.MAVLINK_MSG_ID) linkMessage.msgid == MAVLink.MAVLINK_MSG_ID.HEARTBEAT)){
                if(linkMessage.compid == (int)_Instance.target_mavlink_ID)mav_decode_heartbeat(linkMessage);

            }else if(((MAVLink.MAVLINK_MSG_ID) linkMessage.msgid == MAVLink.MAVLINK_MSG_ID.TUNNEL)){
                if(linkMessage.compid == (int)_Instance.target_mavlink_ID)mav_decode_tunnel(linkMessage);

            }else if(((MAVLink.MAVLINK_MSG_ID) linkMessage.msgid == MAVLink.MAVLINK_MSG_ID.STATUSTEXT)){
                if(linkMessage.compid == (int)_Instance.target_mavlink_ID)mav_decode_statustext(linkMessage);

            }else if (((MAVLink.MAVLINK_MSG_ID) linkMessage.msgid == MAVLink.MAVLINK_MSG_ID.PARAM_VALUE))
            {
                //MAV_CMD_DO_SET_PARAMETER 
                parseParamMsg(linkMessage);
            }

        }


        private bool parseParamMsg(MAVLink.MAVLinkMessage msg) {

            var par = (MAVLink.mavlink_param_value_t)msg.data;

            int strlen = 0;
            for(int i=0; i<16; i++){
                if(par.param_id[i] == 0){
                    strlen = i;
                    break;
                }
            }

            //Console.WriteLine("Param:"+Encoding.UTF8.GetString(par.param_id, 0, strlen)+"- "+strlen);

            set_param(par.param_index, Encoding.UTF8.GetString(par.param_id, 0, strlen), par.param_value, par.param_type);
            return true;
        }


        private int mav_decode_tunnel( MAVLink.MAVLinkMessage msg)
        {

            var tnldta = (MAVLink.mavlink_tunnel_t)msg.data;
            //Console.WriteLine("--------MAV TUNNEL! ----------- " + tnldta.payload_type +" " + tnldta.payload[0]);


            if(tnldta.payload_type == (ushort)0xFFC0)
            {
                //incoming stat data
                mav_decode_tunnel_stat(tnldta);
            }

            return 0;
        }

        private void mav_decode_tunnel_stat(MAVLink.mavlink_tunnel_t tnldta)
        {
            

            if(tnldta.payload[0] != port_to_show)return;
            label_in_rate.Text = tnldta.payload[1].ToString();
            label_out_rate.Text = tnldta.payload[2].ToString();
            //Console.WriteLine("*"+tnldta.payload[0]+"*"+tnldta.payload[1]+"*"+tnldta.payload[2]+"*"+tnldta.payload[3]);
            if(tnldta.payload[1]>0){
                label_status.Text = "ONLINE";
            }else{
                label_status.Text = "OFFLINE";
            }

            int cntr = 3;
            label_RXstat_1.Text = ((((int)tnldta.payload[cntr++])<<8)+tnldta.payload[cntr++]) + " | " + tnldta.payload[cntr++];
            label_RXstat_2.Text = ((((int)tnldta.payload[cntr++])<<8)+tnldta.payload[cntr++]) + " | " + tnldta.payload[cntr++];
            label_RXstat_3.Text = ((((int)tnldta.payload[cntr++])<<8)+tnldta.payload[cntr++]) + " | " + tnldta.payload[cntr++];
            label_RXstat_4.Text = ((((int)tnldta.payload[cntr++])<<8)+tnldta.payload[cntr++]) + " | " + tnldta.payload[cntr++];
            label_RXstat_5.Text = ((((int)tnldta.payload[cntr++])<<8)+tnldta.payload[cntr++]) + " | " + tnldta.payload[cntr++];

            label_TXstat_1.Text = ((((int)tnldta.payload[cntr++])<<8)+tnldta.payload[cntr++]) + " | " + tnldta.payload[cntr++];
            label_TXstat_2.Text = ((((int)tnldta.payload[cntr++])<<8)+tnldta.payload[cntr++]) + " | " + tnldta.payload[cntr++];
            label_TXstat_3.Text = ((((int)tnldta.payload[cntr++])<<8)+tnldta.payload[cntr++]) + " | " + tnldta.payload[cntr++];
            label_TXstat_4.Text = ((((int)tnldta.payload[cntr++])<<8)+tnldta.payload[cntr++]) + " | " + tnldta.payload[cntr++];
            label_TXstat_5.Text = ((((int)tnldta.payload[cntr++])<<8)+tnldta.payload[cntr++]) + " | " + tnldta.payload[cntr++];

        }


        private void fwUpdateClicked(object sender, EventArgs e)
        {

        }



        private int mav_decode_statustext( MAVLink.MAVLinkMessage msg)
        {

            var pckt = (MAVLink.mavlink_statustext_t)msg.data;
            string txt = System.Text.Encoding.UTF8.GetString(pckt.text);
            if (txt.Contains("MavPorter FW:"))
            {
                String[] ver = txt.Split(':');
                var output = new string(ver[1].Where(char.IsNumber).ToArray());
                Console.WriteLine("Firmware rcvd! FW (" + output + ")");
            }
            return 0;
        }


        private int mav_decode_data96( MAVLink.MAVLinkMessage msg)
        {

            var dta = (MAVLink.mavlink_data96_t)msg.data;
            lasttab = dta.type;
            return 0;
        }

        private int mav_decode_heartbeat( MAVLink.MAVLinkMessage msg)
        {

            var hb = (MAVLink.mavlink_heartbeat_t)msg.data;
            
            if(hb.custom_mode == 0xACACACAC){//2896997548
                _porter_online = true;
                //Console.WriteLine("***HB PORTER***");
            }

            return 0;
        }


        public static void mav_send_stat_command()
        {

            byte[] dta = new byte[16];
            dta[0] = 0xA1;
            dta[1] = (byte)_Instance.port_to_show;

            MAVLink.mavlink_tunnel_t msg = new MAVLink.mavlink_tunnel_t
            {
                payload_type = 0xFF32,
                target_system = 1,
                target_component = (byte)_Instance.target_mavlink_ID,
                payload_length = 2,
                payload = dta
            };
            var mav = _Instance.Host.comPort.MAVlist.FirstOrDefault(a => a.compid == _Instance.target_mavlink_ID);
            if (mav != null)mav.parent.generatePacket((int) MAVLink.MAVLINK_MSG_ID.TUNNEL, msg, mav.sysid, mav.compid, true, false);
        }




        private void prt1_btn_Click(object sender, EventArgs e)
        {
            port_to_show = 1;
            refresh_display = true;
            ClearDisplay();
            RefreshButtons();
        }

        private void prt2_btn_Click(object sender, EventArgs e)
        {
            port_to_show = 2;
            refresh_display = true;
            ClearDisplay();
            RefreshButtons();
        }

        private void prt3_btn_Click(object sender, EventArgs e)
        {
            port_to_show = 3;
            refresh_display = true;
            ClearDisplay();
            RefreshButtons();
        }

        private void prt4_btn_Click(object sender, EventArgs e)
        {
            port_to_show = 4;
            refresh_display = true;
            ClearDisplay();
            RefreshButtons();
        }

        private void prt5_btn_Click(object sender, EventArgs e)
        {
            port_to_show = 5;
            refresh_display = true;
            ClearDisplay();
            RefreshButtons();
        }




        private void InitializeComponent()
        {


            this.prt1_btn = new System.Windows.Forms.Button();
            this.prt2_btn = new System.Windows.Forms.Button();
            this.prt3_btn = new System.Windows.Forms.Button();
            this.prt4_btn = new System.Windows.Forms.Button();
            this.prt5_btn = new System.Windows.Forms.Button();
            this.statusGrp = new System.Windows.Forms.GroupBox();
            this.label_status = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.label_TXstat_5 = new System.Windows.Forms.Label();
            this.label_TXstat_4 = new System.Windows.Forms.Label();
            this.label_TXstat_3 = new System.Windows.Forms.Label();
            this.label_TXstat_2 = new System.Windows.Forms.Label();
            this.label_TXstat_1 = new System.Windows.Forms.Label();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.label_baud = new System.Windows.Forms.Label();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.label_RXstat_5 = new System.Windows.Forms.Label();
            this.label_RXstat_4 = new System.Windows.Forms.Label();
            this.label_RXstat_3 = new System.Windows.Forms.Label();
            this.label_RXstat_2 = new System.Windows.Forms.Label();
            this.label_RXstat_1 = new System.Windows.Forms.Label();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label_out_rate = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label_in_rate = new System.Windows.Forms.Label();
            this.GroupBox_portStatus = new System.Windows.Forms.GroupBox();
            this.groupBox_connectionStatus = new System.Windows.Forms.GroupBox();
            this.label_connection = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.statusGrp.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox6.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.GroupBox_portStatus.SuspendLayout();
            this.groupBox_connectionStatus.SuspendLayout();
            //this.SuspendLayout();
            // 
            // prt1_btn
            // 
            this.prt1_btn.Location = new System.Drawing.Point(6, 26);
            this.prt1_btn.Name = "prt1_btn";
            this.prt1_btn.Size = new System.Drawing.Size(75, 23);
            this.prt1_btn.TabIndex = 0;
            this.prt1_btn.Text = "PRT1";
            this.prt1_btn.UseVisualStyleBackColor = true;
            this.prt1_btn.Click += new System.EventHandler(this.prt1_btn_Click);
            // 
            // prt2_btn
            // 
            this.prt2_btn.Location = new System.Drawing.Point(87, 26);
            this.prt2_btn.Name = "prt2_btn";
            this.prt2_btn.Size = new System.Drawing.Size(75, 23);
            this.prt2_btn.TabIndex = 1;
            this.prt2_btn.Text = "PRT2";
            this.prt2_btn.UseVisualStyleBackColor = true;
            this.prt2_btn.Click += new System.EventHandler(this.prt2_btn_Click);
            // 
            // prt3_btn
            // 
            this.prt3_btn.Location = new System.Drawing.Point(168, 26);
            this.prt3_btn.Name = "prt3_btn";
            this.prt3_btn.Size = new System.Drawing.Size(75, 23);
            this.prt3_btn.TabIndex = 2;
            this.prt3_btn.Text = "PRT3";
            this.prt3_btn.UseVisualStyleBackColor = true;
            this.prt3_btn.Click += new System.EventHandler(this.prt3_btn_Click);
            // 
            // prt4_btn
            // 
            this.prt4_btn.Location = new System.Drawing.Point(249, 26);
            this.prt4_btn.Name = "prt4_btn";
            this.prt4_btn.Size = new System.Drawing.Size(75, 23);
            this.prt4_btn.TabIndex = 3;
            this.prt4_btn.Text = "PRT4";
            this.prt4_btn.UseVisualStyleBackColor = true;
            this.prt4_btn.Click += new System.EventHandler(this.prt4_btn_Click);
            // 
            // prt5_btn
            // 
            this.prt5_btn.Location = new System.Drawing.Point(330, 26);
            this.prt5_btn.Name = "prt5_btn";
            this.prt5_btn.Size = new System.Drawing.Size(75, 23);
            this.prt5_btn.TabIndex = 3;
            this.prt5_btn.Text = "PRT5";
            this.prt5_btn.UseVisualStyleBackColor = true;
            this.prt5_btn.Click += new System.EventHandler(this.prt5_btn_Click);
            // 
            // statusGrp
            // 
            this.statusGrp.Controls.Add(this.label_status);
            this.statusGrp.Location = new System.Drawing.Point(15, 19);
            this.statusGrp.Name = "statusGrp";
            this.statusGrp.Size = new System.Drawing.Size(118, 40);
            this.statusGrp.TabIndex = 4;
            this.statusGrp.TabStop = false;
            this.statusGrp.Text = "Status";
            // 
            // label_status
            // 
            this.label_status.AutoSize = true;
            this.label_status.Dock = System.Windows.Forms.DockStyle.Right;
            this.label_status.Location = new System.Drawing.Point(59, 16);
            this.label_status.Name = "label_status";
            this.label_status.Padding = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this.label_status.Size = new System.Drawing.Size(56, 13);
            this.label_status.TabIndex = 0;
            this.label_status.Text = "OFFLINE";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.groupBox6);
            this.groupBox1.Controls.Add(this.groupBox5);
            this.groupBox1.Controls.Add(this.groupBox4);
            this.groupBox1.Controls.Add(this.groupBox3);
            this.groupBox1.Controls.Add(this.groupBox2);
            this.groupBox1.Controls.Add(this.statusGrp);
            this.groupBox1.Location = new System.Drawing.Point(6, 48);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(399, 202);
            this.groupBox1.TabIndex = 5;
            this.groupBox1.TabStop = false;
            // 
            // groupBox6
            // 
            this.groupBox6.Controls.Add(this.label2);
            this.groupBox6.Controls.Add(this.label_TXstat_5);
            this.groupBox6.Controls.Add(this.label_TXstat_4);
            this.groupBox6.Controls.Add(this.label_TXstat_3);
            this.groupBox6.Controls.Add(this.label_TXstat_2);
            this.groupBox6.Controls.Add(this.label_TXstat_1);
            this.groupBox6.Location = new System.Drawing.Point(307, 19);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.Padding = new System.Windows.Forms.Padding(3, 23, 3, 3);
            this.groupBox6.Size = new System.Drawing.Size(75, 120);
            this.groupBox6.TabIndex = 8;
            this.groupBox6.TabStop = false;
            this.groupBox6.Text = "TX data";
            // 
            // label_TXstat_5
            // 
            this.label_TXstat_5.Dock = System.Windows.Forms.DockStyle.Top;
            this.label_TXstat_5.Location = new System.Drawing.Point(3, 96);
            this.label_TXstat_5.Name = "label_TXstat_5";
            this.label_TXstat_5.Padding = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this.label_TXstat_5.Size = new System.Drawing.Size(69, 15);
            this.label_TXstat_5.TabIndex = 4;
            this.label_TXstat_5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label_TXstat_4
            // 
            this.label_TXstat_4.Dock = System.Windows.Forms.DockStyle.Top;
            this.label_TXstat_4.Location = new System.Drawing.Point(3, 81);
            this.label_TXstat_4.Name = "label_TXstat_4";
            this.label_TXstat_4.Padding = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this.label_TXstat_4.Size = new System.Drawing.Size(69, 15);
            this.label_TXstat_4.TabIndex = 3;
            this.label_TXstat_4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label_TXstat_3
            // 
            this.label_TXstat_3.Dock = System.Windows.Forms.DockStyle.Top;
            this.label_TXstat_3.Location = new System.Drawing.Point(3, 66);
            this.label_TXstat_3.Name = "label_TXstat_3";
            this.label_TXstat_3.Padding = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this.label_TXstat_3.Size = new System.Drawing.Size(69, 15);
            this.label_TXstat_3.TabIndex = 2;
            this.label_TXstat_3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label_TXstat_2
            // 
            this.label_TXstat_2.Dock = System.Windows.Forms.DockStyle.Top;
            this.label_TXstat_2.Location = new System.Drawing.Point(3, 51);
            this.label_TXstat_2.Name = "label_TXstat_2";
            this.label_TXstat_2.Padding = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this.label_TXstat_2.Size = new System.Drawing.Size(69, 15);
            this.label_TXstat_2.TabIndex = 1;
            this.label_TXstat_2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label_TXstat_1
            // 
            this.label_TXstat_1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label_TXstat_1.Location = new System.Drawing.Point(3, 36);
            this.label_TXstat_1.Name = "label_TXstat_1";
            this.label_TXstat_1.Padding = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this.label_TXstat_1.Size = new System.Drawing.Size(69, 15);
            this.label_TXstat_1.TabIndex = 0;
            this.label_TXstat_1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.label_baud);
            this.groupBox5.Location = new System.Drawing.Point(15, 74);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(118, 40);
            this.groupBox5.TabIndex = 7;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Baudrate";
            // 
            // label_baud
            // 
            this.label_baud.Dock = System.Windows.Forms.DockStyle.Right;
            this.label_baud.Location = new System.Drawing.Point(35, 16);
            this.label_baud.Name = "label_baud";
            this.label_baud.Padding = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this.label_baud.Size = new System.Drawing.Size(80, 21);
            this.label_baud.TabIndex = 0;
            this.label_baud.Text = "-";
            this.label_baud.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.label1);
            this.groupBox4.Controls.Add(this.label_RXstat_5);
            this.groupBox4.Controls.Add(this.label_RXstat_4);
            this.groupBox4.Controls.Add(this.label_RXstat_3);
            this.groupBox4.Controls.Add(this.label_RXstat_2);
            this.groupBox4.Controls.Add(this.label_RXstat_1);
            this.groupBox4.Location = new System.Drawing.Point(225, 19);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Padding = new System.Windows.Forms.Padding(3, 23, 3, 3);
            this.groupBox4.Size = new System.Drawing.Size(76, 120);
            this.groupBox4.TabIndex = 7;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "RX data";
            // 
            // label_RXstat_5
            // 
            this.label_RXstat_5.Dock = System.Windows.Forms.DockStyle.Top;
            this.label_RXstat_5.Location = new System.Drawing.Point(3, 96);
            this.label_RXstat_5.Name = "label_RXstat_5";
            this.label_RXstat_5.Padding = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this.label_RXstat_5.Size = new System.Drawing.Size(70, 15);
            this.label_RXstat_5.TabIndex = 4;
            this.label_RXstat_5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label_RXstat_4
            // 
            this.label_RXstat_4.Dock = System.Windows.Forms.DockStyle.Top;
            this.label_RXstat_4.Location = new System.Drawing.Point(3, 81);
            this.label_RXstat_4.Name = "label_RXstat_4";
            this.label_RXstat_4.Padding = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this.label_RXstat_4.Size = new System.Drawing.Size(70, 15);
            this.label_RXstat_4.TabIndex = 3;
            this.label_RXstat_4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label_RXstat_3
            // 
            this.label_RXstat_3.Dock = System.Windows.Forms.DockStyle.Top;
            this.label_RXstat_3.Location = new System.Drawing.Point(3, 66);
            this.label_RXstat_3.Name = "label_RXstat_3";
            this.label_RXstat_3.Padding = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this.label_RXstat_3.Size = new System.Drawing.Size(70, 15);
            this.label_RXstat_3.TabIndex = 2;
            this.label_RXstat_3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label_RXstat_2
            // 
            this.label_RXstat_2.Dock = System.Windows.Forms.DockStyle.Top;
            this.label_RXstat_2.Location = new System.Drawing.Point(3, 51);
            this.label_RXstat_2.Name = "label_RXstat_2";
            this.label_RXstat_2.Padding = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this.label_RXstat_2.Size = new System.Drawing.Size(70, 15);
            this.label_RXstat_2.TabIndex = 1;
            this.label_RXstat_2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label_RXstat_1
            // 
            this.label_RXstat_1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label_RXstat_1.Location = new System.Drawing.Point(3, 36);
            this.label_RXstat_1.Name = "label_RXstat_1";
            this.label_RXstat_1.Padding = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this.label_RXstat_1.Size = new System.Drawing.Size(70, 15);
            this.label_RXstat_1.TabIndex = 0;
            this.label_RXstat_1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.label_out_rate);
            this.groupBox3.Location = new System.Drawing.Point(307, 145);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(76, 40);
            this.groupBox3.TabIndex = 6;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Output rate";
            // 
            // label_out_rate
            // 
            this.label_out_rate.Dock = System.Windows.Forms.DockStyle.Right;
            this.label_out_rate.Location = new System.Drawing.Point(33, 16);
            this.label_out_rate.Name = "label_out_rate";
            this.label_out_rate.Padding = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this.label_out_rate.Size = new System.Drawing.Size(40, 21);
            this.label_out_rate.TabIndex = 0;
            this.label_out_rate.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label_in_rate);
            this.groupBox2.Location = new System.Drawing.Point(225, 145);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(76, 40);
            this.groupBox2.TabIndex = 5;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Input rate";
            // 
            // label_in_rate
            // 
            this.label_in_rate.Dock = System.Windows.Forms.DockStyle.Right;
            this.label_in_rate.Location = new System.Drawing.Point(33, 16);
            this.label_in_rate.Name = "label_in_rate";
            this.label_in_rate.Padding = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this.label_in_rate.Size = new System.Drawing.Size(40, 21);
            this.label_in_rate.TabIndex = 0;
            this.label_in_rate.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // GroupBox_portStatus
            // 
            this.GroupBox_portStatus.Controls.Add(this.prt1_btn);
            this.GroupBox_portStatus.Controls.Add(this.prt5_btn);
            this.GroupBox_portStatus.Controls.Add(this.groupBox1);
            this.GroupBox_portStatus.Controls.Add(this.prt4_btn);
            this.GroupBox_portStatus.Controls.Add(this.prt2_btn);
            this.GroupBox_portStatus.Controls.Add(this.prt3_btn);
            this.GroupBox_portStatus.Location = new System.Drawing.Point(12, 73);
            this.GroupBox_portStatus.Name = "GroupBox_portStatus";
            this.GroupBox_portStatus.Size = new System.Drawing.Size(411, 256);
            this.GroupBox_portStatus.TabIndex = 6;
            this.GroupBox_portStatus.TabStop = false;
            this.GroupBox_portStatus.Text = "PORTS STATUS";
            // 
            // groupBox_connectionStatus
            // 
            this.groupBox_connectionStatus.Controls.Add(this.label_connection);
            this.groupBox_connectionStatus.Location = new System.Drawing.Point(12, 12);
            this.groupBox_connectionStatus.Name = "groupBox_connectionStatus";
            this.groupBox_connectionStatus.Size = new System.Drawing.Size(198, 42);
            this.groupBox_connectionStatus.TabIndex = 7;
            this.groupBox_connectionStatus.TabStop = false;
            this.groupBox_connectionStatus.Text = "CONNECTION";
            // 
            // label_connection
            // 
            this.label_connection.AutoSize = true;
            this.label_connection.Dock = System.Windows.Forms.DockStyle.Right;
            this.label_connection.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_connection.Location = new System.Drawing.Point(130, 16);
            this.label_connection.Name = "label_connection";
            this.label_connection.Padding = new System.Windows.Forms.Padding(0, 0, 7, 0);
            this.label_connection.Size = new System.Drawing.Size(65, 13);
            this.label_connection.TabIndex = 0;
            this.label_connection.Text = "OFFLINE";
            this.label_connection.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.label1.Location = new System.Drawing.Point(3, 13);
            this.label1.Name = "label1";
            this.label1.Padding = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this.label1.Size = new System.Drawing.Size(70, 15);
            this.label1.TabIndex = 5;
            this.label1.Text = "MsgId | Freq";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.label2.Location = new System.Drawing.Point(4, 13);
            this.label2.Name = "label2";
            this.label2.Padding = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this.label2.Size = new System.Drawing.Size(70, 15);
            this.label2.TabIndex = 6;
            this.label2.Text = "MsgId | Freq";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            tab.Controls.Add(this.groupBox_connectionStatus);
            tab.Controls.Add(this.GroupBox_portStatus);

        }


        private System.Windows.Forms.Button prt1_btn;
        private System.Windows.Forms.Button prt2_btn;
        private System.Windows.Forms.Button prt3_btn;
        private System.Windows.Forms.Button prt4_btn;
        private System.Windows.Forms.Button prt5_btn;
        private System.Windows.Forms.GroupBox statusGrp;
        private System.Windows.Forms.Label label_status;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Label label_RXstat_5;
        private System.Windows.Forms.Label label_RXstat_4;
        private System.Windows.Forms.Label label_RXstat_3;
        private System.Windows.Forms.Label label_RXstat_2;
        private System.Windows.Forms.Label label_RXstat_1;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label_out_rate;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label_in_rate;
        private System.Windows.Forms.GroupBox groupBox6;
        private System.Windows.Forms.Label label_TXstat_5;
        private System.Windows.Forms.Label label_TXstat_4;
        private System.Windows.Forms.Label label_TXstat_3;
        private System.Windows.Forms.Label label_TXstat_2;
        private System.Windows.Forms.Label label_TXstat_1;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.Label label_baud;
        private System.Windows.Forms.GroupBox GroupBox_portStatus;
        private System.Windows.Forms.GroupBox groupBox_connectionStatus;
        private System.Windows.Forms.Label label_connection;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;

    }
}