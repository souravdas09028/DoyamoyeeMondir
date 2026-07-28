namespace DoyamoyeeMondir.Domain.Enums;

public enum StockTransactionType
{
    OpeningStock = 1,
    Purchase = 2,
    DonationReceived = 3,
    Sale = 4,
    Consumption = 5,
    AdjustmentIn = 6,
    AdjustmentOut = 7,
    TransferIn = 8,
    TransferOut = 9,
    Damage = 10,
    Expired = 11,
    SalesReturn = 12,
    PurchaseReturn = 13
}