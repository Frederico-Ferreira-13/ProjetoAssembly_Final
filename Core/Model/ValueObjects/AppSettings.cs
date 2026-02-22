using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Model.ValueObjects
{
    public class AppSettings
    {
        public string[] AdminEmails { get; set; } = Array.Empty<string>();
    }
}
