using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data;
using System.Linq;
using System.Threading;

namespace Engine
{
    // -----------------------------------------------------------------
    // Data Download items
public class MeasurementHeader
{
        public UInt32 Id { get; set; }
        public String Name { get; set; }
        public bool Valid { get; set; }
}

public class USBdataHandler
    {
        // -----------------------------------------------------------------
        // Type of commands handled by FPGA constants:
        const byte CMD_READ_SD = 0x10;
        const byte CMD_LV_ENABLE = 0x20;
        const byte CMD_DDC_REGISTER = 0x30;
        const byte CMD_EEPROM = 0x40;

        // -----------------------------------------------------------------
        // Type of application operations constants:
        const int MODE_NOP = 0;
        const int MODE_SAVE_SD_DATA_TO_FILE = 1;
        const int MODE_SAVE_SD_DATA_TO_FILE_2 = 2;
        const int MODE_LV_ENABLE = 2;
        const int MODE_DDC_READ_REGISTER = 11;
        const int MODE_DDC_RESET = 12;
        const int MODE_DDC_ENABLE = 13;
        const int MODE_DDC_PROGRAM = 14;
        const int MODE_DDC_DATA_SAVE_TO_FILE = 15;
        const int MODE_EEPROM_READ = 41;
        const int MODE_EEPROM_WRITE = 42;
        const int MODE_EEPROM_WRITE_ENABLE = 42;


        // -----------------------------------------------------------------
        // Other constants:
        const byte LAST_DATA_CHUNK = 0x80;
        // -----------------------------------------------------------------


        private int _OperationMode = MODE_NOP;

        private byte[] _CommandBytes = new byte[10];
        //private byte[] _ReceivedDataBuffer = new byte[131072];
        private byte[] _ReceivedDataBuffer = new byte[300000];
        private uint _ReceivedDataBufferPosition = 0;
        private uint _NumberOfBlocksInChunk = 0;
        private uint _TotalNumberOfBlocks = 0;
        private bool _IsOperationFinished = false;
        private uint _AvailableBlocks = 0;
        private uint _BytesLeftInTheBuffer = 0;
        private uint _NumberOfBlocksToRead = 0;
        private double _progressBarValue = 0;

        private UInt64 _HugeAllBytesCounter = 0;

        private uint _TotalNumberOfBytes = 0;


        public FtdiFifo FtdiFifo;
        private readonly string _AllowedSerialNumber = "NL61W73OA";

        private FileStream _FileStream;
        private int _CurrentDataLength = 0;


        private int _progressInfo = 0;


        public event EventHandler<LogEntryEventArgs> OnLogEntry;



        private byte[] _EepromData = new byte[2];
        private bool _EepromNewData = false;







    // -----------------------------------------------------------------
    // USBdataHandler constructor
    // -----------------------------------------------------------------
    public USBdataHandler()
        {   
            
            // ------------ S: find ftdi once ------------------------------
            //FtdiFifo = new FtdiFifo(); // FTDI object
            //FtdiFifo.IdentifyDevice(); // FTDI device detection
            //if (!FtdiFifo.IsDeviceAllowed(_AllowedSerialNumber)) // Is the connected device allowed? (_AllowedSerialNumber)
            //    System.Environment.Exit(0);
            // ------------ E: find ftdi once ------------------------------


            // ------------ S: find ftdi in a loop -------------------------

            int FtdiDetectedLoop = 0;
            bool FtdiDetected = false;


            FtdiFifo = new FtdiFifo(); // FTDI object


            FtdiFifo.IdentifyDevice(); // FTDI device detection
            FtdiDetected = FtdiFifo.IsDeviceAllowed(_AllowedSerialNumber);

            while (FtdiDetectedLoop < 3 && FtdiDetected == false)
            {
                FtdiFifo.ResetFTDI(_AllowedSerialNumber);
                // FtdiFifo.CycleUsb();
                FtdiFifo = null;
                FtdiFifo = new FtdiFifo(); // FTDI object

                FtdiFifo.IdentifyDevice(); // FTDI device detection
                FtdiDetected = FtdiFifo.IsDeviceAllowed(_AllowedSerialNumber);
                FtdiDetectedLoop++;

                System.Diagnostics.Debug.WriteLine("Loop = " + FtdiDetectedLoop.ToString());

                int milliseconds = 1000;
                Thread.Sleep(milliseconds);
            }


            if (!FtdiDetected) // Is the connected device allowed? (_AllowedSerialNumber)
            {
                System.Diagnostics.Debug.WriteLine("KILLING APP");
                System.Environment.Exit(0);
            }

            // ------------ E: find ftdi in a loop -------------------------

            System.Diagnostics.Debug.WriteLine("Set FIFO mode!");
            FtdiFifo.SetFifoMode(_AllowedSerialNumber);    // FTDI chip configuration to FIFO mode

            // -------------------------------------------------------------
            // Subscription to Events:
            FtdiFifo.OnFtdiBytesReceived += FtdiBytesReceivedHandler;
        }

        public void CloseUSB()
        {
            FtdiFifo.CloseFtdi();
        }



