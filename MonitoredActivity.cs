using Android.App;
using System;

namespace CropImage
{
    public class MonitoredActivity : Activity
    {
        #region IMonitoredActivity implementation

        public event EventHandler Destroying;
        public event EventHandler Stopping;
        public event EventHandler Starting;

        #endregion

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (Destroying != null)
            {
                Destroying(this, EventArgs.Empty);
            }
        }

        protected override void OnStop()
        {
            base.OnStop();

            if (Stopping != null)
            {
                Stopping(this, EventArgs.Empty);
            }
        }

        protected override void OnStart()
        {
            base.OnStart();

            if(Starting != null)
            {
                Starting(this, EventArgs.Empty);
            }
        }
    }
}

