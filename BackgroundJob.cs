using Android.App;
using Android.OS;
using System;
using System.Threading;

namespace CropImage
{
	public class BackgroundJob
    {
        #region Static helpers

        public static void StartBackgroundJob(
            MonitoredActivity activity, string title,
            string message, Action job, Handler handler)
		{
			// Make the progress dialog uncancelable, so that we can gurantee
			// the thread will be done before the activity getting destroyed.
			ProgressDialog dialog = ProgressDialog.Show(activity, title, message, true, false);
			ThreadPool.QueueUserWorkItem((w) => new BackgroundJob(activity, job, dialog, handler).Run());
		}

        #endregion

        #region Members

        private MonitoredActivity activity;
		private ProgressDialog progressDialog;
		private Action job;
		private Handler handler;

        #endregion

        #region Constructor

        public BackgroundJob(MonitoredActivity activity, Action job,
		                     ProgressDialog progressDialog, Handler handler)
		{
			this.activity = activity;
			this.progressDialog = progressDialog;
			this.job = job;			
			this.handler = handler;

			activity.Destroying += (sender, e) =>  {
				// We get here only when the onDestroyed being called before
				// the cleanupRunner. So, run it now and remove it from the queue
				cleanUp();
				handler.RemoveCallbacks(cleanUp);
			};

			activity.Stopping += (sender, e) =>progressDialog.Hide();
			activity.Starting += (sender, e) => progressDialog.Show();
		}

        #endregion

        #region Methods

        public void Run()
		{
			try
			{
				job();
			}
			finally
			{
				handler.Post (cleanUp);
			}
        }

        #endregion

        #region Private helpers

        private void cleanUp()
        {
            if (progressDialog.Window != null)
            {
                progressDialog.Dismiss();
            }
        }

        #endregion
    }   
}