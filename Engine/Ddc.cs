using System;
using System.Collections.Generic;
using System.Text;

namespace Engine
{
    public class Ddc
    {
        public byte[] RegisterValue = new byte[2];
        public ushort[] RegistersData = new ushort[23];
        public byte[] RegisterAddresses = new byte[23] { 0x00, 0x02, 0x06, 0x0F, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x25, 0x31, 0x39, 0x3A, 0x5C, 0x6B, 0x6C };


        public Ddc()
        {

            Array.Clear(RegistersData, 0, RegistersData.Length); // setting buffer to zeros

            // values of default non-zero registers
            RegistersData[9] = 0x1234;
            RegistersData[10] = 0x1234;
            

            System.Diagnostics.Debug.WriteLine("DDC SETTINGS");

            System.Diagnostics.Debug.WriteLine((CommonData.RegistersDictonary[0x15]));

        }
    }
}
