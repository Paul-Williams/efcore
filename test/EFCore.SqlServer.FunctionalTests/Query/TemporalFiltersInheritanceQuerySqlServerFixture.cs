﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore.TestModels.InheritanceModel;

namespace Microsoft.EntityFrameworkCore.Query
{
    public class TemporalFiltersInheritanceQuerySqlServerFixture : FiltersInheritanceQuerySqlServerFixture
    {
        protected override string StoreName { get; } = "TemporalFiltersInheritanceQueryTest";

        public DateTime ChangesDate { get; private set; }

        public string ChangeDateLiteral { get; private set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);

            modelBuilder.Entity<Animal>().ToTable(tb => tb.IsTemporal());
            modelBuilder.Entity<Plant>().ToTable(tb => tb.IsTemporal());
            modelBuilder.Entity<Country>().ToTable(tb => tb.IsTemporal());
            modelBuilder.Entity<Drink>().ToTable(tb => tb.IsTemporal());
        }

        protected override void Seed(InheritanceContext context)
        {
            base.Seed(context);

            ChangesDate = new DateTime(2010, 1, 1);

            context.RemoveRange(context.ChangeTracker.Entries().Where(e => e.Entity is Animal).Select(e => e.Entity));
            context.RemoveRange(context.ChangeTracker.Entries().Where(e => e.Entity is Plant).Select(e => e.Entity));
            context.RemoveRange(context.ChangeTracker.Entries().Where(e => e.Entity is Country).Select(e => e.Entity));
            context.RemoveRange(context.ChangeTracker.Entries().Where(e => e.Entity is Drink).Select(e => e.Entity));
            context.SaveChanges();

            var historyTableInfos = new List<(string table, string historyTable)>()
            {
                ("Animals", "AnimalHistory"),
                ("Plants", "PlantHistory"),
                ("Countries", "CountryHistory"),
                ("Drinks", "DrinkHistory"),
            };

            foreach (var historyTableInfo in historyTableInfos)
            {
                context.Database.ExecuteSqlRaw($"ALTER TABLE [{historyTableInfo.table}] SET (SYSTEM_VERSIONING = OFF)");
                context.Database.ExecuteSqlRaw($"ALTER TABLE [{historyTableInfo.table}] DROP PERIOD FOR SYSTEM_TIME");

                context.Database.ExecuteSqlRaw($"UPDATE [{historyTableInfo.historyTable}] SET PeriodStart = '2000-01-01T01:00:00.0000000Z'");
                context.Database.ExecuteSqlRaw($"UPDATE [{historyTableInfo.historyTable}] SET PeriodEnd = '2020-07-01T07:00:00.0000000Z'");

                context.Database.ExecuteSqlRaw($"ALTER TABLE [{historyTableInfo.table}] ADD PERIOD FOR SYSTEM_TIME ([PeriodStart], [PeriodEnd])");
                context.Database.ExecuteSqlRaw($"ALTER TABLE [{historyTableInfo.table}] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[{historyTableInfo.historyTable}]))");
            }

            ChangeDateLiteral = string.Format(CultureInfo.InvariantCulture, "{0:yyyy-MM-ddTHH:mm:ss.fffffffK}", ChangesDate);
        }
    }
}
