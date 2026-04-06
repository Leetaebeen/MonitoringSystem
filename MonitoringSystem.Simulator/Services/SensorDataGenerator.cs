using MonitoringSystem.Shared.Models;
using MonitoringSystem.Simulator.Config;

namespace MonitoringSystem.Simulator.Services;

public class SensorDataGenerator
{
    private readonly SimulatorOptions _options;
    private readonly Random _random = new();

    private sealed record MetricProfile(
        double TemperatureMin,
        double TemperatureMax,
        double PressureMin,
        double PressureMax,
        double VibrationMin,
        double VibrationMax,
        int RpmMin,
        int RpmMax);

    private static readonly Dictionary<string, MetricProfile> Profiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Furnace"] = new MetricProfile(55, 95, 2.2, 4.8, 1.5, 6.5, 900, 1700),
        ["Conveyor"] = new MetricProfile(28, 58, 1.2, 2.4, 2.5, 8.8, 700, 1400),
        ["Pump"] = new MetricProfile(35, 72, 2.8, 6.2, 1.2, 7.4, 1100, 2400),
        ["Compressor"] = new MetricProfile(45, 86, 4.1, 7.5, 2.0, 8.6, 1300, 2900),
        ["Mixer"] = new MetricProfile(30, 68, 1.6, 3.6, 1.8, 7.1, 600, 1300)
    };

    public SensorDataGenerator(SimulatorOptions options)
    {
        _options = options;
    }

    public SensorData Next()
    {
        var plantId = GetPlantId();
        var lineId = GetRandomValue(_options.LineIds, "LINE-A");
        var equipmentId = GetRandomValue(_options.EquipmentIds, "EQ-001");
        var equipmentType = GetEquipmentType(equipmentId);
        var profile = GetProfile(equipmentType);

        var shiftFactor = GetShiftFactor(DateTime.UtcNow);
        var temperature = Math.Round(GetRandom(profile.TemperatureMin, profile.TemperatureMax) * shiftFactor, 2);
        var pressure = Math.Round(GetRandom(profile.PressureMin, profile.PressureMax) * (0.96 + _random.NextDouble() * 0.08), 2);
        var vibration = Math.Round(GetRandom(profile.VibrationMin, profile.VibrationMax) * (0.9 + _random.NextDouble() * 0.24), 2);
        var rpm = (int)Math.Round(_random.Next(profile.RpmMin, profile.RpmMax + 1) * (0.95 + _random.NextDouble() * 0.12));

        var hasSpike = _random.NextDouble() < 0.08;
        if (hasSpike)
        {
            temperature = Math.Round(temperature + 12 + _random.NextDouble() * 16, 2);
            vibration = Math.Round(vibration + 1.5 + _random.NextDouble() * 3.6, 2);
        }

        var isAlert = temperature >= 82 || vibration >= 8.5 || pressure >= 7.2 || pressure <= 0.9;
        var status = ResolveStatus(isAlert, vibration, rpm);
        var alertCode = ResolveAlertCode(temperature, vibration, pressure, rpm);

        return new SensorData
        {
            PlantId = plantId,
            LineId = lineId,
            EquipmentId = equipmentId,
            EquipmentType = equipmentType,
            Temperature = temperature,
            PressureBar = pressure,
            VibrationMmS = vibration,
            Rpm = rpm,
            Status = status,
            IsAlert = isAlert,
            AlertCode = isAlert ? alertCode : null,
            LogTime = DateTime.UtcNow
        };
    }

    private string GetPlantId()
    {
        if (_options.PlantIds is { Length: > 0 })
        {
            return _options.PlantIds[_random.Next(_options.PlantIds.Length)];
        }

        return string.IsNullOrWhiteSpace(_options.PlantId) ? "PLANT-01" : _options.PlantId;
    }

    private string GetEquipmentType(string equipmentId)
    {
        if (_options.EquipmentTypes is not { Length: > 0 })
        {
            return "Furnace";
        }

        var digits = new string(equipmentId.Where(char.IsDigit).ToArray());
        if (int.TryParse(digits, out var number) && number > 0)
        {
            return _options.EquipmentTypes[(number - 1) % _options.EquipmentTypes.Length];
        }

        return GetRandomValue(_options.EquipmentTypes, "Furnace");
    }

    private static MetricProfile GetProfile(string equipmentType)
        => Profiles.TryGetValue(equipmentType, out var profile)
            ? profile
            : new MetricProfile(28, 70, 1.0, 4.8, 0.8, 7.8, 700, 2200);

    private static double GetShiftFactor(DateTime utcNow)
    {
        var hour = utcNow.Hour;
        if (hour is >= 7 and <= 10)
        {
            return 1.06;
        }

        if (hour is >= 13 and <= 17)
        {
            return 1.08;
        }

        if (hour is >= 0 and <= 5)
        {
            return 0.93;
        }

        return 1.0;
    }

    private double GetRandom(double min, double max)
        => min + _random.NextDouble() * (max - min);

    private string GetRandomValue(string[] values, string fallback)
    {
        if (values is not { Length: > 0 })
        {
            return fallback;
        }

        return values[_random.Next(values.Length)];
    }

    private string ResolveStatus(bool isAlert, double vibration, int rpm)
    {
        if (isAlert)
        {
            return "알람";
        }

        if (rpm < 780)
        {
            return "대기";
        }

        if (vibration >= 7.2)
        {
            return "경고";
        }

        if (_random.NextDouble() < 0.03)
        {
            return "점검";
        }

        return "가동";
    }

    private static string ResolveAlertCode(double temperature, double vibration, double pressure, int rpm)
    {
        var codes = new List<string>();

        if (temperature >= 82)
        {
            codes.Add("TEMP_HIGH");
        }

        if (vibration >= 8.5)
        {
            codes.Add("VIB_HIGH");
        }

        if (pressure >= 7.2)
        {
            codes.Add("PRESSURE_HIGH");
        }

        if (pressure <= 0.9)
        {
            codes.Add("PRESSURE_LOW");
        }

        if (rpm >= 2800)
        {
            codes.Add("RPM_HIGH");
        }

        if (rpm <= 650)
        {
            codes.Add("RPM_LOW");
        }

        return codes.Count > 0 ? string.Join("_", codes) : "THRESHOLD_EXCEEDED";
    }
}
