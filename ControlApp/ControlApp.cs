using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Engine;

namespace ControlApp
{
    public partial class ControlApp : Form
    {
        //private FtdiFifo _Ftdififo;
        //private readonly string allowedSerialNumber = "NL51G6ZIA";
        

        private USBdataHandler _USBdataHandler;
        private Ddc _Ddc;


        public ControlApp()
        {
            InitializeComponent();

            AddEvents(DieGroupBox.Controls);
            AddEvents(AdcGroupBox.Controls);
            AddEvents(TempSensorGroupBox.Controls);
            AddEvents(TestPatternsGroupBox.Controls);


            



            //_FtdiFifo = new FtdiFifo();
            //_FtdiFifo.IdentifyDevice();
            //if (!_FtdiFifo.IsDeviceAllowed(allowedSerialNumber))
            //{
            //    return;
            //}

            //System.Diagnostics.Debug.WriteLine("App continued");

            //_FtdiFifo.SetFifoMode();

            _USBdataHandler = new USBdataHandler();
            _USBdataHandler.FtdiFifo.OnLogEntry += ControlApp_OnLogEntryHandler;
            _USBdataHandler.OnLogEntry += ControlApp_OnLogEntryHandler;


            _Ddc = new Ddc();
            

        }

        private void SendButton_Click(object sender, EventArgs e)
        {
            //_FtdiFifo.SendDataIntoFifo(sendDataTextBox.Text);
            //_USBdataHandler.SendDataIntoFifo(sendDataTextBox.Text);

        }

        private void ReadButton_Click(object sender, EventArgs e)
        {
            readTextBox.AppendText(_USBdataHandler.ReadDataFromFifo());

        }

        private void ReadSdBtn_Click(object sender, EventArgs e)
        {
            _USBdataHandler.ReadSD(argument1Tb.Text + argument2Tb.Text);
        }

        private void writeSdBtn_Click(object sender, EventArgs e)
        {
            OpenFileDialog FileForSdWriteOdb = new OpenFileDialog
            {
                InitialDirectory = @"C:\",
                Title = "Browse Binary Files",

                CheckFileExists = true,
                CheckPathExists = true,

                DefaultExt = "bin",
                Filter = "bin files (*.bin)|*.bin",
                FilterIndex = 2,
                RestoreDirectory = true,

                ReadOnlyChecked = true,
                ShowReadOnly = true
            };

            if (FileForSdWriteOdb.ShowDialog() == DialogResult.OK)
            {
                //textBox1.Text = openFileDialog1.FileName;
            }

            _USBdataHandler.WriteSD(argument3Tb.Text);
        }


        private void DdcRegisterCommand_Click_1(object sender, EventArgs e)
        {
            //_USBdataHandler.DdcRegisterCommand(DdcCommand_textbox.Text + DdcAddres_textbox.Text + DdcValue_textbox.Text);
            _USBdataHandler.DdcRegisterCommand(DdcAddres_textbox.Text);

        }


        private void LvOn_button_Click(object sender, EventArgs e)
        {
            _USBdataHandler.LvOnCommand();
            LvOn_button.Enabled = false;
            LvOff_button.Enabled = true;

            ResetGroupBox.Enabled = true;
            DieGroupBox.Enabled = true;
            AdcGroupBox.Enabled = true;
            TempSensorGroupBox.Enabled = true;
            TestPatternsGroupBox.Enabled = true;

        }

        private void LvOff_button_Click(object sender, EventArgs e)
        {
            _USBdataHandler.LvOffCommand();
            LvOn_button.Enabled = true;
            LvOff_button.Enabled = false;

            ResetGroupBox.Enabled = false;
            DieGroupBox.Enabled = false;
            AdcGroupBox.Enabled = false;
            TempSensorGroupBox.Enabled = false;
            TestPatternsGroupBox.Enabled = false;
        }

        private void DdcRegisterWrite_Click(object sender, EventArgs e)
        {
            //_USBdataHandler.DdcWriteRegisterCommand(DdcAddresWrite_textbox.Text + DdcValueWrite_textbox.Text);
            _USBdataHandler.DdcWriteRegisterCommand(DdcValueWrite_textbox.Text);
        }

        private void DdcReset_button_Click(object sender, EventArgs e)
        {
            _USBdataHandler.DdcResetCommand();
        }

        private void DdcEnable_button_Click(object sender, EventArgs e)
        {
            _USBdataHandler.DdcEnableCommand();
        }

