using System;
using System.Collections.Generic;
using TwinCAT.Ads;

using static System.Console;

namespace MdpDiagnose
{
    class Program
    {
        private static AmsNetId _netId = new AmsNetId("172.16.17.20.1.1");
        private static AdsClient _AdsClient = new AdsClient();
        private static Dictionary<ushort, uint> _mdpModules = new Dictionary<ushort, uint>();
        private static ushort _mdpModuleCount ,_mdpType, _mdpId;
        private static uint _mdpModule;
      
        static void Main(string[] args)
        {

            _AdsClient.Connect(_netId, 10000);

            WriteLine($"Connected {_AdsClient.IsConnected}");
            if (_AdsClient.IsConnected)
            {
                // Reads the numbers of modules
                _mdpModuleCount = (ushort)_AdsClient.ReadAny(0xF302,     // CoE = CAN over EtherCAT (profil definition)
                                                        0xF0200000,      // index to get modul ID List - Flag and Subindex 0
                                                        typeof(ushort)   // first short in list is the count of items 
                                                        );
                WriteLine($"Modules Found: {_mdpModuleCount} ");


                // Iterate through the list of modules to get the index and the type of each module
                for (int i = 0; i < _mdpModuleCount + 1; i++)
                {
                    try
                    {
                        // Composition of the MDPModule number and read the numbers of modules
                        _mdpModule = (uint)_AdsClient.ReadAny(0xF302, (uint)(0xF0200000 + i), typeof(uint)); // get modul ID List at subindex i
                        WriteLine($"Module Id: {_mdpModule}");

                        // Composition of the Type and ID
                        // make &-Operation with 0xFFFF0000 and shift 16 bit to get the type from the high word                      
                        _mdpType = (ushort)((_mdpModule & 0xFFFF0000) >> 16);
                        WriteLine($"Module Type: {_mdpType}");

                        // add the mdpType in Dictionarykey for checking the modules
                        if (!_mdpModules.ContainsKey(_mdpType))
                        {
                            _mdpModules.Add(_mdpType, _mdpModule);
                        }
                        
                    }
                    catch (Exception ex)
                    {
                        WriteLine($"Error: {ex.Message}");
                    }


                }


                //Key TwinCAT Data
                if (_mdpModules.ContainsKey(0x0008))
                {
                    // get TwinCAT information  
                    // make &-Operation with 0x0000FFFF  to get the id from the low word
                    _mdpId = (ushort)(_mdpModules[0x0008] & 0x0000FFFF);

                    // Shift 20 bit and make or-Operation with (0x8nn10001/0x8nn10002/0x8nn10003) to get the mdpAddr with the id at position nn
                    uint mdpTcMajor = (uint)(_mdpId << 20) | 0x80010001;        //Subindex 1 of Table 0x8nn1
                    uint mdpTcMinor = (uint)(_mdpId << 20) | 0x80010002;        //Subindex 2 of Table 0x8nn1
                    uint mpdTcBuild = (uint)(_mdpId << 20) | 0x80010003;        //Subindex 3 of Table 0x8nn1
                    uint mdpTcNetId = (uint)(_mdpId << 20) | 0x80010004;        //Subindex 4 of Table 0x8nn1
                    uint mdpTcSystemId = (uint)(_mdpId << 20) | 0x8001000B;     //Subindex 11 of Table 0x8nn1

                    // TwinCAT Major module
                    ushort dataTcMajor = (ushort)_AdsClient.ReadAny(0xF302, mdpTcMajor, typeof(ushort));
                    WriteLine($"TwinCAT Major: {dataTcMajor.ToString()}");

                    // TwinCAT Minor module
                    ushort dataTcMinor = (ushort)_AdsClient.ReadAny(0xF302, mdpTcMinor, typeof(ushort));
                    WriteLine($"TwinCAT Minor: {dataTcMinor.ToString()}");

                    // TwinCAT Build module
                    ushort dataTcBuild = (ushort)_AdsClient.ReadAny(0xF302, mpdTcBuild, typeof(ushort));
                    WriteLine($"TwinCAT Build: {dataTcBuild.ToString()}");

                    // TwinCAT NetID
                    string dataTcNetId = (string)_AdsClient.ReadAny(0xF302, mdpTcNetId, typeof(string), new int[] { 80 });
                    WriteLine($"TwinCAT NetId: {dataTcNetId.ToString()}");

                    // TwinCAT SystemId
                    string dataTcSystemId = (string)_AdsClient.ReadAny(0xF302, mdpTcSystemId, typeof(string), new int[] { 80 });
                    WriteLine($"TwinCAT SystemId: {dataTcSystemId.ToString()}");

                }
                else
                {
                    WriteLine($"No Key for TwinCAT Data.");
                }



            }


            _AdsClient.Close();
            WriteLine($"Closed");



        }
    }
}
