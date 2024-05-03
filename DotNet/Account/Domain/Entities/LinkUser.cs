﻿using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace LantanaGroup.Link.Account.Domain.Entities
{

    [Table("Accounts")]
    public class LinkUser : IdentityUser
    {      
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public List<string>? FacilityIds { get; set; }       
        public DateTime? LastSeen { get; set; }

        public virtual ICollection<LinkUserClaim> Claims { get; set; } = new List<LinkUserClaim>();
        public virtual ICollection<LinkUserLogin> Logins { get; set; } = new List<LinkUserLogin>();
        public virtual ICollection<LinkUserToken> Tokens { get; set; } = new List<LinkUserToken>();
        public virtual ICollection<LinkUserRole> UserRoles { get; set; } = new List<LinkUserRole>();
    }

    [Table("AccountClaims")]
    public class LinkUserClaim : IdentityUserClaim<string>
    {
        public virtual LinkUser User { get; set; } = default!;
    }

    [Table("AccountLogins")]
    public class LinkUserLogin : IdentityUserLogin<string>
    {
        public virtual LinkUser User { get; set; } = default!;
    }

    [Table("AccountTokens")]
    public class LinkUserToken : IdentityUserToken<string>
    {
        public virtual LinkUser User { get; set; } = default!;
    }   
}
