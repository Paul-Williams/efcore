﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore.TestModels.GearsOfWarModel;
using Microsoft.EntityFrameworkCore.TestUtilities;

namespace Microsoft.EntityFrameworkCore.Query
{
    public class TemporalGearsOfWarQuerySqlServerFixture : GearsOfWarQuerySqlServerFixture//GearsOfWarQueryFixtureBase
    {
        protected override string StoreName { get; } = "TemporalGearsOfWarQueryTest";

        public DateTime ChangesDate { get; private set; }

        public string ChangeDateLiteral { get; private set; }

        protected override ITestStoreFactory TestStoreFactory
            => SqlServerTestStoreFactory.Instance;

        public new RelationalTestStore TestStore
            => (RelationalTestStore)base.TestStore;

        //public TestSqlLoggerFactory TestSqlLoggerFactory
        //    => (TestSqlLoggerFactory)ListLoggerFactory;

        protected override bool ShouldLogCategory(string logCategory)
            => logCategory == DbLoggerCategory.Query.Name;

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            modelBuilder.Entity<City>().ToTable(tb => tb.IsTemporal());
            modelBuilder.Entity<CogTag>().ToTable(tb => tb.IsTemporal());
            modelBuilder.Entity<Faction>().ToTable(tb => tb.IsTemporal());
            modelBuilder.Entity<Gear>().ToTable(tb => tb.IsTemporal());
            modelBuilder.Entity<LocustLeader>().ToTable(tb => tb.IsTemporal());
            modelBuilder.Entity<LocustHighCommand>().ToTable(tb => tb.IsTemporal());
            modelBuilder.Entity<Mission>().ToTable(tb => tb.IsTemporal());
            modelBuilder.Entity<Squad>().ToTable(tb => tb.IsTemporal());
            modelBuilder.Entity<SquadMission>().ToTable(tb => tb.IsTemporal());
            modelBuilder.Entity<Weapon>().ToTable(tb => tb.IsTemporal());

            base.OnModelCreating(modelBuilder, context);
        }

        protected override void Seed(GearsOfWarContext context)
        {
            base.Seed(context);

            ChangesDate = new DateTime(2010, 1, 1);

            //// clean up intermittent history - we do the data fixup in 2 steps (due to cycle)
            //// so we want to remove the temporary states, so that further manipulation is easier
            context.Database.ExecuteSqlRaw("ALTER TABLE [LocustLeaders] SET (SYSTEM_VERSIONING = OFF)");
            context.Database.ExecuteSqlRaw("DELETE FROM [LocustLeaderHistory]");
            context.Database.ExecuteSqlRaw("ALTER TABLE [LocustLeaders] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[LocustLeaderHistory]))");

            context.Database.ExecuteSqlRaw("ALTER TABLE [Missions] SET (SYSTEM_VERSIONING = OFF)");
            context.Database.ExecuteSqlRaw("DELETE FROM [MissionHistory]");
            context.Database.ExecuteSqlRaw("ALTER TABLE [Missions] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[MissionHistory]))");

            context.RemoveRange(context.ChangeTracker.Entries().Where(e => e.Entity is City).Select(e => e.Entity));
            context.RemoveRange(context.ChangeTracker.Entries().Where(e => e.Entity is CogTag).Select(e => e.Entity));
            context.RemoveRange(context.ChangeTracker.Entries().Where(e => e.Entity is Gear).Select(e => e.Entity));
            context.RemoveRange(context.ChangeTracker.Entries().Where(e => e.Entity is LocustHighCommand).Select(e => e.Entity));
            context.RemoveRange(context.ChangeTracker.Entries().Where(e => e.Entity is Mission).Select(e => e.Entity));
            context.RemoveRange(context.ChangeTracker.Entries().Where(e => e.Entity is Squad).Select(e => e.Entity));
            context.RemoveRange(context.ChangeTracker.Entries().Where(e => e.Entity is SquadMission).Select(e => e.Entity));
            context.RemoveRange(context.ChangeTracker.Entries().Where(e => e.Entity is Weapon).Select(e => e.Entity));
            context.SaveChanges();

            context.RemoveRange(context.ChangeTracker.Entries().Where(e => e.Entity is Faction).Select(e => e.Entity));
            context.RemoveRange(context.ChangeTracker.Entries().Where(e => e.Entity is LocustLeader).Select(e => e.Entity));
            context.SaveChanges();

            // clean up Faction history
            context.Database.ExecuteSqlRaw("ALTER TABLE [Factions] SET (SYSTEM_VERSIONING = OFF)");
            context.Database.ExecuteSqlRaw("DELETE FROM [FactionHistory] WHERE CommanderName IS NULL");
            context.Database.ExecuteSqlRaw("ALTER TABLE [Factions] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[FactionHistory]))");

            var historyTableInfos = new List<(string table, string historyTable)>()
            {
                ("Cities", "CityHistory"),
                ("Tags", "CogTagHistory"),
                ("Gears", "GearHistory"),
                ("LocustHighCommands", "LocustHighCommandHistory"),
                ("Missions", "MissionHistory"),
                ("Squads", "SquadHistory"),
                ("SquadMissions", "SquadMissionHistory"),
                ("Weapons", "WeaponHistory"),

                ("LocustLeaders", "LocustLeaderHistory"),
                ("Factions", "FactionHistory"),
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
