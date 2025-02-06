using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce_platforms.Core.Models
{
    public class Address
    {
        public Address()
        {

        }

        public Address(string name, string phone, string city, string detail)
        {
            Name = name;
            Phone = phone;
            City = city;
            Details = detail;
        }

        public string Name { get; set; }
      
        public string Phone { get; set; }

        public string City { get; set; }

        public string Details { get; set; }
    }
}
