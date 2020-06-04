using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Runtime.InteropServices;

namespace EyeCare
{
    // The GUID CLSID must be unique to your app. Create a new GUID if copying this code.
    [ClassInterface(ClassInterfaceType.None)]
    [ComSourceInterfaces(typeof(INotificationActivationCallback))]
    [Guid("a7f0dda3-447a-40ad-b720-bd0ab6e29d69"), ComVisible(true)]
    public class LinkNotificationActivator : NotificationActivator
    {
        public override void OnActivated(string invokedArgs, NotificationUserInput userInput, string appUserModelId)
        {
            // TODO: Handle activation
        }
    }
}
