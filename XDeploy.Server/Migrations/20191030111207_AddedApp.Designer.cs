﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using XDeploy.Server.Infrastructure.Data;

namespace XDeploy.Server.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20191030111207_AddedApp")]
    partial class AddedApp
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("XDeploy.Server.Infrastructure.Data.APIKey", b =>
                {
                    b.Property<string>("Key")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("UserEmail")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Key");

                    b.ToTable("APIKeys");
                });

            modelBuilder.Entity("XDeploy.Server.Infrastructure.Data.Application", b =>
                {
                    b.Property<string>("ID")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("LastUpdate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("OwnerEmail")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("ID");

                    b.ToTable("Applications");
                });
#pragma warning restore 612, 618
        }
    }
}
