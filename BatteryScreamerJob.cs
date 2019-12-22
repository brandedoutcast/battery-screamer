using Android.App;
using Android.App.Job;
using Android.Media;
using System;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace BatteryScreamer
{
    [Service(Name = "com.rohith.batteryscreamer.BatteryScreamerJob", Permission = "android.permission.BIND_JOB_SERVICE")]
    class BatteryScreamerJob : JobService
    {
        bool IsCancelled;
        MediaPlayer Player;

        public override bool OnStartJob(JobParameters args)
        {
            SetupPlayer();
            Battery.BatteryInfoChanged += (_, e) =>
            {
                if ((e.ChargeLevel <= 25 && e.PowerSource == BatteryPowerSource.AC) || (e.ChargeLevel >= 90 && e.PowerSource == BatteryPowerSource.Battery)) Player.Stop();
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
                var Level = Battery.ChargeLevel * 100;

                if (IsCancelled)
                {
                    Player.Stop();
                    return;
                }

                if (((Level <= 25 && Battery.PowerSource == BatteryPowerSource.Battery) || (Level >= 90 && Battery.PowerSource == BatteryPowerSource.AC)) && DateTime.Now.Hour >= 6 && DateTime.Now.Hour <= 21)
                    Player.Prepare();

                JobFinished(args, false);
            });
        }

        void SetupPlayer()
        {
            var Descriptor = Assets.OpenFd("alert.mp3");
            Player = new MediaPlayer { Looping = true };
            Player.Prepared += (s, e) => Player.Start();
            Player.SetDataSource(Descriptor.FileDescriptor, Descriptor.StartOffset, Descriptor.Length);
        }
    }
}