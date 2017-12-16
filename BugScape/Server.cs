using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BugScapeCommon;

namespace BugScape {
    public class ClientStates {
        private readonly Dictionary<int, ClientState> _statesByCharacterID = new Dictionary<int, ClientState>();

        private readonly Dictionary<JsonClient, ClientState> _statesByClient = new Dictionary<JsonClient, ClientState>();
        private readonly Dictionary<int, ClientState> _statesByUserID = new Dictionary<int, ClientState>();

        public ClientState this[JsonClient client]
            => client != null && this._statesByClient.ContainsKey(client) ? this._statesByClient[client].Clone() : null;

        public ClientState this[User user]
            =>
            user != null && this._statesByUserID.ContainsKey(user.ID)
            ? this._statesByUserID[user.ID].Clone()
            : null;

        public ClientState this[Character character]
            =>
            character != null && this._statesByCharacterID.ContainsKey(character.ID)
            ? this._statesByCharacterID[character.ID].Clone()
            : null;

        private void _remove(ClientState clientState) {
            if (this._statesByClient.ContainsKey(clientState.Client)) this._statesByClient.Remove(clientState.Client);
            if (clientState.Character != null &&
                this._statesByCharacterID.ContainsKey(clientState.Character.ID)) this._statesByCharacterID.Remove(clientState.Character.ID);
            if (clientState.User != null && this._statesByUserID.ContainsKey(clientState.User.ID)) this._statesByUserID.Remove(clientState.User.ID);
        }

        private void _updateClient(ClientState clientState) {
            this._remove(clientState); /* Remove if exists */
            var clone = clientState.Clone();
            this._statesByClient[clientState.Client] = clone;
            if (clone.Character != null) this._statesByCharacterID[clone.Character.ID] = clone;
            if (clone.User != null) this._statesByUserID[clone.User.ID] = clone;
        }

        public void AddClient(JsonClient client) { this._updateClient(new ClientState(client)); }

        public void UpdateClient(ClientState clientState, User user) {
            clientState.User = user;
            this._updateClient(clientState);
        }

        public void UpdateClient(ClientState clientState, Character character) {
            clientState.Character = character;
            this._updateClient(clientState);
        }

        public void Remove(JsonClient client) { this._remove(this[client]); }

        public void Remove(User user) { this._remove(this[user]); }

        public void Remove(Character character) { this._remove(this[character]); }

        public class ClientState {
            public ClientState(JsonClient client) { this.Client = client; }
            public User User { get; set; }
            public Character Character { get; set; }
            public JsonClient Client { get; }

            public ClientState Clone() {
                return new ClientState(this.Client) {Character = this.Character, User = this.User};
            }
        }
    }

    public class BugScapeServer {
        private readonly ClientStates _clients = new ClientStates();

        private readonly SemaphoreSlim _loginLock = new SemaphoreSlim(1);
        private readonly SemaphoreSlim _registrationLock = new SemaphoreSlim(1);
        private readonly SemaphoreSlim _characterCreateLock = new SemaphoreSlim(1);

        private readonly Dictionary<int, Map> _onlineMaps = new Dictionary<int, Map>();

        private readonly AsyncJsonTcpReactor<BugScapeMessage> _reactor = new AsyncJsonTcpReactor<BugScapeMessage>();

        private readonly Dictionary<Type, UserMessageHandler> _userMessageHandlers;

        public BugScapeServer() {
            this._userMessageHandlers = new Dictionary<Type, UserMessageHandler> {
                {typeof (BugScapeRequestLogin), this.HandleRequestLoginAsync},
                {typeof (BugScapeRequestRegister), this.HandleRequestRegisterAsync},
                {typeof (BugScapeRequestCharacterCreate), this.HandleRequestCharacterCreateAsync},
                {typeof (BugScapeRequestCharacterRemove), this.HandleRequestCharacterRemoveAsync},
                {typeof (BugScapeRequestCharacterEnter), this.HandleRequestCharacterEnterAsync},
                {typeof (BugScapeRequestMove), this.HandleRequestMoveAsync}
            };
        }

