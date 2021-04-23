using Android.Content;
using Android.Content.PM;
using Android.Database;
using Android.OS;
using Android.Util;
using System;

namespace DeviceIdentifiersWrapper
{
	public class ResultCallbacks : IDIResultCallbacks
	{
		private IDIResultCallbacks callbackInterface;
		private Context _context;
		private Android.Net.Uri _uri;
		private Action<ICursor, Android.Net.Uri, IDIResultCallbacks> _getUriValue;

		public ResultCallbacks(IDIResultCallbacks resultCallbacks)
		{
			callbackInterface = resultCallbacks;
		}

		public ResultCallbacks(IDIResultCallbacks resultCallbacks, Context context, Android.Net.Uri uri, Action<ICursor, Android.Net.Uri, IDIResultCallbacks> getUriValue)
		{
			callbackInterface = resultCallbacks;
			_context = context;
			_uri = uri;
			_getUriValue = getUriValue;
		}

		public void OnDebugStatus(string message)
		{
			if (callbackInterface != null)
			{
				callbackInterface.OnDebugStatus(message);
			}
		}

		public void OnError(string message)
		{
			if (callbackInterface != null)
			{
				callbackInterface.OnError(message);
				return;
			}
		}

		public void OnSuccess(string message)
		{
			// The app has been registered
			// Let's try again to get the identifier
			var cursor2 = _context.ContentResolver.Query(_uri, null, null, null, null);
			if (cursor2 == null || cursor2.Count < 1)
			{
				if (callbackInterface != null)
				{
					callbackInterface.OnError("Fail to register the app for OEM Service call:" + _uri + "\nIt's time to debug this app ;)");
					return;
				}
			}
			_getUriValue(cursor2, _uri, callbackInterface);
			return;
		}
	}

	public class RetrieveOEMInfoTask : AsyncTask<object, bool, bool>
	{
		private static void RetrieveOEMInfo(Context context, Android.Net.Uri uri, IDIResultCallbacks callbackInterface)
		{
			//  For clarity, this code calls ContentResolver.query() on the UI thread but production code should perform queries asynchronously.
			//  See https://developer.android.com/guide/topics/providers/content-provider-basics.html for more information
			var cursor = context.ContentResolver.Query(uri, null, null, null, null);
			if (cursor == null || cursor.Count < 1)
			{
				if (callbackInterface != null)
				{
					callbackInterface.OnDebugStatus("App not registered to call OEM Service:" + uri.ToString() + "\nRegistering current application using profile manger, this may take a couple of seconds...");
				}
				// Let's register the application
				RegisterCurrentApplication(context, uri, new ResultCallbacks(callbackInterface, context, uri, (cursor, uri, callbacks) => { GetURIValue(cursor, uri, callbacks); }));
			}
			else
			{
				// We have the right to call this service, and we obtained some data to parse...
				GetURIValue(cursor, uri, callbackInterface);
			}
		}
		protected override bool RunInBackground(params object[] @params)
		{
			var context = (Context)@params[0];
			var uri = (Android.Net.Uri)@params[1];
			var idiResultCallbacks = (IDIResultCallbacks)@params[2];
			RetrieveOEMInfo(context, uri, idiResultCallbacks);
			return true;
		}

		private static void RegisterCurrentApplication(Context context, Android.Net.Uri serviceIdentifier, IDIResultCallbacks callbackInterface)
		{
			var profileName = "AccessMgr-1";
			var profileData = "";
			try
			{
				var packageInfo = context.PackageManager.GetPackageInfo(context.PackageName, PackageInfoFlags.SigningCertificates);
				var path = context.ApplicationInfo.SourceDir;
				var strName = packageInfo.ApplicationInfo.LoadLabel(context.PackageManager).ToString();
				var strVendor = packageInfo.PackageName;
				var sig = DIHelper.apkCertificate;

				// Let's check if we have a custom certificate
				if (sig == null)
				{
					// Nope, we will get the first apk signing certificate that we find
					// You can copy/paste this snippet if you want to provide your own
					// certificate
					// TODO: use the following code snippet to extract your custom certificate if necessary
					var arrSignatures = packageInfo.SigningInfo.GetApkContentsSigners();
					if (arrSignatures == null || arrSignatures.Length == 0)
					{
						if (callbackInterface != null)
						{
							callbackInterface.OnError("Error : Package has no signing certificates... how's that possible ?");
							return;
						}
					}
					sig = arrSignatures[0];
				}

				/*
				 * Get the X.509 certificate.
				 */
				var rawCert = sig.ToByteArray();

				// Get the certificate as a base64 string
				var encoded = Base64.EncodeToString(rawCert, Base64Flags.Default);

				profileData =
						"<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
								"<characteristic type=\"Profile\">" +
								"<parm name=\"ProfileName\" value=\"" + profileName + "\"/>" +
								"<characteristic type=\"AccessMgr\" version=\"9.2\">" +
								"<parm name=\"OperationMode\" value=\"1\" />" +
								"<parm name=\"ServiceAccessAction\" value=\"4\" />" +
								"<parm name=\"ServiceIdentifier\" value=\"" + serviceIdentifier + "\" />" +
								"<parm name=\"CallerPackageName\" value=\"" + context.PackageName.ToString() + "\" />" +
								"<parm name=\"CallerSignature\" value=\"" + encoded + "\" />" +
								"</characteristic>" +
								"</characteristic>";
				DIProfileManagerCommand profileManagerCommand = new DIProfileManagerCommand(context);
				profileManagerCommand.execute(profileData, profileName, callbackInterface);
			}
			catch (Exception e)
			{
				if (callbackInterface != null)
				{
					callbackInterface.OnError("Error on profile: " + profileName + "\nError:" + e.Message + "\nProfileData:" + profileData);
				}
			}
		}

		public static bool GetURIValue(ICursor cursor, Android.Net.Uri uri, IDIResultCallbacks resultCallbacks)
		{
			while (cursor.MoveToNext())
			{
				if (cursor.ColumnCount == 0)
				{
					//  No data in the cursor.  I have seen this happen on non-WAN devices
					String errorMsg = "Error: " + uri + " does not exist on this device";
					resultCallbacks.OnDebugStatus(errorMsg);
				}
				else
				{
					for (int i = 0; i < cursor.ColumnCount; i++)
					{
						try
						{
							String data = cursor.GetString(cursor.GetColumnIndex(cursor.GetColumnName(i)));
							resultCallbacks.OnSuccess(data);
							cursor.Close();
							return true;
						}
						catch (Exception e)
						{
							resultCallbacks.OnDebugStatus(e.Message);
						}
					}
				}
			}
			cursor.Close();
			resultCallbacks.OnError("Data not found in Uri:" + uri);
			return true;
		}
	}
}