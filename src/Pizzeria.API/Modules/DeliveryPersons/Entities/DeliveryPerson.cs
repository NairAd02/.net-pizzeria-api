namespace Pizzeria.API.Modules.DeliveryPersons.Entities;

public class DeliveryPerson
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string Phone { get; set; }
    public DeliveryPersonStatus Status { get; set; } = DeliveryPersonStatus.Available;
}
