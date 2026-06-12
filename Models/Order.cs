using System;
using System.Collections.Generic;

namespace BakeryOrderSystem.Models
{
    public class Order
    {
        public int Id { get; set; }

        public int CustomerId { get; set; }

        public Customer Customer { get; set; }

        public int UserId { get; set; }

        public User User { get; set; }

        public DateTime OrderDate { get; set; }

        public string Status { get; set; }

        public decimal TotalPrice { get; set; }

        public string Comment { get; set; }

        public List<OrderItem> OrderItems { get; set; }
    }
}