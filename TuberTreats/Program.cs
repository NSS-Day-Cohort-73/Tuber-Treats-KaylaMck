using System.Net.Sockets;
using System.Security.Authentication;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using TuberTreats.Models;
using TuberTreats.Models.DTO;

List<TuberDriver> drivers = new List<TuberDriver>()
{
    new TuberDriver { Id = 1, Name = "John Smith" },
    new TuberDriver { Id = 2, Name = "Sarah Johnson" },
    new TuberDriver { Id = 3, Name = "Mike Wilson" }
};

List<Customer> customers = new List<Customer>()
{
    new Customer { Id = 1, Name = "Alice Brown", Address = "123 Main St" },
    new Customer { Id = 2, Name = "Bob White", Address = "456 Oak Ave" },
    new Customer { Id = 3, Name = "Carol Davis", Address = "789 Pine Rd" },
    new Customer { Id = 4, Name = "David Miller", Address = "321 Elm St" },
    new Customer { Id = 5, Name = "Emma Wilson", Address = "654 Maple Dr" }
};

List<Topping> toppings = new List<Topping>()
{
    new Topping { Id = 1, Name = "Cheese" },
    new Topping { Id = 2, Name = "Bacon" },
    new Topping { Id = 3, Name = "Sour Cream" },
    new Topping { Id = 4, Name = "Chives" },
    new Topping { Id = 5, Name = "Butter" }
};

List<TuberOrder> orders = new List<TuberOrder>()
{
    new TuberOrder
    {
        Id = 1,
        OrderPlacedOnDate = DateTime.Now,
        CustomerId = 1,
        TuberDriverId = 1,
        DeliveredOnDate = null
    },
    new TuberOrder
    {
        Id = 2,
        OrderPlacedOnDate = DateTime.Now.AddHours(-1),
        CustomerId = 3,
        TuberDriverId = 2,
        DeliveredOnDate = DateTime.Now.AddMinutes(-30)
    },
    new TuberOrder
    {
        Id = 3,
        OrderPlacedOnDate = DateTime.Now.AddHours(-2),
        CustomerId = 5,
        TuberDriverId = 3,
        DeliveredOnDate = DateTime.Now.AddMinutes(-45)
    }
};

List<TuberTopping> tuberToppings = new List<TuberTopping>()
{
    new TuberTopping { Id = 1, TuberOrderId = 1, ToppingId = 1 },
    new TuberTopping { Id = 2, TuberOrderId = 1, ToppingId = 2 },

    new TuberTopping { Id = 3, TuberOrderId = 2, ToppingId = 3 },
    new TuberTopping { Id = 4, TuberOrderId = 2, ToppingId = 4 },

    new TuberTopping { Id = 5, TuberOrderId = 3, ToppingId = 5 },
    new TuberTopping { Id = 6, TuberOrderId = 3, ToppingId = 1 }
};

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

//add endpoints here
app.MapGet("/tuberorders", () =>
{
    var tuberOrders = orders.Select(order => new TuberOrderDTO
    {
        Id = order.Id,
        OrderPlacedOnDate = order.OrderPlacedOnDate,
        CustomerId = order.CustomerId,
        Customer = new CustomerDTO
        {
            Id = customers.FirstOrDefault(c => c.Id == order.CustomerId)?.Id ?? 0,
            Name = customers.FirstOrDefault(c => c.Id == order.CustomerId)?.Name ?? "",
            Address = customers.FirstOrDefault(c => c.Id == order.CustomerId)?.Address ?? ""
        },
        TuberDriverId = order.TuberDriverId,
        Driver = order.TuberDriverId.HasValue? new TuberDriverDTO
        {
            Id = drivers.FirstOrDefault(d => d.Id == order.TuberDriverId)?.Id ?? 0,
            Name = drivers.FirstOrDefault(d => d.Id == order.TuberDriverId)?.Name ?? ""
        } : null,
        DeliveredOnDate = order.DeliveredOnDate,
        Toppings = toppings
            .Where(t => tuberToppings.Any(tt => tt.TuberOrderId == order.Id && tt.ToppingId == t.Id))
            .Select(t => new ToppingDTO
            {
                Id = t.Id,
                Name = t.Name
            }).ToList()
    }).ToList();

    return tuberOrders;
});

