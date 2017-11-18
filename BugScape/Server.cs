using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using BugScapeCommon;

namespace BugScape {
    public class BugScapeServer {
        private static readonly SemaphoreSlim RegistrationLock = new SemaphoreSlim(1);
        private static readonly SemaphoreSlim LoginLock = new SemaphoreSlim(1);
        private static readonly AsyncJsonTcpReactor<BugScapeMessage> Reactor = new AsyncJsonTcpReactor<BugScapeMessage>();

        private static readonly Dictionary<int, Map> OnlineMaps = new Dictionary<int, Map>();
        private static readonly Dictionary<JsonClient, Character> OnlineCharacters = new Dictionary<JsonClient, Character>();
        private static readonly HashSet<int> LoggedInUserIDs = new HashSet<int>();

        public static async Task Run() {
            Reactor.SetHandler(AsyncJsonTcpReactor<BugScapeMessage>.ReactorAction.Disconnected, HandleUserDisconnectionAsync);
            Reactor.SetHandler(AsyncJsonTcpReactor<BugScapeMessage>.ReactorAction.ReceivedData, HandleUserMessageAsync);

            /* Load all maps */
            using (var dbContext = new BugScapeDbContext()) {
                foreach (var map in dbContext.Maps) {
                    OnlineMaps[map.MapID] = map.CloneToServer();
                    OnlineMaps[map.MapID].Characters = new List<Character>();
                }
            }

            /* Run reactor */
            await Reactor.RunServerAsync(IPAddress.Any, ServerSettings.ServerPort);
        }
        
        private static async Task HandleUserDisconnectionAsync(JsonClient client,
                                                       AsyncJsonTcpReactor<BugScapeMessage>.ReactorAction action,
                                                       BugScapeMessage data) {
            if (OnlineCharacters.ContainsKey(client)) {
                await RemoveCharacterFromMap(OnlineCharacters[client]);
                LoggedInUserIDs.Remove(OnlineCharacters[client].User.UserID);
                OnlineCharacters.Remove(client);
            }
        }
        private static async Task HandleUserMessageAsync(JsonClient client,
                                                       AsyncJsonTcpReactor<BugScapeMessage>.ReactorAction action,
                                                       BugScapeMessage data) {
            if (data is BugScapeRequestLogin) {
                var response = await HandleRequestLoginAsync((BugScapeRequestLogin)data, client);
                await Reactor.SendDataAsync(client, response);
            } else if (data is BugScapeRequestRegister) {
                var response = await HandleRequestRegisterAsync((BugScapeRequestRegister)data);
                await Reactor.SendDataAsync(client, response);
            } else if (data is BugScapeRequestMove) {
                OnlineCharacters[client].Move(((BugScapeRequestMove)data).Direction);
                await MapUpdated(OnlineCharacters[client].Map);
            }
        }

        private static async Task<BugScapeMessage> HandleRequestRegisterAsync(BugScapeRequestRegister request) {
            using (var dbContext = new BugScapeDbContext()) {
                User createdUser;

                // Critical Section: Cannot let two users try register at the same time
                await RegistrationLock.WaitAsync();
                try {
                    if (
                    await (from user in dbContext.Users where user.Username == request.Username select user).AnyAsync()) {
                        return new BugScapeResponseRegisterAlreadyExist();
                    }

                    createdUser = new User {
                        Username = request.Username,
                        PasswordSalt = new byte[ServerSettings.PasswordHashSaltLength]
                    };
                    new RNGCryptoServiceProvider().GetBytes(createdUser.PasswordSalt);
                    createdUser.PasswordHash = HashPasswordCalculate(request.Password, createdUser.PasswordSalt);

                    var createdCharacter = new Character {
                        User = createdUser,
                        Map = dbContext.Maps.FirstOrDefault(m => m.MapID == 1),
                        Location = new Point2D()
                    };

                    dbContext.Characters.Add(createdCharacter);
                    dbContext.Users.Add(createdUser);
                    await dbContext.SaveChangesAsync();
                } finally {
                    RegistrationLock.Release();
                }
                // End of critical section

                dbContext.Characters.Add(new Character() {
                    Map = await dbContext.Maps.FindAsync(1), // Set to map #1
                    Location = new Point2D(0, 0),
                    User = createdUser
                });

                return new BugScapeResponseRegisterSuccessful();
            }
        }
        private static async Task<BugScapeMessage> HandleRequestLoginAsync(BugScapeRequestLogin request, JsonClient client) {
            using (var dbContext = new BugScapeDbContext()) {
                var matchingUsers =
                await
                (from user in dbContext.Users where user.Username == request.Username select user).ToListAsync();

                if (!matchingUsers.Any()) {
                    return new BugScapeResponseLoginInvalidCredentials();
                }

                var matchingUser = matchingUsers.First();
                if (!HashPasswordCompare(request.Password, matchingUser.PasswordSalt, matchingUser.PasswordHash)) {
                    return new BugScapeResponseLoginInvalidCredentials();
                }

                // Critical Section: Cannot let two users try login at the same time
                await LoginLock.WaitAsync();
                try {
                    if (LoggedInUserIDs.Contains(matchingUser.UserID)) {
                        return new BugScapeResponseLoginAlreadyLoggedIn();
                    }
                    LoggedInUserIDs.Add(matchingUser.UserID);
                } finally {
                    LoginLock.Release();
                }
                // End of critical section

                var character = await LoadCharacter(matchingUser.Characters.First(), client);
                return new BugScapeResponseLoginSuccessful(character);
            }
        }

        private static async Task MapUpdated(Map map) {
            if (map == null) return;

            var update = new BugScapeUpdateMapChanged(map);
            await Task.WhenAll(map.Characters.Select(character => Reactor.SendDataAsync(character.Client, update)));
        }

        private static async Task<Character> LoadCharacter(Character character, JsonClient client) {
            if (OnlineCharacters.ContainsKey(client)) return null; /* Client already has a character online */
            
            /* Load character */
            OnlineCharacters[client] = character.CloneToServer();
            OnlineCharacters[client].Client = client;

            /* Spawn character */
            await SpawnCharacterInMap(OnlineCharacters[client], OnlineMaps[character.Map.MapID]);

            await Task.Delay(0); /* To avoid warning */
            return OnlineCharacters[client];
        }
        private static async Task SpawnCharacterInMap(Character character, Map map) {
            /* Set map to character */
            character.Map = map;

            /* Add character to map */
            map.Characters.Add(character);

            /* Send update */
            await MapUpdated(map);

            await Task.Delay(0); /* To avoid warning */
        }
        private static async Task RemoveCharacterFromMap(Character character) {
            /* Remove character from map */
            character.Map.Characters.Remove(character);

            /* Send update */
            await MapUpdated(character.Map);

            /* Remove map from character */
            character.Map = null;

            await Task.Delay(0); /* To avoid warning */
        }

        private static byte[] HashPasswordCalculate(string password, byte[] passwordSalt) {
            var hash = new Rfc2898DeriveBytes(password, passwordSalt) {
                IterationCount = ServerSettings.PasswordHashIterations
            };
            return hash.GetBytes(ServerSettings.PasswordHashLength);
        }
        private static bool HashPasswordCompare(string password, byte[] passwordSalt, byte[] passwordHash) {
            var hashed = HashPasswordCalculate(password, passwordSalt);

            // Check every byte to eliminate comparing time attacks
            var diff = passwordHash.Length ^ hashed.Length;
            for (var i = 0; i < passwordHash.Length && i < hashed.Length; i++) diff |= passwordHash[i] ^ hashed[i];
            return diff == 0;
        }
    }
}