        public async Task Run() {
            this._reactor.SetHandler(AsyncJsonTcpReactor<BugScapeMessage>.ReactorAction.Disconnected,
                                     this.HandleUserDisconnectionAsync);
            this._reactor.SetHandler(AsyncJsonTcpReactor<BugScapeMessage>.ReactorAction.ReceivedData,
                                     this.HandleUserMessageAsync);

            /* Load all maps */
            using (var dbContext = new BugScapeDbContext()) {
                /* Convert to list here to prevent database access inside loop from interfering with the maps request */
                foreach (var map in await dbContext.Maps.ToListAsync()) {
                    this._onlineMaps[map.ID] = (Map)map.CloneFromDatabase();
                    this._onlineMaps[map.ID].Characters = new List<Character>();
                }
            }

            /* Run reactor */
            var conn =
            new BugscapeServerConnStrBuilder(
            ConfigurationManager.ConnectionStrings["BugScapeServerConnStr"].ConnectionString);
            await this._reactor.RunServerAsync(IPAddress.Parse(conn.Server), conn.Port);
        }

        private async Task HandleUserDisconnectionAsync(JsonClient client,
                                                        AsyncJsonTcpReactor<BugScapeMessage>.ReactorAction action,
                                                        BugScapeMessage data) {
            if (this._clients[client] != null) {
                await this.RemoveCharacterFromMap(this._clients[client].Character);
                this._clients.Remove(client);
            }
        }
        private async Task HandleUserMessageAsync(JsonClient client,
                                                  AsyncJsonTcpReactor<BugScapeMessage>.ReactorAction action,
                                                  BugScapeMessage data) {
            BugScapeMessage response;
            if (this._userMessageHandlers.ContainsKey(data.GetType())) {
                response = await this._userMessageHandlers[data.GetType()](data, client);
            } else {
                response = new BugScapeMessageUnexpectedError {Message = "Invalid message type"};
            }
            if (response != null) await this._reactor.SendDataAsync(client, response);
        }

