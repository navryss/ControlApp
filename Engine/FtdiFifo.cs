using FTD2XX_NET;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;



namespace Engine
{

    public struct RxDataStruct
    {
        public byte[] RxDataBuffer { get; set; }
        public UInt32 RxDataLength { get; set; }
    }

    public class FtdiFifo
    {

        private FTDI ftHandle = new FTDI();
        private FTDI.FT_DEVICE_INFO_NODE[] deviceInfos = new FTDI.FT_DEVICE_INFO_NODE[6];

        private FTDI.FT_STATUS ftStatus;
        private uint numBytesAvailable = 0;

        public List<string> serialNumbers { get; set; }

        public RxDataStruct RxData;

        // Events declarations:
        public event EventHandler<FtdiBytesReceivedEventArgs> OnFtdiBytesReceived;
        public event EventHandler<LogEntryEventArgs> OnLogEntry;


        UInt64 AllBytesAvailable = 0;
        UInt64 AllBytesRead = 0;

        Task _taskA;
        CancellationTokenSource _CancellationTokenSource = new CancellationTokenSource();
        // private CancellationToken token = set _CancellationTokenSource.Token;


        public FtdiFifo()
        {
            System.Diagnostics.Debug.WriteLine("This is a new FtdiFifo object.");
            serialNumbers = new List<string>();

            RxData.RxDataBuffer = new byte[65536];
            RxData.RxDataLength = 0;

        }


        public bool ResetFTDI(string AllowedDevice)
        {
            UInt32 ftdiDeviceCount = 0;
            FTD2XX_NET.FTDI.FT_STATUS ftStatus = FTD2XX_NET.FTDI.FT_STATUS.FT_OK;

            // Create new instance of the FTDI device class
            FTD2XX_NET.FTDI myFtdiDevice = new FTD2XX_NET.FTDI();
            // Determine the number of FTDI devices connected to the machine
            ftStatus = myFtdiDevice.GetNumberOfDevices(ref ftdiDeviceCount);
            // Check status
            if (ftStatus != FTD2XX_NET.FTDI.FT_STATUS.FT_OK)
            {
                System.Diagnostics.Debug.WriteLine("Failed to get number of FTDI devices [" + ftStatus.ToString() + "]");
                return false;
            }
            // If no devices available, return
            if (ftdiDeviceCount == 0)
            {
                System.Diagnostics.Debug.WriteLine("Failed to find any FTDI devices [" + ftStatus.ToString() + "]");
                return false;
            }
            // Allocate storage for device info list
            FTD2XX_NET.FTDI.FT_DEVICE_INFO_NODE[] ftdiDeviceList = new FTD2XX_NET.FTDI.FT_DEVICE_INFO_NODE[ftdiDeviceCount];
            // Populate our device list
            ftStatus = myFtdiDevice.GetDeviceList(ftdiDeviceList);
            if (ftStatus != FTD2XX_NET.FTDI.FT_STATUS.FT_OK)
            {
                System.Diagnostics.Debug.WriteLine("Failed enumerate FTDI devices [" + ftStatus.ToString() + "]");
                return false;
            }
            // Open first device in our list by serial number
            //ftStatus = myFtdiDevice.OpenBySerialNumber(ftdiDeviceList[0].SerialNumber);
            ftStatus = myFtdiDevice.OpenBySerialNumber(AllowedDevice);
            if (ftStatus != FTD2XX_NET.FTDI.FT_STATUS.FT_OK)
            {
                System.Diagnostics.Debug.WriteLine("Failed to open device [" + ftStatus.ToString() + "]");
                return false;
            }
            // Finally, reset the port
            myFtdiDevice.CyclePort();
            return true;
        }

        public void CycleUsb()
        {
            FTDI.FT_STATUS ftdiStatus;

            ftdiStatus = ftHandle.ResetDevice();
            ftdiStatus = ftHandle.Rescan();

            ftdiStatus = ftHandle.CyclePort();
            if (ftdiStatus == FTDI.FT_STATUS.FT_OK)
            {
                // Port has been cycled. Close the handle.
                ftdiStatus = ftHandle.Close();
            }
            else
            {
                // FT_CyclePort FAILED!
                ftdiStatus = ftHandle.Close();
                System.Diagnostics.Debug.WriteLine("FT_CyclePort FAILED!");
            }
        }

