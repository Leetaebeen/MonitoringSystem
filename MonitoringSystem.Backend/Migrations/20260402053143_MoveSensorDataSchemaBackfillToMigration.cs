using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringSystem.Backend.Migrations
{
    /// <inheritdoc />
    public partial class MoveSensorDataSchemaBackfillToMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"IF OBJECT_ID(N'[SensorData]', N'U') IS NOT NULL
                  BEGIN
                      IF COL_LENGTH('SensorData', 'PlantId') IS NULL ALTER TABLE [SensorData] ADD [PlantId] nvarchar(max) NOT NULL CONSTRAINT [DF_SensorData_PlantId] DEFAULT N'PLANT-01';
                      IF COL_LENGTH('SensorData', 'LineId') IS NULL ALTER TABLE [SensorData] ADD [LineId] nvarchar(max) NOT NULL CONSTRAINT [DF_SensorData_LineId] DEFAULT N'LINE-A';
                      IF COL_LENGTH('SensorData', 'EquipmentType') IS NULL ALTER TABLE [SensorData] ADD [EquipmentType] nvarchar(max) NOT NULL CONSTRAINT [DF_SensorData_EquipmentType] DEFAULT N'Furnace';
                      IF COL_LENGTH('SensorData', 'PressureBar') IS NULL ALTER TABLE [SensorData] ADD [PressureBar] float NOT NULL CONSTRAINT [DF_SensorData_PressureBar] DEFAULT(0);
                      IF COL_LENGTH('SensorData', 'VibrationMmS') IS NULL ALTER TABLE [SensorData] ADD [VibrationMmS] float NOT NULL CONSTRAINT [DF_SensorData_VibrationMmS] DEFAULT(0);
                      IF COL_LENGTH('SensorData', 'Rpm') IS NULL ALTER TABLE [SensorData] ADD [Rpm] int NOT NULL CONSTRAINT [DF_SensorData_Rpm] DEFAULT(0);
                      IF COL_LENGTH('SensorData', 'Status') IS NULL ALTER TABLE [SensorData] ADD [Status] nvarchar(max) NOT NULL CONSTRAINT [DF_SensorData_Status] DEFAULT N'Running';
                      IF COL_LENGTH('SensorData', 'IsAlert') IS NULL ALTER TABLE [SensorData] ADD [IsAlert] bit NOT NULL CONSTRAINT [DF_SensorData_IsAlert] DEFAULT(0);
                      IF COL_LENGTH('SensorData', 'AlertCode') IS NULL ALTER TABLE [SensorData] ADD [AlertCode] nvarchar(max) NULL;
                  END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
