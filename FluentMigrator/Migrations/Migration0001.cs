//-----------------------------------------------------------------------------
// <copyright file="Migration0001.cs" company="Microsoft">
//     Copyright (c) 2020-2021 Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------------

using FluentMigrator;

namespace Microsoft.CM.ReviewDB.Migrator.Migrations
{
    /// <summary>
    ///     Handles initial database creation.
    /// </summary>
    [Migration(0001)]
    public sealed class Migration0001 : Migration
    {
        /// <summary>
        ///     Migrates the database up.
        /// </summary>
        public override void Up()
        {
            /*
             * Handle databases that already exist from SQL based deployments.
             * Remove the table specific to that deployment strategy and consider this migration as complete.
             */
            if (this.Schema.Table("Review").Exists())
            {
                if (this.Schema.Table("__RefactorLog").Exists())
                {
                    this.Delete.Table("__RefactorLog");
                }

                return;
            }

            this.Create.Table("Review")
                .WithColumn("Id").AsAnsiString(50).NotNullable().PrimaryKey()
                .WithColumn("Team").AsAnsiString(50).NotNullable();

            this.Create.Table("Job")
                .WithColumn("Id").AsAnsiString(50).NotNullable().PrimaryKey()
                .WithColumn("TeamName").AsAnsiString(50).NotNullable()
                .WithColumn("ContentType").AsInt16().NotNullable()
                .WithColumn("CreatedDate").AsDateTimeOffset(7).NotNullable()
                .WithColumn("ModifiedDate").AsDateTimeOffset(7).Nullable()

            this.Create.Table("TagValues")
                .WithColumn("TagShortCode").AsAnsiString(2).NotNullable().PrimaryKey()
                .WithColumn("ReviewId").AsAnsiString(50).NotNullable().PrimaryKey()
                .WithColumn("Value").AsBoolean().NotNullable();

            this.Create.ForeignKey("FK_TagValues_ToReview")
                .FromTable("TagValues").ForeignColumn("ReviewId")
                .ToTable("Review").PrimaryColumn("Id");

            this.Execute.EmbeddedScript("0001_07_CreateType_tbltype_TagValues.sql");

            this.Execute.EmbeddedScript("0001_01_CreateStoredProc_ssp_GetReviewerStatistics.sql");
        }

        /// <summary>
        ///     Migrates the database down.
        /// </summary>
        public override void Down()
        {
            this.Execute.EmbeddedScript("0001_51_DropStoredProc_ssp_GetReviewerStatistics.sql");

            this.Execute.EmbeddedScript("0001_57_DropType_tbltype_TagValues.sql");

            this.Delete.Table("Review");
            this.Delete.Table("Job");
            this.Delete.Table("TagValues");
        }
    }
}