        // -----------------------------------------------------------------
        // Read SD card command sending function
        // -----------------------------------------------------------------
        public void ReadSD(string strDataToSend)                    
        {
            _OperationMode = MODE_SAVE_SD_DATA_TO_FILE;

            String FileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + "_readout.bin";
            _FileStream = new FileStream(FileName, FileMode.Create, FileAccess.Write, FileShare.Write);
            _CurrentDataLength = 0;
            Array.Clear(_ReceivedDataBuffer, 0, _ReceivedDataBuffer.Length);      // zeroing array
            _ReceivedDataBufferPosition = 0;
            _NumberOfBlocksInChunk = 0;
            _TotalNumberOfBlocks = 0;
            _IsOperationFinished = false;


            byte[] bytesToSend = StringToByteArray(strDataToSend);  // string to bytes conversion

            Array.Clear(_CommandBytes, 0, _CommandBytes.Length);      // zeroing command array
            _CommandBytes[0] = CMD_READ_SD;                          // setting command byte (first byte)
            bytesToSend.CopyTo(_CommandBytes, 1);                    // setting arguments bytes from the function argument
            //System.Diagnostics.Debug.WriteLine(BitConverter.ToString(_CommandBytes));


            // number of blocks to read from SD ------------------------
            var newArr = _CommandBytes.Skip(5).Take(4).ToArray();

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(newArr);
            }

            _NumberOfBlocksToRead = BitConverter.ToUInt32(newArr, 0);
            System.Diagnostics.Debug.WriteLine("\nNumber of blocks to read: " + _NumberOfBlocksToRead.ToString());

            // ---------------------------------------------------------

            FtdiFifo.SendDataIntoFifo2(_CommandBytes);              // sending data through FTDI

            _progressInfo = 0;
        }

        public void WriteEnableEeprom()
        {
            _OperationMode = MODE_EEPROM_WRITE_ENABLE;

            Array.Clear(_CommandBytes, 0, _CommandBytes.Length);      // zeroing command array
            _CommandBytes[0] = CMD_EEPROM;                            // setting command byte (first byte)
            _CommandBytes[1] = 0x03;                                  // setting command byte (first byte)


            //byte[] bytesToSend = StringToByteArray(strDataToSend);  // string to bytes conversion
            //System.Diagnostics.Debug.WriteLine(bytesToSend.Length.ToString());
            //System.Diagnostics.Debug.WriteLine(bytesToSend[0]);
            //System.Diagnostics.Debug.WriteLine(bytesToSend[1]);
            //System.Diagnostics.Debug.WriteLine(bytesToSend[2]);
            //System.Diagnostics.Debug.WriteLine(bytesToSend[3]);
            //System.Diagnostics.Debug.WriteLine(bytesToSend[4]);
            //System.Diagnostics.Debug.WriteLine(bytesToSend[5]);
            //_CommandBytes[6] = bytesToSend[0];
            //_CommandBytes[7] = bytesToSend[1];
            //_CommandBytes[8] = bytesToSend[2];
            //_CommandBytes[9] = bytesToSend[3];

            //System.Diagnostics.Debug.WriteLine(_CommandBytes);

            FtdiFifo.SendDataIntoFifo2(_CommandBytes);              // sending data through FTDI
        }

        public void DdcWrite()
        {
            _OperationMode = MODE_DDC_RESET;

            Array.Clear(_CommandBytes, 0, _CommandBytes.Length);      // zeroing command array
            _CommandBytes[0] = CMD_DDC_REGISTER;                      // setting command byte (first byte)
            _CommandBytes[1] = 0x06;                                  // setting command byte (first byte)

            _CommandBytes[7] = 0x00;
            _CommandBytes[8] = 0x00;
            _CommandBytes[9] = 0x00;

            FtdiFifo.SendDataIntoFifo2(_CommandBytes);              // sending data through FTDI
        }

        public void DdcReadout()
        {
            _OperationMode = MODE_DDC_RESET;

            Array.Clear(_CommandBytes, 0, _CommandBytes.Length);      // zeroing command array
            _CommandBytes[0] = CMD_DDC_REGISTER;                      // setting command byte (first byte)
            _CommandBytes[1] = 0x06;                                  // setting command byte (first byte)

            _CommandBytes[7] = 0x00;
            _CommandBytes[8] = 0x00;
            _CommandBytes[9] = 0x02;

            FtdiFifo.SendDataIntoFifo2(_CommandBytes);              // sending data through FTDI
        }

        public void DdcSdout()
        {
            _OperationMode = MODE_DDC_RESET;

            Array.Clear(_CommandBytes, 0, _CommandBytes.Length);      // zeroing command array
            _CommandBytes[0] = CMD_DDC_REGISTER;                      // setting command byte (first byte)
            _CommandBytes[1] = 0x06;                                  // setting command byte (first byte)

            _CommandBytes[7] = 0x6B;
            _CommandBytes[8] = 0x00;
            _CommandBytes[9] = 0x01;

            FtdiFifo.SendDataIntoFifo2(_CommandBytes);              // sending data through FTDI
        }

        public void DdcClkDiv2()
        {
            _OperationMode = MODE_DDC_RESET;

            Array.Clear(_CommandBytes, 0, _CommandBytes.Length);      // zeroing command array
            _CommandBytes[0] = CMD_DDC_REGISTER;                      // setting command byte (first byte)
            _CommandBytes[1] = 0x06;                                  // setting command byte (first byte)

            _CommandBytes[7] = 0x11;
            _CommandBytes[8] = 0x00;
            _CommandBytes[9] = 0x08;

            FtdiFifo.SendDataIntoFifo2(_CommandBytes);              // sending data through FTDI
        }

