// create a system that inherits from this class to handle authentication on the
// client side.
//
// this class will automatically call BeginAuthentication after connect.
// -> you should send out an authentication message
// -> the message should contain username+password etc.
// -> you can have a whole handshake with multiple messages if needed. there is
//    no limit. simply start the process in BeginAuthentication() and go from
//    there.
using Unity.Entities;

namespace DOTSNET
{
    [ClientWorld]
    [UpdateInGroup(typeof(ClientConnectedSimulationSystemGroup))]
    public abstract class NetworkClientAuthenticatorSystem : SystemBase
    {
        // override to send authentication message to the server and include
        // username/password/etc.
        protected abstract void BeginAuthentication();
        protected override void OnStartRunning()
        {
            BeginAuthentication();
        }
    }
}