        public void ControlApp_OnLogEntryHandler(object sender, LogEntryEventArgs e)
        {

            this.Log_TextBox.Invoke((Action)delegate
             {
                 this.Log_TextBox.AppendText(GetTimestamp(DateTime.Now) + "| " + e.LogEntry + Environment.NewLine);
             });
        }

        public String GetTimestamp(DateTime value)
        {
            String TimeStamp = value.ToString("yyyy-MM-dd HH:mm:ss:fff");
            return TimeStamp;
        }


        private void readMemoryBtn_Click(object sender, EventArgs e)
        {
            _USBdataHandler.ReadEepromUSB(eepromArgument1Tb.Text);
        }

        private void writeEepromBtn_Click(object sender, EventArgs e)
        {
            _USBdataHandler.WriteEeprom(eepromArgument4Tb.Text + eepromArgument3Tb.Text);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _USBdataHandler.WriteEnableEeprom();
        }

        private void reinitFtdiBtn_Click(object sender, EventArgs e)
        {
            _USBdataHandler.CloseUSB();
            _USBdataHandler = new USBdataHandler();
            _USBdataHandler.FtdiFifo.OnLogEntry += ControlApp_OnLogEntryHandler;
            _USBdataHandler.OnLogEntry += ControlApp_OnLogEntryHandler;
        }

        private void WriteConfigurationBtn_Click(object sender, EventArgs e)
        {
            _USBdataHandler.WriteEnableEeprom();

            //// loop over all 23 registers 
            //for (int i=0; i < _Ddc.RegistersData.Length/2; i++)
            //{

            //    byte[] combined = new byte[3];
            //    combined[0] = _Ddc.RegistersData[2*i];
            //    combined[1] = _Ddc.RegistersData[2*i + 1];
            //    combined[2] = _Ddc.RegisterAddresses[i];

            //    _USBdataHandler.WriteEeprom(combined);

            //}


            string[] result = textBox1.Text.Split("\r\n".ToCharArray());

            string address = "";
            string data = "";

            foreach (String str in result)
            {
                int milliseconds = 500;
                Thread.Sleep(milliseconds);

                if (!(str.Length == 0))
                {
                    //address = str.Substring(4,4);
                    //data = str.Substring(0,4);
                    //System.Diagnostics.Debug.WriteLine("address=0x" + address + " data=0x" + data);
                    _USBdataHandler.WriteEeprom(str);
                }
            }

            
        }

        public static byte[] Combine(byte[] first, byte[] second)
        {
            return first.Concat(second).ToArray();
        }

        private void ResetConfigurationBtn_Click(object sender, EventArgs e)
        {
            _Ddc.RegistersData[0] = 0x01;

            byte[] combined = new byte[3];
            //combined[0] = _Ddc.RegistersData[0];
            //combined[1] = _Ddc.RegistersData[1];
            //combined[2] = _Ddc.RegisterAddresses[0];

            _USBdataHandler.WriteEeprom(combined);
        }

        private void readToFifo_Click(object sender, EventArgs e)
        {
            _USBdataHandler.ReadEepromToFifo();
        }

        private void ProgramDDC_Click(object sender, EventArgs e)
        {
            _USBdataHandler.ProgramDdc();

        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            _USBdataHandler.DdcResetHw();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _USBdataHandler.DdcSdout();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            _USBdataHandler.DdcReadout();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            _USBdataHandler.DdcWrite();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            _USBdataHandler.DdcClkDiv2();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            _USBdataHandler.DdcClkSpeed();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            _USBdataHandler.DdcEnableConv();
        }

