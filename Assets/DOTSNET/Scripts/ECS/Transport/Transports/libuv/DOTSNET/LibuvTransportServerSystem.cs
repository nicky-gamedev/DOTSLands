// see also: https://github.com/StormHub/NetUV/blob/dev/examples/EchoServer/TcpServer.cs
using System;
using System.Collections.Generic;
using System.Net;
using Unity.Entities;
using UnityEngine;
using libuv2k;
using libuv2k.Native;

namespace DOTSNET.Libuv
{
    [ServerWorld]
    // use SelectiveSystemAuthoring to create it selectively
    [DisableAutoCreation]
    public class LibuvTransportServerSystem : TransportServerSystem
    {
        // configuration
        public ushort Port = 7777;
        // libuv can be ticked multiple times per frame up to max so we don't
        // deadlock
        public const int LibuvMaxTicksPerFrame = 100;

        // Libuv state
        //
        // IMPORTANT: do NOT create new Loop & Server here, otherwise a loop is
        //            also allocated if we run a test while a scene with this
        //            component on a GameObject is openened.
        //
        //            we need to create it when needed and dispose when we are
        //            done, otherwise dispose isn't called until domain reload.
        //
        Loop loop;
        TcpStream server;
        Dictionary<int, TcpStream> connections = new Dictionary<int, TcpStream>();
        int nextConnectionId = 0;

        public override bool Available()
        {
            // available only where we built the native library
            return Application.platform == RuntimePlatform.OSXEditor ||
                   Application.platform == RuntimePlatform.OSXPlayer ||
                   Application.platform == RuntimePlatform.WindowsEditor ||
                   Application.platform == RuntimePlatform.WindowsPlayer ||
                   Application.platform == RuntimePlatform.LinuxEditor ||
                   Application.platform == RuntimePlatform.LinuxPlayer;
        }

        public override int GetMaxPacketSize() => TcpStream.MaxMessageSize;

        public override bool IsActive() => server != null;

        public override void Start()
        {
            if (server != null)
                return;

            // start server
            IPEndPoint EndPoint = new IPEndPoint(IPAddress.Any, Port);

            Debug.Log($"libuv sv: starting TCP..." + EndPoint);
            loop = new Loop();
            server = new TcpStream(loop);
            server.SimultaneousAccepts(true);
            server.Listen(EndPoint, OnLibuvConnected);
            Debug.Log($"libuv sv: TCP started!");
        }

        // note: DOTSNET already packs messages. Transports don't need to.
        public override bool Send(int connectionId, ArraySegment<byte> segment, Channel channel)
        {
            if (server != null && connections.TryGetValue(connectionId, out TcpStream connection))
            {
                //Debug.Log("libuv sv sending " + segment.Count + " bytes: " + BitConverter.ToString(segment.Array, segment.Offset, segment.Count));
                connection.Send(segment);
                return true;
            }
            return false;
        }

        public override bool Disconnect(int connectionId)
        {
            if (server != null && connections.TryGetValue(connectionId, out TcpStream connection))
            {
                // CloseHandle will disconnect, and OnLibuvClosed will clean up
                connection.CloseHandle();
                return true;
            }
            return false;
        }

        public override string GetAddress(int connectionId)
        {
            if (server != null && connections.TryGetValue(connectionId, out TcpStream connection))
            {
                return connection.GetPeerEndPoint().Address.ToString();
            }
            return "";
        }

        public override void Stop()
        {
            if (server != null)
            {
                server.Dispose();
                server = null;
                loop.Dispose();
                loop = null;
                connections.Clear();
                Debug.Log("libuv sv: TCP stopped!");
            }
        }

        // libuv callbacks /////////////////////////////////////////////////////
        void OnLibuvConnected(TcpStream handle, Exception error)
        {
            // setup callbacks
            handle.onMessage = OnLibuvMessage;
            handle.onError = OnLibuvError;
            handle.onClosed = OnLibuvClosed;

            // close if errors (AFTER setting up onClosed callback!)
            if (error != null)
            {
                Debug.Log($"libuv sv: client connection failed {error}");
                handle.CloseHandle();
                return;
            }

            // assign a connectionId via UserToken.
            // this is better than using handle.InternalHandle.ToInt32() because
            // the InternalHandle isn't available in OnLibuvClosed anymore.
            handle.UserToken = nextConnectionId++;
            connections[(int)handle.UserToken] = handle;

            Debug.Log("libuv sv: client connected with connectionId=" + (int)handle.UserToken);

            // dotsnet event
            base.OnConnected((int)handle.UserToken);
        }

        void OnLibuvMessage(TcpStream handle, ArraySegment<byte> segment)
        {
            // valid connection?
            if (connections.ContainsKey((int)handle.UserToken))
            {
                // DOTSNET event
                base.OnData((int)handle.UserToken, segment);
            }
            else Debug.LogError("libuv sv: invalid connectionid: " + (int)handle.UserToken);
        }

        void OnLibuvError(TcpStream handle, Exception error)
        {
            Debug.Log($"libuv sv: error {error}");
            connections.Remove((int)handle.UserToken);
            handle.CloseHandle();

            // TODO invoke OnDisconnected or does OnLibuvClosed get called anyway?
        }

        void OnLibuvClosed(TcpStream handle)
        {
            Debug.Log($"libuv sv: closed client {handle}");

            // important: remove the connection BEFORE calling the DOTSNET event
            // otherwise DOTSNET OnDisconnected Unspawn might try to send to a
            // disposed connection which we didn't remove yet. do it first.
            connections.Remove((int)handle.UserToken);
            handle.Dispose();

            // dotsnet event
            base.OnDisconnected((int)handle.UserToken);
        }

        // ECS /////////////////////////////////////////////////////////////////
        protected override void OnStartRunning()
        {
            // TODO sharp_uv buffer sizes aren't configurable yet because
            // calling SetSend/RecvBufferSize gives EBADF in TcpStream ctor.
            //
            // configure buffer sizes
            //Pipeline.ReceiveBufferSize = Pipeline.SendBufferSize = SendReceiveBufferSize;

            // configure logging
            Log.Info = Debug.Log;
            Log.Warning = Debug.LogWarning;
            Log.Error = Debug.LogError;
        }

        protected override void OnUpdate()
        {
            // tick once
            if (loop != null && server != null)
            {
                // Run with UV_RUN_NOWAIT returns 0 when nothing to do, but we
                // should avoid deadlocks via LibuvMaxTicksPerFrame
                for (int i = 0; i < LibuvMaxTicksPerFrame; ++i)
                {
                    if (loop.Run(uv_run_mode.UV_RUN_NOWAIT) == 0)
                    {
                        //Debug.Log("libuv sv ticked only " + i + " times");
                        break;
                    }
                }
            }
        }

        protected override void OnDestroy()
        {
            // IMPORTANT: dispose loop.
            // otherwise Unity will dispose it after recompiling a script, at
            // which point it would crash because of some outdated null pointers
            loop?.Dispose();

            // clean up everything else properly
            libuv2k.libuv2k.Shutdown();
        }
    }
}