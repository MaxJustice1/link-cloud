﻿
using System.ComponentModel.DataAnnotations;

namespace LantanaGroup.Link.Tenant.Entities
{
    public class BaseEntity : Shared.Domain.Entities.BaseEntity
    {
        [Key]
        public new Guid Id { get; set; }
    }
}
