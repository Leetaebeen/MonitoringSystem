namespace MonitoringSystem.Simulator.Config;

public class SimulatorOptions
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = "sensor-topic";
    public int IntervalMs { get; set; } = 1000;
    public string PlantId { get; set; } = "PLANT-01";
    public string[] PlantIds { get; set; } = ["PLANT-01"];
    public string[] LineIds { get; set; } = ["LINE-A", "LINE-B"];
    public string[] EquipmentIds { get; set; } = ["EQ-001", "EQ-002", "EQ-003"];
    public string[] EquipmentTypes { get; set; } = ["Furnace", "Conveyor", "Pump"];
}
