using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace GodTower.Net
{
    public static class NetworkInfo
    {
        /// <summary>First non-loopback IPv4 address of an active interface, or null.</summary>
        public static string GetLocalIPv4()
        {
            try
            {
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != OperationalStatus.Up || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    foreach (UnicastIPAddressInformation address in nic.GetIPProperties().UnicastAddresses)
                    {
                        if (address.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address.Address))
                            return address.Address.ToString();
                    }
                }
            }
            catch (NetworkInformationException)
            {
                // Some Android versions restrict interface enumeration; the caller falls back to localhost.
            }
            return null;
        }
    }
}
