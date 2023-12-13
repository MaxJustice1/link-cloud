﻿using LantanaGroup.Link.Audit.Application.Interfaces;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace LantanaGroup.Link.Audit.Domain.Entities
{
    public class BaseEntity : IBaseEntity
    {
        [BsonId]        
        public string? Id { get; set; }
        public DateTime CreatedOn { get; set; }
        public Guid? CreatedBy { get; set; }
    }
}
