using System.Collections.Generic;
using System.Linq;

namespace LiqPayBot_Telegram.Structures
{
    class Cart
    {
        public List<CartItem> items = new List<CartItem>();
        public long userId;
        public int TotalCount => items.Select(e => e.Count).Sum();
        public decimal TotalAmount => items.Select(e => e.AmountForCount).Sum();
    }
}
