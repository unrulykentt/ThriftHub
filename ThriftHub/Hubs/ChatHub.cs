using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace ThriftHub.Hubs
{
    public class ChatHub : Hub
    {
        // ============================================================
        // CONNECTION TRACKING
        // ============================================================

        private static readonly ConcurrentDictionary<
            string,
            ConcurrentDictionary<string, byte>
        > ConnectedUsers = new();


        // ============================================================
        // USER CONNECTED
        // ============================================================

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                var connections =
                    ConnectedUsers.GetOrAdd(
                        userId,
                        _ => new ConcurrentDictionary<string, byte>()
                    );

                connections.TryAdd(
                    Context.ConnectionId,
                    0
                );

                await Clients.All.SendAsync(
                    "UserOnline",
                    userId
                );
            }

            await base.OnConnectedAsync();
        }


        // ============================================================
        // USER DISCONNECTED
        // ============================================================

        public override async Task OnDisconnectedAsync(
            Exception? exception)
        {
            var userId = Context.UserIdentifier;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                if (ConnectedUsers.TryGetValue(
                    userId,
                    out var connections))
                {
                    connections.TryRemove(
                        Context.ConnectionId,
                        out _
                    );

                    if (connections.IsEmpty)
                    {
                        ConnectedUsers.TryRemove(
                            userId,
                            out _
                        );

                        await Clients.All.SendAsync(
                            "UserOffline",
                            userId
                        );
                    }
                }
            }

            await base.OnDisconnectedAsync(exception);
        }


        // ============================================================
        // CHECK USER STATUS
        // ============================================================

        public async Task CheckUserStatus(
            string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            var online =
                ConnectedUsers.ContainsKey(userId);

            await Clients.Caller.SendAsync(
                "UserStatus",
                userId,
                online
            );
        }


        // ============================================================
        // START VOICE CALL
        // ============================================================

        public async Task StartCall(
            string recipientId,
            string type)
        {
            var callerId =
                Context.UserIdentifier;

            // --------------------------------------------------------
            // CHECK CALLER
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(callerId))
            {
                await Clients.Caller.SendAsync(
                    "CallError",
                    "Your account could not be identified."
                );

                return;
            }


            // --------------------------------------------------------
            // CHECK RECIPIENT
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(recipientId))
            {
                await Clients.Caller.SendAsync(
                    "CallError",
                    "The person you are trying to call could not be found."
                );

                return;
            }


            // --------------------------------------------------------
            // PREVENT CALLING YOURSELF
            // --------------------------------------------------------

            if (callerId == recipientId)
            {
                await Clients.Caller.SendAsync(
                    "CallError",
                    "You cannot call yourself."
                );

                return;
            }


            // --------------------------------------------------------
            // ONLY AUDIO CALLS
            // --------------------------------------------------------

            if (!string.Equals(
                type,
                "audio",
                StringComparison.OrdinalIgnoreCase))
            {
                await Clients.Caller.SendAsync(
                    "CallError",
                    "Only voice calls are currently supported."
                );

                return;
            }


            // --------------------------------------------------------
            // CHECK IF RECIPIENT IS ONLINE
            // --------------------------------------------------------

            if (!ConnectedUsers.TryGetValue(
                recipientId,
                out var recipientConnections))
            {
                await Clients.Caller.SendAsync(
                    "CallUnavailable",
                    recipientId,
                    "This user is currently offline."
                );

                return;
            }


            // --------------------------------------------------------
            // CHECK CONNECTIONS
            // --------------------------------------------------------

            if (recipientConnections.IsEmpty)
            {
                await Clients.Caller.SendAsync(
                    "CallUnavailable",
                    recipientId,
                    "This user is currently offline."
                );

                return;
            }


            // ========================================================
            // SEND INCOMING CALL TO RECIPIENT
            // ========================================================

            foreach (var connectionId
                in recipientConnections.Keys)
            {
                await Clients.Client(
                    connectionId
                ).SendAsync(
                    "IncomingVoiceCall",
                    callerId
                );
            }


            // ========================================================
            // INFORM CALLER
            // ========================================================

            await Clients.Caller.SendAsync(
                "CallStarted",
                recipientId
            );
        }


        // ============================================================
        // ACCEPT CALL
        // ============================================================

        public async Task AcceptCall(
            string callerId)
        {
            var receiverId =
                Context.UserIdentifier;

            if (string.IsNullOrWhiteSpace(
                receiverId))
            {
                await Clients.Caller.SendAsync(
                    "CallError",
                    "Your account could not be identified."
                );

                return;
            }


            if (string.IsNullOrWhiteSpace(
                callerId))
            {
                return;
            }


            // --------------------------------------------------------
            // CHECK CALLER CONNECTION
            // --------------------------------------------------------

            if (!ConnectedUsers.TryGetValue(
                callerId,
                out var callerConnections))
            {
                await Clients.Caller.SendAsync(
                    "CallError",
                    "The caller is no longer connected."
                );

                return;
            }


            // --------------------------------------------------------
            // INFORM CALLER
            // --------------------------------------------------------

            foreach (var connectionId
                in callerConnections.Keys)
            {
                await Clients.Client(
                    connectionId
                ).SendAsync(
                    "CallAccepted",
                    receiverId
                );
            }


            // --------------------------------------------------------
            // INFORM RECEIVER
            // --------------------------------------------------------

            await Clients.Caller.SendAsync(
                "CallAccepted",
                receiverId
            );
        }


        // ============================================================
        // DECLINE CALL
        // ============================================================

        public async Task DeclineCall(
            string callerId)
        {
            var receiverId =
                Context.UserIdentifier;


            if (string.IsNullOrWhiteSpace(
                callerId))
            {
                return;
            }


            if (!ConnectedUsers.TryGetValue(
                callerId,
                out var callerConnections))
            {
                return;
            }


            foreach (var connectionId
                in callerConnections.Keys)
            {
                await Clients.Client(
                    connectionId
                ).SendAsync(
                    "CallDeclined",
                    receiverId
                );
            }
        }


        // ============================================================
        // END CALL
        // ============================================================

        public async Task EndCall(
            string otherUserId)
        {
            var currentUserId =
                Context.UserIdentifier;


            if (string.IsNullOrWhiteSpace(
                otherUserId))
            {
                return;
            }


            if (!ConnectedUsers.TryGetValue(
                otherUserId,
                out var connections))
            {
                return;
            }


            foreach (var connectionId
                in connections.Keys)
            {
                await Clients.Client(
                    connectionId
                ).SendAsync(
                    "CallEnded",
                    currentUserId
                );
            }
        }


        // ============================================================
        // WEBRTC VOICE SIGNAL
        // ============================================================

        public async Task SendVoiceSignal(
            string recipientId,
            object signal)
        {
            var senderId =
                Context.UserIdentifier;


            if (string.IsNullOrWhiteSpace(
                senderId))
            {
                return;
            }


            if (string.IsNullOrWhiteSpace(
                recipientId))
            {
                return;
            }


            // --------------------------------------------------------
            // CHECK RECIPIENT
            // --------------------------------------------------------

            if (!ConnectedUsers.TryGetValue(
                recipientId,
                out var connections))
            {
                await Clients.Caller.SendAsync(
                    "CallError",
                    "The other user is no longer connected."
                );

                return;
            }


            // --------------------------------------------------------
            // SEND WEBRTC SIGNAL
            // --------------------------------------------------------

            foreach (var connectionId
                in connections.Keys)
            {
                await Clients.Client(
                    connectionId
                ).SendAsync(
                    "VoiceSignal",
                    senderId,
                    signal
                );
            }
        }
    }
}