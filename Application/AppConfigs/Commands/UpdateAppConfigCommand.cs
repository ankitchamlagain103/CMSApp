namespace Application.AppConfigs.Commands
{
    public class UpdateAppConfigCommand
    {
        public string ConfigParam { get; set; }
        public string ConfigValue { get; set; }
        public string ConfigGroup { get; set; }
        public bool IsEnable { get; set; }
    }
}
