using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Modules;
using StreamRC.Core;
using StreamRC.Core.Http;
using StreamRC.Core.Timer;
using StreamRC.Streaming.Extensions;

namespace StreamRC.Streaming.Users {

    /// <summary>
    /// module managing users
    /// </summary>
    [Module(Key="users", AutoCreate = true)]
    public class UserModule : ITimerService, IHttpService {
        readonly DatabaseModule database;
        readonly object userlock = new object();

        readonly List<UserCacheEntry> users = new List<UserCacheEntry>();
        readonly Dictionary<UserKey, UserCacheEntry> usersbyname = new Dictionary<UserKey, UserCacheEntry>();
        readonly Dictionary<long, UserCacheEntry> usersbyid = new Dictionary<long, UserCacheEntry>();

        readonly HashSet<UserKey> activeusers=new HashSet<UserKey>();

        /// <summary>
        /// creates a new <see cref="UserModule"/>
        /// </summary>
        /// <param name="context"></param>
        public UserModule(DatabaseModule database, IHttpServiceModule httpservice, TimerModule timer) {
            this.database = database;
            database.Database.UpdateSchema<User>();
            httpservice.AddServiceHandler("/streamrc/users/flag", this);
            timer.AddService(this, 1.0);
        }

        /// <summary>
        /// triggered when the flags of a user have changed
        /// </summary>
        public event Action<User> UserFlagsChanged;

        /// <summary>
        /// number of users currently active in channel (chat)
        /// </summary>
        public int ActiveUserCount => activeusers.Count;

        /// <summary>
        /// users currently active in channel
        /// </summary>
        public IEnumerable<UserKey> ActiveUsers
        {
            get
            {
                lock(userlock)
                    foreach(UserKey user in activeusers)
                        yield return user;
            }
        }

        UserCacheEntry AddUserToCache(User user) {
            UserCacheEntry cacheentry = new UserCacheEntry(user);
            usersbyname[new UserKey(user.Service, user.Name)] = cacheentry;
            usersbyid[user.ID] = cacheentry;
            users.Add(new UserCacheEntry(user));
            return cacheentry;
        }

        /// <summary>
        /// get id of user
        /// </summary>
        /// <param name="service">service of user</param>
        /// <param name="name">username</param>
        /// <returns>id of specified user if a user is found, 0 otherwise</returns>
        public long GetUserID(string service, string name) {
            lock(userlock)
                return GetUser(service, name).ID;
        }

        /// <summary>
        /// find ids of users specified in name enumeration
        /// </summary>
        /// <param name="names">names to search for</param>
        /// <returns>ids of matching users</returns>
        public IEnumerable<long> FindUserIDs(IEnumerable<string> names) {
            return database.Database.Load<User>(u => u.ID).Where(u => names.Contains(u.Name)).ExecuteSet<long>();
        }

        /// <summary>
        /// get user information by key
        /// </summary>
        /// <param name="service">service user is linked to</param>
        /// <param name="keys">user keys (eg. ids)</param>
        /// <returns>user information</returns>
        public User[] GetUsersByKey(string service, params string[] keys) {
            return database.Database.LoadEntities<User>().Where(u => u.Service == service && keys.Contains(u.Key)).Execute().ToArray();
        }

        /// <summary>
        /// updates the link to the avatar of the user
        /// </summary>
        /// <param name="user">user of which to update avatar</param>
        /// <param name="url">link to avatar image</param>
        public void UpdateUserAvatar(User user, string url) {
            lock(userlock) {
                database.Database.Update<User>().Set(u => u.Avatar == url).Where(u => u.ID == user.ID).Execute();
                user.Avatar = url;
            }
        }

        /// <summary>
        /// updates the color for username representation
        /// </summary>
        /// <param name="service">service user is registered to</param>
        /// <param name="user">user of which to update avatar</param>
        /// <param name="color">color of user</param>
        public void UpdateUserColor(string service, string user, string color) {
            lock(userlock) {
                UpdateUserColor(GetUser(service, user), color.ParseColor());
            }
        }

