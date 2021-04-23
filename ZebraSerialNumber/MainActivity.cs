using Android.App;
using Android.OS;
using Android.Widget;
using AndroidX.AppCompat.App;
using DeviceIdentifiersWrapper;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ZebraSerialNumber
{
	[Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
	public class MainActivity : AppCompatActivity
	{
		private string TAG = "DeviceID";

		private static string status = "";
		private TextView tvStatus;
		private TextView tvSerialNumber;
		private TextView tvIMEI;

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
			Xamarin.Essentials.Platform.Init(this, savedInstanceState);
			SetContentView(Resource.Layout.activity_main);

			tvStatus = FindViewById<TextView>(Resource.Id.tv_status);
			tvSerialNumber = FindViewById<TextView>(Resource.Id.txtSerialNumber);
			tvIMEI = FindViewById<TextView>(Resource.Id.txtImei);

			// The call is asynchronous, since we may have to register the app to
			// allow calling device identifier service, we don't want to get two
			// concurrent calls to it, so we will ask for the IMEI number only at
			// the end of the getSerialNumber method call (success or error)

			GetSerialNumber();
			//GetIMEINumber();
		}

		private void AddMessageToStatusText(string message)
		{
			status += message + "\n";
			RunOnUiThread(() =>
			{
				tvStatus.Text = status;
			});
		}

		private void UpdateTextViewContent(TextView tv, string text)
		{
			RunOnUiThread(() =>
			{
				tv.Text = status;
			});
		}

		private void GetSerialNumber()
		{
			DIHelper.getSerialNumber(this, new ResultCallbacks(
				new CustomCallback(
					(message) => { AddMessageToStatusText(message); },
					(message) => { AddMessageToStatusText(message); },
					(message) => { UpdateTextViewContent(tvSerialNumber, message); }
			)));
		}

		private void GetIMEINumber()
		{
			DIHelper.getIMEINumber(this, new ResultCallbacks(new CustomCallback(
					(message) => { AddMessageToStatusText(message); },
					(message) => { AddMessageToStatusText(message); },
					(message) => { UpdateTextViewContent(tvIMEI, message); }
			)));
		}

		public class CustomCallback : IDIResultCallbacks
		{
			private Action<string> _debug;
			private Action<string> _error;
			private Action<string> _success;

			public CustomCallback(Action<string> debug, Action<string> error, Action<string> success)
			{
				_debug = debug;
				_error = error;
				_success = success;
			}

			public void OnDebugStatus(string message)
			{
				_debug(message);
			}

			public void OnError(string message)
			{
				_error(message);
			}

			public void OnSuccess(string message)
			{
				_success(message);
			}
		}
	}
}