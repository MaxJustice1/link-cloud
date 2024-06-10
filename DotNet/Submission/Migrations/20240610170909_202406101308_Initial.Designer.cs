﻿// <auto-generated />
using System;
using LantanaGroup.Link.Submission.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace LantanaGroup.Link.Submission.Migrations
{
    [DbContext(typeof(TenantSubmissionDbContext))]
    [Migration("20240610170909_202406101308_Initial")]
    partial class _202406101308_Initial
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("LantanaGroup.Link.Submission.Domain.Entities.TenantSubmissionConfigEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("CreateDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("FacilityId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("ModifyDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("ReportType")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"), false);

                    b.ToTable("TenantSubmissionConfigs", (string)null);
                });

            modelBuilder.Entity("LantanaGroup.Link.Submission.Domain.Entities.TenantSubmissionConfigEntity", b =>
                {
                    b.OwnsMany("LantanaGroup.Link.Submission.Application.Models.ApiModels.Method", "Methods", b1 =>
                        {
                            b1.Property<Guid>("TenantSubmissionConfigEntityId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<int>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("int");

                            b1.Property<DateTime?>("CreateDate")
                                .HasColumnType("datetime2");

                            b1.Property<DateTime?>("ModifyDate")
                                .HasColumnType("datetime2");

                            b1.HasKey("TenantSubmissionConfigEntityId", "Id");

                            b1.ToTable("TenantSubmissionConfigs");

                            b1.ToJson("Methods");

                            b1.WithOwner()
                                .HasForeignKey("TenantSubmissionConfigEntityId");
                        });

                    b.Navigation("Methods");
                });
#pragma warning restore 612, 618
        }
    }
}
