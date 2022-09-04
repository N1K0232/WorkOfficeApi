using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkOfficeApi.DataAccessLayer.Entities.Common;

namespace WorkOfficeApi.DataAccessLayer.Configurations.Common;

internal abstract class BaseEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity> where TEntity : BaseEntity
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.CreationDate).IsRequired();
        builder.Property(x => x.UpdatedDate).IsRequired(false);
    }
}