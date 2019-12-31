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
        const double LOW = 0.25, HIGH = 0.9;

        public override bool OnStartJob(JobParameters args)
        {
            Battery.BatteryInfoChanged += delegate
            {
                if ((Battery.ChargeLevel <= LOW && Battery.PowerSource == BatteryPowerSource.AC) || (Battery.ChargeLevel >= HIGH && Battery.PowerSource == BatteryPowerSource.Battery))
                {
                    Player?.Stop();
                    Player = null;
                }
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
                    Player?.Stop();
                    return;
                }

                if (DateTime.Now.Hour >= 6 && DateTime.Now.Hour <= 21)
                {
                    if (Battery.ChargeLevel <= LOW && Battery.PowerSource == BatteryPowerSource.Battery) SetupPlayer("low");
                    else if (Battery.ChargeLevel >= HIGH && Battery.PowerSource == BatteryPowerSource.AC) SetupPlayer("high");
                }

                JobFinished(args, false);
            });
        }

        void SetupPlayer(string assetName)
        {
            var Descriptor = Assets.OpenFd($"{assetName}.mp3");
            Player = new MediaPlayer { Looping = true };
            Player.Prepared += (s, e) => Player.Start();
            Player.SetDataSource(Descriptor.FileDescriptor, Descriptor.StartOffset, Descriptor.Length);
            Player.Prepare();
        }
    }
}