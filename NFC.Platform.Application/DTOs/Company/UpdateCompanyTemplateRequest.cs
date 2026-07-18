using System;

namespace NFC.Platform.Application.DTOs.Company
{
    /// <summary>
    /// Request payload for PATCH /api/company/template.
    /// Sets the company's digital profile template.
    /// </summary>
    public class UpdateCompanyTemplateRequest
    {
        /// <summary>
        /// FK to the CardTemplate defining the digital profile layout for all company employees.
        /// Choose from the active template catalog (GET /api/templates).
        /// </summary>
        public Guid? ProfileTemplateId { get; set; }
    }
}