        /// <summary>
        /// updates the color for username representation
        /// </summary>
        /// <param name="user">user of which to update avatar</param>
        /// <param name="color">color of user</param>
        public void UpdateUserColor(User user, string color) {
            lock(userlock) {
                database.Database.Update<User>().Set(u => u.Color == color).Where(u => u.ID == user.ID).Execute();
                user.Color = color;
            }
        }

        /// <summary>
        /// updates the color for username representation
        /// </summary>
        /// <param name="user">user of which to update avatar</param>
        /// <param name="color">color of user</param>
        public void UpdateUserColor(User user, Color color)
        {
            UpdateUserColor(user, $"#{color.R:X2}{color.G:X2}{color.B:X2}");
        }

        /// <summary>
        /// get color used to display a username
        /// </summary>
        /// <param name="service">service user is registered to</param>
        /// <param name="user">name of user</param>
        /// <returns>custom color string registered to user</returns>
        public string GetUserColor(string service, string user) {
            lock(userlock) {
                return GetUser(service, user).Color;
            }
        }

        /// <summary>
        /// get a user existing in database or throws an exception if user is not found
        /// </summary>
        /// <param name="service">service user is linked to</param>
        /// <param name="name">name of user</param>
        /// <returns>user object</returns>
        public User GetExistingUser(string service, string name) {
            lock(userlock) {
                UserKey key = new UserKey(service, name);
                usersbyname.TryGetValue(key, out UserCacheEntry cacheentry);
                if (cacheentry == null)
                {
                    User user = database.Database.LoadEntities<User>().Where(u => u.Service == key.Service && u.Name == key.Username).Execute().FirstOrDefault();
                    if(user == null)
                        throw new Exception($"User '{name}' for service '{service}' not found.");

                    cacheentry = AddUserToCache(user);
                }
                cacheentry.LifeTime = 300.0;
                return cacheentry.User;
            }
        }

        /// <summary>
        /// get a user by service and username
        /// </summary>
        /// <param name="service">service user is registered to</param>
        /// <param name="name">username</param>
        /// <returns>user</returns>
        public User GetUser(string service, string name) {
            lock(userlock) {
                UserKey key = new UserKey(service, name);
                usersbyname.TryGetValue(key, out UserCacheEntry cacheentry);
                if(cacheentry == null) {
                    User user = database.Database.LoadEntities<User>().Where(u => u.Service == key.Service && u.Name == key.Username).Execute().FirstOrDefault();
                    if(user == null) {
                        database.Database.Insert<User>().Columns(u => u.Service, u => u.Name, u => u.Status).Values(service, name, UserStatus.Free).Execute();
                        user = database.Database.LoadEntities<User>().Where(u => u.Service == key.Service && u.Name == key.Username).Execute().FirstOrDefault();
                    }
                    cacheentry = AddUserToCache(user);
                }
                cacheentry.LifeTime = 300.0;
                return cacheentry.User;
            }
        }

        /// <summary>
        /// adds a user to database or updates key information
        /// </summary>
        /// <param name="service">service user is linked to</param>
        /// <param name="name">username</param>
        /// <param name="key">service related key of user</param>
        /// <returns>user data</returns>
        public User AddOrUpdateUser(string service, string name, string key = null) {
            lock(userlock) {
                User user = GetUser(service, name);
                if(key != null && user.Key != key) {
                    user.Key = key;
                    database.Database.Update<User>().Set(u => u.Key == key).Where(u => u.ID == user.ID).Execute();
                }
                return user;
            }
        }

        /// <summary>
        /// sets flags for a user
        /// </summary>
        /// <param name="service">service name</param>
        /// <param name="username">username</param>
        /// <param name="flag">flag to be set</param>
        public void SetFlags(string service, string username, UserFlags flag)
        {
            User user = GetExistingUser(service, username);
            user.Flags |= flag;
            database.Database.Update<User>().Set(u => u.Flags == user.Flags).Where(u => u.ID == user.ID).Execute();
            NightlyCode.Core.Logs.Logger.Info(this, $"{service}/{username} is now flagged as {flag}.");
            UserFlagsChanged?.Invoke(user);
        }

