using Android.App;
using Android.App.Job;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Widget;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace BatteryScreamer
{
    [Activity(Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        const string TAG = "BATTERY_SCREAMER";
        const int JOB_ID = 41383;

        TextView Info, Status;
        Button Start, Stop, StopAlert;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.main);

            Info = FindViewById<TextView>(Resource.Id.info);
            Status = FindViewById<TextView>(Resource.Id.status);
            Start = FindViewById<Button>(Resource.Id.scheduleJob);
            Stop = FindViewById<Button>(Resource.Id.cancelJob);
            StopAlert = FindViewById<Button>(Resource.Id.stopAlert);

            Init();
        }

        void Init()
        {
            Start.Click += delegate { ScheduleJob(); };
            Stop.Click += delegate { CancelJob(); };
            StopAlert.Click += delegate { StopPlay(); };

            UpdateInfo();
            Battery.BatteryInfoChanged += delegate { UpdateInfo(); };

            UpdateJobStatus();
            Task.Run(() =>
            {
                do
                {
                    UpdateJobStatus();
                    Task.Delay(30000).Wait();
                } while (true);
            });
        }

        void UpdateInfo() => Info.Text = $"Level: {Battery.ChargeLevel * 100}%\nSource: {Battery.PowerSource.ToString()}\nStatus: {Battery.State.ToString()}";

        void UpdateJobStatus(bool? isActive = null)
        {
            if (!isActive.HasValue)
                isActive = ((JobScheduler)GetSystemService(JobSchedulerService)).AllPendingJobs.Any(j => j.Id == JOB_ID);

            var ColorResource = isActive.Value ? Resource.Color.colorHappy : Resource.Color.colorDanger;
            var Color = Build.VERSION.SdkInt >= BuildVersionCodes.M ? Resources.GetColor(ColorResource, null) : Resources.GetColor(ColorResource);

            Status.Text = $"Alerts are " + (isActive.Value ? "Active" : "Inactive");
            Status.SetTextColor(Color);
        }

        void ScheduleJob()
        {
            var JavaClass = Java.Lang.Class.FromType(typeof(BatteryScreamerJob));
            var Component = new ComponentName(this, JavaClass);
            var JobInfo = new JobInfo.Builder(JOB_ID, Component)
                                .SetRequiredNetworkType(NetworkType.Unmetered)
                                .SetPersisted(true)
                                .SetRequiresDeviceIdle(true)
                                .SetPeriodic(15 * 60 * 1000)
                                .Build();

            var Scheduler = (JobScheduler)GetSystemService(JobSchedulerService);
            var IsScheduled = Scheduler.Schedule(JobInfo) == JobScheduler.ResultSuccess;
            UpdateJobStatus(IsScheduled);
        }

        void CancelJob()
        {
            var Scheduler = (JobScheduler)GetSystemService(JobSchedulerService);
            Scheduler.Cancel(JOB_ID);
            UpdateJobStatus(false);
        }

        void StopPlay()
        {
            var notificationManager = (NotificationManager)GetSystemService(NotificationService);
            NotificationCompat.Builder builder;
            var soundUri = Android.Net.Uri.Parse("android.resource://com.rohith.batteryscreamer/raw/high");

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
                .SetAutoCancel(true)
                .SetSound(soundUri);

            Notification mNotification = builder.Build();

            mNotification.Flags |= NotificationFlags.Insistent | NotificationFlags.HighPriority;

            StartForegroundService(mNotification);

            notificationManager.Notify(41383, mNotification);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}