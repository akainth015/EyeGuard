using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications; // Notifications library
using Windows.Data.Xml.Dom;
using System.Threading;
using Windows.UI.Xaml;
using Windows.Storage;
using System;

namespace EyeGuard
{
    class Program
    {
        static void Main()
        {
            ToastVisual visual = new ToastVisual()
            {
                BindingGeneric = new ToastBindingGeneric()
                {
                    Children =
                    {
                        new AdaptiveText()
                        {
                            Text = "Give your eyes a break"
                        },
                        new AdaptiveProgressBar()
                        {
                            Title = "Focus on something 20 feet away",
                            Value = new BindableProgressBarValue("progressValue"),
                            ValueStringOverride = new BindableString("progressValueString"),
                            Status = "Keep looking"
                        }
                    }
                }
            };
            ToastContent content = new ToastContent()
            {
                Visual = visual
            };

            XmlDocument document = new XmlDocument();
            document.LoadXml(content.GetContent());

            while (true)
            {
                Thread.Sleep(20 * 60 * 1000);

                ToastNotification toast = new ToastNotification(document);
                toast.Tag = "refocus";
                toast.Data = new NotificationData();
                toast.Data.Values["progressValue"] = "0";
                toast.Data.Values["progressValueString"] = "0/20 seconds";

                ToastNotificationManager.CreateToastNotifier().Show(toast);

                for (uint seconds = 0; seconds <= 20; seconds++)
                {
                    var data = new NotificationData
                    {
                        SequenceNumber = seconds
                    };
                    data.Values["progressValue"] = (seconds / 20f).ToString();
                    data.Values["progressValueString"] = $"{seconds}/20 seconds";
                    if (ToastNotificationManager.CreateToastNotifier().Update(data, "refocus") != NotificationUpdateResult.Succeeded)
                    {
                        break;
                    }
                    Thread.Sleep(1000);
                }
                ToastNotificationManager.History.Remove("refocus");
            }
        }
    }
}
