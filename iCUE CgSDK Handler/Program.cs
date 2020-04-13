﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Cg = CgSDK_Interop.CgSDK;

namespace iCUE_CgSDK_Handler
{
    class Program
    {
        public static bool running;

        private static string pre { get { return string.Format("{0}    Handler -- ", DateTime.Now.ToString("[HH:mm:ss]")); } }

        // The CgSDK Handler is only used a proxy to CgSDK due to SetGame limitations (can't call twice)
        // Therefore all the Handler does is run a PipeClient and relay messages
        static void Main(string[] args)
        {
            running = true;
            PipeClient.Run();
        }

        // Implement all CgSDK proxy functions
        public static int GetLastError() { return Cg.GetLastError(); }
        public static void PerformProtocolHandshake() { Cg.PerformProtocolHandshake(); }
        public static bool RequestControl() { return Cg.RequestControl(); }
        public static bool ReleaseControl() { return Cg.ReleaseControl(); }

        public static bool SetGame(string gameName)
        {
            return Cg.SetGame(gameName);
        }

        public static bool SetState(string stateName)
        {
            return Cg.SetState(stateName);
        }

        public static bool SetEvent(string eventName)
        {
            return Cg.SetEvent(eventName);
        }

        public static bool ClearState(string stateName)
        {
            return Cg.ClearState(stateName);
        }

        public static bool ClearAllStates()
        {
            return Cg.ClearAllStates();
        }

        public static bool ClearAllEvents()
        {
            return Cg.ClearAllEvents();
        }
    }
}
