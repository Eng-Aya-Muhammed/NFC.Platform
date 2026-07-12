using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NFC.Platform.Application.DTOs;
using NFC.Platform.BuildingBlocks.Results;

namespace NFC.Platform.Application.Interfaces.Services
{
    public interface ICardTemplateService
    {
        Task<ServiceResult<IReadOnlyList<CardTemplateDto>>> GetActiveTemplatesAsync();
    }
}
