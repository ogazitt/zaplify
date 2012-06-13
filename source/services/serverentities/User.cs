using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class User : ServerEntity
    {
        // ServerEntity properties
        public override Guid ID { get; set; }
        public override string Name { get; set; }

        public string Email { get; set; }
        public DateTime CreateDate { get; set; }

        public List<UserCredential>UserCredentials { get; set; }  
        public List<ItemType> ItemTypes { get; set; }  
        public List<Tag> Tags { get; set; }
        public List<Item> Items { get; set; }
        public List<Folder> Folders { get; set; }

        public UserCredential GetCredential(string credentialType)
        {
            if (this.UserCredentials.Any(uc => uc.CredentialType == credentialType))
            {   // return existing credential
                return this.UserCredentials.Single<UserCredential>(uc => uc.CredentialType == credentialType);
            }
            return null;
        }

        public bool AddCredential(string credentialType, string accessToken, DateTime? expires, string renewalToken = null)
        {
            bool exists = false;
            UserCredential credential;
            // TODO: encrypt token
            if (this.UserCredentials.Any(uc => uc.CredentialType == credentialType))
            {   // update existing token
                credential = this.UserCredentials.Single<UserCredential>(uc => uc.CredentialType == credentialType);
                exists = true;
            }
            else
            {   // add new token
                credential = new UserCredential()
                {
                    UserID = this.ID,
                    CredentialType = credentialType,
                };
                this.UserCredentials.Add(credential);
            }
            credential.AccessToken = accessToken;
            credential.AccessTokenExpiration = expires;
            if (renewalToken != null) { credential.RenewalToken = renewalToken; }
            credential.LastModified = DateTime.UtcNow;
            return exists;
        }
    }

    public class UserCredential
    {
        public const string PASSWORD = "PASSWORD";
        public const string FB_CONSENT = "FB_CONSENT";
        public const string GOOGLE_CONSENT = "GOOGLE_CONSENT";
        public const string CLOUDAD_CONSENT = "CLOUDAD_CONSENT";

        public long ID { get; set; }
        public Guid UserID { get; set; }

        // do not serialize credential information
        [IgnoreDataMember]
        public string CredentialType { get; set; }
        [IgnoreDataMember]
        public string AccessToken { get; set; }
        [IgnoreDataMember]
        public DateTime? AccessTokenExpiration { get; set; }
        [IgnoreDataMember]
        public string RenewalToken { get; set; }

        public DateTime LastModified { get; set; }
        public DateTime? LastAccessed { get; set; }
    }
}