// create a system that inherits from this class to handle authentication on the
// server side.
//
// this class will automatically call BeginAuthentication after connect.
// -> you could send out an authentication message to the client. or simply
//    do nothing and wait for the client to send an authentication message to us
// -> you can have a whole handshake with multiple messages if needed. there is
//    no limit. simply start the process in BeginAuthentication() and go from
//    there.
namespace DOTSNET
{
    public abstract class NetworkServerAuthenticatorSystem : NetworkServerMessageSystem<ConnectMessage>
    {
        // override to send authentication message to the server and include
        // username/password/etc.
        protected abstract void BeginAuthentication(int connectionId);
        protected override bool RequiresAuthentication() { return false; }
        protected override void OnMessage(int connectionId, ConnectMessage message)
        {
            // reset authenticated state (it is set to true by default, and
            // authenticators reset it before starting custom authentication)
            SetAuthenticated(connectionId, false);

            // start the authentication process
            BeginAuthentication(connectionId);
        }

        // helper function to set authenticated state for a connection
        protected void SetAuthenticated(int connectionId, bool authenticated)
        {
            server.connections[connectionId].authenticated = authenticated;
        }
    }
}
