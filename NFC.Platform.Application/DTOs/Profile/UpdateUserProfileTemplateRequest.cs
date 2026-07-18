using System;

namespace NFC.Platform.Application.DTOs.Profile
{
    /// <summary>
    /// Request payload for PATCH /api/user/profile/template.
    /// Sets the individual user's digital profile template.
    /// </summary>
    public class UpdateUserProfileTemplateRequest
    {
        /// <summary>
        /// FK to the CardTemplate defining this individual's digital profile layout.
        /// Choose from the active template catalog (GET /api/templates).
        /// </summary>
        public Guid? ProfileTemplateId { get; set; }
    }
}