app.MapGet("/tuberorders/{id}", (int id) =>
{
    var order = orders.FirstOrDefault(o => o.Id == id);

    if (order == null)
    {
        return Results.NotFound("Order not found");
    }

    var customer = customers.FirstOrDefault(c => c.Id == order.CustomerId);
    
    var customerInfo = new CustomerDTO
    {
        Id = customer.Id,
        Name = customer.Name,
        Address = customer.Address
    };

    var driver = drivers.FirstOrDefault(d => d.Id == order.TuberDriverId);

    var driverInfo = driver == null ? null : new TuberDriverDTO
    {
        Id = driver.Id,
        Name = driver.Name
    };

    var tuberToppingsList = tuberToppings.Where(tt => tt.TuberOrderId == id).ToList();
    var toppingList = tuberToppingsList.Select(tt => toppings.FirstOrDefault(t => t.Id == tt.ToppingId)).ToList();

    var toppingsDetails = toppingList.Select(t => new ToppingDTO
    {
        Id = t.Id,
        Name = t.Name
    }).ToList();

    var orderDTO = new TuberOrderDTO
    {
        Id = order.Id,
        OrderPlacedOnDate = order.OrderPlacedOnDate,
        CustomerId = order.CustomerId,
        Customer = customerInfo,
        TuberDriverId = order.TuberDriverId,
        Driver = driverInfo,
        DeliveredOnDate = order.DeliveredOnDate,
        Toppings = toppingsDetails
    };
    
    return Results.Ok(orderDTO);
});

app.MapPost("/tuberorders", (TuberOrder createOrder) => 
{
    int newId = orders.Max(o => o.Id) + 1;

    var order = new TuberOrder
    {
        Id = newId,
        OrderPlacedOnDate = DateTime.Now,
        CustomerId = createOrder.CustomerId
    };

    orders.Add(order);

    return Results.Created($"/tuberorders", order);
});

app.MapPut("/tuberorders/{id}", (int id, AssignDriverDTO assignDriver) =>
{

    var order = orders.FirstOrDefault(o => o.Id == id);
    if (order == null)
    {
        return Results.NotFound("Order Not Found");
    }

    var driver = drivers.FirstOrDefault(d => d.Id == assignDriver.DriverId);
    if (driver == null)
    {
        return Results.NotFound("Driver Not Found");
    }

    order.TuberDriverId = assignDriver.DriverId;

    var tuberOrderDTO = new TuberOrderDTO
    {
        Id = order.Id,
        OrderPlacedOnDate = order.OrderPlacedOnDate,
        CustomerId = order.CustomerId,
        Customer = new CustomerDTO
        {
            Id = customers.FirstOrDefault(c => c.Id == order.CustomerId)?.Id ?? 0,
            Name = customers.FirstOrDefault(c => c.Id == order.CustomerId)?.Name ?? "",
            Address = customers.FirstOrDefault(c => c.Id == order.CustomerId)?.Address ?? ""
        },
        TuberDriverId = order.TuberDriverId,
        Driver = new TuberDriverDTO
        {
            Id = driver.Id,
            Name = driver.Name
        },
        DeliveredOnDate = order.DeliveredOnDate,
        Toppings = toppings
            .Where(t => tuberToppings.Any(tt => tt.TuberOrderId == order.Id && tt.ToppingId == t.Id))
            .Select(t => new ToppingDTO
            {
                Id = t.Id,
                Name = t.Name
            }).ToList()
    };

    return Results.Ok(tuberOrderDTO);
});

app.MapPost("/tuberorders/{id}/complete", (int id) => 
{
  var order = orders.FirstOrDefault(o => o.Id == id);
  if (order == null)
  {
    return Results.NotFound("Order not found");
  }

  order.DeliveredOnDate = DateTime.Now;

  return Results.Ok();
});

app.MapGet("/toppings", () =>
{
    var toppingDTO = toppings.Select(t => new ToppingDTO
    {
        Id = t.Id,
        Name = t.Name 
    }).ToList();

    return Results.Ok(toppingDTO);
});

app.MapGet("/toppings/{id}", (int id) =>
{
    var topping = toppings.FirstOrDefault(t => t.Id == id);

    if (topping == null)
    {
        return Results.NotFound("Topping not found");
    }

    var toppingDTO = new ToppingDTO
    {
        Id = topping.Id,
        Name = topping.Name
    };

    return Results.Ok(toppingDTO);
});

app.MapGet("/tubertoppings", () => 
{
    var tuberList = tuberToppings.Select(tt => new TuberToppingDTO
    {
        Id = tt.Id,
        ToppingId = tt.ToppingId,
        TuberOrderId = tt.TuberOrderId
    });

    return Results.Ok(tuberList);
});


