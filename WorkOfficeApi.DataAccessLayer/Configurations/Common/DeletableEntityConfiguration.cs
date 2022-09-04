﻿using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkOfficeApi.DataAccessLayer.Entities.Common;

namespace WorkOfficeApi.DataAccessLayer.Configurations.Common;

internal abstract class DeletableEntityConfiguration<TEntity> : BaseEntityConfiguration<TEntity> where TEntity : DeletableEntity
{
    public override void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.DeletedDate).IsRequired(false);

        base.Configure(builder);
    }
}