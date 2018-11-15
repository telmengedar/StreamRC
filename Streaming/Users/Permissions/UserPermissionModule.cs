using NightlyCode.Core.Logs;
using NightlyCode.Database.Entities.Operations.Fields;
using NightlyCode.Modules;
using StreamRC.Core;

namespace StreamRC.Streaming.Users.Permissions {

    /// <summary>
    /// module managing custom permissions for users
    /// </summary>
    [Module(Key="permissions")]
    public class UserPermissionModule {
        readonly DatabaseModule database;
        readonly UserModule usermodule;

        /// <summary>
        /// creates a new <see cref="UserPermissionModule"/>
        /// </summary>
        /// <param name="database">access to database</param>
        /// <param name="usermodule">access to user data</param>
        public UserPermissionModule(DatabaseModule database, UserModule usermodule) {
            this.database = database;
            this.usermodule = usermodule;
            database.Database.UpdateSchema<UserPermission>();
        }

        /// <summary>
        /// determines whether a user has a permission
        /// </summary>
        /// <param name="service">service user is registered to</param>
        /// <param name="username">name of user</param>
        /// <param name="permission">permission to check</param>
        /// <returns>true if user has permission, false otherwise</returns>
        public bool HasPermission(string service, string username, string permission) {
            return HasPermission(usermodule.GetUserID(service, username), permission);
        }

        /// <summary>
        /// determines whether a user has a permission
        /// </summary>
        /// <param name="userid">id of user</param>
        /// <param name="permission">permission to check</param>
        /// <returns></returns>
        public bool HasPermission(long userid, string permission) {
            return database.Database.Load<UserPermission>(DBFunction.Count).Where(p => p.UserID == userid && p.Permission == permission).ExecuteScalar<long>() > 0;

        }

        /// <summary>
        /// adds a permission to the database
        /// </summary>
        /// <param name="service">service user is registered to</param>
        /// <param name="user">name of user</param>
        /// <param name="permission">permission to add</param>
        public void AddPermission(string service, string user, string permission) {
            AddPermission(usermodule.GetUserID(service, user), permission);
        }

        /// <summary>
        /// adds a permission to the database
        /// </summary>
        /// <param name="userid">id of user</param>
        /// <param name="permission">permission to add</param>
        public void AddPermission(long userid, string permission) {
            database.Database.Insert<UserPermission>().Columns(u => u.UserID, u => u.Permission).Values(userid, permission).Execute();
            Logger.Info(this, $"Permission '{permission}' added for user '{userid}'");
        }

        /// <summary>
        /// removes a permission from database
        /// </summary>
        /// <param name="service">service user is registered to</param>
        /// <param name="username">name of user</param>
        /// <param name="permission">permission to remove</param>
        public void RemovePermission(string service, string username, string permission) {
            RemovePermission(usermodule.GetUserID(service, username), permission);
        }

        /// <summary>
        /// removes a permission from database
        /// </summary>
        /// <param name="userid">id of user</param>
        /// <param name="permission">permission to remove</param>
        public void RemovePermission(long userid, string permission) {
            database.Database.Delete<UserPermission>().Where(p => p.UserID == userid).Execute();
            Logger.Info(this, $"Permission '{permission}' removed for user '{userid}'");
        }
    }
}