// DOTSNET is server authoritative, meaning that server syncs state to the
// client.
//
// Some components might have a client authoritative option where the client
// syncs state to the server, and the server trusts it. This can be useful for
// prototyping, movement, etc.
//
// Let's have an enum that we can reuse.
namespace DOTSNET
{
    public enum SyncDirection : byte
    {
        SERVER_TO_CLIENT,
        CLIENT_TO_SERVER
    }
}