        private async Task<BugScapeMessage> HandleRequestRegisterAsync(BugScapeMessage data, JsonClient client) {
            var request = (BugScapeRequestRegister)data;

            using (var dbContext = new BugScapeDbContext()) {
                // Critical Section: Cannot let two users try register at the same time
                await this._registrationLock.WaitAsync();
                try {
                    if (await dbContext.Users.AnyAsync(user => user.Username == request.User.Username)) {
                        return new BugScapeResponseRegisterAlreadyExist();
                    }

                    var createdUser = new User {
                        Username = request.User.Username,
                        Password = new HashedPassword(request.Password)
                    };

                    dbContext.Users.Add(createdUser);
                    await dbContext.SaveChangesAsync();
                } finally {
                    this._registrationLock.Release();
                }
                // End of critical section
            }

            return new BugScapeResponseRegisterSuccessful();
        }
        private async Task<BugScapeMessage> HandleRequestLoginAsync(BugScapeMessage data, JsonClient client) {
            var request = (BugScapeRequestLogin)data;
            using (var dbContext = new BugScapeDbContext()) {
                var matchingUser = await dbContext.Users.FirstOrDefaultAsync(user => user.Username == request.Username);

                if (matchingUser == null) {
                    return new BugScapeResponseLoginInvalidCredentials();
                }
                
                if (!matchingUser.Password.Compare(request.Password)) {
                    return new BugScapeResponseLoginInvalidCredentials();
                }

                // Critical Section: Cannot let two users try login at the same time
                await this._loginLock.WaitAsync();
                try {
                    if (this._clients[matchingUser] != null) {
                        return new BugScapeResponseLoginAlreadyLoggedIn();
                    }
                    this._clients.AddClient(client);
                    this._clients.UpdateClient(this._clients[client], (User)matchingUser.CloneFromDatabase());
                } finally {
                    this._loginLock.Release();
                }
                // End of critical section
            }

            return new BugScapeResponseLoginSuccessful { User = this._clients[client].User };
        }
        private async Task<BugScapeMessage> HandleRequestCharacterCreateAsync(BugScapeMessage data, JsonClient client) {
            var request = (BugScapeRequestCharacterCreate)data;

            if (this._clients[client]?.User == null) {
                return new BugScapeMessageUnexpectedError {Message = "Client is not logged in"};
            }

            using (var dbContext = new BugScapeDbContext()) {
                var loggedUser = this._clients[client].User;
                var matchingUser = await dbContext.Users.FirstAsync(user => user.ID == loggedUser.ID);
                var spawnMap = await dbContext.Maps.FirstAsync(map => map.IsNewCharacterMap);

                // Critical Section: Cannot let two users try create character at the same time
                await this._characterCreateLock.WaitAsync();
                try {
                    if (
                    await dbContext.Characters.AnyAsync(character => character.DisplayName == request.Character.DisplayName)) {
                        return new BugScapeResponseCharacterCreateAlreadyExist();
                    }

                    dbContext.Characters.Add(new Character {
                        DisplayName = request.Character.DisplayName,
                        Color = request.Character.Color,
                        User = matchingUser,
                        Map = spawnMap,
                        Location = new Point2D(1, 1),
                        Size = new Point2D(50, 50),
                        Speed = 50
                    });
                    await dbContext.SaveChangesAsync();
                } finally {
                    this._characterCreateLock.Release();
                }
                // End of critical section

                this._clients.UpdateClient(this._clients[client], (User)matchingUser.CloneFromDatabase());
            }

            return new BugScapeResponseCharacterCreateSuccessful {User = this._clients[client].User};
        }
        private async Task<BugScapeMessage> HandleRequestCharacterRemoveAsync(BugScapeMessage data, JsonClient client) {
            var request = (BugScapeRequestCharacterRemove)data;

            if (this._clients[client]?.User == null) {
                return new BugScapeMessageUnexpectedError {Message = "Client is not logged in"};
            }

            if (
            this._clients[client].User.Characters.All(
                                                      character =>
                                                      character.ID != request.Character.ID)) {
                return new BugScapeMessageUnexpectedError {Message = "Character not found"};
            }

            using (var dbContext = new BugScapeDbContext()) {
                var matchingCharacter =
                await
                dbContext.Characters.FirstAsync(character => character.ID == request.Character.ID);
                dbContext.Characters.Remove(matchingCharacter);
                await dbContext.SaveChangesAsync();
                var oldUser = this._clients[client].User;
                var newUser = await dbContext.Users.FirstAsync(user => user.ID == oldUser.ID);
                this._clients.UpdateClient(this._clients[client], (User)newUser.CloneFromDatabase());
            }

            return new BugScapeRequestCharacterRemoveSuccessful() { User = this._clients[client].User };
        }
        private async Task<BugScapeMessage> HandleRequestCharacterEnterAsync(BugScapeMessage data, JsonClient client) {
            var request = (BugScapeRequestCharacterEnter)data;

            if (this._clients[client]?.User == null) {
                return new BugScapeMessageUnexpectedError { Message = "Client is not logged in" };
            }
            if (this._clients[client]?.Character != null) {
                return new BugScapeMessageUnexpectedError { Message = "Client is already in-game" };
            }

            if (
            this._clients[client].User.Characters.All(
                                                      character =>
                                                      character.ID != request.Character.ID)) {
                return new BugScapeMessageUnexpectedError {Message = "Character not found"};
            }

            this._clients.UpdateClient(this._clients[client],
                                       this._clients[client].User.Characters.First(
                                                                                   character =>
                                                                                   character.ID ==
                                                                                   request.Character.ID));

            int mapID;
            using (var dbContext = new BugScapeDbContext()) {
                var matchingCharacter =
                await
                dbContext.Characters.FirstAsync(character => character.ID == request.Character.ID);
                mapID = matchingCharacter.Map.ID;
            }
            await this.SpawnCharacterInMap(this._clients[client].Character, this._onlineMaps[mapID]);
            
            return new BugScapeResponseCharacterEnterSuccessful {
                Map = this._clients[client].Character.Map,
                Character = this._clients[client].Character
            };
        }
        private async Task<BugScapeMessage> HandleRequestMoveAsync(BugScapeMessage data, JsonClient client) {
            var request = (BugScapeRequestMove)data;
            this._clients[client]?.Character?.Move(request.Direction, request.MoveMax);
            await this.MapUpdated(this._clients[client]?.Character?.Map);
            return null;
        }

        private async Task MapUpdated(Map map) {
            if (map == null) return;
            var update = new BugScapeResponseMapChanged {Map = map};
            await
            Task.WhenAll(
                         map.Characters.Select(
                                               character =>
                                               this._reactor.SendDataAsync(this._clients[character].Client, update)));
        }
        
        private async Task SpawnCharacterInMap(Character character, Map map) {
            /* Set map to character */
            character.Map = map;

            /* Add character to map */
            map.Characters.Add(character);

            /* Send update */
            await this.MapUpdated(map);

            await Task.Delay(0); /* To avoid warning */
        }
        private async Task RemoveCharacterFromMap(Character character) {
            if (character?.Map == null) return;

            /* Remove character from map */
            character.Map.Characters.Remove(character);

            /* Send update */
            await this.MapUpdated(character.Map);

            /* Remove map from character */
            character.Map = null;

            await Task.Delay(0); /* To avoid warning */
        }

        private delegate Task<BugScapeMessage> UserMessageHandler(BugScapeMessage data, JsonClient client);
    }
}
