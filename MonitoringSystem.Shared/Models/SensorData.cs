namespace MonitoringSystem.Shared.Models
{
    public class SensorData
    {
        public int Id { get; set; }
        public string PlantId { get; set; } = "PLANT-01";
        public string LineId { get; set; } = "LINE-A";
        public string EquipmentId { get; set; } = string.Empty;
        public string EquipmentType { get; set; } = "Furnace";
        public double Temperature { get; set; }
        public double PressureBar { get; set; }
        public double VibrationMmS { get; set; }
        public int Rpm { get; set; }
        public string Status { get; set; } = "Running";
        public bool IsAlert { get; set; }
        public string? AlertCode { get; set; }
        public DateTime LogTime { get; set; }
    }
}