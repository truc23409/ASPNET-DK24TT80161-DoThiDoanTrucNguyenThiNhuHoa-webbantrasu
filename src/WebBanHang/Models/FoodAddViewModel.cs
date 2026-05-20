using System.Collections.Generic;

namespace WebBanHang.Models
{
    public class FoodAddViewModel
    {
        public Food Food { get; set; }
        public List<int> SelectedIngredientIds { get; set; } = new List<int>();// chứa các IngredientId được chọn trong form
    }
}