        /// <summary>
        /// sets flags for a user
        /// </summary>
        /// <param name="service">service name</param>
        /// <param name="username">username</param>
        /// <param name="flag">flag to be reset</param>
        public void ResetFlags(string service, string username, UserFlags flag)
        {
            User user = GetExistingUser(service, username);
            user.Flags &= ~flag;
            database.Database.Update<User>().Set(u => u.Flags == user.Flags).Where(u => u.ID == user.ID).Execute();
            NightlyCode.Core.Logs.Logger.Info(this, $"{service}/{username} is not flagged as {flag} anymore.");
        }

        /// <summary>
        /// get a user by its user id
        /// </summary>
        /// <param name="userid">id of user</param>
        /// <returns>user</returns>
        public User GetUser(long userid) {
            lock(userlock) {
                usersbyid.TryGetValue(userid, out UserCacheEntry cacheentry);
                if(cacheentry == null) {
                    User user = database.Database.LoadEntities<User>().Where(u => u.ID == userid).Execute().FirstOrDefault();
                    cacheentry = AddUserToCache(user);
                }
                cacheentry.LifeTime = 300.0;
                return cacheentry.User;
            }
        }

        public void BeginInitialization(string service) {
            database.Database.Update<User>().Set(u => u.Flags == (u.Flags|UserFlags.Initializing)).Where(u=>u.Service==service).Execute();
        }

        public void EndInitialization(string service) {
            UserFlags flags = ~UserFlags.Initializing;
            database.Database.Update<User>().Set(u => u.Status==UserStatus.Free, u=> u.Flags==(u.Flags & flags)).Where(u => u.Service == service && (u.Flags&UserFlags.Initializing)==UserFlags.Initializing).Execute();
        }

        public UserStatus GetUserStatus(string service, string user) {
            return database.Database.Load<User>(u => u.Status).Where(u => u.Service==service && u.Name == user).ExecuteScalar<UserStatus>();
        }

        public void SetInitialized(string service, string user) {
            UserFlags flags = ~UserFlags.Initializing;
            database.Database.Update<User>().Set(u => u.Flags == (u.Flags & flags)).Where(u => u.Service == service && u.Name == user).Execute();
        }

        public bool SetUserStatus(string service, string user, UserStatus status) {
            // enforce that user exists
            GetUser(service, user);
            

            return database.Database.Update<User>().Set(u => u.Status == status).Where(u => u.Service == service && u.Name == user && u.Status < status).Execute() > 0;
        }

        public void UserJoined(string service, string user) {
            lock (userlock)
                activeusers.Add(new UserKey(service, user));
        }

        public void UserLeft(string service, string user) {
            lock (userlock)
                activeusers.Remove(new UserKey(service, user));
        }

        void ITimerService.Process(double time) {
            lock(userlock) {
                for(int i = users.Count - 1; i >= 0; --i) {
                    UserCacheEntry user = users[i];
                    user.LifeTime -= time;
                    if(user.LifeTime <= 0.0) {
                        usersbyname.Remove(new UserKey(user.User.Service, user.User.Name));
                        usersbyid.Remove(user.User.ID);
                        users.RemoveAt(i);
                    }
                }
            }
        }

        void IHttpService.ProcessRequest(IHttpRequest request, IHttpResponse response) {
            switch(request.Resource) {
                case "/streamrc/users/flag":
                    ServeUserFlagImage(response, (UserFlags)request.GetParameter<int>("id"));
                    break;
            }
        }

        void ServeUserFlagImage(IHttpResponse response, UserFlags flag) {
            response.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>($"StreamRC.Streaming.Users.Flags.{flag.ToString().ToLower()}.png"), ".png");
        }
    }
}