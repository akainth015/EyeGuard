using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Linq;
using System.Threading;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace EyeCare
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Any((arg) => arg.Contains("ToastActivated"))) Environment.Exit(0);

            // Register COM server and activator type
            DesktopNotificationManagerCompat.RegisterActivator<ClearingActivator>();
            DesktopNotificationManagerCompat.RegisterAumidAndComServer<ClearingActivator>("Aanand.EyeCare");

            // Construct the visuals of the toast (using Notifications library)
            ToastContent toastContent = new ToastContent()
            {

                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
            {
                new AdaptiveText()
                {
                    Text = "Give your eyes a break!"
                },
                new AdaptiveText()
                {
                    Text = "Refocus your eyes on something at least 20 feet away for 20 seconds"
                },
            }
                    }
                }
            };

            // Create the XML document (BE SURE TO REFERENCE WINDOWS.DATA.XML.DOM)
            var doc = new XmlDocument();
            doc.LoadXml(toastContent.GetContent());

            while (true)
            {
                Thread.Sleep(20 * 59 * 1000);

                // And create the toast notification
                var toast = new ToastNotification(doc);

                // And then show it
                DesktopNotificationManagerCompat.CreateToastNotifier().Show(toast);

                Thread.Sleep(20 * 1000);

                DesktopNotificationManagerCompat.History.Clear();
            }
        }
    }
}
