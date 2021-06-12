using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace DOTSNET.Libuv
{
    public static class LibuvUtils
    {
        // libuv doesn't resolve host names.
        // and it only works with IPv4 with our configuration.
        public static bool ResolveToIPV4(string hostname, out IPAddress address)
        {
            // resolve host name (if hostname. otherwise it returns the IP)
            // and connect to the first available address (IPv4 or IPv6)
            // => GetHostAddresses is BLOCKING (for a very short time). we could
            //    move it to the ConnectThread, but it's hardly worth the extra
            //    code since we would have to create the socket in ConnectThread
            //    too, which would require us to use locks around socket every-
            //    where. it's better to live with a <1s block (if any).
            try
            {
                // resolving usually gives an IPv6 and an IPv4 address.
                // find the IPv4 address.
                IPAddress[] addresses = Dns.GetHostAddresses(hostname);
                foreach (IPAddress ip in addresses)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        address = ip;
                        return true;
                    }
                }
            }
            catch (SocketException exception)
            {
                // it's not an error. just an invalid host so log a warning.
                Debug.LogWarning("Libuv Connect: failed to resolve host: " + hostname + " reason: " + exception);
            }
            address = null;
            return false;
        }
    }
}