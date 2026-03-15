using System;
using System.Collections.Generic;
using System.Text;

namespace Alissa.Core.Models
{
    public class ErrorResult
    {
        public bool IsFatal { get; set; }
        public string PublicMessage { get; set; } = "An error occurred.";
    }
}
