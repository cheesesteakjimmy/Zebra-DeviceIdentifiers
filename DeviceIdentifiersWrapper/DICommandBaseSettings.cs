using System;
using System.Collections.Generic;
using System.Text;

namespace DeviceIdentifiersWrapper
{
    public class DICommandBaseSettings
    {
        /*
        Use this to track the source of the intent
         */
        public string mCommandId = "";

        /*
        Some method return only errors (StartScan, StopScan)
        We do not need a time out for them
         */
        public bool mEnableTimeOutMechanism = true;

        /*
        A time out, in case we don't receive an answer
        from PrintConnect
         */
        public long mTimeOutMS = 5000;
    }
}
