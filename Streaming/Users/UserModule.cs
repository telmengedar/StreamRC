using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Timer;
using StreamRC.Streaming.Stream;

namespace StreamRC.Streaming.Users {

    /// <summary>
    /// module managing users
    /// </summary>
    [Dependency(nameof(TimerModule), DependencyType.Type)]
    [Dependency(nameof(StreamModule), DependencyType.Type)]
    public class UserModule : IInitializableModule, IRunnableModule, ITimerService {
        readonly Context context;
        readonly object userlock = new object();

        readonly List<UserCacheEntry> users = new List<UserCacheEntry>();
        readonly Dictionary<UserKey, UserCacheEntry> usersbyname = new Dictionary<UserKey, UserCacheEntry>();
        readonly Dictionary<long, UserCacheEntry> usersbyid = new Dictionary<long, UserCacheEntry>();   
         
        /// <summary>
        /// creates a new <see cref="UserModule"/>
        /// </summary>
        /// <param name="context"></param>
        public UserModule(Context context) {
            this.context = context;
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
        /// get user information by key
        /// </summary>
        /// <param name="service">service user is linked to</param>
        /// <param name="keys">user keys (eg. ids)</param>
        /// <returns>user information</returns>
        public User[] GetUsersByKey(string service, params string[] keys) {
            return context.Database.LoadEntities<User>().Where(u => u.Service == service && keys.Contains(u.Key)).Execute().ToArray();
        }

        /// <summary>
        /// updates the link to the avatar of the user
        /// </summary>
        /// <param name="user">user of which to update avatar</param>
        /// <param name="url">link to avatar image</param>
        public void UpdateUserAvatar(User user, string url) {
            lock(userlock) {
                context.Database.Update<User>().Set(u => u.Avatar == url).Where(u => u.ID == user.ID).Execute();
                user.Avatar = url;
            }
        }

        /// <summary>
        /// updates the color for username representation
        /// </summary>
        /// <param name="user">user of which to update avatar</param>
        /// <param name="color">color of user</param>
        public void UpdateUserColor(User user, string color) {
            lock(userlock) {
                context.Database.Update<User>().Set(u => u.Color == color).Where(u => u.ID == user.ID).Execute();
                user.Color = color;
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
                UserCacheEntry cacheentry;
                usersbyname.TryGetValue(key, out cacheentry);
                if (cacheentry == null)
                {
                    User user = context.Database.LoadEntities<User>().Where(u => u.Service == key.Service && u.Name == key.Username).Execute().FirstOrDefault();
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
                UserCacheEntry cacheentry;
                usersbyname.TryGetValue(key, out cacheentry);
                if(cacheentry == null) {
                    User user = context.Database.LoadEntities<User>().Where(u => u.Service == key.Service && u.Name == key.Username).Execute().FirstOrDefault();
                    if(user == null) {
                        user = new User {
                            Service = service,
                            Name = name,
                            ID = context.Database.Insert<User>().Columns(u => u.Service, u => u.Name, u => u.Status).Values(service, name, UserStatus.Free).ReturnID().Execute()
                        };
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
                    context.Database.Update<User>().Set(u => u.Key == key).Where(u => u.ID == user.ID).Execute();
                }
                return user;
            }
        }

        /// <summary>
        /// get a user by its user id
        /// </summary>
        /// <param name="userid">id of user</param>
        /// <returns>user</returns>
        public User GetUser(long userid) {
            lock(userlock) {
                UserCacheEntry cacheentry;
                usersbyid.TryGetValue(userid, out cacheentry);
                if(cacheentry == null) {
                    User user = context.Database.LoadEntities<User>().Where(u => u.ID == userid).Execute().FirstOrDefault();
                    cacheentry = AddUserToCache(user);
                }
                cacheentry.LifeTime = 300.0;
                return cacheentry.User;
            }
        }

        void IInitializableModule.Initialize() {
            context.Database.UpdateSchema<User>();

            context.GetModule<StreamModule>().NewFollower += OnFollower;
            context.GetModule<StreamModule>().NewSubscriber += OnSubscriber;
            context.GetModule<StreamModule>().ChatMessage += OnChatMessage;
        }

        void OnChatMessage(ChatMessage message) {
            User user = GetExistingUser(message.Service, message.User);

            string color = $"#{message.UserColor.R.ToString("X2")}{message.UserColor.G.ToString("X2")}{message.UserColor.B.ToString("X2")}";
            if(user.Color != color)
                UpdateUserColor(user, color);
        }

        public void ResetUserStatus(string service) {
            context.Database.Update<User>().Set(u => u.Status == UserStatus.Free).Where(u=>u.Service==service).Execute();
        }

        public UserStatus GetUserStatus(string service, string user) {
            return context.Database.Load<User>(u => u.Status).Where(u => u.Service==service && u.Name == user).ExecuteScalar<UserStatus>();
        }

        public void SetUserStatus(string service, string user, UserStatus status) {
            // enforce that user exists
            GetUser(service, user);
            context.Database.Update<User>().Set(u => u.Status == status).Where(u => u.Service==service && u.Name == user).Execute();
        }

        void OnSubscriber(SubscriberInformation user) {
            SetUserStatus(user.Service, user.Username, user.Status);
        }

        void OnFollower(UserInformation user) {
            SetUserStatus(user.Service, user.Username, UserStatus.Follower);
        }

        void IRunnableModule.Start() {
            context.GetModule<TimerModule>().AddService(this, 1.0);
        }

        void IRunnableModule.Stop() {
            context.GetModule<TimerModule>().RemoveService(this);
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
    }
}