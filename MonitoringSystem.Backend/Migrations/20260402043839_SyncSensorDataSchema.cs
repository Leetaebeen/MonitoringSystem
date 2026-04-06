using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringSystem.Backend.Migrations
{
    /// <inheritdoc />
    public partial class SyncSensorDataSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"IF OBJECT_ID(N'[SensorData]', N'U') IS NULL
                  BEGIN
                      CREATE TABLE [SensorData] (
                          [Id] int NOT NULL IDENTITY,
                          [PlantId] nvarchar(max) NOT NULL,
                          [LineId] nvarchar(max) NOT NULL,
                          [EquipmentId] nvarchar(max) NOT NULL,
                          [EquipmentType] nvarchar(max) NOT NULL,
                          [Temperature] float NOT NULL,
                          [PressureBar] float NOT NULL,
                          [VibrationMmS] float NOT NULL,
                          [Rpm] int NOT NULL,
                          [Status] nvarchar(max) NOT NULL,
                          [IsAlert] bit NOT NULL,
                          [AlertCode] nvarchar(max) NULL,
                          [LogTime] datetime2 NOT NULL,
                          CONSTRAINT [PK_SensorData] PRIMARY KEY ([Id])
                      );
                  END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"IF OBJECT_ID(N'[SensorData]', N'U') IS NOT NULL
                  BEGIN
                      DROP TABLE [SensorData];
                  END");
        }
    }
}
