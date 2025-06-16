using System.Reflection.Emit;
using System.Text.Json;
using Domain.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel;

namespace Infrastructure.Patients
{
    internal sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
    {
        public void Configure(EntityTypeBuilder<Patient> builder)
        {
            // Primary Key - postgres will automatically get B-tree index postgres
            builder.HasKey(p => p.Id);

            builder.HasIndex(p => p.BirthDate);

            builder.Property(p => p.NameUse)
                .HasMaxLength(50);

            builder.Property(p => p.FamilyName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.GivenNames)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null));

            // Configure Gender enum storage as text
            builder.Property(p => p.Gender)
                .HasConversion<string>() // Stores enum as text
                .HasMaxLength(20);

            builder.Property(p => p.BirthDate)
                .IsRequired()
                .HasColumnType("timestamp with time zone");

            builder.Property(p => p.BirthDateOffset)
                .HasColumnType("interval");

            builder.Property(p => p.Active)
                .HasDefaultValue(false);

            builder.Property(p => p.CreatedAt)
                .IsRequired();
        }
    }
}