        public void IdentifyDevice()
        {
            FTDI.FT_STATUS ftdiStatus;
            uint ftdiDeviceCount = 0;
            serialNumbers.Clear();

            ftdiStatus = ftHandle.GetNumberOfDevices(ref ftdiDeviceCount);
            if (ftdiDeviceCount > 0)
            {
                System.Diagnostics.Debug.WriteLine("Number of FTDI devices: " + ftdiDeviceCount.ToString() + "\n\n");
            }
            else if (ftdiDeviceCount == 0) // If no devices available, return
            {
                System.Diagnostics.Debug.WriteLine("No FTDI device detected (" + ftdiStatus.ToString() + ")");
                return;
            }


            ftdiStatus = ftHandle.GetDeviceList(deviceInfos);
            if (ftdiStatus == FTDI.FT_STATUS.FT_OK)
            {
                for (UInt32 i = 0; i < ftdiDeviceCount; i++)
                {
                    System.Diagnostics.Debug.WriteLine("Device Index: " + i.ToString());
                    System.Diagnostics.Debug.WriteLine("Flags: " + String.Format("{0:x}", deviceInfos[i].Flags));
                    System.Diagnostics.Debug.WriteLine("Type: " + deviceInfos[i].Type.ToString());
                    System.Diagnostics.Debug.WriteLine("ID: " + String.Format("{0:x}", deviceInfos[i].ID));
                    System.Diagnostics.Debug.WriteLine("Location ID: " + String.Format("{0:x}", deviceInfos[i].LocId));
                    System.Diagnostics.Debug.WriteLine("Serial Number: " + deviceInfos[i].SerialNumber.ToString());
                    System.Diagnostics.Debug.WriteLine("Description: " + deviceInfos[i].Description.ToString());
                    System.Diagnostics.Debug.WriteLine("");

                    serialNumbers.Add(deviceInfos[i].SerialNumber.ToString());
                }
            }

            return;
        }



        public void SetFifoMode(string allowedDevice)
        {
            System.Diagnostics.Debug.WriteLine("Setting FIFO mode...\n");

            System.Diagnostics.Debug.WriteLine(allowedDevice);
            ftStatus = ftHandle.OpenBySerialNumber(allowedDevice);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                System.Diagnostics.Debug.WriteLine("Cannot open device");
                return;
            }
            System.Diagnostics.Debug.WriteLine(ftStatus);

            ftStatus = ftHandle.SetTimeouts(5000, 5000);
            System.Diagnostics.Debug.WriteLine(ftStatus);


            System.Threading.Thread.Sleep(100);
            ftStatus = ftHandle.SetBitMode(0xff, 0x00);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                System.Diagnostics.Debug.WriteLine("Cannot set bit mode");
                return;
            }
            System.Diagnostics.Debug.WriteLine(ftStatus);



