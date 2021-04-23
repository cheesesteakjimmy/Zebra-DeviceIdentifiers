using Android.Content;
using Android.OS;
using Java.Lang;

namespace DeviceIdentifiersWrapper
{
	public abstract class DICommandBase
	{
		/*
        A TAG if we want to log something
         */
		protected static string TAG = "DIWrapperMX";

		/*
        A context to work with intents
         */
		protected Context mContext = null;

		protected DICommandBaseSettings mSettings = null;

		/*
        A handler that will be used by the derived
        class to prevent waiting to long for DW in case
        of problem
         */
		protected Handler mTimeOutHandler;

		/*
        What will be done at the end of the TimeOut
         */
		protected Runnable mTimeOutRunnable = null;

		public DICommandBase(Context aContext)
		{
			mContext = aContext;
			mTimeOutHandler = new Handler(mContext.MainLooper);

			mTimeOutRunnable = new Runnable(() =>
			{
				OnTimeOut(mSettings);
			});
		}

		public void execute(DICommandBaseSettings settings)
		{
			mSettings = settings;
			/*
			Start time out mechanism
			Enabled by default in DWProfileBaseSettings
			 */
			if (settings.mEnableTimeOutMechanism)
			{
				mTimeOutHandler.PostDelayed(mTimeOutRunnable,
						mSettings.mTimeOutMS);
			}
		}

		protected virtual void OnTimeOut(DICommandBaseSettings settings)
		{
			CleanAll();
		}

		protected void CleanAll()
		{
			if (mTimeOutHandler != null)
			{
				mTimeOutHandler.RemoveCallbacks(mTimeOutRunnable);
			}
		}
	}
}