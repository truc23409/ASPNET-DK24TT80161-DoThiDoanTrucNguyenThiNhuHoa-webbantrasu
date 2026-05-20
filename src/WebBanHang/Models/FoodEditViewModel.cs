using System.Collections.Generic;

namespace WebBanHang.Models
{
    public class FoodEditViewModel
    {
        public Food Food { get; set; }
        public List<int> SelectedIngredientIds { get; set; } = new List<int>();
    }
}