        private void radioButton6_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void ApplyBtn_Click(object sender, EventArgs e)
        {
            // grey-out Apply button
            ApplyBtn.Enabled = false;

            // translate Die selection to register value
            String DieSelection = DieGroupBox.Controls.OfType<RadioButton>().FirstOrDefault(r => r.Checked).Name;
            switch (DieSelection)
            {
                case "DieBoth_Rb":
                    CommonData.RegistersDictonary[0x02] = 0x0000;
                    break;

                case "DieTop_Rb":
                    CommonData.RegistersDictonary[0x02] = 0xC000;
                    break;

                case "DieBottom_Rb":
                    CommonData.RegistersDictonary[0x02] = 0x4000;
                    break;
            }

            // translate ADC shutdown to register value
            String AdcShutdown = AdcGroupBox.Controls.OfType<RadioButton>().FirstOrDefault(r => r.Checked).Name;
            switch (AdcShutdown)
            {
                case "AdcSdYes_Rb":
                    CommonData.RegistersDictonary[0x5C] = 0x0000;
                    break;

                case "AdcSdNo_Rb":
                    CommonData.RegistersDictonary[0x5C] = 0x1000;
                    break;
            }


            // translate Test Patterns to register value
            String TestPaterns = TestPatternsGroupBox.Controls.OfType<RadioButton>().FirstOrDefault(r => r.Checked).Name;
            System.Diagnostics.Debug.WriteLine(TestPaterns);
            switch (TestPaterns)
            {
                case "TestPatternDisabled_Rb":
                    CommonData.RegistersDictonary[0x1A] = 0x0000;
                    break;

                case "TestPatternEnabled_Rb":
                    String TestPatternType = PatternMode_Cb.GetItemText(PatternMode_Cb.SelectedItem);
                    System.Diagnostics.Debug.WriteLine(TestPatternType);
                    switch (TestPatternType)
                    {
                        case "All zeros":
                            CommonData.RegistersDictonary[0x1A] = 0x0001;
                            break;
                        case "All ones":
                            CommonData.RegistersDictonary[0x1A] = 0x0003;
                            break;
                        case "Ramp":
                            CommonData.RegistersDictonary[0x1A] = 0x0005;
                            break;
                        case "Custom Pattern":
                            CommonData.RegistersDictonary[0x1A] = (ushort)(Convert.ToUInt16(CustomPaternEven_Tb.Text.Substring(0, 2), 16) << 8 | 0x0007); // CHANGE IT
                            CommonData.RegistersDictonary[0x1B] = (ushort)(Convert.ToUInt16(CustomPaternEven_Tb.Text.Substring(2, 4), 16));

                            CommonData.RegistersDictonary[0x13] = (ushort)(Convert.ToUInt16(CustomPaternOdd_Tb.Text.Substring(0, 2), 16));
                            CommonData.RegistersDictonary[0x14] = (ushort)(Convert.ToUInt16(CustomPaternOdd_Tb.Text.Substring(2, 4), 16));
                            break;
                        case "Half zeros & half ones":
                            CommonData.RegistersDictonary[0x1A] = 0x0009;
                            break;
                        case "Toggle(1010...)":
                            CommonData.RegistersDictonary[0x1A] = 0x000B;
                            break;
                        case "Toggle(0101...)":
                            CommonData.RegistersDictonary[0x1A] = 0x000D;
                            break;
                        case "Random":
                            CommonData.RegistersDictonary[0x1A] = 0x000F;
                            break;
                    }


                    
                    break;
            }


            // ------------------------------
            // print all registers in console
            // ------------------------------
            Console.WriteLine("REGISTERS:\n");
            foreach (KeyValuePair<byte, ushort> pair in CommonData.RegistersDictonary)
            {
                
                Console.WriteLine("Address: 0x{0} Value: 0x{1}",
                    pair.Key.ToString("X2"),
                    pair.Value.ToString("X4"));
            }


        }

        void DdcConfigChangeHandler(object obj, EventArgs e)
        {
            ApplyBtn.Enabled = true;
        }

        void AddEvents(System.Windows.Forms.Control.ControlCollection Controls)
        {
            foreach (Control c in Controls)
            {
                if (c is GroupBox)
                {
                    AddEvents(((GroupBox)c).Controls);
                }
                else if (c is Panel)
                {
                    AddEvents(((Panel)c).Controls);
                }
                //Expand this series of if...else... to include any 
                //other type of container control
                else if (c is TextBox)
                {
                    ((TextBox)c).TextChanged += new EventHandler(DdcConfigChangeHandler);
                }
                else if (c is RichTextBox)
                {
                    ((RichTextBox)c).TextChanged += new EventHandler(DdcConfigChangeHandler);
                }
                else if (c is CheckBox)
                {
                    ((CheckBox)c).CheckedChanged += new EventHandler(DdcConfigChangeHandler);
                }
                else if (c is DateTimePicker)
                {
                    ((DateTimePicker)c).ValueChanged += new EventHandler(DdcConfigChangeHandler);
                }
                else if (c is RadioButton)
                {
                    ((RadioButton)c).CheckedChanged += new EventHandler(DdcConfigChangeHandler);
                }
                else if (c is ComboBox)
                {
                    ((ComboBox)c).SelectedValueChanged += new EventHandler(DdcConfigChangeHandler);
                }
                //Expand this to include any other type of controls your form 
                //has that you need to add the event to
            }
        }

        private void AppLoad(object sender, EventArgs e)
        {
            PatternMode_Cb.SelectedIndex = 0;
        }

        private void label6_Click(object sender, EventArgs e)
        {

        }
    }
}
