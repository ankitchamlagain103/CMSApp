using Application.AppConfigs.Dtos;
using Domain.Entities;

namespace Application.AppConfigs
{
    public static class AppConfigMapper
    {
        public static AppConfigDto ToDto(AppConfig appConfig)
        {
            var appConfigDto = new AppConfigDto
            {
                Id = appConfig.Id,
                ConfigParam = appConfig.ConfigParam,
                ConfigValue = appConfig.ConfigValue,
                ConfigGroup = appConfig.ConfigGroup,
                IsEnable = appConfig.IsEnable
            };

            return appConfigDto;
        }

        public static PublicAppConfigDto ToPublicDto(AppConfig appConfig)
        {
            var publicAppConfigDto = new PublicAppConfigDto
            {
                ConfigParam = appConfig.ConfigParam,
                ConfigValue = appConfig.ConfigValue,
                ConfigGroup = appConfig.ConfigGroup
            };

            return publicAppConfigDto;
        }
    }
}
