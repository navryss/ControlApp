using System;
using System.Collections.Generic;
using System.Text;

namespace Engine
{
    public static class CommonData
    {

        public static Dictionary<byte, ushort> RegistersDictonary = new Dictionary<byte, ushort>();

        static CommonData()
        {
            PopulateRegistersDictionary();
        }
       
        private static void PopulateRegistersDictionary()
        {
            RegistersDictonary.Add(0x00, 0x0000);
            RegistersDictonary.Add(0x02, 0x0000);
            RegistersDictonary.Add(0x06, 0x0000);
            RegistersDictonary.Add(0x0F, 0x0000);
            RegistersDictonary.Add(0x10, 0x0000);
            RegistersDictonary.Add(0x11, 0x0000);
            RegistersDictonary.Add(0x12, 0x0000);
            RegistersDictonary.Add(0x13, 0x0000);
            RegistersDictonary.Add(0x14, 0x0000);
            RegistersDictonary.Add(0x15, 0x1234);
            RegistersDictonary.Add(0x16, 0x5678);
            RegistersDictonary.Add(0x17, 0x9ABC);
            RegistersDictonary.Add(0x18, 0x00FE);
            RegistersDictonary.Add(0x19, 0xDCBA);
            RegistersDictonary.Add(0x1A, 0x0000);
            RegistersDictonary.Add(0x1B, 0x0000);
            RegistersDictonary.Add(0x25, 0x0000);
            RegistersDictonary.Add(0x31, 0x0000);
            RegistersDictonary.Add(0x39, 0x0000);
            RegistersDictonary.Add(0x3A, 0x0000);
            RegistersDictonary.Add(0x5C, 0x0000);
            RegistersDictonary.Add(0x6B, 0x0000);
            RegistersDictonary.Add(0x6C, 0x0000);

        }



    }
}
