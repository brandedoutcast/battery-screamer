using Android.App;
using Android.App.Job;
using Android.Media;
using Android.Net;
using Android.OS;
using Android.Support.V4.App;
using System;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace BatteryScreamer
{
    [Service(Name = "com.rohith.batteryscreamer.BatteryScreamerJob", Permission = "android.permission.BIND_JOB_SERVICE")]
    class BatteryScreamerJob : JobService
    {
        bool IsCancelled;
        const double LOW = 0.25, HIGH = 0.9;

        public override bool OnStartJob(JobParameters args)
        {
            Battery.BatteryInfoChanged += delegate
            {
                if ((Battery.ChargeLevel <= LOW && Battery.PowerSource == BatteryPowerSource.AC) || (Battery.ChargeLevel >= HIGH && Battery.PowerSource == BatteryPowerSource.Battery))
                    Alerter.Instance.Stop();
            };
            DoWork(args);
            return true;
        }

        public override bool OnStopJob(JobParameters args)
        {
            IsCancelled = true;
            return true;
        }

        void DoWork(JobParameters args)
        {
            Task.Run(() =>
            {
                if (IsCancelled)
                {
                    Alerter.Instance.Stop();
                    return;
                }

                if (DateTime.Now.Hour >= 6 && DateTime.Now.Hour <= 21)
                {
                    if (Battery.ChargeLevel <= LOW && Battery.PowerSource == BatteryPowerSource.Battery) Alerter.Instance.Play("low");
                    else if (Battery.ChargeLevel >= HIGH && Battery.PowerSource == BatteryPowerSource.AC) Show(); Alerter.Instance.Play("high");
                }

                JobFinished(args, false);
            });
        }

        void Show()
        {
            var notificationManager = (NotificationManager)GetSystemService(NotificationService);
            NotificationCompat.Builder builder;
            var soundUri = Android.Net.Uri.Parse("android.resource://com.androidbook.samplevideo/raw/high");

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var importance = NotificationImportance.High;
                NotificationChannel channel = new NotificationChannel("41383", "Battery Screamer", importance);
                channel.Description = "desc";
                notificationManager.CreateNotificationChannel(channel);

                builder = new NotificationCompat.Builder(ApplicationContext, "41383");
            }
            else
            {
                builder = new NotificationCompat.Builder(ApplicationContext);
            }

            builder.SetSmallIcon(Resource.Drawable.navigation_empty_icon)
                .SetContentTitle("Battery Screamer")
                .SetSound(soundUri);

            Notification mNotification = builder.Build();

            mNotification.Flags |= NotificationFlags.OngoingEvent;

            notificationManager.Notify(1, mNotification);
        }
    }
}