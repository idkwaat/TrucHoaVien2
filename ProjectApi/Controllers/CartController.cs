using Microsoft.AspNetCore.Mvc;
using ProjectApi.Models;

[Route("api/[controller]")]
[ApiController]
public class CartController : ControllerBase
{
    // Tạm lưu giỏ hàng trên bộ nhớ (demo)
    private static List<CartItem> Cart = new();

    [HttpGet]
    public IActionResult GetCart()
    {
        return Ok(Cart);
    }

    [HttpPost("add")]
    public IActionResult AddToCart([FromBody] CartItem item)
    {
        var existing = Cart.FirstOrDefault(x => x.ProductId == item.ProductId);
        if (existing != null)
        {
            existing.Quantity += item.Quantity;
        }
        else
        {
            item.Id = Cart.Count > 0 ? Cart.Max(x => x.Id) + 1 : 1;
            Cart.Add(item);
        }
        return Ok(Cart);
    }

    [HttpPut("update/{id}")]
    public IActionResult UpdateQuantity(int id, [FromBody] int quantity)
    {
        var item = Cart.FirstOrDefault(x => x.Id == id);
        if (item == null) return NotFound();
        item.Quantity = quantity;
        return Ok(Cart);
    }

    [HttpDelete("remove/{id}")]
    public IActionResult RemoveItem(int id)
    {
        var item = Cart.FirstOrDefault(x => x.Id == id);
        if (item == null) return NotFound();
        Cart.Remove(item);
        return Ok(Cart);
    }

    [HttpPost("clear")]
    public IActionResult ClearCart()
    {
        Cart.Clear();
        return Ok();
    }
}
