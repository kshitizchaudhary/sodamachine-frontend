using Microsoft.Extensions.Logging;
using VendingMachineApp.OrderManagementAPIClient;

namespace VendingMachineApp
{
    public class SodaMachine
    {
        private readonly ILogger<SodaMachine> _logger;
        private readonly IOrderAPIClient _orderService;
        private ICollection<ProductItem> _products;
        private OrderItem _order;

        public SodaMachine(ILogger<SodaMachine> logger, IOrderAPIClient orderService)
        {
            _logger = logger; // Default logger is console, hence not using it
            _orderService = orderService;
            _order = new();
        }

        /// <summary>
        /// This is the starter method for the machine
        /// </summary>
        public void Start()
        {
            _products = _orderService.GetProductsAsync().GetAwaiter().GetResult();
            var commaSeparatedProductNames = string.Join(", ", _products.Select(p => p.Name.ToLower()));

            while (true)
            {
                Console.WriteLine("\n\nAvailable commands:");
                Console.WriteLine("insert (money) - Money put into money slot");
                Console.WriteLine($"order ({commaSeparatedProductNames}) - Order from machines buttons");
                Console.WriteLine($"sms order ({commaSeparatedProductNames}) - Order sent by sms");
                Console.WriteLine("recall - gives money back");
                Console.WriteLine("-------");
                Console.WriteLine($"Inserted money: {_order.CreditAmount}");
                Console.WriteLine("-------\n\n");

                var input = Console.ReadLine();

                if (input == null)
                {
                    Console.WriteLine("Select an option");
                    continue;
                }

                if (input.StartsWith("insert"))
                {
                    //Add to credit                    
                    var money = decimal.Parse(input.Split(' ')[1]);

                    if (OrderExists(_order))
                    {
                        // insert the money to existing order
                        var updatedOrder = _orderService.AddCreditToOrderAsync(new AddCreditToOrderRequest { OrderId = _order.Id, CreditAmount = money, PaymentType = "Cash" }).GetAwaiter().GetResult();
                        _order.CreditAmount = updatedOrder.CreditAmount;
                    }
                    else
                    {
                        // Add new order
                        AddOrder("Cash", money);
                    }

                    Console.WriteLine($"Adding {_order.CreditAmount} to credit");
                }

                if (input.StartsWith("order"))
                {
                    // split string on space
                    var csoda = input.Split(' ')[1];
                    ProcessOrder(csoda);
                }

                if (input.StartsWith("sms order"))
                {
                    // split string on space
                    var csoda = input.Split(' ')[2];
                    ProcessOrder(csoda, true);
                }

                if (input.Equals("recall"))
                {
                    //Give money back
                    Recall(_order);
                }
            }
        }

        private void Recall(OrderItem order)
        {
            //Give money back
            if (order.CreditAmount > 0)
            {
                Console.WriteLine($"Returning {order.CreditAmount} to customer");
                _orderService.RecallOrderAsync(order.Id).GetAwaiter().GetResult();
            }

            //Reset the order
            _order = new();
        }

        private static bool OrderExists(OrderItem order) => order.Id > 0;

        private void AddOrder(string paymentType, decimal creditAmount)
        {
            _order = _orderService.AddOrderAsync(new AddOrderRequest { PaymentType = paymentType, CreditAmount = creditAmount }).GetAwaiter().GetResult();
        }
        
        private void ProcessOrder(string productName, bool isSMSOrder = false)
        {
            // Find product
            var product = _products.FirstOrDefault(p => p.Name.ToLower().Trim() == productName.ToLower().Trim());

            if (product == null)
            {
                Console.WriteLine($"No such {productName} soda");
                return;
            }

            // Order and Payment Validation
            if (isSMSOrder) 
            {
                if (OrderExists(_order) && _order.CreditAmount > 0)
                {
                    Console.WriteLine($"SMS order cannot be processed. Previous order with credit amount {_order.CreditAmount} is not completed.");
                    return;
                }
                else
                {
                    // Create SMS order
                    AddOrder("SMS", product.PricePerUnit);
                }
            }
            else if (!OrderExists(_order))
            {
                // No order exists with athorized payment
                Console.WriteLine($"Need {product.PricePerUnit} more");
                return;
            }

            // Add Product
            var addProductResponse = _orderService.AddProductAsync(_order.Id, product.Id).GetAwaiter().GetResult();

            if (addProductResponse == null || addProductResponse.Order == null)
            {
                Console.WriteLine($"Some thing goes wrong. please retry.");
                return;
            }

            // Update order
            _order = addProductResponse.Order;

            // Check the response
            switch (_order.OrderStatus)
            {
                case OrderStatus.ProductShipped:
                    Console.WriteLine($"Giving {productName} out");
                    Recall(_order); // Recall available credit amount
                    
                    break;

                case OrderStatus.InsufficientCreditAmount:
                    if (!addProductResponse.MissingCreditAmount.HasValue)
                    {
                        Console.WriteLine($"Some thing goes wrong. please retry.");
                        return;
                    }
                    Console.WriteLine($"Need {addProductResponse.MissingCreditAmount ?? 0} more");
                    
                    break;
                
                case OrderStatus.ProductOutOfStock:
                    Console.WriteLine($"No {productName} left");

                    if (isSMSOrder)
                    {
                        // immediately return the money
                        Recall(_order);
                    }
                        

                    break;
                
                default:
                    break;
            }
        }
    }
}