            System.Threading.Thread.Sleep(100);
            ftStatus = ftHandle.SetBitMode(0xff, 0x40);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                System.Diagnostics.Debug.WriteLine("Cannot set ftStatus");
                return;
            }
            System.Diagnostics.Debug.WriteLine(ftStatus);

            System.Threading.Thread.Sleep(100);
            ftStatus = ftHandle.SetLatency(2);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                System.Diagnostics.Debug.WriteLine("Cannot set latency");
                return;
            }

            System.Threading.Thread.Sleep(100);
            ftStatus = ftHandle.InTransferSize(0x10000);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                System.Diagnostics.Debug.WriteLine("Cannot set In transfer size");
                return;
            }

            System.Threading.Thread.Sleep(100);
            ftStatus = ftHandle.SetFlowControl(FTDI.FT_FLOW_CONTROL.FT_FLOW_RTS_CTS, 0, 0); // FT_FLOW_RTS_CTS = 256
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                System.Diagnostics.Debug.WriteLine("Cannot set flow control");
                return;
            }

            System.Threading.Thread.Sleep(100);
            ftStatus = ftHandle.Purge(FTDI.FT_PURGE.FT_PURGE_RX);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                System.Diagnostics.Debug.WriteLine("Cannot purge FTDI");
                return;
            }

            System.Diagnostics.Debug.WriteLine("Synchronous FIFO mode enabled for " + allowedDevice);





            var token = _CancellationTokenSource.Token;

            _taskA = Task.Run(() =>
            {

                token.ThrowIfCancellationRequested();

                var receivedDataEvent = new AutoResetEvent(false);
                ftHandle.SetEventNotification(FTDI.FT_EVENTS.FT_EVENT_RXCHAR, receivedDataEvent);


                while (true)
                {
                    receivedDataEvent.WaitOne();
                    ReadAvailable();
                }
                Console.WriteLine("ReceiveLoop Task Cancel requested");

                if (token.IsCancellationRequested)
                {
                    Console.WriteLine("Operation is going to be cancelled");
                    // do some clean up
                    token.ThrowIfCancellationRequested();
                }

            }, _CancellationTokenSource.Token);


            System.Diagnostics.Debug.WriteLine("ReceiveLoop enabled");



        }


        public bool IsDeviceAllowed(string allowedDevice)
        {
            foreach (string device in serialNumbers)
            {
                if (device == allowedDevice)
                {
                    System.Diagnostics.Debug.WriteLine("Allowed device");
                    OnLogEntry?.Invoke(this, new LogEntryEventArgs("INFO | " + "HARDWARE STATUS".PadRight(22, ' ') + " | " + "".PadRight(3, ' ') + " | " + "Hardware detected: " + allowedDevice));

                    return true;
                }
            }

            System.Diagnostics.Debug.WriteLine("No allowed device");
            OnLogEntry?.Invoke(this, new LogEntryEventArgs("ERR  | " + "HARDWARE STATUS".PadRight(22, ' ') +" | " + "".PadRight(3, ' ') + " | " + "Hardware not detected"));

            return false;
        }

        public void CloseFtdi()
        {
            _CancellationTokenSource.Cancel(true);

            _taskA.Wait();
            if (_taskA.Status == TaskStatus.Canceled)
                Console.WriteLine("Task was cancelled");
            else
                Console.WriteLine("Task: " + _taskA.Status);
            ftStatus = ftHandle.Close();
        }

        public void SendDataIntoFifo(string strDataToSend)
        {
            uint txQueue = 0;

            // check buffer
            ftStatus = ftHandle.GetTxBytesWaiting(ref txQueue);

            //System.Diagnostics.Debug.WriteLine(ftStatus);

            uint written = 0;
            byte[] dataToSend = new byte[1];
            dataToSend[0] = Convert.ToByte(strDataToSend,16);

            ftStatus = ftHandle.Write(dataToSend, dataToSend.Length, ref written);
            //System.Diagnostics.Debug.WriteLine(ftStatus);

        }

        public void SendDataIntoFifo2(byte[] bytesToSend)
        {

            uint txQueue = 0;

            // check buffer
            ftStatus = ftHandle.GetTxBytesWaiting(ref txQueue);

            //System.Diagnostics.Debug.WriteLine("Status before send\n");
            //System.Diagnostics.Debug.WriteLine(ftStatus);

            uint written = 0;
            System.Diagnostics.Debug.WriteLine("==================================================");
            System.Diagnostics.Debug.WriteLine("Command:");
            System.Diagnostics.Debug.WriteLine(BitConverter.ToString(bytesToSend));
            ftStatus = ftHandle.Write(bytesToSend, bytesToSend.Length, ref written);
            System.Diagnostics.Debug.WriteLine(ftStatus);
            System.Diagnostics.Debug.WriteLine("==================================================");
            System.Diagnostics.Debug.WriteLine("");

            OnLogEntry?.Invoke(this, new LogEntryEventArgs("CMD  | " + ParseCommand(bytesToSend, bytesToSend.Length) + " | " + bytesToSend.Length.ToString().PadRight(3, ' ') + " | " + BitConverter.ToString(bytesToSend)));


        }

        private string ParseCommand(byte[] Command, int CommandLength)
        {
            String CommandString = "";

            switch (Command[0])
            {
                case 0x20:
                    if (Command[9] == 0x0F)
                    { CommandString = "LV ON"; }
                    else
                    { CommandString = "LV OFF"; }
                    break;

                case 0x30:
                    if (Command[1] == 0x01)
                    { CommandString = "-"; }
                    else if (Command[1] == 0x02)
                    { CommandString = "-"; }
                    else if (Command[1] == 0x03)
                    { CommandString = "DDC REGISTERS PROGRAMMING"; }
                    else if (Command[1] == 0x04)
                    { CommandString = "DDC REGISTER READ"; }
                    else if (Command[1] == 0x05)
                    { CommandString = "DDC HW RESET"; }
                    else if (Command[1] == 0x06)
                    { CommandString = "DDC REGISTER WRITE"; }
                    else if (Command[1] == 0x07)
                    { CommandString = "-"; }
                    else { CommandString = "DDC UNDEFINED COMMAND"; }
                    break;

                case 0x40:
                    if (Command[1] == 0x01)
                    { CommandString = "EEPROM READ"; }
                    else if (Command[1] == 0x02)
                    { CommandString = "EEPROM WRITE"; }
                    else if (Command[1] == 0x03)
                    { CommandString = "EEPROM WRITE ENABLE"; }
                    else if (Command[1] == 0x04)
                    { CommandString = "EEPROM ERASE"; }
                    else { CommandString = "EEPROM UNDEFINED COMMAND"; }
                    break;

                default:
                    CommandString = "UNDEFINED COMMAND";
                    break;

            }

            return CommandString.PadRight(26, ' ');
        }

        public string ReadDataFromFifo()
        {

            ftStatus = ftHandle.GetRxBytesAvailable(ref numBytesAvailable);
            System.Diagnostics.Debug.WriteLine(ftStatus + "  bytes available: " + numBytesAvailable.ToString());

            if (numBytesAvailable < 1)
               return "NO_READ_DATA_AVAILABLE\n";

            AllBytesAvailable = AllBytesAvailable + numBytesAvailable;
            System.Diagnostics.Debug.WriteLine("\nNEW ALL BYTES AVAILABLE: " + AllBytesAvailable.ToString() + "\n");


            byte[] bytes = new byte[numBytesAvailable];
            UInt32 numBytesRead = 0;
            ftHandle.Read(bytes, numBytesAvailable, ref numBytesRead);
            if (numBytesAvailable != numBytesRead)
                System.Diagnostics.Debug.WriteLine("Something bad happened");

            AllBytesRead = AllBytesRead + numBytesRead;
            System.Diagnostics.Debug.WriteLine("\nNEW ALL BYTES READ: " + AllBytesRead.ToString() + "\n");

            return "Dec: " + String.Join(" ", bytes) + "\nHex: " + BitConverter.ToString(bytes).Replace("-", string.Empty) +"\nText: " + Encoding.Default.GetString(bytes) + "\n";


        }


        void ReadAvailable()
        {       

            ftStatus = ftHandle.GetRxBytesAvailable(ref numBytesAvailable);
            System.Diagnostics.Debug.WriteLine(ftStatus + "  bytes available: " + numBytesAvailable.ToString());

            //AllBytesAvailable = AllBytesAvailable + numBytesAvailable;
            //System.Diagnostics.Debug.WriteLine("\nALL BYTES AVAILABLE: " + AllBytesAvailable.ToString() + "\n");


                byte[] bytes = new byte[numBytesAvailable];
                UInt32 numBytesRead = 0;
                ftHandle.Read(bytes, numBytesAvailable, ref numBytesRead);
                //if (numBytesAvailable != numBytesRead)
                //    System.Diagnostics.Debug.WriteLine("Something bad happened");

                //AllBytesRead = AllBytesRead + numBytesRead;
                //System.Diagnostics.Debug.WriteLine("\nALL BYTES READ: " + AllBytesRead.ToString() + "\n");


                //System.Diagnostics.Debug.WriteLine("Dec: " + String.Join(" ", bytes) + "\nHex: " + BitConverter.ToString(bytes).Replace("-", string.Empty) + "\nText: " + Encoding.Default.GetString(bytes) + "\n");

                // -------------------------------
                // Calling Received Data Event with passing data to the event subscriber (USBdataHandler)
                OnFtdiBytesReceived?.Invoke(this, new FtdiBytesReceivedEventArgs(bytes, numBytesAvailable));
            
            // -------------------------------
        }



        //public void ReceiveLoop()
        //{
        //    var receivedDataEvent = new AutoResetEvent(false);
        //    ftHandle.SetEventNotification(FTDI.FT_EVENTS.FT_EVENT_RXCHAR, receivedDataEvent);

        //    Token = _CancellationTokenSource.Token;

        //    while (!Token.IsCancellationRequested)
        //    //while (true)
        //    {
        //        Token.ThrowIfCancellationRequested();
        //        receivedDataEvent.WaitOne();
        //        ReadAvailable();
        //    }

        //    Token.ThrowIfCancellationRequested();
        //    Console.WriteLine("ReceiveLoop Task Cancel requested");
        //}
    }

    public class FtdiBytesReceivedEventArgs : EventArgs
    {
        public FtdiBytesReceivedEventArgs(byte[] iBytes, UInt32 iNumBytesAvailable)
        { 
            Bytes = iBytes;
            NumBytesAvailable = iNumBytesAvailable;
        }
        public byte[] Bytes { get; set; }
        public UInt32 NumBytesAvailable { get; set; }
    }
}
