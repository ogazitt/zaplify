namespace BuiltSteady.Zaplify.Website.Models.AccessControl
{ 
    using System;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Security.Principal;
    using System.Text.RegularExpressions;
    using System.Web.Security;
    using System.Web;

    using BuiltSteady.Zaplify.ServerEntities;
    using BuiltSteady.Zaplify.ServiceHost;

    public class UserMembershipProvider : MembershipProvider
    {
        const int authTicketLifetime = 120;      // minutes

        public override string ApplicationName
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public override bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            try
            {
                CredentialStorageContext storage = Storage.NewCredentialContext;
                UserCredential user = storage.Credentials.Single<UserCredential>(u => u.Name == username.ToLower());
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
            CredentialStorageContext storage = Storage.NewCredentialContext;

            const string emailPattern = "^[a-z0-9_\\+-]+([\\.[a-z0-9_\\+-]+)*@[a-z0-9-]+(\\.[a-z0-9-]+)*\\.([a-z]{2,4})$";
            if (!Regex.IsMatch(email.ToLower(), emailPattern))
            {   // not valid email address
                status = MembershipCreateStatus.InvalidEmail;
                LoggingHelper.TraceInfo("Failed to create user account due to invalid email: " + email);
                return null;
            }

            if (password.Length < MinRequiredPasswordLength)
            {   // not a valid password
                status = MembershipCreateStatus.InvalidPassword;
                LoggingHelper.TraceInfo("Failed to create user account due to invalid password: " + password);
                return null;
            }

            if (storage.Credentials.Any<UserCredential>(u => u.Name == username))
            {   // username already exists
                status = MembershipCreateStatus.DuplicateUserName;
                LoggingHelper.TraceInfo("Failed to create duplicate user account: " + username);
                return null;
            }

            // create salt for each user and store hash of password
            string salt = CreateSalt(64);
            password = HashPassword(password, salt);
            Guid userID = (providerUserKey != null && providerUserKey is Guid) ? (Guid)providerUserKey : Guid.NewGuid();
            
            UserCredential user = new UserCredential()
            {
                ID = userID,
                Name = username.ToLower(),        
                Password = password,    
                PasswordSalt = salt,
                Email = email.ToLower(),
                CreateDate = DateTime.UtcNow
            };

            storage.Credentials.Add(user);
            storage.SaveChanges();
            user = storage.Credentials.Single<UserCredential>(u => u.Name == username);
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
            if (userIsOnline && HttpContext.Current.User != null)
            {   // check auth ticket first
                UserCredential user = ExtractUserFromTicket(HttpContext.Current.User);
                if (user != null)
                {
                    return AsMembershipUser(user);
                }
            }
            return AsMembershipUser(LookupUserByName(username));
        }

        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            return AsMembershipUser(LookupUserByID((Guid)providerUserKey));
        }

        public override string GetUserNameByEmail(string email)
        {
            UserCredential user = LookupUserByEmail(email);
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
            get { return 6; }
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
        {   // TODO: allow update of more than just email?
            CredentialStorageContext storage = Storage.NewCredentialContext;
            UserCredential user = storage.Credentials.Single<UserCredential>(u => u.Name == mu.UserName);
            user.Email = mu.Email;
            storage.SaveChanges();
        }

        public override bool ValidateUser(string username, string password)
        {
            UserCredential user = LookupUserByName(username);
            return ((user != null) && IsValidPassword(user, password));
        }

        public static User AsUser(MembershipUser mu)
        {
            return new User()
            {
                Name = mu.UserName,
                ID = (Guid)mu.ProviderUserKey,
                Email = mu.Email
            };
        }

        public static HttpCookie CreateAuthCookie(UserCredential user)
        {
            if (user.ID == Guid.Empty)
            {   // get id from storage to attach to cookie 
                user = LookupUserByName(user.Name);
            }

            string userData = user.ID.ToString();
            if (!string.IsNullOrEmpty(user.Email))
            {
                userData += "|" + user.Email;
            }
            FormsAuthenticationTicket authTicket = new FormsAuthenticationTicket(1, user.Name, 
                DateTime.Now, DateTime.Now.AddMinutes(authTicketLifetime), true, userData);

            HttpCookie authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(authTicket));
            authCookie.Expires = DateTime.Now.AddMinutes(authTicketLifetime);
            return authCookie;
        }

        static bool IsValidPassword(UserCredential user, string password)
        {   // hash of given password should match stored hash  
            string hash = HashPassword(password, user.PasswordSalt);
            return user.Password.Equals(hash, StringComparison.Ordinal);
        }

        static UserCredential LookupUserByName(string username)
        {
            username = username.ToLower();
            CredentialStorageContext storage = Storage.NewCredentialContext;
            if (storage.Credentials.Any<UserCredential>(u => u.Name == username))
            {
                UserCredential user = storage.Credentials.Single<UserCredential>(u => u.Name == username);
                return user;
            }
            return null;
        }

        static UserCredential LookupUserByEmail(string email)
        {
            email = email.ToLower();
            CredentialStorageContext storage = Storage.NewCredentialContext;
            if (storage.Credentials.Any<UserCredential>(u => u.Email == email))
            {
                UserCredential user = storage.Credentials.Single<UserCredential>(u => u.Email == email);
                return user;
            }
            return null;
        }

        static UserCredential LookupUserByID(Guid id)
        {
            CredentialStorageContext storage = Storage.NewCredentialContext;
            if (storage.Credentials.Any<UserCredential>(u => u.ID == id))
            {
                UserCredential user = storage.Credentials.Single<UserCredential>(u => u.ID == id);
                return user;
            }
            return null;
        }

        static UserCredential ExtractUserFromTicket(IPrincipal principal)
        {
            if (principal.Identity != null && principal.Identity.IsAuthenticated)
            {
                FormsIdentity identity = (FormsIdentity)principal.Identity;
                FormsAuthenticationTicket ticket = identity.Ticket;
                if (ticket != null && !string.IsNullOrEmpty(ticket.UserData))
                {
                    string[] userData = ticket.UserData.Split('|');
                    Guid userID;
                    if (Guid.TryParse(userData[0], out userID))
                    {
                        string email = (userData.Length > 1) ? userData[1] : string.Empty;
                        return new UserCredential() { Name = identity.Name, ID = userID, Email = email };
                    }
                }
            }
            return null;
        }

        static MembershipUser AsMembershipUser(UserCredential user)
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