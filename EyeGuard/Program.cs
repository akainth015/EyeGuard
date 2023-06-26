using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Diagnostics;
using System.Threading;

namespace EyeGuard
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();

            // Check whether there is already a version of EyeGuard running
            var mainInstance = AppInstance.FindOrRegisterForKey("Main");
            var currentInstance = AppInstance.GetCurrent();
            var activationArguments = currentInstance.GetActivatedEventArgs();

            if (mainInstance != currentInstance)
            {
                // Close this instance of the app
                mainInstance.RedirectActivationToAsync(activationArguments).AsTask().Wait();
            }
            else
            {
                Microsoft.UI.Xaml.Application.Start((p) =>
                    {
                        var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                        SynchronizationContext.SetSynchronizationContext(context);
                        new App();
                    });
            }
        }
    }
}