        public void DdcClkSpeed()
        {
            _OperationMode = MODE_DDC_RESET;

            Array.Clear(_CommandBytes, 0, _CommandBytes.Length);      // zeroing command array
            _CommandBytes[0] = CMD_DDC_REGISTER;                      // setting command byte (first byte)
            _CommandBytes[1] = 0x06;                                  // setting command byte (first byte)

            _CommandBytes[7] = 0x0F;
            _CommandBytes[8] = 0x00;
            _CommandBytes[9] = 0x10;

            FtdiFifo.SendDataIntoFifo2(_CommandBytes);              // sending data through FTDI
        }

        public void DdcEnableConv()
        {
            _OperationMode = MODE_DDC_DATA_SAVE_TO_FILE;

            //String FileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + "_ddc_readout.bin";
            String FileName = "ddc_readout.bin";
            _FileStream = new FileStream(FileName, FileMode.Create, FileAccess.Write, FileShare.Write);
            _CurrentDataLength = 0;
            Array.Clear(_ReceivedDataBuffer, 0, _ReceivedDataBuffer.Length);      // zeroing array
            _ReceivedDataBufferPosition = 0;
            _TotalNumberOfBytes = 0;
            _IsOperationFinished = false;


            Array.Clear(_CommandBytes, 0, _CommandBytes.Length);      // zeroing command array
            _CommandBytes[0] = CMD_DDC_REGISTER;                      // setting command byte (first byte)
            _CommandBytes[1] = 0x07;                                  // setting command byte (first byte)

            _CommandBytes[7] = 0x00;
            _CommandBytes[8] = 0x00;
            _CommandBytes[9] = 0x01;

            FtdiFifo.SendDataIntoFifo2(_CommandBytes);              // sending data through FTDI
        }

        public void ReadEepromToFifo()
        {
            _OperationMode = MODE_EEPROM_READ;

            Array.Clear(_CommandBytes, 0, _CommandBytes.Length);      // zeroing command array
            _CommandBytes[0] = CMD_EEPROM;                            // setting command byte (first byte)
            _CommandBytes[1] = 0x01;                                  // setting command byte (first byte)

            byte EepromAddress = 0x00;

            for (UInt32 i = 0; i < 23; i++)
            {

                //int milliseconds = 500;
                //Thread.Sleep(milliseconds);

                _CommandBytes[9] = EepromAddress;

                System.Diagnostics.Debug.WriteLine(_CommandBytes);

                FtdiFifo.SendDataIntoFifo2(_CommandBytes);              // sending data through FTDI

                EepromAddress += 0x01;
            }
        }

        //public void ReadEeprom(string strDataToSend)
        //{
        //    _OperationMode = MODE_EEPROM_READ;

        //    Array.Clear(_CommandBytes, 0, _CommandBytes.Length);      // zeroing command array
        //    _CommandBytes[0] = CMD_EEPROM;                            // setting command byte (first byte)
        //    _CommandBytes[1] = 0x01;                                  // setting command byte (first byte)


        //    byte[] bytesToSend = StringToByteArray(strDataToSend);  // string to bytes conversion
        //    //System.Diagnostics.Debug.WriteLine(bytesToSend.Length.ToString());
        //    //System.Diagnostics.Debug.WriteLine(bytesToSend[0]);
        //    //System.Diagnostics.Debug.WriteLine(bytesToSend[1]);
        //    _CommandBytes[8] = bytesToSend[0];
        //    _CommandBytes[9] = bytesToSend[1];

        //    System.Diagnostics.Debug.WriteLine(_CommandBytes);

        //    FtdiFifo.SendDataIntoFifo2(_CommandBytes);              // sending data through FTDI
        //}

        public void ReadEepromUSB(string strDataToSend)
        {
            _OperationMode = MODE_EEPROM_READ;

            Array.Clear(_CommandBytes, 0, _CommandBytes.Length);      // zeroing command array
            _CommandBytes[0] = CMD_EEPROM;                            // setting command byte (first byte)
            _CommandBytes[1] = 0x04;                                  // setting command byte (first byte)


            byte[] bytesToSend = StringToByteArray(strDataToSend);  // string to bytes conversion
            //System.Diagnostics.Debug.WriteLine(bytesToSend.Length.ToString());
            //System.Diagnostics.Debug.WriteLine(bytesToSend[0]);
            //System.Diagnostics.Debug.WriteLine(bytesToSend[1]);
            _CommandBytes[8] = bytesToSend[0];
            _CommandBytes[9] = bytesToSend[1];

            System.Diagnostics.Debug.WriteLine(_CommandBytes);

            FtdiFifo.SendDataIntoFifo2(_CommandBytes);              // sending data through FTDI
        }

        public void ReadEeprom(string strDataToSend)
        {
            _OperationMode = MODE_EEPROM_READ;

            Array.Clear(_CommandBytes, 0, _CommandBytes.Length);      // zeroing command array
            _CommandBytes[0] = CMD_EEPROM;                            // setting command byte (first byte)
            _CommandBytes[1] = 0x04;                                  // setting command byte (first byte)


            byte[] bytesToSend = StringToByteArray(strDataToSend);  // string to bytes conversion
            _CommandBytes[8] = bytesToSend[0];
            _CommandBytes[9] = bytesToSend[1];

            System.Diagnostics.Debug.WriteLine(_CommandBytes);

            FtdiFifo.SendDataIntoFifo2(_CommandBytes);              // sending data through FTDI
        }

