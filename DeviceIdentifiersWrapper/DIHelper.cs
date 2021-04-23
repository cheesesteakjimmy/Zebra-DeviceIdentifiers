using Android.Content;
using Android.Content.PM;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeviceIdentifiersWrapper
{
	public class DIHelper
	{
        // Placeholder for custom certificate
        // Otherwise, the app will use the first certificate found with the method:
        // final Signature[] arrSignatures = packageInfo.signingInfo.getApkContentsSigners();
        // TODO: Put your custom certificate in the apkCertificate member for MX AccessMgr registering (only if necessary and if you know what you are doing)
        public static Signature apkCertificate = null;

        // This method will return the serial number in the string passed through the onSuccess method
        public static void getSerialNumber(Context context, IDIResultCallbacks callbackInterface)
        {
            new RetrieveOEMInfoTask().Execute(context, Android.Net.Uri.Parse("content://oem_info/oem.zebra.secure/build_serial"), callbackInterface);
        }

        // This method will return the imei number in the string passed through the onSuccess method
        public static void getIMEINumber(Context context, IDIResultCallbacks callbackInterface)
        {
            new RetrieveOEMInfoTask().Execute(context, Android.Net.Uri.Parse("content://oem_info/wan/imei"), callbackInterface);
        }
    }
}
