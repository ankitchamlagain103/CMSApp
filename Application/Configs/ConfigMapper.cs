using Application.Common.Models;
using Application.Configs.Dtos;
using Domain.Entities;

namespace Application.Configs
{
    public static class ConfigMapper
    {
        public static ConfigTypeDto ToDto(ConfigType configType)
        {
            var configTypeDto = new ConfigTypeDto
            {
                Id = configType.Id,
                TypeCode = configType.TypeCode,
                Name = configType.Name,
                Description = configType.Description
            };

            return configTypeDto;
        }

        public static ConfigDto ToDto(Config config)
        {
            var configDto = new ConfigDto
            {
                Id = config.Id,
                TypeCode = config.TypeCode,
                Code = config.Code,
                Label = config.Label,
                Order = config.Order,
                AdditionalValue1 = config.AdditionalValue1,
                AdditionalValue2 = config.AdditionalValue2,
                AdditionalValue3 = config.AdditionalValue3
            };

            return configDto;
        }

        public static DropdownItemDto ToDropdownItemDto(Config config)
        {
            var dropdownItemDto = new DropdownItemDto
            {
                Value = config.Code,
                Label = config.Label,
                Order = config.Order,
                AdditionalValue1 = config.AdditionalValue1,
                AdditionalValue2 = config.AdditionalValue2,
                AdditionalValue3 = config.AdditionalValue3
            };

            return dropdownItemDto;
        }
    }
}