        public void WriteEeprom(string strDataToSend)
        {
            _OperationMode = MODE_EEPROM_WRITE;

            Array.Clear(_CommandBytes, 0, _CommandBytes.Length);      // zeroing command array
            _CommandBytes[0] = CMD_EEPROM;                            // setting command byte (first byte)
            _CommandBytes[1] = 0x02;                                  // setting command byte (first byte)


            byte[] bytesToSend = StringToByteArray(strDataToSend);  // string to bytes conversion
            //System.Diagnostics.Debug.WriteLine(bytesToSend.Length.ToString());
            //System.Diagnostics.Debug.WriteLine(bytesToSend[0]);
            //System.Diagnostics.Debug.WriteLine(bytesToSend[1]);
            //System.Diagnostics.Debug.WriteLine(bytesToSend[2]);
            //System.Diagnostics.Debug.WriteLine(bytesToSend[3]);
            //System.Diagnostics.Debug.WriteLine(bytesToSend[4]);
            //System.Diagnostics.Debug.WriteLine(bytesToSend[5]);
            _CommandBytes[6] = bytesToSend[0];
            _CommandBytes[7] = bytesToSend[1];
            _CommandBytes[8] = bytesToSend[2];
            _CommandBytes[9] = bytesToSend[3];

            //System.Diagnostics.Debug.WriteLine(_CommandBytes);

            FtdiFifo.SendDataIntoFifo2(_CommandBytes);              // sending data through FTDI
        }

        public void WriteEeprom(byte[] DataToSend)
        {
            _OperationMode = MODE_EEPROM_WRITE;

            Array.Clear(_CommandBytes, 0, _CommandBytes.Length);      // zeroing command array
            _CommandBytes[0] = CMD_EEPROM;                            // setting command byte (first byte)
            _CommandBytes[1] = 0x02;                                  // setting command byte (first byte)


            byte[] bytesToSend = DataToSend;
            System.Diagnostics.Debug.WriteLine(bytesToSend.Length.ToString());
            System.Diagnostics.Debug.WriteLine(bytesToSend[0]);
            System.Diagnostics.Debug.WriteLine(bytesToSend[1]);
            System.Diagnostics.Debug.WriteLine(bytesToSend[2]);

            _CommandBytes[7] = bytesToSend[2];
            _CommandBytes[8] = bytesToSend[1];
            _CommandBytes[9] = bytesToSend[0];

            //System.Diagnostics.Debug.WriteLine(_CommandBytes);

            FtdiFifo.SendDataIntoFifo2(_CommandBytes);              // sending data through FTDI
        }

        // -----------------------------------------------------------------
        // DDC RESET ON
        // -----------------------------------------------------------------
        public void DdcResetCommand()
        {
            _OperationMode = MODE_DDC_RESET;

            Array.Clear(_CommandBytes, 0, _CommandBytes.Length);      // zeroing command array
            _CommandBytes[0] = CMD_DDC_REGISTER;                      // setting command byte (first byte)
            _CommandBytes[1] = 0x03;                                  // setting command byte (first byte)

            FtdiFifo.SendDataIntoFifo2(_CommandBytes);              // sending data through FTDI
        }

        // -----------------------------------------------------------------
        // DDC RESET OFF
        // -----------------------------------------------------------------
        public void DdcEnableCommand()
        {
            _OperationMode = MODE_DDC_ENABLE;

            Array.Clear(_CommandBytes, 0, _CommandBytes.Length);      // zeroing command array
            _CommandBytes[0] = CMD_DDC_REGISTER;                      // setting command byte (first byte)
            _CommandBytes[1] = 0x04;                                  // setting command byte (first byte)

            FtdiFifo.SendDataIntoFifo2(_CommandBytes);              // sending data through FTDI
        }




        // -----------------------------------------------------------------
        // DDC REGISTER READ
        // -----------------------------------------------------------------
        public void DdcRegisterCommand(string strDataToSend)
        {
            _OperationMode = MODE_DDC_READ_REGISTER;

            byte[] bytesToSend = StringToByteArray(strDataToSend);  // string to bytes conversion

            Array.Clear(_CommandBytes, 0, _CommandBytes.Length);      // zeroing command array
            _CommandBytes[0] = CMD_DDC_REGISTER;                      // setting command byte (first byte)
            _CommandBytes[1] = 0x04; // READ
            //System.Diagnostics.Debug.WriteLine(bytesToSend.Length.ToString());
            //bytesToSend.CopyTo(_CommandBytes, 7);                     // setting arguments bytes from the function argument
            _CommandBytes[9] = bytesToSend[0];                          // it's just one byte - address - to set in the command
            //System.Diagnostics.Debug.WriteLine(BitConverter.ToString(_CommandBytes));

            FtdiFifo.SendDataIntoFifo2(_CommandBytes);              // sending data through FTDI

        }

        // -----------------------------------------------------------------
        // DDC REGISTER WRITE
        // -----------------------------------------------------------------
        public void DdcWriteRegisterCommand(string strDataToSend)
        {
            _OperationMode = MODE_DDC_READ_REGISTER;

            byte[] bytesToSend = StringToByteArray(strDataToSend);  // string to bytes conversion

            Array.Clear(_CommandBytes, 0, _CommandBytes.Length);      // zeroing command array
            _CommandBytes[0] = CMD_DDC_REGISTER;                      // setting command byte (first byte)
            _CommandBytes[1] = 0x06; // WRITE
            //System.Diagnostics.Debug.WriteLine(bytesToSend.Length.ToString());
            // bytesToSend.CopyTo(_CommandBytes, 7);                     // setting arguments bytes from the function argument
            //System.Diagnostics.Debug.WriteLine(BitConverter.ToString(_CommandBytes));

            // FtdiFifo.SendDataIntoFifo2(_CommandBytes);              // sending data through FTDI
            FtdiFifo.SendDataIntoFifo2(bytesToSend);              // sending data through FTDI

        }

