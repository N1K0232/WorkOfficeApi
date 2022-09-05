using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkOfficeApi.DataAccessLayer.Configurations.Common;
using WorkOfficeApi.DataAccessLayer.Entities;

namespace WorkOfficeApi.DataAccessLayer.Configurations;

internal sealed class WorkerConfiguration : BaseEntityConfiguration<Worker>
{
    public override void Configure(EntityTypeBuilder<Worker> builder)
    {
        builder.ToTable("Workers");

        builder.Property(w => w.FirstName).HasMaxLength(256).IsRequired();
        builder.Property(w => w.LastName).HasMaxLength(256).IsRequired();
        builder.Property(w => w.DateOfBirth).IsRequired();

        builder.Property(w => w.City).HasMaxLength(50).IsRequired();
        builder.Property(w => w.Country).HasMaxLength(50).IsRequired();
        builder.Property(w => w.HomeAddress).HasMaxLength(256).IsRequired();

        builder.Property(w => w.CellphoneNumber).HasMaxLength(30).IsRequired().IsUnicode(false);
        builder.Property(w => w.EmailAddress).HasMaxLength(100).IsRequired().IsUnicode(false);

        builder.Property(w => w.WorkerType).HasConversion<string>().HasMaxLength(20).IsRequired();

        base.Configure(builder);
    }
}