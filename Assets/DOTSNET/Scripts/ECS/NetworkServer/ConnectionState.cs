using System.Collections.Generic;
using Unity.Entities;

namespace DOTSNET
{
    // note: we use booleans instead of an enum. this way it is easier to check
    //       if authenticated (easier than state == AUTH || state == WORLD) etc.
    // -> struct would avoid allocations, but class is just way easier to use
    //    especially when modifying state while iterating
    public class ConnectionState
    {
        // each connection needs to authenticate before it can send/receive
        // game specific messages
        public bool authenticated;

        // has the connection selected a player and joined the game world?
        public bool joinedWorld;

        // objects owned by this connection.
        // it's easier to unspawn them if we keep a list here.
        // otherwise we would have to iterate all server objects on each
        // disconnect.
        // (HashSet so that Add/Remove(Entity) is extremely fast!
        public HashSet<Entity> ownedEntities = new HashSet<Entity>();

        // if Send fails only once, we will flag the connection as broken to
        // avoid possibly logging thousands of 'Send Message failed' warnings
        // in between the time send failed, and transport update removes the
        // connection.
        // it would just slow down the server significantly, and spam the logs.
        public bool broken;
    }
}