        // -----------------------------------------------------------------
        // DDC LV ON
        // -----------------------------------------------------------------
        public void LvOnCommand()
        {
            _OperationMode = MODE_LV_ENABLE;

            Array.Clear(_CommandBytes, 0, _CommandBytes.Length);      // zeroing command array
            _CommandBytes[0] = CMD_LV_ENABLE;                      // setting command byte (first byte)
            _CommandBytes[9] = 0x0F; // 0000 | LV_BP-2.0V_EN | LV_BP+2.7V_EN | LV_BP+2.0V_EN | LV_AUX+3.3V_EN
            //System.Diagnostics.Debug.WriteLine(BitConverter.ToString(_CommandBytes));

            FtdiFifo.SendDataIntoFifo2(_CommandBytes);              // sending data through FTDI

        }

        // -----------------------------------------------------------------
        // DDC LV OFF
        // -----------------------------------------------------------------
        public void LvOffCommand()
        {
            _OperationMode = MODE_LV_ENABLE;

            Array.Clear(_CommandBytes, 0, _CommandBytes.Length);      // zeroing command array
            _CommandBytes[0] = CMD_LV_ENABLE;                      // setting command byte (first byte)
            _CommandBytes[9] = 0x00; // 0000 | LV_BP-2.0V_EN | LV_BP+2.7V_EN | LV_BP+2.0V_EN | LV_AUX+3.3V_EN
            //System.Diagnostics.Debug.WriteLine(BitConverter.ToString(_CommandBytes));

            FtdiFifo.SendDataIntoFifo2(_CommandBytes);              // sending data through FTDI

            //_progressInfo = 0;
        }

        // -----------------------------------------------------------------
        // Program DDC registers with data stored in Eeprom Fifo
        // -----------------------------------------------------------------
        public void ProgramDdc()
        {
            _OperationMode = MODE_DDC_PROGRAM;

            Array.Clear(_CommandBytes, 0, _CommandBytes.Length);      // zeroing command array
            _CommandBytes[0] = CMD_DDC_REGISTER;                      // setting command byte (first byte)
            _CommandBytes[1] = 0x03;

            System.Diagnostics.Debug.WriteLine(_CommandBytes);

            FtdiFifo.SendDataIntoFifo2(_CommandBytes);              // sending data through FTDI
        }

        // -----------------------------------------------------------------
        // Program DDC registers with data stored in Eeprom Fifo
        // -----------------------------------------------------------------
        public void DdcResetHw()
        {
            _OperationMode = MODE_DDC_PROGRAM;

            Array.Clear(_CommandBytes, 0, _CommandBytes.Length);      // zeroing command array
            _CommandBytes[0] = CMD_DDC_REGISTER;                      // setting command byte (first byte)
            _CommandBytes[1] = 0x05;

            System.Diagnostics.Debug.WriteLine(_CommandBytes);

            FtdiFifo.SendDataIntoFifo2(_CommandBytes);              // sending data through FTDI
        }

        // -----------------------------------------------------------------
        // Helper function: converts HEX formatted string to byte array
        // -----------------------------------------------------------------
        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        public string ReadDataFromFifo()
        {

            return FtdiFifo.ReadDataFromFifo();

        }

