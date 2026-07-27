namespace NFC.Platform.Application.DTOs.VipCustomer;

public class UpdateVipStatusRequest
{
    public bool IsVip { get; set; }
    public int VipDisplayOrder { get; set; } = 0;
}
