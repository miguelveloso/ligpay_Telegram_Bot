namespace LiqPayBot_Telegram.Structures
{
    public class CartItem
    {
        public Item item;
        public int Count;
        public decimal AmountForCount => item.Amount * Count;
    }
}