        // -----------------------------------------------------------------
        // Handler: USB Data Reception Event Handler
        // -----------------------------------------------------------------
        void FtdiBytesReceivedHandler(object sender, FtdiBytesReceivedEventArgs e)
        {                                            
            //System.Diagnostics.Debug.WriteLine("\nNumber of bytes received: " + e.NumBytesAvailable.ToString());
            //System.Diagnostics.Debug.WriteLine("\nReceived bytes: " + BitConverter.ToString(e.Bytes));

            _HugeAllBytesCounter += e.NumBytesAvailable;
            //System.Diagnostics.Debug.WriteLine("\nReceived ALL bytes: " + _HugeAllBytesCounter.ToString());


            switch (_OperationMode)
            {
                case MODE_SAVE_SD_DATA_TO_FILE:

                    //_progressInfo += 1;
                    //System.Diagnostics.Debug.WriteLine("\n\n Progres: " + _progressInfo.ToString() + "\n\n");

                    // copying received to local _ReceivedDataBuffer buffer
                    // _ReceivedDataBufferPosition indicates the first available position in the buffer
                    Array.Copy(e.Bytes, 0, _ReceivedDataBuffer, _ReceivedDataBufferPosition, e.NumBytesAvailable);
                    _ReceivedDataBufferPosition += e.NumBytesAvailable;

                    // read from the MSB the number of blocks sent in the current chunk
                    _NumberOfBlocksInChunk = (uint)(_ReceivedDataBuffer[0] & 0b_0111_1111);

                    // read the MSBit of the MSByte (control byte); if = 1 --> last data chunk in the read process
                    _IsOperationFinished = false;
                    if ((_ReceivedDataBuffer[0] & LAST_DATA_CHUNK) == LAST_DATA_CHUNK)
                        _IsOperationFinished = true;

                    // Not the last one and received part of this and the next one chunk, so the _ReceivedDataBufferPosition has higher value than the max chunk size
                    while ((_IsOperationFinished == false) && (_ReceivedDataBufferPosition > ((63 * 512) + 1)))
                    {
                        //System.Diagnostics.Debug.WriteLine("\nWhile loop");

                        _TotalNumberOfBlocks += _NumberOfBlocksInChunk;

                        _FileStream.Write(_ReceivedDataBuffer, 1, (int)_NumberOfBlocksInChunk * 512);
                        _FileStream.Flush();

                        Array.Copy(_ReceivedDataBuffer, (_NumberOfBlocksInChunk * 512) + 1, _ReceivedDataBuffer, 0, _ReceivedDataBufferPosition - (_NumberOfBlocksInChunk * 512 + 1));
                        _ReceivedDataBufferPosition -= (_NumberOfBlocksInChunk * 512 + 1);

                        // The MSB is different now so...

                        // read from the MSB the number of blocks sent in the current chunk
                        _NumberOfBlocksInChunk = (uint)(_ReceivedDataBuffer[0] & 0b_0111_1111);

                        // read the MSBit of the MSByte (control byte); if = 1 --> last data chunk in the read process
                        _IsOperationFinished = false;
                        if ((_ReceivedDataBuffer[0] & LAST_DATA_CHUNK) == LAST_DATA_CHUNK)
                            _IsOperationFinished = true;


                        //System.Diagnostics.Debug.WriteLine("\n_ReceivedDataBufferPosition: " + _ReceivedDataBufferPosition.ToString());
                        //System.Diagnostics.Debug.WriteLine("\n_ReceivedDataBuffer[0]: " + _ReceivedDataBuffer[0].ToString());
                        //System.Diagnostics.Debug.WriteLine("\n_IsOperationFinished: " + _IsOperationFinished.ToString());
                        //System.Diagnostics.Debug.WriteLine("\n_NumberOfBlocksInChunk: " + _NumberOfBlocksInChunk.ToString());
                        //System.Diagnostics.Debug.WriteLine("\n_TotalNumberOfBlocks: " + _TotalNumberOfBlocks.ToString());
                        //System.Diagnostics.Debug.WriteLine("\n_OperationMode: " + _OperationMode.ToString());



                    }

                    // if it is last chunk AND all needed bytes are received
                    if ((_IsOperationFinished == true) && (_ReceivedDataBufferPosition == ((_NumberOfBlocksInChunk * 512) + 1)))
                    {
                        //System.Diagnostics.Debug.WriteLine("\nLast IF");
                        _TotalNumberOfBlocks += _NumberOfBlocksInChunk;

                        _FileStream.Write(_ReceivedDataBuffer, 1, (int)_NumberOfBlocksInChunk * 512);
                        _FileStream.Flush();
                        _FileStream.Close();

                        _ReceivedDataBufferPosition = 0;
                        _OperationMode = 0;

                        System.Diagnostics.Debug.WriteLine("\nReceived ALL bytes: " + _HugeAllBytesCounter.ToString());


                        //System.Diagnostics.Debug.WriteLine("\n\n Finished data download" + "\n\n");

                        //System.Diagnostics.Debug.WriteLine("\n_ReceivedDataBufferPosition: " + _ReceivedDataBufferPosition.ToString());
                        //System.Diagnostics.Debug.WriteLine("\n_ReceivedDataBuffer[0]: " + _ReceivedDataBuffer[0].ToString());
                        //System.Diagnostics.Debug.WriteLine("\n_IsOperationFinished: " + _IsOperationFinished.ToString());
                        //System.Diagnostics.Debug.WriteLine("\n_NumberOfBlocksInChunk: " + _NumberOfBlocksInChunk.ToString());
                        //System.Diagnostics.Debug.WriteLine("\n_TotalNumberOfBlocks: " + _TotalNumberOfBlocks.ToString());
                        //System.Diagnostics.Debug.WriteLine("\n_OperationMode: " + _OperationMode.ToString());

                    }

                    // Not enough data in the last chunk.
                    //if ((_IsOperationFinished == true) && (_ReceivedDataBufferPosition < ((_NumberOfBlocksInChunk * 512) + 1)))
                    //{
                    //System.Diagnostics.Debug.WriteLine("\nNot enough data in the last chunk. Waiting for more.\n");

                    //_AvailableBlocks = (_ReceivedDataBufferPosition - 1) / 512;

                    //if (_AvailableBlocks > 0)
                    //{
                    //    _TotalNumberOfBlocks += _AvailableBlocks;

                    //    _FileStream.Write(_ReceivedDataBuffer, 1, (int)_AvailableBlocks * 512);
                    //    _FileStream.Flush();
                    //}

                    //_BytesLeftInTheBuffer = _ReceivedDataBufferPosition;
                    //_ReceivedDataBufferPosition -= (_AvailableBlocks * 512 + 1);

                    //    System.Diagnostics.Debug.WriteLine("\n_ReceivedDataBufferPosition: " + _ReceivedDataBufferPosition.ToString());
                    //    System.Diagnostics.Debug.WriteLine("\n_ReceivedDataBuffer[0]: " + _ReceivedDataBuffer[0].ToString());
                    //    System.Diagnostics.Debug.WriteLine("\n_IsOperationFinished: " + _IsOperationFinished.ToString());
                    //    System.Diagnostics.Debug.WriteLine("\n_NumberOfBlocksInChunk: " + _NumberOfBlocksInChunk.ToString());
                    //    System.Diagnostics.Debug.WriteLine("\n_TotalNumberOfBlocks: " + _TotalNumberOfBlocks.ToString());
                    //    System.Diagnostics.Debug.WriteLine("\n_OperationMode: " + _OperationMode.ToString());

                    //}
                    // TTTTTTTTTTT
                    // Not the last chunk and all the data received
                    //if ((_IsOperationFinished == false) && (_ReceivedDataBufferPosition == ((63 * 512) + 1)))
                    //{
                    //    //System.Diagnostics.Debug.WriteLine("\nEqual IF");
                    //    _TotalNumberOfBlocks += _NumberOfBlocksInChunk;

                    //    _FileStream.Write(_ReceivedDataBuffer, 1, (int)_NumberOfBlocksInChunk * 512);
                    //    _FileStream.Flush();

                    //    System.Diagnostics.Debug.WriteLine("\n_ReceivedDataBufferPosition: " + _ReceivedDataBufferPosition.ToString());
                    //    System.Diagnostics.Debug.WriteLine("\n_ReceivedDataBuffer[0]: " + _ReceivedDataBuffer[0].ToString());
                    //    System.Diagnostics.Debug.WriteLine("\n_IsOperationFinished: " + _IsOperationFinished.ToString());
                    //    System.Diagnostics.Debug.WriteLine("\n_NumberOfBlocksInChunk: " + _NumberOfBlocksInChunk.ToString());
                    //    System.Diagnostics.Debug.WriteLine("\n_TotalNumberOfBlocks: " + _TotalNumberOfBlocks.ToString());
                    //    System.Diagnostics.Debug.WriteLine("\n_OperationMode: " + _OperationMode.ToString());

                    //}
                    // TTTTTTTTTTTT

                    // Not the last chunk and still missing some data... waiting for more
                    //if ((_IsOperationFinished == false) && (_ReceivedDataBufferPosition < ((63 * 512) + 1)))
                    //{
                    //    System.Diagnostics.Debug.WriteLine("\nNot enough data in the current chunk. Waiting for more.\n");
                        
                    //    System.Diagnostics.Debug.WriteLine("\n_ReceivedDataBufferPosition: " + _ReceivedDataBufferPosition.ToString());
                    //    System.Diagnostics.Debug.WriteLine("\n_ReceivedDataBuffer[0]: " + _ReceivedDataBuffer[0].ToString());
                    //    System.Diagnostics.Debug.WriteLine("\n_IsOperationFinished: " + _IsOperationFinished.ToString());
                    //    System.Diagnostics.Debug.WriteLine("\n_NumberOfBlocksInChunk: " + _NumberOfBlocksInChunk.ToString());
                    //    System.Diagnostics.Debug.WriteLine("\n_TotalNumberOfBlocks: " + _TotalNumberOfBlocks.ToString());
                    //    System.Diagnostics.Debug.WriteLine("\n_OperationMode: " + _OperationMode.ToString());

                    //}

                    //_progressBarValue = ((double)_TotalNumberOfBlocks / (double)_NumberOfBlocksToRead);
                    //System.Diagnostics.Debug.WriteLine("\n>>>>>>>>> PROGRESS: " + _progressBarValue.ToString());


                    break;
                case MODE_SAVE_SD_DATA_TO_FILE_2:

                    //_progressInfo += 1;
                    //System.Diagnostics.Debug.WriteLine("\n\n Progres: " + _progressInfo.ToString() + "\n\n");

                    //// copying received to local _ReceivedDataBuffer buffer
                    //// _ReceivedDataBufferPosition indicates the next available position in the buffer
                    //Array.Copy(e.Bytes, 0, _ReceivedDataBuffer, _ReceivedDataBufferPosition, e.NumBytesAvailable);
                    //_ReceivedDataBufferPosition += e.NumBytesAvailable;

                    //// read from the MSB the number of blocks sent in the current chunk
                    //_NumberOfBlocksInChunk = (uint)(_ReceivedDataBuffer[0] & 0b_0111_1111);

                    //// read the MSBit of the MSByte (control byte); if = 1 --> last data chunk in the read process
                    //_IsOperationFinished = false;
                    //if ((_ReceivedDataBuffer[0] & LAST_DATA_CHUNK) == LAST_DATA_CHUNK)
                    //    _IsOperationFinished = true;

                    //// Not the last one and received part of this and the next one chunk, so the _ReceivedDataBufferPosition has higher value than the max chunk size
                    //if ((_IsOperationFinished == false) && (_ReceivedDataBufferPosition > ((63 * 512) + 1)))
                    //{

                    //}

                    //do
                    //{
                    //    _AvailableBlocks = (_ReceivedDataBufferPosition - 1) / 512;

                    //    // Not the last chunk and still missing some data... waiting for more
                    //    if ((_IsOperationFinished == false) && (_ReceivedDataBufferPosition < ((63 * 512) + 1)))
                    //    {
                    //        System.Diagnostics.Debug.WriteLine("\nNot enough data in the current chunk. Waiting for more.\n");
                    //    }

                    //} while (_IsOperationFinished = false || _ReceivedDataBufferPosition != 0);

                    break;

                case MODE_DDC_READ_REGISTER:
                    
                    Array.Copy(e.Bytes, 0, _ReceivedDataBuffer, _ReceivedDataBufferPosition, e.NumBytesAvailable);

                    System.Diagnostics.Debug.WriteLine("--------------------------------------------------");
                    System.Diagnostics.Debug.WriteLine("Received " + e.NumBytesAvailable.ToString() + " bytes:");
                    var newArr2 = _ReceivedDataBuffer.Take((int)e.NumBytesAvailable).ToArray();
                    System.Diagnostics.Debug.WriteLine(BitConverter.ToString(newArr2));
                    System.Diagnostics.Debug.WriteLine("--------------------------------------------------");
                    System.Diagnostics.Debug.WriteLine("");


                    OnLogEntry?.Invoke(this, new LogEntryEventArgs("RESP | " + ParseResponse(_ReceivedDataBuffer[0]) + " | " + e.NumBytesAvailable.ToString().PadRight(3, ' ') + " | " + BitConverter.ToString(newArr2)));



                    break;

                case MODE_EEPROM_READ:

                    Array.Copy(e.Bytes, 0, _ReceivedDataBuffer, _ReceivedDataBufferPosition, e.NumBytesAvailable);

                    _EepromData = _ReceivedDataBuffer.Take(2).ToArray();
                    _EepromNewData = true;

                    System.Diagnostics.Debug.WriteLine("--------------------------------------------------");
                    System.Diagnostics.Debug.WriteLine("Received " + e.NumBytesAvailable.ToString() + " bytes:");
                    var newArr3 = _ReceivedDataBuffer.Take((int)e.NumBytesAvailable).ToArray();
                    System.Diagnostics.Debug.WriteLine(BitConverter.ToString(newArr3));
                    System.Diagnostics.Debug.WriteLine("--------------------------------------------------");
                    System.Diagnostics.Debug.WriteLine("");


                    OnLogEntry?.Invoke(this, new LogEntryEventArgs("RESP | " + ParseResponse(_ReceivedDataBuffer[0]) + " | " + e.NumBytesAvailable.ToString().PadRight(3, ' ') + " | " + BitConverter.ToString(newArr3)));



                    break;

                case MODE_DDC_DATA_SAVE_TO_FILE:

                    Array.Copy(e.Bytes, 0, _ReceivedDataBuffer, _ReceivedDataBufferPosition, e.NumBytesAvailable);
                    _ReceivedDataBufferPosition += e.NumBytesAvailable;

                    _IsOperationFinished = false;

                    if (_ReceivedDataBufferPosition == 10)
                    {
                        _IsOperationFinished = true;
                    }

                    //while (_IsOperationFinished == false)
                    //{
                    //    _FileStream.Write(_ReceivedDataBuffer, 0, (int)e.NumBytesAvailable);
                    //    _FileStream.Flush();
                    //}

                    if(_IsOperationFinished == true)
                    {
                        _FileStream.Write(_ReceivedDataBuffer, 0, (int)e.NumBytesAvailable);
                        _FileStream.Flush();
                        _FileStream.Close();

                        _ReceivedDataBufferPosition = 0;
                        _OperationMode = 0;

                        System.Diagnostics.Debug.WriteLine("\nReceived ALL bytes: " + _HugeAllBytesCounter.ToString());
                    }


                    System.Diagnostics.Debug.WriteLine("\n_ReceivedDataBufferPosition: " + _ReceivedDataBufferPosition.ToString());
                    System.Diagnostics.Debug.WriteLine("\n_ReceivedDataBuffer[0]: " + _ReceivedDataBuffer[0].ToString());
                    System.Diagnostics.Debug.WriteLine("\n_IsOperationFinished: " + _IsOperationFinished.ToString());
                    System.Diagnostics.Debug.WriteLine("\n_OperationMode: " + _OperationMode.ToString());


                    System.Diagnostics.Debug.WriteLine("--------------------------------------------------");
                    System.Diagnostics.Debug.WriteLine("Received " + e.NumBytesAvailable.ToString() + " bytes:");
                    var newArr4 = _ReceivedDataBuffer.Take((int)e.NumBytesAvailable).ToArray();
                    System.Diagnostics.Debug.WriteLine(BitConverter.ToString(newArr4));
                    System.Diagnostics.Debug.WriteLine("--------------------------------------------------");
                    System.Diagnostics.Debug.WriteLine("");
                    OnLogEntry?.Invoke(this, new LogEntryEventArgs("RESP | " + ParseResponse(_ReceivedDataBuffer[0]) + " | " + e.NumBytesAvailable.ToString().PadRight(3, ' ') + " | " + BitConverter.ToString(newArr4)));


                    break;


                default:
                    System.Diagnostics.Debug.WriteLine("Unrecognized operation on received USB data");
                    break;
            }
        }

        private string ParseResponse(byte ResponseID)
        {
            String ResponseString = "";

            switch (ResponseID)
            {
                case 0x30:
                    ResponseString = "DDC REGISTER RESPONSE";
                    break;

                case 41:
                    ResponseString = "EEPROM DATA";
                    break;

                default:
                    ResponseString =  "UNDEFINED_RESPONSE";
                    break;                   
            }

            return ResponseString.PadRight(26, ' ');
        }

        // -----------------------------------------------------------------
        // Helper function: saves bytes data to file
        // -----------------------------------------------------------------
        public void SaveRxDataToFile(byte[] rxData)
        {

            String FileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_readout") + ".bin";

            const int dataLengthTotal = 1024000;
            int CurrentDataLength = 0;
            do
            {

                using (var fileStream = new FileStream(FileName, FileMode.Append, FileAccess.Write, FileShare.Write))
                {
                    fileStream.Write(rxData, CurrentDataLength, rxData.Length);
                }
                CurrentDataLength += rxData.Length;

            } while (CurrentDataLength < dataLengthTotal);
        }

        // -----------------------------------------------------------------
        // Write SD card command sending function
        // -----------------------------------------------------------------
        public void WriteSD(string text)
        {
            //throw new NotImplementedException();
        }

    }
}
