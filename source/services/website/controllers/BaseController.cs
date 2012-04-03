namespace BuiltSteady.Zaplify.Website.Controllers
{
    using System.Web.Mvc;
    using System.Web.Security;

    using BuiltSteady.Zaplify.ServerEntities;
    using BuiltSteady.Zaplify.ServiceHost;
    using BuiltSteady.Zaplify.Website.Models.AccessControl;

    [Authorize]
    public class BaseController : Controller
    {
        static string fbAppID;
        public static string FBAppID
        {
            get
            {
                if (fbAppID == null)
                {
                    fbAppID = ConfigurationSettings.Get("FBAppID");
                }
                return fbAppID;
            }
        }

        static string fbAppSecret;
        public static string FBAppSecret
        {
            get
            {
                if (fbAppSecret == null)
                {
                    fbAppSecret = ConfigurationSettings.Get("FBAppSecret");
                }
                return fbAppSecret;
            }
        }

        User currentUser;
        public User CurrentUser
        {
            get
            {
                if (currentUser == null)
                {
                    MembershipUser mu = Membership.GetUser();
                    currentUser = UserMembershipProvider.AsUser(mu);
                }
                return currentUser;
            }
        }

        UserStorageContext storageContext;
        public UserStorageContext StorageContext
        {
            get
            {
                if (storageContext == null)
                {
                    storageContext = Storage.NewUserContext;
                }
                return storageContext;
            }
        }
    }
}
