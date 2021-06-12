// attributes to specify which world a system should run in.
// example: [ServerWorld, ClientWorld]
using System;

namespace DOTSNET
{
    public class ClientWorldAttribute : Attribute {}
    public class ServerWorldAttribute : Attribute {}
}