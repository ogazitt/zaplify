namespace BuiltSteady.Zaplify.Website.Models.AccessControl
{ 
    using System;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Web.Security;

    using BuiltSteady.Zaplify.ServerEntities;
    using BuiltSteady.Zaplify.ServiceHelpers;

    public class UserMembershipProvider : MembershipProvider
    {

        public override string ApplicationName
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public override bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            try
            {
                StorageContext storage = Storage.NewContext;
                User user = storage.Users.Single<User>(u => u.Name == username.ToLower());
                // verify old password
                if (IsValidPassword(user, oldPassword))
                {   // TODO: verify new password meets requirements
                    user.Password = HashPassword(newPassword, user.PasswordSalt);
                    return (storage.SaveChanges() > 0);
                }
            }
            catch (Exception)
            { }
            return false;
        }

        public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer)
        {
            throw new NotImplementedException();
        }

        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
        {
            status = MembershipCreateStatus.Success;
            StorageContext storage = Storage.NewContext;

            if (storage.Users.Any<User>(u => u.Name == username))
            {   // username already exists
                status = MembershipCreateStatus.DuplicateUserName;
                // Log failure to create duplicate user account
                LoggingHelper.TraceInfo("Failed to create duplicate user account: " + username);
                return null;
            }

            // create salt for each user and store hash of password
            string salt = CreateSalt(64);
            password = HashPassword(password, salt);
            
            User user = new User()
            {
                ID = (Guid)providerUserKey,
                Name = username.ToLower(),        
                Password = password,    
                PasswordSalt = salt,
                Email = email.ToLower(),
                CreateDate = DateTime.UtcNow
            };

            storage.Users.Add(user);
            storage.SaveChanges();
            user = storage.Users.Single<User>(u => u.Name == username);
            status = MembershipCreateStatus.Success;

            // Log creation of new user account
            LoggingHelper.TraceInfo("Created new user account: " + username);

            return AsMembershipUser(user);
        }

        private static string CreateSalt(int size)
        {   // generate a cryptographic random number
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] bytes = new byte[size];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);   // return as Base64 string for hashing
        }

        private static string HashPassword(string password, string salt)
        {
            return FormsAuthentication.HashPasswordForStoringInConfigFile(string.Concat(password, salt), "SHA1");
        }

        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {   // always delete all related data
            StorageContext storage = Storage.NewContext;
            User dbUser = storage.Users.
                Include("ItemTypes.Fields").
                Include("Tags").
                Include("Items.ItemTags").
                Include("Folders.FolderUsers").
                Single<User>(u => u.Name == username.ToLower());

            storage.Users.Remove(dbUser);
            int rows = storage.SaveChanges();
            return (rows > 0);
        }

        public override bool EnablePasswordReset
        {
            get { throw new NotImplementedException(); }
        }

        public override bool EnablePasswordRetrieval
        {
            get { throw new NotImplementedException(); }
        }

        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override int GetNumberOfUsersOnline()
        {
            throw new NotImplementedException();
        }

        public override string GetPassword(string username, string answer)
        {
            throw new NotImplementedException();
        }

        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            return AsMembershipUser(LookupUserByName(username));
        }

        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            return AsMembershipUser(LookupUserByID((Guid)providerUserKey));
        }

        public override string GetUserNameByEmail(string email)
        {
            User user = LookupUserByEmail(email);
            return user.Name;
        }

        public override int MaxInvalidPasswordAttempts
        {
            get { throw new NotImplementedException(); }
        }

        public override int MinRequiredNonAlphanumericCharacters
        {
            get { throw new NotImplementedException(); }
        }

        public override int MinRequiredPasswordLength
        {
            get { throw new NotImplementedException(); }
        }

        public override int PasswordAttemptWindow
        {
            get { throw new NotImplementedException(); }
        }

        public override MembershipPasswordFormat PasswordFormat
        {
            get { throw new NotImplementedException(); }
        }

        public override string PasswordStrengthRegularExpression
        {
            get { throw new NotImplementedException(); }
        }

        public override bool RequiresQuestionAndAnswer
        {
            get { throw new NotImplementedException(); }
        }

        public override bool RequiresUniqueEmail
        {
            get { throw new NotImplementedException(); }
        }

        public override string ResetPassword(string username, string answer)
        {
            throw new NotImplementedException();
        }

        public override bool UnlockUser(string userName)
        {
            throw new NotImplementedException();
        }

        public override void UpdateUser(MembershipUser mu)
        {   // TODO: allow update of username or more than just email?
            StorageContext storage = Storage.NewContext;
            User user = storage.Users.Single<User>(u => u.Name == mu.UserName);
            user.Email = mu.Email;
            storage.SaveChanges();
        }

        public override bool ValidateUser(string username, string password)
        {
            User user = LookupUserByName(username);
            return ((user != null) && IsValidPassword(user, password));
        }

        static bool IsValidPassword(User user, string password)
        {   // hash of given password should match stored hash  
            string hash = HashPassword(password, user.PasswordSalt);
            return user.Password.Equals(hash, StringComparison.Ordinal);
        }

        static User LookupUserByName(string username)
        {
            username = username.ToLower();
            StorageContext storage = Storage.NewContext;
            if (storage.Users.Any<User>(u => u.Name == username))
            {
                User user = storage.Users.Single<User>(u => u.Name == username);
                return user;
            }
            return null;
        }

        static User LookupUserByEmail(string email)
        {
            email = email.ToLower();
            StorageContext storage = Storage.NewContext;
            if (storage.Users.Any<User>(u => u.Email == email))
            {
                User user = storage.Users.Single<User>(u => u.Email == email);
                return user;
            }
            return null;
        }

        static User LookupUserByID(Guid id)
        {
            StorageContext storage = Storage.NewContext;
            if (storage.Users.Any<User>(u => u.ID == id))
            {
                User user = storage.Users.Single<User>(u => u.ID == id);
                return user;
            }
            return null;
        }

        static MembershipUser AsMembershipUser(User user)
        {
            MembershipUser member = null;
            if (user != null)
            {
                member = new MembershipUser(
                    typeof(UserMembershipProvider).Name,    // provider
                    user.Name,                              // username
                    user.ID,                                // user key
                    user.Email,                             // email
                    null,                                   // password question
                    null,                                   // comment
                    true,                                   // isApproved
                    false,                                  // isLockedOut
                    user.CreateDate,                        // createDate
                    DateTime.Now,                           // lastLoginDate
                    DateTime.Now,                           // lastActivityDate
                    DateTime.Now,                           // lastPasswordChangeDate
                    DateTime.Now);                          // lastLockoutDate
            }
            return member;
        }
    }
}