using Microsoft.AspNetCore.Http;
using System;

namespace mHealthBank.Models.Forms
{
    public class CustomerModel
    {
        public string Id { get; set; }
        public string CustomerName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public IFormFile File { get; set; }
    }
}
