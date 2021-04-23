using Android.Content;
using Symbol.XamarinEMDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace DeviceIdentifiersWrapper
{
	public class DIProfileManagerCommand : DICommandBase
	{
		// Members
		private string TAG = "DIWrapperMX";

		// Callback interface to get the hand back when the profile has been executed
		private IDIResultCallbacks idiProfileManagerCommandResult = null;

		// Profile content in XML
		private string msProfileData = "";

		// Profile name to execute
		private string msProfileName = "";

		//Declare a variable to store ProfileManager object
		private static ProfileManager mProfileManager = null;

		//Declare a variable to store EMDKManager object
		private static EMDKManager mEMDKManager = null;

		// An ArrayList that will contains errors if we find some
		private List<ErrorHolder> mErrors = new List<ErrorHolder>();

		// Provides full error description string
		public string msErrorString = "";

		// Status Listener implementation (ensure that we retrieve the profile manager asynchronously
		public class StatusListener : Java.Lang.Object, EMDKManager.IStatusListener
		{
			private Action<ProfileManager> _onProfileManagerInitialized;

			public StatusListener(Action<ProfileManager> onProfileManagerInitialized)
			{
				_onProfileManagerInitialized = onProfileManagerInitialized;
			}

			public void OnStatus(EMDKManager.StatusData statusData, EMDKBase emdkBase)
			{
				if (statusData.Result == EMDKResults.STATUS_CODE.Success)
				{
					_onProfileManagerInitialized((ProfileManager)emdkBase);
				}
				else
				{
					var errorMessage = "Error when trying to retrieve ProfileManager: " + statusData.Result.ToString();
				}
			}
		}

		public StatusListener mStatusListener = null;

		// EMDKListener implementation
		public class EMDKListener : Java.Lang.Object, EMDKManager.IEMDKListener
		{
			private Action _onEMDKManagerClosed;
			private Action<EMDKManager> _onEMDKManagerRetrieved;

			public EMDKListener(Action onEMDKManagerClosed, Action<EMDKManager> onEMDKManagerRetrieved)
			{
				_onEMDKManagerClosed = onEMDKManagerClosed;
				_onEMDKManagerRetrieved = onEMDKManagerRetrieved;
			}

			public void OnClosed()
			{
				//logMessage("EMDKManager.EMDKListener.onClosed", EMessageType.DEBUG);
				_onEMDKManagerClosed();
			}

			public void OnOpened(EMDKManager emdkManager)
			{
				//logMessage("EMDKManager.EMDKListener.onOpened", EMessageType.DEBUG);
				_onEMDKManagerRetrieved(emdkManager);
			}
		}

		public EMDKListener mEMDKListener = null;

		public DIProfileManagerCommand(Context aContext) : base(aContext)
		{
			mSettings = new DICommandBaseSettings()
			{
				mTimeOutMS = 200000,
				mEnableTimeOutMechanism = true,
				mCommandId = "DWProfileManagerCommand"
			};

			mStatusListener = new StatusListener((profileManager) => { onProfileManagerInitialized(profileManager); });
			mEMDKListener = new EMDKListener(() => { onEMDKManagerClosed(); }, (mEMDKManager) => { onEMDKManagerRetrieved(mEMDKManager); });
		}

		protected override void OnTimeOut(DICommandBaseSettings settings)
		{
			base.OnTimeOut(settings);
			onEMDKManagerClosed();
		}

		public void execute(String mxProfile, String mxProfileName, IDIResultCallbacks resutCallback)
		{
			// Let's start the timeout mechanism
			base.execute(mSettings);
			idiProfileManagerCommandResult = resutCallback;
			msProfileData = mxProfile;
			msProfileName = mxProfileName;
			initializeEMDK();
		}

		private void initializeEMDK()
		{
			if (mEMDKManager == null)
			{
				EMDKResults results = null;
				try
				{
					//The EMDKManager object will be created and returned in the callback.
					results = EMDKManager.GetEMDKManager(mContext.ApplicationContext, mEMDKListener);
				}
				catch (Exception e)
				{
					logMessage("Error while requesting EMDKManager.\n" + e.Message, EMessageType.ERROR);
					return;
				}

				//Check the return status of EMDKManager object creation.
				if (results.StatusCode == EMDKResults.STATUS_CODE.Success)
				{
					logMessage("EMDKManager request command issued with success", EMessageType.DEBUG);
				}
				else
				{
					logMessage("EMDKManager request command error", EMessageType.ERROR);
				}
			}
			else
			{
				onEMDKManagerRetrieved(mEMDKManager);
			}
		}

		private void onEMDKManagerRetrieved(EMDKManager emdkManager)
		{
			mEMDKManager = emdkManager;
			logMessage("EMDK Manager retrieved.", EMessageType.DEBUG);
			if (mProfileManager == null)
			{
				try
				{
					emdkManager.GetInstanceAsync(EMDKManager.FEATURE_TYPE.Profile, mStatusListener);
				}
				catch (EMDKException e)
				{
					logMessage("Error when trying to retrieve profile manager: " + e.Message, EMessageType.ERROR);
				}
			}
			else
			{
				logMessage("EMDK Manager already initialized.", EMessageType.DEBUG);
				onProfileManagerInitialized(mProfileManager);
			}
		}

		private void onEMDKManagerClosed()
		{
			releaseManagers();
		}

		private void releaseManagers()
		{
			if (mProfileManager != null)
			{
				mProfileManager = null;
				logMessage("Profile Manager reseted.", EMessageType.DEBUG);
			}

			//This callback will be issued when the EMDK closes unexpectedly.
			if (mEMDKManager != null)
			{
				mEMDKManager.Release();
				logMessage("EMDKManager released.", EMessageType.DEBUG);
				mEMDKManager = null;
				logMessage("EMDKManager reseted.", EMessageType.DEBUG);
			}
		}

		private void onProfileManagerInitialized(ProfileManager profileManager)
		{
			mProfileManager = profileManager;
			logMessage("Profile Manager retrieved.", EMessageType.DEBUG);
			ProcessMXContent();
		}

		private void onProfileExecutedWithSuccess()
		{
			releaseManagers();
			if (idiProfileManagerCommandResult != null)
			{
				idiProfileManagerCommandResult.OnSuccess("Success applying profile:" + msProfileName + "\nProfileData:" + msProfileData);
			}
		}

		private void onProfileExecutedError(String message)
		{
			releaseManagers();
			if (idiProfileManagerCommandResult != null)
			{
				idiProfileManagerCommandResult.OnError("Error on profile: " + msProfileName + "\nError:" + message + "\nProfileData:" + msProfileData);
			}
		}

		private void onProfileExecutedStatusChanged(String message)
		{
			if (idiProfileManagerCommandResult != null)
			{
				idiProfileManagerCommandResult.OnDebugStatus(message);
			}
		}

		private void ProcessMXContent()
		{
			var parameters = new String[1];
			parameters[0] = msProfileData;

			EMDKResults results = mProfileManager.ProcessProfile(msProfileName, ProfileManager.PROFILE_FLAG.Set, parameters);

			//Check the return status of processProfile
			if (results.StatusCode == EMDKResults.STATUS_CODE.CheckXml)
			{
				try
				{
					// Empty Error Holder Array List if it already exists
					mErrors.Clear();

					//Inspect the XML response to see if there are any errors, if not report success
					using (XmlReader reader = XmlReader.Create(new StringReader(results.StatusString)))
					{
						//while (reader.Read())
						//{

						//}
					}
					onProfileExecutedWithSuccess();

				}
				catch (Exception e)
				{
				}
			}
			else if (results.StatusCode == EMDKResults.STATUS_CODE.Success)
			{
				logMessage("Profile executed with success: " + msProfileName, EMessageType.DEBUG);
				onProfileExecutedWithSuccess();
				return;
			}
			else
			{
				String errorMessage = "Profile update failed." + GetResultCode(results.StatusCode) + "\nProfile:\n" + msProfileName;
				logMessage(errorMessage, EMessageType.ERROR);
				onProfileExecutedError(errorMessage);
				return;
			}
		}

		// Method to parse the XML response using XML Pull Parser
		private void parseXML(XmlReader myParser)
		{
			try
			{
				// TODO: Parse XML
			}
			catch (Exception e)
			{
			}
		}

		public void logMessage(String message, EMessageType messageType)
		{
			switch (messageType)
			{
				case EMessageType.ERROR:

					onProfileExecutedStatusChanged("ERROR:" + message);
					break;

				case EMessageType.SUCCESS:

					onProfileExecutedStatusChanged("SUCCESS:" + message);
					break;

				case EMessageType.VERBOSE:

					onProfileExecutedStatusChanged("VERBOSE:" + message);
					break;

				case EMessageType.WARNING:

					onProfileExecutedStatusChanged("WARNING:" + message);
					break;

				case EMessageType.DEBUG:

					onProfileExecutedStatusChanged("DEBUG:" + message);
					break;
			}
		}

		private string GetResultCode(EMDKResults.STATUS_CODE aStatusCode)
		{
			return aStatusCode.ToString();
		}

		public class ErrorHolder
		{
			// Provides the error type for characteristic-error
			public String sErrorType = "";

			// Provides the parm name for parm-error
			public String sParmName = "";

			// Provides error description
			public String sErrorDescription = "";
		}
	}
}