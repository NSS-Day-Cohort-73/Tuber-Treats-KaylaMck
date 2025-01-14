namespace TuberTreats.Models.DTO;

public class TuberOrderDTO 
{
    public int Id { get; set; }
    public DateTime OrderPlacedOnDate { get; set; }
    public int CustomerId { get; set; }
    public CustomerDTO Customer { get; set; }
    public int? TuberDriverId { get; set; }
    public TuberDriverDTO Driver { get; set; }
    public DateTime? DeliveredOnDate { get; set; }
    public List<ToppingDTO> Toppings { get; set; } = new();
}