app.MapPost("/tubertoppings", (TuberTopping tuberTopping) =>
{
    int newId = tuberToppings.Max(tt => tt.Id) + 1;

    var newTuberTopping = new TuberTopping
    {
        Id = newId,
        TuberOrderId = tuberTopping.TuberOrderId,
        ToppingId = tuberTopping.ToppingId
    };

    tuberToppings.Add(newTuberTopping);

    return Results.Ok(newTuberTopping);
});


app.MapDelete("/tubertoppings/{id}", (int id) =>
{
    var tuberToppingToDelete = tuberToppings.FirstOrDefault(tt => tt.Id == id);
    if (tuberToppingToDelete == null)
    {
        return Results.NotFound("TuberTopping not found");
    }
    
    tuberToppings.Remove(tuberToppingToDelete);
    return Results.Ok("Deleted");
});

app.MapGet("/customers", () =>
{
    var customerDTO = customers.Select(c => new CustomerDTO
    {
        Id = c.Id,
        Name = c.Name,
        Address = c.Address
    }).ToList();

    return Results.Ok(customerDTO);
});

app.MapGet("/customers/{id}", (int id) =>
{
    var customer = customers.FirstOrDefault(c => c.Id == id);
    if (customer == null)
    {
        return Results.NotFound("Customer not found");
    }

    var orderList = orders
        .Where(o => o.CustomerId == id)
        .ToList();

    var customerDTO = new CustomerDTO
    {
        Id = customer.Id,
        Name = customer.Name,
        Address = customer.Address,
        TuberOrders = orderList
    };

    return Results.Ok(customerDTO);
});

app.MapPost("/customers", (CreateCustomerDTO createCustomer) =>
{
    if (string.IsNullOrWhiteSpace(createCustomer.Name) || string.IsNullOrWhiteSpace(createCustomer.Address))
    {
        return Results.BadRequest("Name and Address required");
    }

    var customer = new Customer
    {
        Id = customers.Max(c => c.Id) + 1,
        Name = createCustomer.Name,
        Address = createCustomer.Address
    };

    customers.Add(customer);

    var customerDTO = new CustomerDTO
    {
        Id = customer.Id,
        Name = customer.Name,
        Address = customer.Address
    };

    return Results.Created($"/customers/{customer.Id}", customerDTO);
});

app.MapDelete("/customers/{id}", (int id) =>
{
    var customer = customers.FirstOrDefault(c => c.Id == id);
    
    if (customer == null)
    {
        return Results.NotFound("Customer not found");
    }

    customers.Remove(customer);
    return Results.NoContent();
});

app.MapGet("/tuberdrivers", () =>
{
    var driverDTO = drivers.Select(d => new TuberDriverDTO
    {
        Id = d.Id,
        Name = d.Name,
    }).ToList();

    return Results.Ok(driverDTO);
});

app.MapGet("/tuberdrivers/{id}", (int id) =>
{
    var driver = drivers.FirstOrDefault(d => d.Id == id);

    if (driver == null)
    {
        return Results.NotFound("Driver not found");
    }

    var driverDTO = new TuberDriverDTO
    {
        Id = driver.Id,
        Name = driver.Name,
        TuberDeliveries = orders
            .Where(o => o.TuberDriverId == id)
            .Select(o => new TuberOrderDTO
            {
                Id = o.Id,
                OrderPlacedOnDate = o.OrderPlacedOnDate,
                CustomerId = o.CustomerId,
                Customer = new CustomerDTO
                {
                    Id = customers.FirstOrDefault(c => c.Id == o.CustomerId)?.Id ?? 0,
                    Name = customers.FirstOrDefault(c => c.Id == o.CustomerId)?.Name ?? "",
                    Address = customers.FirstOrDefault(c => c.Id == o.CustomerId)?.Address ?? ""
                },
                TuberDriverId = o.TuberDriverId,
                DeliveredOnDate = o.DeliveredOnDate,
                Toppings = toppings
                    .Where(t => tuberToppings.Any(tt =>
                        tt.TuberOrderId == o.Id &&
                        tt.ToppingId == t.Id))
                    .Select(t => new ToppingDTO
                    {
                        Id = t.Id,
                        Name = t.Name
                    }).ToList()
            }).ToList()
    };

    return Results.Ok(driverDTO);
});



app.Run();
//don't touch or move this!
public partial class Program { }