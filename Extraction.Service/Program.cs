﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroService.TextAndConcept
{
    class Program
    {
        static void Main(string[] args)
        {
            var service = new Service();
            service.Start();
        }
    }
}
