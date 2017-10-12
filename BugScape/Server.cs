using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BugScapeCommon;
using Newtonsoft.Json;

namespace BugScape {
    public class BugScapeServer {
        private readonly CancellationToken _cancel = new CancellationToken();
        private static readonly SemaphoreSlim RegistrationLock = new SemaphoreSlim(1);
        
        private static readonly Dictionary<Type, AsyncJsonTcpReactor<BugScapeRequest, BugScapeResponse>.RequestHandler> HandlerDictionary = new Dictionary<Type, AsyncJsonTcpReactor<BugScapeRequest, BugScapeResponse>.RequestHandler>
        {
            {typeof(BugScapeRequestMapState), HandleRequestMapStateAsync},
            {typeof(BugScapeRequestMove), HandleRequestMoveAsync},
            {typeof(BugScapeRequestRegister), HandleRequestRegisterAsync},
            {typeof(BugScapeRequestLogin), HandleRequestLoginAsync},
        };

        public async Task Run() {
            var reactor = new AsyncJsonTcpReactor<BugScapeRequest, BugScapeResponse>(IPAddress.Any, ServerSettings.ServerPort);
            foreach (var entry in HandlerDictionary) {
                reactor.SetHandler(entry.Key, entry.Value);
            }
            await reactor.Run();
        }
        
        private static async Task<BugScapeResponse> HandleRequestMapStateAsync(BugScapeRequest request) {
            using (var dbContext = new BugScapeDbContext()) {
                var specRequest = request as BugScapeRequestMapState;
                if (specRequest == null) return new BugScapeResponse(EBugScapeResult.Error);
                var character = await dbContext.Characters.FindAsync(specRequest.CharacterID);
                if (character == null) return new BugScapeResponseMapState(null);
                await dbContext.Entry(character).Reference(c => c.Map).LoadAsync();
                await dbContext.Entry(character.Map).Collection(c => c.Characters).LoadAsync();
                return new BugScapeResponseMapState(character.Map);
            }
        }
        private static async Task<BugScapeResponse> HandleRequestMoveAsync(BugScapeRequest request) {
            using (var dbContext = new BugScapeDbContext()) {
                var specRequest = request as BugScapeRequestMove;
                if (specRequest == null) return new BugScapeResponse(EBugScapeResult.Error);
                var character = await dbContext.Characters.FindAsync(specRequest.CharacterID);
                character?.Move(specRequest.Direction);
                await dbContext.SaveChangesAsync();
                return new BugScapeResponse(EBugScapeResult.Success);
            }
        }
        private static async Task<BugScapeResponse> HandleRequestRegisterAsync(BugScapeRequest request) {
            using (var dbContext = new BugScapeDbContext()) {
                var specRequest = request as BugScapeRequestRegister;
                if (specRequest == null) return new BugScapeResponse(EBugScapeResult.Error);

                // Critical Section: Cannot let two users try register at the same time
                await RegistrationLock.WaitAsync();
                if (
                await (from user in dbContext.Users where user.Username == specRequest.Username select user).AnyAsync()) {
                    return new BugScapeResponse(EBugScapeResult.ErrorUserAlreadyExists);
                }

                var createdUser = new User {
                    Username = specRequest.Username,
                    PasswordSalt = new byte[ServerSettings.PasswordHashSaltLength]
                };
                new RNGCryptoServiceProvider().GetBytes(createdUser.PasswordSalt);
                createdUser.PasswordHash = HashPasswordCalculate(specRequest.Password, createdUser.PasswordSalt);

                dbContext.Users.Add(createdUser);
                await dbContext.SaveChangesAsync();
                RegistrationLock.Release();
                // End of critical section

                dbContext.Characters.Add(new Character() {
                    Map = await dbContext.Maps.FindAsync(1), // Set to map #1
                    Location = new Point2D(0, 0),
                    User = createdUser
                });

                return new BugScapeResponse(EBugScapeResult.Success);
            }
        }
        private static async Task<BugScapeResponse> HandleRequestLoginAsync(BugScapeRequest request) {
            using (var dbContext = new BugScapeDbContext()) {
                var specRequest = request as BugScapeRequestLogin;
                if (specRequest == null) return new BugScapeResponse(EBugScapeResult.Error);

                var matchingUsers =
                await
                (from user in dbContext.Users where user.Username == specRequest.Username select user).Include(user => user.Characters).ToListAsync();

                if (!matchingUsers.Any()) {
                    return new BugScapeResponse(EBugScapeResult.ErrorInvalidCredentials);
                }

                var matchingUser = matchingUsers.First();
                return HashPasswordCompare(specRequest.Password, matchingUser.PasswordSalt, matchingUser.PasswordHash)
                       ? new BugScapeResponseUser(matchingUser)
                       : new BugScapeResponse(EBugScapeResult.ErrorInvalidCredentials);
            }
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
